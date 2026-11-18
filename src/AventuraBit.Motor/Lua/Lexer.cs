using System.Globalization;
using System.Text;

namespace AventuraBit.Motor.Lua;

public enum TipoToken { Nombre, Numero, Cadena, Interpolada, PalabraClave, Simbolo, FinArchivo }

public sealed record Token(TipoToken Tipo, string Texto, object? Valor, int Linea, int Columna)
{
    public bool Es(string texto) =>
        (Tipo == TipoToken.PalabraClave || Tipo == TipoToken.Simbolo) && Texto == texto;
}

/// <summary>Trozo de un texto con interpolación: `Hola {nombre}`.</summary>
public sealed record ParteInterpolada(string? Texto, string? Codigo, int Linea);

/// <summary>Convierte el código en una lista de piezas (tokens).</summary>
public static class Lexer
{
    public static readonly HashSet<string> PalabrasClave = new(StringComparer.Ordinal)
    {
        "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "if", "in",
        "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while", "continue",
    };

    private static readonly string[] Simbolos3 = { "...", "//=", "..=" };
    private static readonly string[] Simbolos2 =
        { "==", "~=", "<=", ">=", "..", "+=", "-=", "*=", "/=", "%=", "^=", "//", "::", "!=", "->", "&&", "||" };
    private const string Simbolos1 = "+-*/%^#=<>(){}[];:,.&|!?";

