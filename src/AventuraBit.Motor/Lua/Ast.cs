namespace AventuraBit.Motor.Lua;

public abstract record Nodo(int Linea);

// ---------- Expresiones ----------
public abstract record Exp(int Linea) : Nodo(Linea);

public sealed record ExpLiteral(object? Valor, int Linea) : Exp(Linea);

public sealed record ExpInterpolada(IReadOnlyList<Exp> Partes, int Linea) : Exp(Linea);

public sealed record ExpVariable(string Nombre, int Linea) : Exp(Linea);

public sealed record ExpIndice(Exp Objeto, Exp Clave, int Linea) : Exp(Linea)
{
    /// <summary>Nombre si se escribió con punto (jugador.vidas).</summary>
    public string? NombreCampo => Clave is ExpLiteral { Valor: string s } ? s : null;
}

public sealed record ExpLlamada(Exp Funcion, IReadOnlyList<Exp> Argumentos, int Linea) : Exp(Linea);

public sealed record ExpMetodo(Exp Objeto, string Metodo, IReadOnlyList<Exp> Argumentos, int Linea) : Exp(Linea);

public sealed record ExpFuncion(string Nombre, IReadOnlyList<string> Parametros, Bloque Cuerpo, int Linea) : Exp(Linea);

public sealed record ExpBinaria(string Operador, Exp Izquierda, Exp Derecha, int Linea) : Exp(Linea);

public sealed record ExpUnaria(string Operador, Exp Operando, int Linea) : Exp(Linea);

public sealed record ExpParentesis(Exp Interior, int Linea) : Exp(Linea);

public sealed record CampoTabla(Exp? Clave, Exp Valor);

public sealed record ExpTabla(IReadOnlyList<CampoTabla> Campos, int Linea) : Exp(Linea);

// ---------- Instrucciones ----------
public abstract record Instruccion(int Linea) : Nodo(Linea);

public sealed record Bloque(IReadOnlyList<Instruccion> Instrucciones);

public sealed record InsLocal(IReadOnlyList<string> Nombres, IReadOnlyList<Exp> Valores, int Linea) : Instruccion(Linea);

public sealed record InsLocalFuncion(string Nombre, ExpFuncion Funcion, int Linea) : Instruccion(Linea);

public sealed record InsFuncion(Exp Destino, ExpFuncion Funcion, int Linea) : Instruccion(Linea);

public sealed record InsAsignacion(IReadOnlyList<Exp> Destinos, IReadOnlyList<Exp> Valores, int Linea) : Instruccion(Linea);

public sealed record InsAsignacionCompuesta(Exp Destino, string Operador, Exp Valor, int Linea) : Instruccion(Linea);

public sealed record InsLlamada(Exp Llamada, int Linea) : Instruccion(Linea);

public sealed record InsSi(IReadOnlyList<Exp> Condiciones, IReadOnlyList<Bloque> Bloques, Bloque? SiNo, int Linea) : Instruccion(Linea);

public sealed record InsMientras(Exp Condicion, Bloque Cuerpo, int Linea) : Instruccion(Linea);

public sealed record InsRepetir(Bloque Cuerpo, Exp Condicion, int Linea) : Instruccion(Linea);

public sealed record InsParaNumerico(string Variable, Exp Inicio, Exp Fin, Exp? Paso, Bloque Cuerpo, int Linea) : Instruccion(Linea);

public sealed record InsParaEn(IReadOnlyList<string> Variables, IReadOnlyList<Exp> Valores, Bloque Cuerpo, int Linea) : Instruccion(Linea);

public sealed record InsHacer(Bloque Cuerpo, int Linea) : Instruccion(Linea);

public sealed record InsRetorno(IReadOnlyList<Exp> Valores, int Linea) : Instruccion(Linea);

public sealed record InsRomper(int Linea) : Instruccion(Linea);

public sealed record InsContinuar(int Linea) : Instruccion(Linea);

public static class Arbol
{
    /// <summary>Recorre todos los nodos del programa (para comprobar qué usa el niño).</summary>
    public static IEnumerable<Nodo> Nodos(Bloque bloque)
    {
        foreach (var ins in bloque.Instrucciones)
            foreach (var n in Nodos(ins))
                yield return n;
    }

    public static IEnumerable<Nodo> Nodos(Nodo nodo)
    {
        yield return nodo;
        IEnumerable<Nodo> hijos = nodo switch
        {
            ExpInterpolada e => e.Partes,
            ExpIndice e => new Nodo[] { e.Objeto, e.Clave },
            ExpLlamada e => e.Argumentos.Prepend(e.Funcion),
            ExpMetodo e => e.Argumentos.Prepend(e.Objeto),
            ExpFuncion e => e.Cuerpo.Instrucciones,
            ExpBinaria e => new Nodo[] { e.Izquierda, e.Derecha },
            ExpUnaria e => new Nodo[] { e.Operando },
            ExpParentesis e => new Nodo[] { e.Interior },
            ExpTabla e => e.Campos.SelectMany(c => c.Clave is null ? new Nodo[] { c.Valor } : new Nodo[] { c.Clave, c.Valor }),
            InsLocal i => i.Valores,
            InsLocalFuncion i => new Nodo[] { i.Funcion },
            InsFuncion i => new Nodo[] { i.Destino, i.Funcion },
            InsAsignacion i => i.Destinos.Concat(i.Valores),
            InsAsignacionCompuesta i => new Nodo[] { i.Destino, i.Valor },
            InsLlamada i => new Nodo[] { i.Llamada },
            InsSi i => i.Condiciones.Cast<Nodo>().Concat(i.Bloques.SelectMany(b => b.Instrucciones))
                .Concat(i.SiNo?.Instrucciones ?? Array.Empty<Instruccion>()),
            InsMientras i => i.Cuerpo.Instrucciones.Cast<Nodo>().Prepend(i.Condicion),
            InsRepetir i => i.Cuerpo.Instrucciones.Cast<Nodo>().Append(i.Condicion),
            InsParaNumerico i => new Nodo?[] { i.Inicio, i.Fin, i.Paso }.OfType<Nodo>().Concat(i.Cuerpo.Instrucciones),
            InsParaEn i => i.Valores.Cast<Nodo>().Concat(i.Cuerpo.Instrucciones),
            InsHacer i => i.Cuerpo.Instrucciones,
            InsRetorno i => i.Valores,
            _ => Array.Empty<Nodo>(),
        };
        foreach (var h in hijos)
            foreach (var n in Nodos(h))
                yield return n;
    }

    /// <summary>Nombre de la función a la que se llama (print, moverDerecha, math.floor...).</summary>
    public static string? NombreLlamada(Nodo n) => n switch
    {
        ExpLlamada { Funcion: ExpVariable v } => v.Nombre,
        ExpLlamada { Funcion: ExpIndice { Objeto: ExpVariable o } i } when i.NombreCampo is not null => o.Nombre + "." + i.NombreCampo,
        ExpMetodo m => m.Metodo,
        _ => null,
    };
}
