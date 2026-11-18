using AventuraBit.Motor.Lua;

namespace AventuraBit.Motor.Mundo;

public enum TipoCasilla { Suelo, Muro, Agua, Meta }

public readonly record struct Pos(int X, int Y);

// ---------- Eventos que se animan en pantalla ----------
public abstract record EventoMundo;
public sealed record EventoMover(Pos Desde, Pos Hasta) : EventoMundo;
public sealed record EventoChoque(Pos En, int Dx, int Dy) : EventoMundo;
public sealed record EventoRecoger(Pos En, bool Exito) : EventoMundo;
public sealed record EventoPintar(Pos En, string Color) : EventoMundo;
public sealed record EventoDecir(string Texto) : EventoMundo;
public sealed record EventoImprimir(string Texto) : EventoMundo;
public sealed record EventoEsperar(double Segundos) : EventoMundo;
public sealed record EventoCaer(Pos En) : EventoMundo;

public static class Colores
{
    /// <summary>Colores que entiende pintar(), con su valor para dibujar.</summary>
    public static readonly IReadOnlyDictionary<string, string> Hex = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["rojo"] = "#EF4444", ["azul"] = "#3B82F6", ["verde"] = "#22C55E", ["amarillo"] = "#FACC15",
        ["naranja"] = "#F97316", ["morado"] = "#A855F7", ["rosa"] = "#EC4899", ["blanco"] = "#F8FAFC",
        ["negro"] = "#1E293B", ["gris"] = "#94A3B8", ["marron"] = "#92400E",
    };

    public static readonly IReadOnlyDictionary<char, string> PorLetra = new Dictionary<char, string>
    {
        ['r'] = "rojo", ['z'] = "azul", ['v'] = "verde", ['a'] = "amarillo", ['n'] = "naranja",
        ['m'] = "morado", ['p'] = "rosa", ['b'] = "blanco", ['k'] = "negro", ['g'] = "gris",
    };

    private static readonly Dictionary<string, string> Sinonimos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["red"] = "rojo", ["blue"] = "azul", ["green"] = "verde", ["yellow"] = "amarillo", ["orange"] = "naranja",
        ["purple"] = "morado", ["pink"] = "rosa", ["white"] = "blanco", ["black"] = "negro", ["grey"] = "gris",
        ["gray"] = "gris", ["brown"] = "marron", ["marrón"] = "marron", ["lila"] = "morado", ["violeta"] = "morado",
    };

    public static string? Normalizar(string nombre)
    {
        var n = nombre.Trim().ToLowerInvariant();
        if (Hex.ContainsKey(n)) return n;
        return Sinonimos.TryGetValue(n, out var s) ? s : null;
    }
}

/// <summary>Estado del tablero: casillas, gemas, pintura y la posición de Bit.</summary>
public sealed class EstadoMundo
{
    public EstadoMundo(int ancho, int alto)
    {
        Ancho = ancho;
        Alto = alto;
        Casillas = new TipoCasilla[ancho, alto];
    }

    public int Ancho { get; }
    public int Alto { get; }
    public TipoCasilla[,] Casillas { get; }
    public HashSet<Pos> Gemas { get; } = new();
    public Dictionary<Pos, string> Pintura { get; } = new();
    public Pos Bit { get; set; }

    public static EstadoMundo DesdeMapa(IReadOnlyList<string> filas, Pos? inicio = null)
    {
        int ancho = filas.Max(f => f.Length), alto = filas.Count;
        var e = new EstadoMundo(ancho, alto);
        for (int y = 0; y < alto; y++)
            for (int x = 0; x < ancho; x++)
            {
                char c = x < filas[y].Length ? filas[y][x] : '#';
                var p = new Pos(x, y);
                e.Casillas[x, y] = c switch
                {
                    '#' => TipoCasilla.Muro,
                    '~' => TipoCasilla.Agua,
                    'F' => TipoCasilla.Meta,
                    _ => TipoCasilla.Suelo,
                };
                if (c == 'R') e.Bit = p;
                if (c == 'G') e.Gemas.Add(p);
                if (Colores.PorLetra.TryGetValue(c, out var color)) e.Pintura[p] = color;
            }
        if (inicio is { } i) e.Bit = i;
        return e;
    }

    public EstadoMundo Clonar()
    {
        var e = new EstadoMundo(Ancho, Alto) { Bit = Bit };
        Array.Copy(Casillas, e.Casillas, Casillas.Length);
        e.Gemas.UnionWith(Gemas);
        foreach (var kv in Pintura) e.Pintura[kv.Key] = kv.Value;
        return e;
    }

    public bool Dentro(int x, int y) => x >= 0 && y >= 0 && x < Ancho && y < Alto;

    public TipoCasilla En(int x, int y) => Dentro(x, y) ? Casillas[x, y] : TipoCasilla.Muro;
}

/// <summary>El mundo de Bit durante una ejecución: aplica las órdenes del programa.</summary>
public sealed class Mundo
{
    private const int LimiteMovimientos = 1500;
    private int _acciones;

    public Mundo(EstadoMundo inicial)
    {
        Inicial = inicial;
        Estado = inicial.Clonar();
    }

    public EstadoMundo Inicial { get; }
    public EstadoMundo Estado { get; }
    public int GemasRecogidas { get; private set; }
    public int RecogidasFallidas { get; private set; }
    public int Choques { get; private set; }
    public bool Caido { get; private set; }
    public List<string> Dichos { get; } = new();
    public Action<EventoMundo>? AlEvento { get; set; }

