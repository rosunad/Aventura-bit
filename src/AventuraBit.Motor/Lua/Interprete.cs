namespace AventuraBit.Motor.Lua;

/// <summary>Ejecuta el árbol de un programa Luau paso a paso.</summary>
public sealed class Interprete
{
    private enum Senal { Nada, Romper, Continuar, Retornar }

    private readonly HashSet<string> _nativos = new(StringComparer.Ordinal);
    private object?[] _retorno = LuaUtil.Nada;
    private int _profundidad;

    public Interprete()
    {
        Global = new Ambito(null);
        AmbitoActual = Global;
    }

    public Ambito Global { get; }

    /// <summary>Ámbito del programa principal (sus variables locales siguen ahí al terminar).</summary>
    public Ambito? AmbitoRaiz { get; private set; }

    public Ambito AmbitoActual { get; private set; }

    public int LimitePasos { get; set; } = 20000;
    public int LimiteProfundidad { get; set; } = 150;
    public int Pasos { get; private set; }
    public int LineaActual { get; private set; }

    /// <summary>Se llama antes de ejecutar cada instrucción (para grabar la ejecución).</summary>
    public Action<int>? AlInstruccion { get; set; }

    public Action<string>? AlImprimir { get; set; }
    public Action<double>? AlEsperar { get; set; }

    /// <summary>Consejos que no son errores, como cambiar una constante.</summary>
    public List<(int Linea, string Mensaje)> Avisos { get; } = new();

    public Random Azar { get; set; } = new(2024);

    public bool EsNativa(string nombre) => _nativos.Contains(nombre);

    public IEnumerable<string> NombresNativos => _nativos;

    public void RegistrarNativa(string nombre, Func<object?[], object?[]> cuerpo)
    {
        Global.Declarar(nombre, new FuncionNativa(nombre, cuerpo));
        _nativos.Add(nombre);
    }

    public void RegistrarValor(string nombre, object? valor, bool esNativo = true)
    {
        Global.Declarar(nombre, valor);
        if (esNativo) _nativos.Add(nombre);
        else _delJuego.Add(nombre);
    }

    private readonly HashSet<string> _delJuego = new(StringComparer.Ordinal);

    /// <summary>Variables que el juego crea para el nivel (no las ha escrito el niño).</summary>
    public bool EsDelJuego(string nombre) => _delJuego.Contains(nombre);

    public IEnumerable<string> NombresDelJuego => _delJuego;

    public void Ejecutar(Bloque programa)
    {
        AmbitoRaiz = new Ambito(Global);
        EjecutarBloque(programa, AmbitoRaiz);
    }

    /// <summary>Busca una variable del programa ya ejecutado (local del programa o global).</summary>
    public bool TryObtenerVariable(string nombre, out object? valor)
    {
        var c = AmbitoRaiz?.BuscarAqui(nombre) ?? (EsNativa(nombre) ? null : Global.BuscarAqui(nombre));
        valor = c?.Valor;
        return c is not null;
    }

    public object?[] LlamarFuncion(object? funcion, object?[] argumentos, int linea = 0) =>
        Llamar(funcion, argumentos, linea, "función");

    // ---------------- Instrucciones ----------------
    private void Paso(int linea)
    {
        LineaActual = linea;
        if (++Pasos > LimitePasos)
            throw new ErrorLua("¡El programa no termina nunca! Seguramente hay un bucle infinito: revisa que la condición del bucle llegue a cambiar.", linea);
        AlInstruccion?.Invoke(linea);
    }

    private Senal EjecutarBloque(Bloque bloque, Ambito ambito)
    {
        var anterior = AmbitoActual;
        AmbitoActual = ambito;
        try
        {
            foreach (var ins in bloque.Instrucciones)
            {
                var s = EjecutarInstruccion(ins, ambito);
                if (s != Senal.Nada) return s;
            }
            return Senal.Nada;
        }
        finally
        {
            AmbitoActual = anterior;
        }
    }

