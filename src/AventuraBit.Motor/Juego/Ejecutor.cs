using AventuraBit.Motor.Lua;
using AventuraBit.Motor.Mundo;

namespace AventuraBit.Motor.Juego;

/// <summary>Una variable tal y como se ve en el panel de "cajas".</summary>
public sealed record VariableVista(string Nombre, string Valor, string Tipo, bool EsConstante, bool EsDelJuego);

/// <summary>Una foto de la ejecución: qué línea se ejecuta, qué pasa en el mundo y qué valen las variables.</summary>
public sealed record Fotograma(int Linea, EventoMundo? Evento, IReadOnlyList<VariableVista> Variables);

public sealed class ResultadoEjecucion
{
    public required Variante Variante { get; init; }
    public required int IndiceVariante { get; init; }
    public required string Codigo { get; init; }
    public required EstadoMundo Inicial { get; init; }
    public Mundo.Mundo? Mundo { get; set; }
    public Interprete? Interprete { get; set; }
    public Bloque? Programa { get; set; }
    public List<string> Salida { get; } = new();
    public ErrorLua? Error { get; set; }
    public bool ErrorDeSintaxis { get; set; }
    public List<Fotograma> Fotogramas { get; } = new();
    public List<(Objetivo Objetivo, bool Cumplido)> Objetivos { get; } = new();

    public bool Superado => Error is null && Objetivos.All(o => o.Cumplido);
}

public static class Ejecutor
{
    private const int MaxFotogramasDeLinea = 3000;

    public static ResultadoEjecucion Ejecutar(Nivel nivel, string codigo, int indiceVariante, bool grabar = true)
    {
        var variante = nivel.VariantesEfectivas[indiceVariante];
        var inicial = EstadoMundo.DesdeMapa(variante.Mapa, variante.Inicio);
        var r = new ResultadoEjecucion
        {
            Variante = variante,
            IndiceVariante = indiceVariante,
            Codigo = codigo,
            Inicial = inicial,
        };

        int hueco = BuscarHueco(codigo);
        if (hueco > 0)
        {
            r.Error = new ErrorLua("Todavía quedan huecos ___ por rellenar. Cambia cada ___ por el código que falta.", hueco);
            r.ErrorDeSintaxis = true;
            return r;
        }

        try
        {
            r.Programa = Parser.Analizar(codigo);
        }
        catch (ErrorLua e)
        {
            r.Error = e;
            r.ErrorDeSintaxis = true;
            return r;
        }

        var mundo = new Mundo.Mundo(inicial);
        var it = new Interprete();
        r.Mundo = mundo;
        r.Interprete = it;
        Biblioteca.Registrar(it);
        mundo.Registrar(it);
        foreach (var g in variante.Globales)
            it.RegistrarValor(g.Key, O.Normalizar(g.Value), esNativo: false);

        int lineasGrabadas = 0;
        it.AlImprimir = texto =>
        {
            r.Salida.Add(texto);
            if (grabar) r.Fotogramas.Add(new Fotograma(it.LineaActual, new EventoImprimir(texto), Foto(it)));
        };
        if (grabar)
        {
            it.AlInstruccion = linea =>
            {
                if (lineasGrabadas++ < MaxFotogramasDeLinea)
                    r.Fotogramas.Add(new Fotograma(linea, null, Foto(it)));
            };
            it.AlEsperar = s => r.Fotogramas.Add(new Fotograma(it.LineaActual, new EventoEsperar(s), Foto(it)));
            mundo.AlEvento = e => r.Fotogramas.Add(new Fotograma(it.LineaActual, e, Foto(it)));
        }

        try
        {
            it.Ejecutar(r.Programa);
        }
        catch (ErrorLua e)
        {
            if (e.Linea == 0) e.Linea = it.LineaActual;
            r.Error = e;
        }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException or OverflowException or IndexOutOfRangeException)
        {
            r.Error = new ErrorLua("Algo raro ha pasado al ejecutar esta línea: " + e.Message, it.LineaActual);
        }

        if (r.Error is null)
        {
            foreach (var o in nivel.Objetivos.Concat(variante.Objetivos))
                r.Objetivos.Add((o, o.Comprobar(r)));
        }
        else
        {
            foreach (var o in nivel.Objetivos.Concat(variante.Objetivos))
                r.Objetivos.Add((o, false));
        }
        return r;
    }

    private static int BuscarHueco(string codigo)
    {
        var lineas = codigo.Split('\n');
        for (int i = 0; i < lineas.Length; i++)
        {
            var l = lineas[i];
            int com = l.IndexOf("--", StringComparison.Ordinal);
            if (com >= 0) l = l[..com];
            if (l.Contains("___")) return i + 1;
        }
        return 0;
    }

    /// <summary>Copia de las variables visibles en este momento.</summary>
    public static IReadOnlyList<VariableVista> Foto(Interprete it)
    {
        var ambitos = new List<Ambito>();
        for (var a = it.AmbitoActual; a is not null && a != it.Global; a = a.Padre) ambitos.Add(a);
        ambitos.Reverse();

        // Primero las globales (del juego o creadas sin local), luego de fuera hacia dentro.
        var nombres = new List<string>();
        foreach (var (nombre, _) in it.Global.Variables)
            if (!it.EsNativa(nombre)) nombres.Add(nombre);
        foreach (var a in ambitos)
            foreach (var (nombre, _) in a.Variables)
                if (!nombres.Contains(nombre)) nombres.Add(nombre);

        // Si un nombre se repite, se ve el valor de la variable más interior.
        return nombres
            .Select(n => Vista(n, it.AmbitoActual.Buscar(n)?.Valor, it.EsDelJuego(n)))
            .ToList();
    }

    private static VariableVista Vista(string nombre, object? valor, bool delJuego) =>
        new(nombre, LuaUtil.Representar(valor), LuaUtil.TipoAmigable(valor), Interprete.EsNombreConstante(nombre), delJuego);
}
