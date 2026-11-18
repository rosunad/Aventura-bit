using System.Text;
using System.Text.RegularExpressions;
using AventuraBit.Motor.Lua;

namespace AventuraBit.Motor.Juego;

public sealed record ConsejoEstilo(int Linea, string Mensaje);

/// <summary>Revisa que el código esté bien escrito: sangría, camelCase, local...</summary>
public static class RevisorEstilo
{
    private static readonly Regex CamelCase = new("^_?[a-z][a-zA-Z0-9]*$");

    public static List<ConsejoEstilo> Revisar(string codigo, Bloque programa, IEnumerable<string>? globalesPermitidas = null)
    {
        var consejos = new List<ConsejoEstilo>();
        consejos.AddRange(RevisarSangria(codigo).Take(3));
        consejos.AddRange(RevisarNombres(programa, new HashSet<string>(globalesPermitidas ?? Array.Empty<string>())).Take(3));
        return consejos;
    }

    // ---------------- Sangría ----------------
    private static readonly HashSet<string> Abren = new() { "then", "do", "repeat", "function", "{", "(" };
    private static readonly HashSet<string> Cierran = new() { "end", "until", "}", ")" };

    public static List<ConsejoEstilo> RevisarSangria(string codigo)
    {
        var consejos = new List<ConsejoEstilo>();
        List<Token> tokens;
        try { tokens = Lexer.Analizar(codigo); }
        catch (ErrorLua) { return consejos; }

        var lineas = codigo.Replace("\r", "").Split('\n');
        var porLinea = tokens.Where(t => t.Tipo != TipoToken.FinArchivo).GroupBy(t => t.Linea).OrderBy(g => g.Key);
        var pila = new List<int>(); // línea en la que se abrió cada bloque
        int unidad = 0;
        int ultimaLineaConToken = 0;

        foreach (var grupo in porLinea)
        {
            int numLinea = grupo.Key;
            var toks = grupo.ToList();
            bool empiezaEnEstaLinea = toks[0].Columna <= LongitudSangria(lineas[numLinea - 1], contarTab: false) + 1;
            int k = 0;
            // Cierres al principio de la línea
            while (k < toks.Count && EsCierre(toks[k]))
            {
                if (pila.Count > 0) pila.RemoveAt(pila.Count - 1);
                if (toks[k].Es("else")) { k++; break; }
                k++;
            }
            int nivel = pila.Distinct().Count();

            // La primera pieza de la línea puede venir de una línea anterior (texto largo); entonces no se revisa.
            if (empiezaEnEstaLinea && numLinea != ultimaLineaConToken && toks[0].Linea == numLinea)
            {
                string texto = lineas[numLinea - 1];
                int ancho = LongitudSangria(texto, contarTab: true);
                if (unidad == 0 && nivel > 0 && ancho > 0 && ancho % nivel == 0) unidad = ancho / nivel;
                int esperado = nivel * (unidad == 0 ? 4 : unidad);
                if (ancho != esperado)
                    consejos.Add(new ConsejoEstilo(numLinea, MensajeSangria(numLinea, nivel, ancho, esperado, toks[0])));
            }
            ultimaLineaConToken = numLinea;

            // Resto de la línea (si la línea empezaba con else, vuelve a abrir)
            if (k > 0 && toks[k - 1].Es("else")) pila.Add(numLinea);
            for (; k < toks.Count; k++)
            {
                var t = toks[k];
                if (t.Es("else")) continue;
                if (EsCierre(t)) { if (pila.Count > 0) pila.RemoveAt(pila.Count - 1); }
                else if (t.Tipo is TipoToken.PalabraClave or TipoToken.Simbolo && Abren.Contains(t.Texto)) pila.Add(numLinea);
            }
        }
        return consejos;
    }

    private static bool EsCierre(Token t) =>
        t.Tipo is TipoToken.PalabraClave or TipoToken.Simbolo && (Cierran.Contains(t.Texto) || t.Texto is "else" or "elseif");

    private static int LongitudSangria(string linea, bool contarTab)
    {
        int n = 0;
        foreach (char c in linea)
        {
            if (c == ' ') n++;
            else if (c == '\t') n += contarTab ? 4 : 1;
            else break;
        }
        return n;
    }

    private static string MensajeSangria(int linea, int nivel, int ancho, int esperado, Token primero)
    {
        string que = primero.Tipo == TipoToken.PalabraClave && primero.Texto is "end" or "else" or "elseif" or "until"
            ? $"'{primero.Texto}' debe quedar alineado con la línea que abrió el bloque"
            : nivel == 0 ? "esta línea no está dentro de ningún bloque, así que no lleva sangría"
            : $"esta línea está dentro de {nivel} bloque{(nivel > 1 ? "s" : "")}, así que lleva {nivel} sangría{(nivel > 1 ? "s" : "")} (pulsa Tab {nivel} vez{(nivel > 1 ? "es" : "")} al principio)";
        string como = ancho < esperado ? "le falta sangría" : "le sobra sangría";
        return $"Línea {linea}: {como}: {que}.";
    }