    private Senal EjecutarInstruccion(Instruccion ins, Ambito ambito)
    {
        Paso(ins.Linea);
        switch (ins)
        {
            case InsLocal l:
            {
                var valores = EvaluarLista(l.Valores, ambito);
                for (int i = 0; i < l.Nombres.Count; i++)
                    ambito.Declarar(l.Nombres[i], i < valores.Length ? valores[i] : null);
                return Senal.Nada;
            }
            case InsLocalFuncion lf:
                ambito.Declarar(lf.Nombre, null);
                ambito.BuscarAqui(lf.Nombre)!.Valor = new FuncionLua(lf.Nombre, lf.Funcion, ambito);
                return Senal.Nada;
            case InsFuncion f:
                Asignar(f.Destino, new FuncionLua(f.Funcion.Nombre, f.Funcion, ambito), ambito, f.Linea);
                return Senal.Nada;
            case InsAsignacion a:
            {
                var valores = EvaluarLista(a.Valores, ambito);
                for (int i = 0; i < a.Destinos.Count; i++)
                    Asignar(a.Destinos[i], i < valores.Length ? valores[i] : null, ambito, a.Linea);
                return Senal.Nada;
            }
            case InsAsignacionCompuesta c:
            {
                var actual = Evaluar(c.Destino, ambito);
                var valor = Evaluar(c.Valor, ambito);
                var resultado = Operar(c.Operador, actual, valor, c.Destino, c.Valor, c.Linea);
                Asignar(c.Destino, resultado, ambito, c.Linea);
                return Senal.Nada;
            }
            case InsLlamada ll:
                EvaluarMultiple(ll.Llamada, ambito);
                return Senal.Nada;
            case InsSi si:
            {
                for (int i = 0; i < si.Condiciones.Count; i++)
                {
                    if (LuaUtil.EsVerdadero(Evaluar(si.Condiciones[i], ambito)))
                        return EjecutarBloque(si.Bloques[i], new Ambito(ambito));
                    if (i + 1 < si.Condiciones.Count) Paso(si.Condiciones[i + 1].Linea);
                }
                return si.SiNo is null ? Senal.Nada : EjecutarBloque(si.SiNo, new Ambito(ambito));
            }
            case InsMientras m:
            {
                bool primera = true;
                while (true)
                {
                    if (!primera) Paso(m.Linea);
                    primera = false;
                    if (!LuaUtil.EsVerdadero(Evaluar(m.Condicion, ambito))) break;
                    var s = EjecutarBloque(m.Cuerpo, new Ambito(ambito));
                    if (s == Senal.Romper) break;
                    if (s == Senal.Retornar) return s;
                }
                return Senal.Nada;
            }
            case InsRepetir r:
            {
                while (true)
                {
                    var interior = new Ambito(ambito);
                    var s = EjecutarBloque(r.Cuerpo, interior);
                    if (s == Senal.Romper) break;
                    if (s == Senal.Retornar) return s;
                    var anterior = AmbitoActual;
                    AmbitoActual = interior;
                    try
                    {
                        Paso(r.Condicion.Linea);
                        if (LuaUtil.EsVerdadero(Evaluar(r.Condicion, interior))) break;
                    }
                    finally { AmbitoActual = anterior; }
                }
                return Senal.Nada;
            }
            case InsParaNumerico p:
            {
                double inicio = NumeroDelFor(Evaluar(p.Inicio, ambito), "el valor inicial", p.Linea);
                double fin = NumeroDelFor(Evaluar(p.Fin, ambito), "el valor final", p.Linea);
                double paso = p.Paso is null ? 1 : NumeroDelFor(Evaluar(p.Paso, ambito), "el paso", p.Linea);
                if (paso == 0) throw new ErrorLua("El paso del 'for' no puede ser 0: el bucle no avanzaría nunca.", p.Linea);
                bool primera = true;
                for (double i = inicio; paso > 0 ? i <= fin : i >= fin; i += paso)
                {
                    if (!primera) Paso(p.Linea);
                    primera = false;
                    var interior = new Ambito(ambito);
                    interior.Declarar(p.Variable, i);
                    var s = EjecutarBloque(p.Cuerpo, interior);
                    if (s == Senal.Romper) break;
                    if (s == Senal.Retornar) return s;
                }
                return Senal.Nada;
            }
            case InsParaEn pe:
                return EjecutarParaEn(pe, ambito);
            case InsHacer h:
                return EjecutarBloque(h.Cuerpo, new Ambito(ambito));
            case InsRetorno ret:
                _retorno = EvaluarLista(ret.Valores, ambito);
                return Senal.Retornar;
            case InsRomper:
                return Senal.Romper;
            case InsContinuar:
                return Senal.Continuar;
            default:
                throw new ErrorLua("Instrucción desconocida.", ins.Linea);
        }
    }

