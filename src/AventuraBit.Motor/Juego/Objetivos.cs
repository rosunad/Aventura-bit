using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AventuraBit.Motor.Lua;
using AventuraBit.Motor.Mundo;

namespace AventuraBit.Motor.Juego;

/// <summary>Algo que el programa tiene que conseguir para superar el nivel.</summary>
public abstract class Objetivo
{
    public abstract string Descripcion { get; }
    public abstract bool Comprobar(ResultadoEjecucion r);
    public bool Oculto { get; init; }
}

public enum Construccion { If, Else, ElseIf, For, While, Repeat, FuncionPropia, Return, Local, Tabla, Ipairs, MasIgual, Concatenar, And, Or, Not, Interpolacion }

/// <summary>Fábrica corta para escribir los niveles: O.Meta(), O.Imprime("..."), ...</summary>
public static class O
{
    public static Objetivo Meta() => new ObjetivoSimple("Llega a la bandera", r => r.Mundo?.EnMeta == true);
    public static Objetivo Gemas() => new ObjetivoSimple("Recoge todas las gemas", r => r.Mundo is { } m && m.Estado.Gemas.Count == 0);
    public static Objetivo SinRecogerEnVacio() => new ObjetivoSimple("No intentes recoger donde no hay gema", r => r.Mundo?.RecogidasFallidas == 0);
    public static Objetivo SinChoques() => new ObjetivoSimple("No te choques con ningún muro", r => r.Mundo?.Choques == 0);
    public static Objetivo Posicion(int x, int y, string descripcion) =>
        new ObjetivoSimple(descripcion, r => r.Mundo?.Estado.Bit == new Pos(x, y));

    public static Objetivo Imprime(string texto) =>
        new ObjetivoSimple($"Muestra con print: {texto}", r => r.Salida.Any(l => Texto.Igual(l, texto)));

    /// <summary>Muestra exactamente un mensaje y es ese (para que no valga imprimirlo todo).</summary>
    public static Objetivo ImprimeSolo(string texto) =>
        new ObjetivoSimple($"Muestra solo: {texto}", r => r.Salida.Count == 1 && Texto.Igual(r.Salida[0], texto));

    public static Objetivo UsaAlguno(string descripcion, params Construccion[] cs) =>
        new ObjetivoSimple(descripcion, r => r.Programa is { } p && cs.Any(c => Analisis.Usa(p, c)));

    public static Objetivo Longitud(string nombre, int longitud) =>
        new ObjetivoSimple($"La tabla '{nombre}' tiene {longitud} elementos al final", r =>
            r.Interprete is { } it && it.TryObtenerVariable(nombre, out var v) && v is LuaTabla t && t.Longitud == longitud);

    public static Objetivo ImprimeLineas(params string[] lineas) =>
        new ObjetivoSimple($"Muestra con print, en orden: {string.Join(" / ", lineas)}", r =>
        {
            int k = 0;
            foreach (var l in r.Salida)
                if (k < lineas.Length && Texto.Igual(l, lineas[k])) k++;
            return k == lineas.Length;
        });

    public static Objetivo Dice(string texto) =>
        new ObjetivoSimple($"Bit dice: \"{texto}\"", r => r.Mundo?.Dichos.Any(d => Texto.Igual(d, texto)) == true);

    public static Objetivo NoDice() =>
        new ObjetivoSimple("Bit no dice nada", r => r.Mundo?.Dichos.Count == 0);

    public static Objetivo Variable(string nombre, object valor) =>
        new ObjetivoSimple($"La variable '{nombre}' vale {LuaUtil.Representar(Normalizar(valor))} al final", r =>
            r.Interprete is { } it && it.TryObtenerVariable(nombre, out var v) && LuaUtil.SonIguales(v, Normalizar(valor)));

    public static Objetivo VariableTipo(string nombre, string tipoLua, string tipoAmigable) =>
        new ObjetivoSimple($"Existe la variable '{nombre}' y guarda {tipoAmigable}", r =>
            r.Interprete is { } it && it.TryObtenerVariable(nombre, out var v) && LuaUtil.TipoDe(v) == tipoLua);

    public static Objetivo ImprimeVariable(string nombre) =>
        new ObjetivoSimple($"Muestra con print el valor de '{nombre}'", r =>
            r.Interprete is { } it && it.TryObtenerVariable(nombre, out var v) && v is not null
            && r.Salida.Any(l => Texto.Normalizar(l).Contains(Texto.Normalizar(LuaUtil.ATexto(v)))));

    public static Objetivo Usa(Construccion c, string descripcion) =>
        new ObjetivoSimple(descripcion, r => r.Programa is { } p && Analisis.Usa(p, c));

    public static Objetivo Llama(string funcion, string descripcion, int veces = 1) =>
        new ObjetivoSimple(descripcion, r => r.Programa is { } p && Arbol.Nodos(p).Count(n => Arbol.NombreLlamada(n) == funcion) >= veces);

    public static Objetivo Codigo(string patron, string descripcion) =>
        new ObjetivoSimple(descripcion, r => Regex.IsMatch(Analisis.SinComentarios(r.Codigo), patron));

    public static Objetivo SinCodigo(string patron, string descripcion) =>
        new ObjetivoSimple(descripcion, r => !Regex.IsMatch(Analisis.SinComentarios(r.Codigo), patron));