    public bool EnMeta => Estado.En(Estado.Bit.X, Estado.Bit.Y) == TipoCasilla.Meta;

    private void Emitir(EventoMundo e)
    {
        if (++_acciones > LimiteMovimientos)
            throw new ErrorLua("¡Bit está agotado! Ha hecho demasiadas acciones. ¿Hay un bucle que no para?", 0);
        AlEvento?.Invoke(e);
    }

    public void Registrar(Interprete it)
    {
        it.RegistrarNativa("moverDerecha", a => { Mover(1, 0, Pasos(a, "moverDerecha")); return LuaUtil.Nada; });
        it.RegistrarNativa("moverIzquierda", a => { Mover(-1, 0, Pasos(a, "moverIzquierda")); return LuaUtil.Nada; });
        it.RegistrarNativa("moverArriba", a => { Mover(0, -1, Pasos(a, "moverArriba")); return LuaUtil.Nada; });
        it.RegistrarNativa("moverAbajo", a => { Mover(0, 1, Pasos(a, "moverAbajo")); return LuaUtil.Nada; });
        it.RegistrarNativa("recogerGema", _ => new object?[] { Recoger() });
        it.RegistrarNativa("pintar", a => { Pintar(a); return LuaUtil.Nada; });
        it.RegistrarNativa("decir", a =>
        {
            string texto = string.Join(" ", a.Select(LuaUtil.ATexto));
            Dichos.Add(texto);
            Emitir(new EventoDecir(texto));
            return LuaUtil.Nada;
        });
        it.RegistrarNativa("hayMuro", a => new object?[] { Mirar(a, "hayMuro") is TipoCasilla.Muro });
        it.RegistrarNativa("hayAgua", a => new object?[] { Mirar(a, "hayAgua") is TipoCasilla.Agua });
        it.RegistrarNativa("hayGema", _ => new object?[] { Estado.Gemas.Contains(Estado.Bit) });
        it.RegistrarNativa("enMeta", _ => new object?[] { EnMeta });
        it.RegistrarNativa("colorSuelo", _ => new object?[] { Estado.Pintura.TryGetValue(Estado.Bit, out var c) ? c : null });
        it.RegistrarNativa("gemasRecogidas", _ => new object?[] { (double)GemasRecogidas });
    }

    private static int Pasos(object?[] a, string funcion)
    {
        if (a.Length == 0 || a[0] is null) return 1;
        if (a[0] is double d)
        {
            if (d < 0 || d != Math.Floor(d))
                throw new ErrorLua($"{funcion} necesita un número entero de pasos, como {funcion}(3).", 0);
            return (int)d;
        }
        if (a[0] is string s && double.TryParse(s, out _))
            throw new ErrorLua($"{funcion}(\"{s}\") lleva el número entre comillas, y entonces es un texto. Quita las comillas: {funcion}({s})", 0);
        throw new ErrorLua($"{funcion} necesita un número de pasos, por ejemplo {funcion}(3), pero recibió {LuaUtil.TipoAmigable(a[0])}.", 0);
    }

    private static (int Dx, int Dy) Direccion(object?[] a, string funcion)
    {
        string dir = a.Length > 0 && a[0] is string s ? s.Trim().ToLowerInvariant() : "";
        return dir switch
        {
            "derecha" => (1, 0),
            "izquierda" => (-1, 0),
            "arriba" => (0, -1),
            "abajo" => (0, 1),
            _ => throw new ErrorLua($"{funcion} necesita una dirección entre comillas: \"derecha\", \"izquierda\", \"arriba\" o \"abajo\". Ejemplo: {funcion}(\"derecha\")", 0),
        };
    }

    private TipoCasilla Mirar(object?[] a, string funcion)
    {
        var (dx, dy) = Direccion(a, funcion);
        return Estado.En(Estado.Bit.X + dx, Estado.Bit.Y + dy);
    }

    private void Mover(int dx, int dy, int pasos)
    {
        for (int i = 0; i < pasos; i++)
        {
            var desde = Estado.Bit;
            int nx = desde.X + dx, ny = desde.Y + dy;
            if (Estado.En(nx, ny) == TipoCasilla.Muro)
            {
                Choques++;
                Emitir(new EventoChoque(desde, dx, dy));
                return;
            }
            Estado.Bit = new Pos(nx, ny);
            Emitir(new EventoMover(desde, Estado.Bit));
            if (Estado.En(nx, ny) == TipoCasilla.Agua)
            {
                Caido = true;
                Emitir(new EventoCaer(Estado.Bit));
                throw new ErrorJuego("¡Plof! Bit se ha caído al agua. Revisa los pasos que le has dado.");
            }
        }
    }

    private bool Recoger()
    {
        bool ok = Estado.Gemas.Remove(Estado.Bit);
        if (ok) GemasRecogidas++; else RecogidasFallidas++;
        Emitir(new EventoRecoger(Estado.Bit, ok));
        return ok;
    }

    private void Pintar(object?[] a)
    {
        if (a.Length == 0 || a[0] is not string nombre)
            throw new ErrorLua("pintar necesita un color entre comillas, por ejemplo pintar(\"rojo\").", 0);
        var color = Colores.Normalizar(nombre)
            ?? throw new ErrorLua($"No conozco el color \"{nombre}\". Puedes usar: {string.Join(", ", Colores.Hex.Keys)}.", 0);
        Estado.Pintura[Estado.Bit] = color;
        Emitir(new EventoPintar(Estado.Bit, color));
    }
}