    private static double NumeroDelFor(object? v, string que, int linea) => v is double d
        ? d
        : throw new ErrorLua($"En el 'for', {que} tiene que ser un número, pero es {LuaUtil.TipoAmigable(v)}.", linea);

    private Senal EjecutarParaEn(InsParaEn pe, Ambito ambito)
    {
        var valores = EvaluarLista(pe.Valores, ambito);
        object? f = valores.Length > 0 ? valores[0] : null;
        object? estado = valores.Length > 1 ? valores[1] : null;
        object? control = valores.Length > 2 ? valores[2] : null;

        if (f is LuaTabla tablaDirecta)
        {
            // Luau permite "for clave, valor in tabla do"
            f = IteradorPares(tablaDirecta);
            estado = tablaDirecta;
            control = null;
        }
        if (f is not IFuncionLua)
            throw new ErrorLua("Después de 'in' hace falta algo que se pueda recorrer, como ipairs(lista) o pairs(tabla).", pe.Linea);

        bool primera = true;
        while (true)
        {
            if (!primera) Paso(pe.Linea);
            primera = false;
            var r = Llamar(f, new[] { estado, control }, pe.Linea, "iterador");
            object? primero = r.Length > 0 ? r[0] : null;
            if (primero is null) break;
            control = primero;
            var interior = new Ambito(ambito);
            for (int i = 0; i < pe.Variables.Count; i++)
                interior.Declarar(pe.Variables[i], i < r.Length ? r[i] : null);
            var s = EjecutarBloque(pe.Cuerpo, interior);
            if (s == Senal.Romper) break;
            if (s == Senal.Retornar) return s;
        }
        return Senal.Nada;
    }

    public static FuncionNativa IteradorPares(LuaTabla t)
    {
        var pares = t.Pares();
        int i = 0;
        return new FuncionNativa("pairs", _ =>
        {
            if (i >= pares.Count) return new object?[] { null };
            var kv = pares[i++];
            return new[] { kv.Key, kv.Value };
        });
    }

    private void Asignar(Exp destino, object? valor, Ambito ambito, int linea)
    {
        switch (destino)
        {
            case ExpVariable v:
            {
                var celda = ambito.Buscar(v.Nombre);
                if (celda is null)
                {
                    Global.Declarar(v.Nombre, valor);
                    return;
                }
                if (EsNombreConstante(v.Nombre) && !Avisos.Any(a => a.Mensaje.Contains($"'{v.Nombre}'")))
                    Avisos.Add((linea, $"Línea {linea}: has cambiado el valor de la constante '{v.Nombre}'. Lo que se escribe en MAYÚSCULAS es una constante y no debería cambiar."));
                celda.Valor = valor;
                return;
            }
            case ExpIndice i:
            {
                var obj = Evaluar(i.Objeto, ambito);
                var clave = Evaluar(i.Clave, ambito);
                if (obj is not LuaTabla t)
                    throw new ErrorLua(obj is null
                        ? $"{NombreDe(i.Objeto)} vale nil, así que no se le puede poner {DescribirClave(i)}. ¿Has creado la tabla antes?"
                        : $"Solo las tablas tienen campos, y {NombreDe(i.Objeto)} es {LuaUtil.TipoAmigable(obj)}.", linea);
                if (clave is null) throw new ErrorLua("La clave de una tabla no puede ser nil.", linea);
                t.Asignar(clave, valor);
                return;
            }
            default:
                throw new ErrorLua("No se puede asignar un valor ahí.", linea);
        }
    }

    public static bool EsNombreConstante(string nombre) =>
        nombre.Length > 1 && nombre.Any(char.IsLetter) && nombre.All(c => char.IsUpper(c) || char.IsDigit(c) || c == '_');

    private static string NombreDe(Exp e) => e switch
    {
        ExpVariable v => $"'{v.Nombre}'",
        ExpIndice { NombreCampo: not null } i => $"'{TextoExp(i)}'",
        _ => "ese valor",
    };

    private static string TextoExp(Exp e) => e switch
    {
        ExpVariable v => v.Nombre,
        ExpIndice { NombreCampo: not null } i => TextoExp(i.Objeto) + "." + i.NombreCampo,
        ExpIndice i => TextoExp(i.Objeto) + "[...]",
        ExpLlamada l => TextoExp(l.Funcion) + "()",
        _ => "...",
    };