    public static Objetivo Pinta(string descripcion, params (int X, int Y, string Color)[] casillas) =>
        new ObjetivoSimple(descripcion, r => r.Mundo is { } m &&
            casillas.All(c => m.Estado.Pintura.TryGetValue(new Pos(c.X, c.Y), out var col) && col == c.Color));

    public static Objetivo PintaConVariable(string variable, string descripcion, params (int X, int Y)[] casillas) =>
        new ObjetivoSimple(descripcion, r => r.Mundo is { } m && r.Interprete is { } it
            && it.TryObtenerVariable(variable, out var v) && v is string s && Colores.Normalizar(s) is { } color
            && casillas.All(c => m.Estado.Pintura.TryGetValue(new Pos(c.X, c.Y), out var col) && col == color));

    /// <summary>Llama a una función del niño con unos valores y comprueba lo que devuelve.</summary>
    public static Objetivo Funcion(string nombre, object?[] argumentos, object? esperado) =>
        new ObjetivoSimple(
            $"{nombre}({string.Join(", ", argumentos.Select(a => LuaUtil.Representar(Normalizar(a))))}) devuelve {LuaUtil.Representar(Normalizar(esperado))}",
            r =>
            {
                if (r.Interprete is not { } it || !it.TryObtenerVariable(nombre, out var f) || f is not IFuncionLua) return false;
                try
                {
                    var res = it.LlamarFuncion(f, argumentos.Select(Normalizar).ToArray());
                    return LuaUtil.SonIguales(res.Length > 0 ? res[0] : null, Normalizar(esperado));
                }
                catch (ErrorLua)
                {
                    return false;
                }
            });

    public static object? Normalizar(object? v) => v switch
    {
        int i => (double)i,
        long l => (double)l,
        float f => (double)f,
        _ => v,
    };
}

public sealed class ObjetivoSimple : Objetivo
{
    private readonly Func<ResultadoEjecucion, bool> _comprobar;

    public ObjetivoSimple(string descripcion, Func<ResultadoEjecucion, bool> comprobar)
    {
        Descripcion = descripcion;
        _comprobar = comprobar;
    }

    public override string Descripcion { get; }

    public override bool Comprobar(ResultadoEjecucion r)
    {
        try { return _comprobar(r); }
        catch { return false; }
    }
}

public static class Texto
{
    /// <summary>Compara textos sin fijarse en mayúsculas, tildes, signos ni espacios de más.</summary>
    public static bool Igual(string a, string b) => Normalizar(a) == Normalizar(b);

    public static string Normalizar(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in s.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c) || c == '-' || c == '+' || c == '=' || c == '.') sb.Append(char.ToLowerInvariant(c));
            else if (char.IsWhiteSpace(c) || c == ',' || c == ':') sb.Append(' ');
        }
        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }
}

public static class Analisis
{
    public static string SinComentarios(string codigo) =>
        Regex.Replace(Regex.Replace(codigo, @"--\[(=*)\[[\s\S]*?\]\1\]", ""), @"--[^\n]*", "");

    public static bool Usa(Bloque p, Construccion c)
    {
        var nodos = Arbol.Nodos(p).ToList();
        return c switch
        {
            Construccion.If => nodos.OfType<InsSi>().Any(),
            Construccion.Else => nodos.OfType<InsSi>().Any(s => s.SiNo is not null),
            Construccion.ElseIf => nodos.OfType<InsSi>().Any(s => s.Condiciones.Count > 1),
            Construccion.For => nodos.Any(n => n is InsParaNumerico or InsParaEn),
            Construccion.While => nodos.OfType<InsMientras>().Any(),
            Construccion.Repeat => nodos.OfType<InsRepetir>().Any(),
            Construccion.FuncionPropia => nodos.Any(n => n is InsLocalFuncion or InsFuncion or ExpFuncion),
            Construccion.Return => nodos.OfType<InsRetorno>().Any(),
            Construccion.Local => nodos.Any(n => n is InsLocal or InsLocalFuncion),
            Construccion.Tabla => nodos.OfType<ExpTabla>().Any(),
            Construccion.Ipairs => nodos.Any(n => Arbol.NombreLlamada(n) is "ipairs" or "pairs") || nodos.OfType<InsParaEn>().Any(),
            Construccion.MasIgual => nodos.OfType<InsAsignacionCompuesta>().Any(),
            Construccion.Concatenar => nodos.OfType<ExpBinaria>().Any(b => b.Operador == "..") || nodos.Any(n => n is InsAsignacionCompuesta { Operador: ".." }),
            Construccion.And => nodos.OfType<ExpBinaria>().Any(b => b.Operador == "and"),
            Construccion.Or => nodos.OfType<ExpBinaria>().Any(b => b.Operador == "or"),
            Construccion.Not => nodos.OfType<ExpUnaria>().Any(b => b.Operador == "not"),
            Construccion.Interpolacion => nodos.OfType<ExpInterpolada>().Any(),
            _ => false,
        };
    }

    /// <summary>Líneas con código de verdad (sin contar vacías ni comentarios).</summary>
    public static int LineasDeCodigo(string codigo)
    {
        try
        {
            return Lexer.Analizar(codigo).Where(t => t.Tipo != TipoToken.FinArchivo).Select(t => t.Linea).Distinct().Count();
        }
        catch (ErrorLua)
        {
            return codigo.Split('\n').Count(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("--"));
        }
    }
}
