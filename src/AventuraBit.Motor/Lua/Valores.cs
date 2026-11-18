using System.Globalization;
using System.Text;

namespace AventuraBit.Motor.Lua;

/// <summary>Error de un programa Luau, con un mensaje pensado para un niño.</summary>
public class ErrorLua : Exception
{
    public ErrorLua(string mensaje, int linea) : base(mensaje) => Linea = linea;

    /// <summary>Línea del código (1 en adelante). 0 si aún no se conoce.</summary>
    public int Linea { get; set; }
}

/// <summary>Algo que le pasa a Bit en el mundo y termina el programa (por ejemplo, caer al agua).</summary>
public sealed class ErrorJuego : ErrorLua
{
    public ErrorJuego(string mensaje) : base(mensaje, 0) { }
}

/// <summary>Una "caja" donde vive el valor de una variable.</summary>
public sealed class Celda
{
    public Celda(object? valor) => Valor = valor;
    public object? Valor { get; set; }
}

/// <summary>Conjunto de variables visibles en un trozo de código.</summary>
public sealed class Ambito
{
    private readonly Dictionary<string, Celda> _variables = new(StringComparer.Ordinal);
    private readonly List<string> _orden = new();

    public Ambito(Ambito? padre) => Padre = padre;

    public Ambito? Padre { get; }

    public void Declarar(string nombre, object? valor)
    {
        _variables[nombre] = new Celda(valor);
        if (!_orden.Contains(nombre)) _orden.Add(nombre);
    }

    public Celda? BuscarAqui(string nombre) => _variables.TryGetValue(nombre, out var c) ? c : null;

    public Celda? Buscar(string nombre)
    {
        for (var a = this; a is not null; a = a.Padre)
            if (a._variables.TryGetValue(nombre, out var c)) return c;
        return null;
    }

    public IEnumerable<(string Nombre, Celda Celda)> Variables => _orden.Select(n => (n, _variables[n]));
}

/// <summary>Tabla de Luau: sirve de lista y de objeto con propiedades.</summary>
public sealed class LuaTabla
{
    private readonly Dictionary<object, object?> _datos = new();

    public object? Obtener(object? clave)
    {
        if (clave is null) return null;
        return _datos.TryGetValue(clave, out var v) ? v : null;
    }

    public void Asignar(object clave, object? valor)
    {
        if (valor is null) _datos.Remove(clave);
        else _datos[clave] = valor;
    }

    public int Longitud
    {
        get
        {
            int n = 0;
            while (_datos.ContainsKey((double)(n + 1))) n++;
            return n;
        }
    }

    public IReadOnlyList<KeyValuePair<object, object?>> Pares()
    {
        // Primero la parte de lista, en orden; después el resto.
        var lista = new List<KeyValuePair<object, object?>>();
        int n = Longitud;
        for (int i = 1; i <= n; i++) lista.Add(new((double)i, _datos[(double)i]));
        foreach (var kv in _datos)
            if (!(kv.Key is double d && d >= 1 && d <= n && d == Math.Floor(d))) lista.Add(kv);
        return lista;
    }

    public void Insertar(int posicion, object? valor)
    {
        int n = Longitud;
        for (int i = n; i >= posicion; i--) Asignar((double)(i + 1), Obtener((double)i));
        Asignar((double)posicion, valor);
    }

    public object? Quitar(int posicion)
    {
        int n = Longitud;
        if (n == 0) return null;
        var valor = Obtener((double)posicion);
        for (int i = posicion; i < n; i++) Asignar((double)i, Obtener((double)(i + 1)));
        Asignar((double)n, null);
        return valor;
    }
}

public interface IFuncionLua
{
    string Nombre { get; }
}

public sealed class FuncionNativa : IFuncionLua
{
    public FuncionNativa(string nombre, Func<object?[], object?[]> cuerpo)
    {
        Nombre = nombre;
        Cuerpo = cuerpo;
    }

    public string Nombre { get; }
    public Func<object?[], object?[]> Cuerpo { get; }
}

public sealed class FuncionLua : IFuncionLua
{
    public FuncionLua(string nombre, ExpFuncion definicion, Ambito cierre)
    {
        Nombre = nombre;
        Definicion = definicion;
        Cierre = cierre;
    }

    public string Nombre { get; }
    public ExpFuncion Definicion { get; }
    public Ambito Cierre { get; }
}

public static class LuaUtil
{
    public static readonly object?[] Nada = Array.Empty<object?>();

    public static bool EsVerdadero(object? v) => v is not null && !(v is bool b && !b);

    public static string TipoDe(object? v) => v switch
    {
        null => "nil",
        bool => "boolean",
        double => "number",
        string => "string",
        LuaTabla => "table",
        IFuncionLua => "function",
        _ => "userdata",
    };

    public static string TipoAmigable(object? v) => v switch
    {
        null => "nil (nada)",
        bool => "booleano",
        double => "número",
        string => "texto",
        LuaTabla => "tabla",
        IFuncionLua => "función",
        _ => "objeto",
    };

    public static string NumeroATexto(double d)
    {
        if (double.IsPositiveInfinity(d)) return "inf";
        if (double.IsNegativeInfinity(d)) return "-inf";
        if (double.IsNaN(d)) return "nan";
        if (d == Math.Floor(d) && Math.Abs(d) < 1e15) return ((long)d).ToString(CultureInfo.InvariantCulture);
        return d.ToString("0.##############", CultureInfo.InvariantCulture);
    }

    public static string ATexto(object? v) => v switch
    {
        null => "nil",
        bool b => b ? "true" : "false",
        double d => NumeroATexto(d),
        string s => s,
        LuaTabla => "table",
        IFuncionLua f => "function: " + f.Nombre,
        _ => v.ToString() ?? "",
    };

    /// <summary>Texto corto para enseñar el valor en el panel de variables.</summary>
    public static string Representar(object? v, int profundidad = 0)
    {
        switch (v)
        {
            case string s: return "\"" + s + "\"";
            case LuaTabla t:
                if (profundidad > 1) return "{...}";
                var sb = new StringBuilder("{");
                int n = t.Longitud, i = 0;
                foreach (var kv in t.Pares())
                {
                    if (i++ > 0) sb.Append(", ");
                    if (sb.Length > 60) { sb.Append("..."); break; }
                    bool esLista = kv.Key is double d && d >= 1 && d <= n;
                    if (!esLista)
                        sb.Append(kv.Key is string k ? k : "[" + Representar(kv.Key, profundidad + 1) + "]").Append(" = ");
                    sb.Append(Representar(kv.Value, profundidad + 1));
                }
                return sb.Append('}').ToString();
            case IFuncionLua f: return "función " + f.Nombre;
            default: return ATexto(v);
        }
    }

    public static bool SonIguales(object? a, object? b) => a is null ? b is null : a.Equals(b);
}