    private static string DescribirClave(ExpIndice i) => i.NombreCampo is { } n ? $"'.{n}'" : "un elemento";

    // ---------------- Expresiones ----------------
    private object?[] EvaluarLista(IReadOnlyList<Exp> lista, Ambito ambito)
    {
        if (lista.Count == 0) return LuaUtil.Nada;
        var resultado = new List<object?>();
        for (int i = 0; i < lista.Count; i++)
        {
            if (i == lista.Count - 1) resultado.AddRange(EvaluarMultiple(lista[i], ambito));
            else resultado.Add(Evaluar(lista[i], ambito));
        }
        return resultado.ToArray();
    }

    private object?[] EvaluarMultiple(Exp e, Ambito ambito)
    {
        switch (e)
        {
            case ExpLlamada ll:
            {
                object? f;
                string nombre;
                if (ll.Funcion is ExpVariable v)
                {
                    nombre = v.Nombre;
                    f = LeerVariable(v.Nombre, ambito, v.Linea, esLlamada: true);
                }
                else
                {
                    nombre = TextoExp(ll.Funcion);
                    f = Evaluar(ll.Funcion, ambito);
                }
                var args = EvaluarLista(ll.Argumentos, ambito);
                return Llamar(f, args, ll.Linea, nombre);
            }
            case ExpMetodo m:
            {
                var obj = Evaluar(m.Objeto, ambito);
                object? f = obj switch
                {
                    string => Biblioteca.MetodoTexto(this, m.Metodo),
                    LuaTabla t => t.Obtener(m.Metodo),
                    null => throw new ErrorLua($"{NombreDe(m.Objeto)} vale nil, así que no tiene el método ':{m.Metodo}()'.", m.Linea),
                    _ => throw new ErrorLua($"{NombreDe(m.Objeto)} es {LuaUtil.TipoAmigable(obj)} y no tiene el método ':{m.Metodo}()'.", m.Linea),
                };
                if (f is null)
                    throw new ErrorLua($"{NombreDe(m.Objeto)} no tiene ningún método llamado '{m.Metodo}'.", m.Linea);
                var args = EvaluarLista(m.Argumentos, ambito);
                return Llamar(f, args.Prepend(obj).ToArray(), m.Linea, m.Metodo);
            }
            default:
                return new[] { Evaluar(e, ambito) };
        }
    }

    private object?[] Llamar(object? f, object?[] args, int linea, string nombre)
    {
        switch (f)
        {
            case FuncionNativa n:
                try
                {
                    return n.Cuerpo(args);
                }
                catch (ErrorLua e) when (e.Linea == 0)
                {
                    e.Linea = linea;
                    throw;
                }
            case FuncionLua lf:
            {
                if (++_profundidad > LimiteProfundidad)
                    throw new ErrorLua($"Demasiadas llamadas a funciones una dentro de otra. ¿La función '{lf.Nombre}' se llama a sí misma sin parar?", linea);
                try
                {
                    var ambito = new Ambito(lf.Cierre);
                    var pars = lf.Definicion.Parametros;
                    for (int i = 0; i < pars.Count; i++)
                        ambito.Declarar(pars[i], i < args.Length ? args[i] : null);
                    var s = EjecutarBloque(lf.Definicion.Cuerpo, ambito);
                    if (s == Senal.Retornar)
                    {
                        var r = _retorno;
                        _retorno = LuaUtil.Nada;
                        return r;
                    }
                    return LuaUtil.Nada;
                }
                finally
                {
                    _profundidad--;
                }
            }
            case null:
                throw new ErrorLua($"'{nombre}' no es una función: vale nil.", linea);
            default:
                throw new ErrorLua($"'{nombre}' no es una función, es {LuaUtil.TipoAmigable(f)}. Solo se pueden llamar funciones con ().", linea);
        }
    }

    private object? LeerVariable(string nombre, Ambito ambito, int linea, bool esLlamada = false)
    {
        var c = ambito.Buscar(nombre);
        if (c is not null) return c.Valor;