    // ---------------- Nombres ----------------
    private static List<ConsejoEstilo> RevisarNombres(Bloque programa, HashSet<string> permitidas)
    {
        var consejos = new List<ConsejoEstilo>();
        var revisados = new HashSet<string>();
        var locales = new List<HashSet<string>> { new() };

        void Nombre(string n, int linea, bool esFuncion)
        {
            if (n == "_" || n == "self" || !revisados.Add(n)) return;
            if (CamelCase.IsMatch(n)) return;
            if (!esFuncion && Interprete.EsNombreConstante(n)) return;
            string sugerido = ACamelCase(n);
            string porQue = n.Contains('_')
                ? "en Luau no se usan guiones bajos en los nombres"
                : char.IsUpper(n[0]) ? "los nombres empiezan en minúscula" : "cada palabra nueva empieza en mayúscula";
            string extra = !esFuncion && n.ToUpperInvariant() != n && n.Contains('_') ? "" : "";
            consejos.Add(new ConsejoEstilo(linea, $"Línea {linea}: '{n}' debería escribirse en camelCase: '{sugerido}' ({porQue}).{extra}"));
        }

        bool EsLocal(string n) => locales.Any(s => s.Contains(n));

        void Recorrer(Bloque b)
        {
            locales.Add(new HashSet<string>());
            foreach (var ins in b.Instrucciones) Instruccion(ins);
            locales.RemoveAt(locales.Count - 1);
        }

        void Funcion(ExpFuncion f)
        {
            locales.Add(new HashSet<string>(f.Parametros));
            foreach (var p in f.Parametros) Nombre(p, f.Linea, false);
            Recorrer(f.Cuerpo);
            locales.RemoveAt(locales.Count - 1);
        }

        void Expresiones(Nodo n)
        {
            // Funciones anónimas (por ejemplo, las que se pasan a :Connect)
            foreach (var f in Arbol.Nodos(n).OfType<ExpFuncion>().Where(f => !ReferenceEquals(f, n)).ToList())
                Funcion(f);
        }

        void Instruccion(Instruccion ins)
        {
            switch (ins)
            {
                case InsLocal l:
                    foreach (var ev in l.Valores) Expresiones(ev);
                    foreach (var n in l.Nombres) { Nombre(n, l.Linea, false); locales[^1].Add(n); }
                    break;
                case InsLocalFuncion lf:
                    Nombre(lf.Nombre, lf.Linea, true);
                    locales[^1].Add(lf.Nombre);
                    Funcion(lf.Funcion);
                    break;
                case InsFuncion f:
                    if (f.Destino is ExpVariable fv && !EsLocal(fv.Nombre))
                    {
                        Nombre(fv.Nombre, f.Linea, true);
                        if (!permitidas.Contains(fv.Nombre) && revisados.Add("global:" + fv.Nombre))
                            consejos.Add(new ConsejoEstilo(f.Linea, $"Línea {f.Linea}: en Roblox las funciones se crean con 'local function {fv.Nombre}()'."));
                    }
                    Funcion(f.Funcion);
                    break;
                case InsAsignacion a:
                    foreach (var ev in a.Valores) Expresiones(ev);
                    foreach (var d in a.Destinos.OfType<ExpVariable>())
                        if (!EsLocal(d.Nombre) && !permitidas.Contains(d.Nombre) && revisados.Add("global:" + d.Nombre))
                        {
                            Nombre(d.Nombre, a.Linea, false);
                            consejos.Add(new ConsejoEstilo(a.Linea, $"Línea {a.Linea}: la variable '{d.Nombre}' se ha creado sin 'local'. En Roblox se escribe: local {d.Nombre} = ..."));
                        }
                    break;
                case InsSi s:
                    foreach (var c in s.Condiciones) Expresiones(c);
                    foreach (var b in s.Bloques) Recorrer(b);
                    if (s.SiNo is not null) Recorrer(s.SiNo);
                    break;
                case InsMientras m: Expresiones(m.Condicion); Recorrer(m.Cuerpo); break;
                case InsRepetir r: Recorrer(r.Cuerpo); break;
                case InsHacer h: Recorrer(h.Cuerpo); break;
                case InsParaNumerico p:
                    Nombre(p.Variable, p.Linea, false);
                    locales.Add(new HashSet<string> { p.Variable });
                    Recorrer(p.Cuerpo);
                    locales.RemoveAt(locales.Count - 1);
                    break;
                case InsParaEn pe:
                    foreach (var n in pe.Variables) Nombre(n, pe.Linea, false);
                    locales.Add(new HashSet<string>(pe.Variables));
                    Recorrer(pe.Cuerpo);
                    locales.RemoveAt(locales.Count - 1);
                    break;
                case InsLlamada ll: Expresiones(ll.Llamada); break;
                case InsRetorno ret: foreach (var ev in ret.Valores) Expresiones(ev); break;
                case InsAsignacionCompuesta c: Expresiones(c.Valor); break;
            }
        }

        Recorrer(programa);
        return consejos;
    }

    public static string ACamelCase(string n)
    {
        var palabras = Regex.Matches(n, "[A-Z]?[a-z0-9]+|[A-Z]+(?![a-z])").Select(m => m.Value).ToList();
        if (palabras.Count == 0) return n;
        var sb = new StringBuilder(palabras[0].ToLowerInvariant());
        foreach (var p in palabras.Skip(1))
            sb.Append(char.ToUpperInvariant(p[0])).Append(p[1..].ToLowerInvariant());
        return sb.ToString();
    }
}