    public static List<Token> Analizar(string codigo, int lineaInicial = 1)
    {
        var tokens = new List<Token>();
        int i = 0, linea = lineaInicial, inicioLinea = 0, n = codigo.Length;

        while (true)
        {
            // Espacios y comentarios
            while (i < n)
            {
                char c = codigo[i];
                if (c == '\n') { linea++; i++; inicioLinea = i; }
                else if (char.IsWhiteSpace(c)) i++;
                else if (c == '-' && i + 1 < n && codigo[i + 1] == '-')
                {
                    i += 2;
                    int nivel = i < n && codigo[i] == '[' ? NivelCorchete(codigo, i) : -1;
                    if (nivel >= 0)
                    {
                        int lineaComentario = linea;
                        var fin = SaltarLargo(codigo, i, nivel, ref linea, ref inicioLinea, out _);
                        i = fin ?? throw new ErrorLua("El comentario largo --[[ no está cerrado. Falta ]] al final.", lineaComentario);
                    }
                    else
                    {
                        while (i < n && codigo[i] != '\n') i++;
                    }
                }
                else break;
            }

            if (i >= n)
            {
                tokens.Add(new Token(TipoToken.FinArchivo, "fin del código", null, linea, i - inicioLinea + 1));
                return tokens;
            }

            int col = i - inicioLinea + 1;
            char ch = codigo[i];

            if (EsLetraAscii(ch) || ch == '_')
            {
                int ini = i;
                while (i < n && (EsLetraAscii(codigo[i]) || char.IsAsciiDigit(codigo[i]) || codigo[i] == '_')) i++;
                if (i < n && char.IsLetter(codigo[i]))
                    throw ErrorLetraRara(codigo, ini, i, linea);
                string palabra = codigo[ini..i];
                tokens.Add(new Token(PalabrasClave.Contains(palabra) ? TipoToken.PalabraClave : TipoToken.Nombre,
                    palabra, null, linea, col));
                continue;
            }

            if (char.IsLetter(ch))
                throw ErrorLetraRara(codigo, i, i, linea);

            if (char.IsAsciiDigit(ch) || (ch == '.' && i + 1 < n && char.IsAsciiDigit(codigo[i + 1])))
            {
                int ini = i;
                double valor;
                if (ch == '0' && i + 1 < n && (codigo[i + 1] == 'x' || codigo[i + 1] == 'X'))
                {
                    i += 2;
                    int iniHex = i;
                    while (i < n && (Uri.IsHexDigit(codigo[i]) || codigo[i] == '_')) i++;
                    valor = long.Parse(codigo[iniHex..i].Replace("_", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
                else
                {
                    while (i < n && (char.IsAsciiDigit(codigo[i]) || codigo[i] == '_')) i++;
                    if (i < n && codigo[i] == '.' && !(i + 1 < n && codigo[i + 1] == '.'))
                    {
                        i++;
                        while (i < n && (char.IsAsciiDigit(codigo[i]) || codigo[i] == '_')) i++;
                    }
                    if (i < n && (codigo[i] == 'e' || codigo[i] == 'E'))
                    {
                        int j = i + 1;
                        if (j < n && (codigo[j] == '+' || codigo[j] == '-')) j++;
                        if (j < n && char.IsAsciiDigit(codigo[j]))
                        {
                            i = j;
                            while (i < n && char.IsAsciiDigit(codigo[i])) i++;
                        }
                    }
                    valor = double.Parse(codigo[ini..i].Replace("_", ""), NumberStyles.Float, CultureInfo.InvariantCulture);
                }
                if (i < n && (char.IsLetter(codigo[i]) || codigo[i] == '_'))
                {
                    int fin = i;
                    while (fin < n && (char.IsLetterOrDigit(codigo[fin]) || codigo[fin] == '_')) fin++;
                    throw new ErrorLua($"'{codigo[ini..fin]}' no es un nombre válido: los nombres no pueden empezar por un número.", linea);
                }
                tokens.Add(new Token(TipoToken.Numero, codigo[ini..i], valor, linea, col));
                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                int lineaTexto = linea;
                var sb = new StringBuilder();
                int ini = i;
                i++;
                while (true)
                {
                    if (i >= n || codigo[i] == '\n')
                        throw new ErrorLua($"Al texto le falta la comilla de cierre {ch}. Los textos van entre comillas: {ch}hola{ch}", lineaTexto);
                    char c = codigo[i];
                    if (c == ch) { i++; break; }
                    if (c == '\\') { i = Escape(codigo, i, sb, ref linea, ref inicioLinea); continue; }
                    sb.Append(c);
                    i++;
                }
                tokens.Add(new Token(TipoToken.Cadena, codigo[ini..i], sb.ToString(), lineaTexto, col));
                continue;
            }

            if (ch == '“' || ch == '”' || ch == '‘' || ch == '’' || ch == '«' || ch == '»')
                throw new ErrorLua($"La comilla {ch} no vale en código. Usa las comillas normales del teclado: \" o '.", linea);

            if (ch == '`')
            {
                int lineaTexto = linea;
                var partes = new List<ParteInterpolada>();
                var sb = new StringBuilder();
                int ini = i;
                i++;
                while (true)
                {
                    if (i >= n)
                        throw new ErrorLua("Al texto con ` le falta el acento grave ` de cierre.", lineaTexto);
                    char c = codigo[i];
                    if (c == '`') { i++; break; }
                    if (c == '\\') { i = Escape(codigo, i, sb, ref linea, ref inicioLinea); continue; }
                    if (c == '\n') { linea++; inicioLinea = i + 1; }
                    if (c == '{')
                    {
                        if (sb.Length > 0) { partes.Add(new ParteInterpolada(sb.ToString(), null, linea)); sb.Clear(); }
                        int profundidad = 1, iniExp = ++i, lineaExp = linea;
                        while (i < n && profundidad > 0)
                        {
                            if (codigo[i] == '{') profundidad++;
                            else if (codigo[i] == '}') profundidad--;
                            else if (codigo[i] == '\n') { linea++; inicioLinea = i + 1; }
                            if (profundidad > 0) i++;
                        }
                        if (i >= n) throw new ErrorLua("Falta la llave } de cierre dentro del texto con `.", lineaExp);
                        string exp = codigo[iniExp..i];
                        if (string.IsNullOrWhiteSpace(exp))
                            throw new ErrorLua("Dentro de { } tienes que poner algo, por ejemplo {nombre}.", lineaExp);
                        partes.Add(new ParteInterpolada(null, exp, lineaExp));
                        i++;
                        continue;
                    }
                    sb.Append(c);
                    i++;
                }
                if (sb.Length > 0 || partes.Count == 0) partes.Add(new ParteInterpolada(sb.ToString(), null, linea));
                tokens.Add(new Token(TipoToken.Interpolada, codigo[ini..i], partes, lineaTexto, col));
                continue;
            }

            if (ch == '[')
            {
                int nivel = NivelCorchete(codigo, i);
                if (nivel >= 0)
                {
                    int lineaTexto = linea, ini = i;
                    var fin = SaltarLargo(codigo, i, nivel, ref linea, ref inicioLinea, out var contenido);
                    i = fin ?? throw new ErrorLua("El texto largo [[ no está cerrado con ]].", lineaTexto);
                    tokens.Add(new Token(TipoToken.Cadena, codigo[ini..i], contenido, lineaTexto, col));
                    continue;
                }
            }

            string? simbolo = null;
            foreach (var s in Simbolos3)
                if (string.CompareOrdinal(codigo, i, s, 0, 3) == 0) { simbolo = s; break; }
            if (simbolo is null)
                foreach (var s in Simbolos2)
                    if (string.CompareOrdinal(codigo, i, s, 0, 2) == 0) { simbolo = s; break; }
            if (simbolo is null && Simbolos1.Contains(ch)) simbolo = ch.ToString();
            if (simbolo is null)
                throw new ErrorLua($"No entiendo el símbolo '{ch}'.", linea);

            tokens.Add(new Token(TipoToken.Simbolo, simbolo, null, linea, col));
            i += simbolo.Length;
        }
    }

    private static bool EsLetraAscii(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

    private static ErrorLua ErrorLetraRara(string codigo, int ini, int pos, int linea)
    {
        int fin = pos;
        while (fin < codigo.Length && (char.IsLetterOrDigit(codigo[fin]) || codigo[fin] == '_')) fin++;
        string palabra = codigo[ini..fin];
        string sugerida = new string(palabra.Replace("ñ", "ni").Replace("Ñ", "Ni").Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        return new ErrorLua($"Los nombres de variables y funciones no pueden llevar tildes ni ñ. Cambia '{palabra}' por '{sugerida}'.", linea);
    }

    private static int Escape(string codigo, int i, StringBuilder sb, ref int linea, ref int inicioLinea)
    {
        i++;
        if (i >= codigo.Length) return i;
        char e = codigo[i];
        switch (e)
        {
            case 'n': sb.Append('\n'); break;
            case 't': sb.Append('\t'); break;
            case 'r': sb.Append('\r'); break;
            case '\\': sb.Append('\\'); break;
            case '"': sb.Append('"'); break;
            case '\'': sb.Append('\''); break;
            case '`': sb.Append('`'); break;
            case '{': sb.Append('{'); break;
            case '\n': sb.Append('\n'); linea++; inicioLinea = i + 1; break;
            default: sb.Append(e); break;
        }
        return i + 1;
    }

    /// <summary>Si en <paramref name="i"/> empieza [[ o [==[, devuelve el número de '='. Si no, -1.</summary>
    private static int NivelCorchete(string s, int i)
    {
        if (s[i] != '[') return -1;
        int j = i + 1, nivel = 0;
        while (j < s.Length && s[j] == '=') { nivel++; j++; }
        return j < s.Length && s[j] == '[' ? nivel : -1;
    }

    private static int? SaltarLargo(string s, int i, int nivel, ref int linea, ref int inicioLinea, out string contenido)
    {
        int ini = i + nivel + 2;
        string cierre = "]" + new string('=', nivel) + "]";
        int j = ini;
        if (j < s.Length && s[j] == '\n') { linea++; inicioLinea = j + 1; ini = ++j; }
        while (j < s.Length)
        {
            if (string.CompareOrdinal(s, j, cierre, 0, cierre.Length) == 0)
            {
                contenido = s[ini..j];
                return j + cierre.Length;
            }
            if (s[j] == '\n') { linea++; inicioLinea = j + 1; }
            j++;
        }
        contenido = "";
        return null;
    }
}