        var conocidos = new HashSet<string>();
        for (var a = ambito; a is not null; a = a.Padre)
            foreach (var (n, _) in a.Variables) conocidos.Add(n);
        string? sugerencia = Sugerencias.Parecido(nombre, conocidos);
        string extra = sugerencia is null ? "" : $" ¿Querías decir '{sugerencia}'? Recuerda que las mayúsculas y minúsculas cuentan.";
        if (esLlamada)
            throw new ErrorLua($"No conozco ninguna función llamada '{nombre}'.{extra}", linea);
        throw new ErrorLua(sugerencia is not null
            ? $"No existe la variable '{nombre}'.{extra}"
            : $"No existe la variable '{nombre}'. ¿Te has olvidado de crearla con: local {nombre} = ...?", linea);
    }

    private object? Evaluar(Exp e, Ambito ambito)
    {
        switch (e)
        {
            case ExpLiteral l: return l.Valor;
            case ExpVariable v: return LeerVariable(v.Nombre, ambito, v.Linea);
            case ExpParentesis p: return Evaluar(p.Interior, ambito);
            case ExpInterpolada ip:
                return string.Concat(ip.Partes.Select(x => LuaUtil.ATexto(Evaluar(x, ambito))));
            case ExpLlamada or ExpMetodo:
            {
                var r = EvaluarMultiple(e, ambito);
                return r.Length > 0 ? r[0] : null;
            }
            case ExpIndice i:
            {
                var obj = Evaluar(i.Objeto, ambito);
                var clave = Evaluar(i.Clave, ambito);
                return obj switch
                {
                    LuaTabla t => t.Obtener(clave),
                    string when clave is string metodo => Biblioteca.MetodoTexto(this, metodo),
                    null => throw new ErrorLua($"{NombreDe(i.Objeto)} vale nil, así que no puedo leer {DescribirClave(i)}. ¿Está bien escrito?", i.Linea),
                    _ => throw new ErrorLua($"Solo las tablas tienen campos, y {NombreDe(i.Objeto)} es {LuaUtil.TipoAmigable(obj)}.", i.Linea),
                };
            }
            case ExpFuncion f:
                return new FuncionLua(f.Nombre == "" ? "anónima" : f.Nombre, f, ambito);
            case ExpTabla t:
            {
                var tabla = new LuaTabla();
                int indice = 1;
                for (int k = 0; k < t.Campos.Count; k++)
                {
                    var campo = t.Campos[k];
                    if (campo.Clave is null)
                    {
                        if (k == t.Campos.Count - 1 && campo.Valor is ExpLlamada or ExpMetodo)
                            foreach (var v in EvaluarMultiple(campo.Valor, ambito)) tabla.Asignar((double)indice++, v);
                        else
                            tabla.Asignar((double)indice++, Evaluar(campo.Valor, ambito));
                    }
                    else
                    {
                        var clave = Evaluar(campo.Clave, ambito) ?? throw new ErrorLua("La clave de una tabla no puede ser nil.", t.Linea);
                        tabla.Asignar(clave, Evaluar(campo.Valor, ambito));
                    }
                }
                return tabla;
            }
            case ExpUnaria u:
            {
                var v = Evaluar(u.Operando, ambito);
                switch (u.Operador)
                {
                    case "not": return !LuaUtil.EsVerdadero(v);
                    case "-":
                        if (v is double d) return -d;
                        throw new ErrorLua($"Solo se puede poner '-' delante de un número, y {NombreDe(u.Operando)} es {LuaUtil.TipoAmigable(v)}.", u.Linea);
                    case "#":
                        return v switch
                        {
                            string s => (double)s.Length,
                            LuaTabla t => (double)t.Longitud,
                            _ => throw new ErrorLua($"'#' sirve para contar elementos de una tabla o letras de un texto, y {NombreDe(u.Operando)} es {LuaUtil.TipoAmigable(v)}.", u.Linea),
                        };
                }
                break;
            }
            case ExpBinaria b:
            {
                if (b.Operador == "and")
                {
                    var izq = Evaluar(b.Izquierda, ambito);
                    return LuaUtil.EsVerdadero(izq) ? Evaluar(b.Derecha, ambito) : izq;
                }
                if (b.Operador == "or")
                {
                    var izq = Evaluar(b.Izquierda, ambito);
                    return LuaUtil.EsVerdadero(izq) ? izq : Evaluar(b.Derecha, ambito);
                }
                var a = Evaluar(b.Izquierda, ambito);
                var c = Evaluar(b.Derecha, ambito);
                return Operar(b.Operador, a, c, b.Izquierda, b.Derecha, b.Linea);
            }
        }
        throw new ErrorLua("No sé calcular esta expresión.", e.Linea);
    }

    private object? Operar(string op, object? a, object? b, Exp ea, Exp eb, int linea)
    {
        switch (op)
        {
            case "==": return LuaUtil.SonIguales(a, b);
            case "~=": return !LuaUtil.SonIguales(a, b);
            case "..":
            {
                if (a is string or double && b is string or double)
                    return LuaUtil.ATexto(a) + LuaUtil.ATexto(b);
                var (malo, exp) = a is string or double ? (b, eb) : (a, ea);
                if (malo is null)
                    throw new ErrorLua($"No puedo unir con '..' porque {NombreDe(exp)} vale nil. ¿Tiene valor esa variable?", linea);
                if (malo is bool)
                    throw new ErrorLua($"Para unir un booleano (true/false) a un texto conviértelo antes con tostring({TextoExp(exp)}).", linea);
                throw new ErrorLua($"Con '..' solo se pueden unir textos y números, y {NombreDe(exp)} es {LuaUtil.TipoAmigable(malo)}.", linea);
            }
            case "<" or ">" or "<=" or ">=":
            {
                int cmp;
                if (a is double x && b is double y) cmp = x.CompareTo(y);
                else if (a is string s1 && b is string s2) cmp = string.CompareOrdinal(s1, s2);
                else
                {
                    if (a is null || b is null)
                        throw new ErrorLua($"No puedo comparar con '{op}' porque {NombreDe(a is null ? ea : eb)} vale nil.", linea);
                    throw new ErrorLua($"No se puede comparar {LuaUtil.TipoAmigable(a)} con {LuaUtil.TipoAmigable(b)} usando '{op}'. Los dos lados tienen que ser números (o los dos textos).", linea);
                }
                return op switch { "<" => cmp < 0, ">" => cmp > 0, "<=" => cmp <= 0, _ => cmp >= 0 };
            }
        }

        if (a is double n1 && b is double n2)
        {
            return op switch
            {
                "+" => n1 + n2,
                "-" => n1 - n2,
                "*" => n1 * n2,
                "/" => n1 / n2,
                "//" => Math.Floor(n1 / n2),
                "%" => n1 - Math.Floor(n1 / n2) * n2,
                "^" => Math.Pow(n1, n2),
                _ => throw new ErrorLua($"Operador desconocido '{op}'.", linea),
            };
        }

        string nombreOp = op switch
        {
            "+" => "sumar", "-" => "restar", "*" => "multiplicar", "/" or "//" => "dividir", "%" => "calcular el resto", _ => "operar",
        };
        if (a is null || b is null)
            throw new ErrorLua($"No puedo {nombreOp} porque {NombreDe(a is null ? ea : eb)} vale nil. ¿Le has dado un valor antes?", linea);
        if (op == "+" && (a is string || b is string))
            throw new ErrorLua("Con '+' solo se suman números. Para unir textos usa '..', por ejemplo: \"Hola \" .. nombre", linea);
        var (raro, expRara) = a is double ? (b, eb) : (a, ea);
        if (raro is string)
            throw new ErrorLua($"No puedo {nombreOp} un texto ({NombreDe(expRara)}). Si es un número escrito como texto, conviértelo con tonumber(...).", linea);
        throw new ErrorLua($"Solo se puede {nombreOp} con números, y {NombreDe(expRara)} es {LuaUtil.TipoAmigable(raro)}.", linea);
    }
}

public static class Sugerencias
{
    /// <summary>Busca un nombre conocido que se parezca al que se ha escrito mal.</summary>
    public static string? Parecido(string nombre, IEnumerable<string> candidatos)
    {
        var lista = candidatos.ToList();
        var igualSinMayusculas = lista.FirstOrDefault(c => string.Equals(c, nombre, StringComparison.OrdinalIgnoreCase));
        if (igualSinMayusculas is not null) return igualSinMayusculas;
        if (nombre.Length < 3) return null;
        int maximo = nombre.Length <= 5 ? 1 : 2;
        return lista
            .Select(c => (c, d: Distancia(c.ToLowerInvariant(), nombre.ToLowerInvariant())))
            .Where(x => x.d <= maximo)
            .OrderBy(x => x.d)
            .Select(x => x.c)
            .FirstOrDefault();
    }

    public static int Distancia(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return d[a.Length, b.Length];
    }
}
