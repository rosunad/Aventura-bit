namespace AventuraBit.Motor.Lua;

/// <summary>Convierte los tokens en un árbol de instrucciones, con errores explicados en español.</summary>
public sealed class Parser
{
    private static readonly Dictionary<string, (int Izq, int Der)> Prioridad = new()
    {
        ["or"] = (1, 1), ["and"] = (2, 2),
        ["<"] = (3, 3), [">"] = (3, 3), ["<="] = (3, 3), [">="] = (3, 3), ["~="] = (3, 3), ["=="] = (3, 3),
        [".."] = (9, 8),
        ["+"] = (10, 10), ["-"] = (10, 10),
        ["*"] = (11, 11), ["/"] = (11, 11), ["//"] = (11, 11), ["%"] = (11, 11),
        ["^"] = (14, 13),
    };

    private const int PrioridadUnaria = 12;

    private static readonly HashSet<string> OperadoresCompuestos = new() { "+=", "-=", "*=", "/=", "%=", "^=", "..=", "//=" };

    private readonly List<Token> _t;
    private int _p;

    private Parser(List<Token> tokens) => _t = tokens;

    public static Bloque Analizar(string codigo) => new Parser(Lexer.Analizar(codigo)).Programa();

    /// <summary>Analiza una sola expresión (para los textos con `{...}`).</summary>
    public static Exp AnalizarExpresion(string codigo, int linea)
    {
        var p = new Parser(Lexer.Analizar(codigo, linea));
        var e = p.Expresion();
        if (p.Actual.Tipo != TipoToken.FinArchivo)
            throw new ErrorLua($"Dentro de las llaves {{ }} sobra '{p.Actual.Texto}'.", linea);
        return e;
    }

    private Token Actual => _t[_p];
    private Token Anterior => _t[Math.Max(0, _p - 1)];
    private Token Avanzar() => _t[_p++];

    private bool Acepta(string texto)
    {
        if (!Actual.Es(texto)) return false;
        _p++;
        return true;
    }

    private string Describir(Token t) => t.Tipo switch
    {
        TipoToken.FinArchivo => "el final del código",
        TipoToken.Cadena or TipoToken.Interpolada => "el texto " + t.Texto,
        _ => "'" + t.Texto + "'",
    };

    private void Esperar(string texto, string mensaje)
    {
        if (Acepta(texto)) return;
        int linea = Actual.Tipo == TipoToken.FinArchivo ? Anterior.Linea : Actual.Linea;
        throw new ErrorLua(mensaje, linea);
    }

    private string EsperarNombre(string mensaje)
    {
        if (Actual.Tipo == TipoToken.Nombre) return Avanzar().Texto;
        if (Actual.Tipo == TipoToken.PalabraClave)
            throw new ErrorLua($"'{Actual.Texto}' es una palabra reservada de Luau y no se puede usar como nombre.", Actual.Linea);
        throw new ErrorLua(mensaje, Actual.Linea);
    }

    private Bloque Programa()
    {
        var b = LeerBloque();
        if (Actual.Tipo != TipoToken.FinArchivo)
        {
            string que = Actual.Texto;
            throw new ErrorLua(que switch
            {
                "end" => "Sobra este 'end': no hay ningún bloque abierto que cerrar.",
                "until" => "Este 'until' no tiene un 'repeat' antes.",
                "else" => "Este 'else' no tiene un 'if' antes.",
                "elseif" => "Este 'elseif' no tiene un 'if' antes.",
                _ => $"No esperaba {Describir(Actual)} aquí.",
            }, Actual.Linea);
        }
        return b;
    }

    private bool FinDeBloque() =>
        Actual.Tipo == TipoToken.FinArchivo || Actual.Es("end") || Actual.Es("else") || Actual.Es("elseif") || Actual.Es("until");

    private Bloque LeerBloque()
    {
        var lista = new List<Instruccion>();
        while (!FinDeBloque())
        {
            if (Acepta(";")) continue;
            var ins = LeerInstruccion();
            lista.Add(ins);
            if (ins is InsRetorno)
            {
                Acepta(";");
                if (!FinDeBloque())
                    throw new ErrorLua("Después de 'return' no puede haber más instrucciones en el mismo bloque.", Actual.Linea);
                break;
            }
        }
        return new Bloque(lista);
    }

    private void Cerrar(string palabra, string construccion, int lineaApertura, string pista = "")
    {
        if (Acepta(palabra)) return;
        if (Actual.Tipo == TipoToken.FinArchivo)
            throw new ErrorLua($"Falta '{palabra}' para cerrar {construccion} que empieza en la línea {lineaApertura}.{pista}", lineaApertura);
        throw new ErrorLua($"Esperaba '{palabra}' para cerrar {construccion} de la línea {lineaApertura}, pero encontré {Describir(Actual)}.{pista}", Actual.Linea);
    }

    private Instruccion LeerInstruccion()
    {
        var t = Actual;
        if (t.Tipo == TipoToken.PalabraClave)
        {
            switch (t.Texto)
            {
                case "local": return LeerLocal();
                case "function": return LeerFuncionGlobal();
                case "if": return LeerSi();
                case "while": return LeerMientras();
                case "for": return LeerPara();
                case "repeat": return LeerRepetir();
                case "do":
                {
                    _p++;
                    var cuerpo = LeerBloque();
                    Cerrar("end", "el bloque 'do'", t.Linea);
                    return new InsHacer(cuerpo, t.Linea);
                }
                case "return":
                {
                    _p++;
                    var valores = FinDeBloque() || Actual.Es(";") ? new List<Exp>() : ListaExp();
                    return new InsRetorno(valores, t.Linea);
                }
                case "break": _p++; return new InsRomper(t.Linea);
                case "continue": _p++; return new InsContinuar(t.Linea);
                case "then":
                    throw new ErrorLua("Este 'then' está suelto. 'then' va después de la condición de un 'if'.", t.Linea);
                case "do ":
                    break;
            }
        }
        if (t.Tipo == TipoToken.Nombre && t.Texto == "type" && _t[_p + 1].Tipo == TipoToken.Nombre)
            throw new ErrorLua("Los tipos con 'type' son avanzados y este juego no los usa.", t.Linea);
        return LeerInstruccionExpresion();
    }

    private Instruccion LeerInstruccionExpresion()
    {
        int linea = Actual.Linea;
        var inicio = Actual;
        Exp e;
        try
        {
            e = ExpSufijo();
        }
        catch (ErrorLua) when (inicio.Tipo is TipoToken.Numero or TipoToken.Cadena)
        {
            throw new ErrorLua($"Una línea no puede empezar con {Describir(inicio)}. ¿Querías ponerlo dentro de print(...)?", linea);
        }

        if (Actual.Es("=") || Actual.Es(","))
        {
            var destinos = new List<Exp> { e };
            while (Acepta(",")) destinos.Add(ExpSufijo());
            Esperar("=", "Falta el '=' para dar valor a las variables.");
            foreach (var d in destinos) ComprobarDestino(d);
            var valores = ListaExp();
            return new InsAsignacion(destinos, valores, linea);
        }

        if (Actual.Tipo == TipoToken.Simbolo && OperadoresCompuestos.Contains(Actual.Texto))
        {
            string op = Avanzar().Texto[..^1];
            ComprobarDestino(e);
            var valor = Expresion();
            return new InsAsignacionCompuesta(e, op, valor, linea);
        }

        if (e is ExpLlamada or ExpMetodo) return new InsLlamada(e, linea);

        if (Actual.Es("=="))
            throw new ErrorLua("Para guardar un valor en una variable se usa un solo '='. Los dos '==' sirven para comparar dentro de un 'if'.", linea);
        if (Actual.Es("++") || (Actual.Es("+") && _t[_p + 1].Es("+")))
            throw new ErrorLua("En Luau no existe '++'. Para sumar uno escribe: variable += 1", linea);
        if (e is ExpVariable v)
        {
            if (Actual.Tipo == TipoToken.Nombre && Actual.Linea == linea)
                throw new ErrorLua($"No entiendo '{v.Nombre} {Actual.Texto}'. Los nombres no pueden tener espacios: prueba con camelCase, por ejemplo '{v.Nombre}{Mayuscula(Actual.Texto)}'.", linea);
            throw new ErrorLua($"'{v.Nombre}' está solo y no hace nada. Si es una función, llámala con paréntesis: {v.Nombre}()", linea);
        }
        throw new ErrorLua("Esta línea no hace nada. Una instrucción tiene que llamar a una función o guardar un valor.", linea);
    }

    private static string Mayuscula(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static void ComprobarDestino(Exp e)
    {
        if (e is ExpVariable or ExpIndice) return;
        if (e is ExpLlamada or ExpMetodo)
            throw new ErrorLua("No se puede dar valor a una llamada a función. A la izquierda del '=' va el nombre de una variable.", e.Linea);
        throw new ErrorLua("A la izquierda del '=' tiene que ir el nombre de una variable.", e.Linea);
    }

    private Instruccion LeerLocal()
    {
        int linea = Avanzar().Linea; // local
        if (Actual.Es("function"))
        {
            _p++;
            string nombre = EsperarNombre("Después de 'local function' va el nombre de la función, por ejemplo: local function saltar()");
            var f = CuerpoFuncion(nombre, linea, false);
            return new InsLocalFuncion(nombre, f, linea);
        }
        var nombres = new List<string>();
        do
        {
            nombres.Add(EsperarNombre("Después de 'local' va el nombre de la variable, por ejemplo: local puntos = 0"));
            SaltarAnotacionTipo();
        } while (Acepta(","));

        var valores = new List<Exp>();
        if (Acepta("="))
            valores = ListaExp();
        else if (Actual.Es("=="))
            throw new ErrorLua("Para dar valor a una variable se usa un solo '='. Ejemplo: local puntos = 0", linea);
        return new InsLocal(nombres, valores, linea);
    }

    private Instruccion LeerFuncionGlobal()
    {
        int linea = Avanzar().Linea; // function
        string nombre = EsperarNombre("Después de 'function' va el nombre de la función, por ejemplo: function saltar()");
        Exp destino = new ExpVariable(nombre, linea);
        string nombreCompleto = nombre;
        bool metodo = false;
        while (Actual.Es(".") || Actual.Es(":"))
        {
            bool dosPuntos = Avanzar().Texto == ":";
            string campo = EsperarNombre("Falta el nombre después del punto.");
            destino = new ExpIndice(destino, new ExpLiteral(campo, linea), linea);
            nombreCompleto += (dosPuntos ? ":" : ".") + campo;
            if (dosPuntos) { metodo = true; break; }
        }
        var f = CuerpoFuncion(nombreCompleto, linea, metodo);
        return new InsFuncion(destino, f, linea);
    }

    private ExpFuncion CuerpoFuncion(string nombre, int linea, bool metodo)
    {
        var parametros = new List<string>();
        if (metodo) parametros.Add("self");
        Esperar("(", $"Falta '(' después del nombre de la función. Ejemplo: function {(nombre == "" ? "saltar" : nombre)}()");
        if (!Actual.Es(")"))
        {
            do
            {
                if (Actual.Es("..."))
                    throw new ErrorLua("Los parámetros '...' son avanzados y este juego no los usa.", Actual.Linea);
                parametros.Add(EsperarNombre("Los parámetros de una función son nombres separados por comas: function sumar(a, b)"));
                SaltarAnotacionTipo();
            } while (Acepta(","));
        }
        Esperar(")", "Falta cerrar el paréntesis ')' de los parámetros de la función.");
        SaltarAnotacionTipo();
        var cuerpo = LeerBloque();
        string que = nombre == "" ? "la función" : $"la función '{nombre}'";
        Cerrar("end", que, linea);
        return new ExpFuncion(nombre, parametros, cuerpo, linea);
    }

    /// <summary>Luau permite anotar tipos (local x: number = 1). Los aceptamos y los ignoramos.</summary>
    private void SaltarAnotacionTipo()
    {
        if (!Acepta(":")) return;
        SaltarTipo();
    }

    private void SaltarTipo()
    {
        do
        {
            if (Actual.Es("(") || Actual.Es("{"))
            {
                SaltarEquilibrado();
                if (Acepta("->")) SaltarTipo();
            }
            else if (Actual.Tipo is TipoToken.Nombre || Actual.Es("nil"))
            {
                _p++;
                while (Acepta(".")) EsperarNombre("Tipo incompleto.");
                if (Actual.Es("<")) SaltarEquilibrado();
            }
            else if (Actual.Tipo == TipoToken.Cadena) _p++;
            else throw new ErrorLua("Después de ':' esperaba un tipo, como number o string.", Actual.Linea);
            while (Acepta("?")) { }
        } while (Acepta("|") || Acepta("&"));
    }

    private void SaltarEquilibrado()
    {
        int nivel = 0;
        do
        {
            var t = Avanzar();
            if (t.Es("(") || t.Es("{") || t.Es("<")) nivel++;
            else if (t.Es(")") || t.Es("}") || t.Es(">")) nivel--;
            else if (t.Tipo == TipoToken.FinArchivo) throw new ErrorLua("Tipo sin cerrar.", t.Linea);
        } while (nivel > 0);
    }

    private Exp Condicion(string donde)
    {
        if (Actual.Es("then") || Actual.Es("do"))
            throw new ErrorLua($"Falta la condición del '{donde}'.", Actual.Linea);
        var c = Expresion();
        if (Actual.Es("="))
            throw new ErrorLua("Para comparar se usan dos iguales '=='. Un solo '=' sirve para guardar valores en variables.", Actual.Linea);
        return c;
    }

    private Instruccion LeerSi()
    {
        bool trasElse = Anterior.Es("else") && Anterior.Linea == Actual.Linea;
        int linea = Avanzar().Linea; // if
        var condiciones = new List<Exp>();
        var bloques = new List<Bloque>();
        Bloque? siNo = null;

        condiciones.Add(Condicion("if"));
        EsperarThen("if");
        bloques.Add(LeerBloque());
        while (true)
        {
            if (Actual.Es("elseif"))
            {
                _p++;
                condiciones.Add(Condicion("elseif"));
                EsperarThen("elseif");
                bloques.Add(LeerBloque());
            }
            else if (Acepta("else"))
            {
                siNo = LeerBloque();
                break;
            }
            else break;
        }
        string pista = trasElse ? " ¿Querías escribir 'elseif' todo junto? 'else if' separado necesita su propio 'end'." : "";
        Cerrar("end", "el 'if'", linea, pista);
        return new InsSi(condiciones, bloques, siNo, linea);
    }

    private void EsperarThen(string donde)
    {
        if (Acepta("then")) return;
        if (Actual.Es("do"))
            throw new ErrorLua($"En un '{donde}' se escribe 'then', no 'do'.", Actual.Linea);
        if (Actual.Es("{") || Actual.Es(":"))
            throw new ErrorLua($"En Luau, después de la condición del '{donde}' se escribe 'then'.", Actual.Linea);
        throw new ErrorLua($"Falta 'then' después de la condición del '{donde}'. Ejemplo: if puntos > 10 then", Anterior.Linea);
    }

    private void EsperarDo(string donde)
    {
        if (Acepta("do")) return;
        if (Actual.Es("then"))
            throw new ErrorLua($"En un '{donde}' se escribe 'do', no 'then'.", Actual.Linea);
        throw new ErrorLua($"Falta 'do' al final de la línea del '{donde}'.", Anterior.Linea);
    }

    private Instruccion LeerMientras()
    {
        int linea = Avanzar().Linea;
        var c = Condicion("while");
        EsperarDo("while");
        var cuerpo = LeerBloque();
        Cerrar("end", "el bucle 'while'", linea);
        return new InsMientras(c, cuerpo, linea);
    }

    private Instruccion LeerRepetir()
    {
        int linea = Avanzar().Linea;
        var cuerpo = LeerBloque();
        Cerrar("until", "el bucle 'repeat'", linea);
        var c = Condicion("until");
        return new InsRepetir(cuerpo, c, linea);
    }

    private Instruccion LeerPara()
    {
        int linea = Avanzar().Linea;
        string primera = EsperarNombre("Después de 'for' va el nombre de una variable. Ejemplo: for i = 1, 10 do");
        SaltarAnotacionTipo();
        if (Acepta("="))
        {
            var inicio = Expresion();
            if (Actual.Tipo == TipoToken.Nombre && Actual.Texto is "to" or "hasta")
                throw new ErrorLua("En Luau los límites del 'for' se separan con una coma: for i = 1, 10 do", Actual.Linea);
            Esperar(",", "Falta la coma y el número final del 'for'. Ejemplo: for i = 1, 10 do");
            var fin = Expresion();
            Exp? paso = null;
            if (Acepta(",")) paso = Expresion();
            EsperarDo("for");
            var cuerpo = LeerBloque();
            Cerrar("end", "el bucle 'for'", linea);
            return new InsParaNumerico(primera, inicio, fin, paso, cuerpo, linea);
        }
        var nombres = new List<string> { primera };
        while (Acepta(","))
        {
            nombres.Add(EsperarNombre("Falta un nombre de variable después de la coma."));
            SaltarAnotacionTipo();
        }
        if (!Acepta("in"))
            throw new ErrorLua("Después de 'for i' va '=' (for i = 1, 10 do) o 'in' (for i, valor in ipairs(lista) do).", Actual.Linea);
        var valores = ListaExp();
        EsperarDo("for");
        var cuerpoEn = LeerBloque();
        Cerrar("end", "el bucle 'for'", linea);
        return new InsParaEn(nombres, valores, cuerpoEn, linea);
    }

    // ---------- Expresiones ----------
    private List<Exp> ListaExp()
    {
        var lista = new List<Exp> { Expresion() };
        while (Acepta(",")) lista.Add(Expresion());
        return lista;
    }

    private Exp Expresion() => SubExp(0);

    private Exp SubExp(int limite)
    {
        Exp izq;
        var t = Actual;
        if (t.Es("not") || t.Es("-") || t.Es("#"))
        {
            _p++;
            izq = new ExpUnaria(t.Texto, SubExp(PrioridadUnaria), t.Linea);
        }
        else if (t.Es("!"))
            throw new ErrorLua("En Luau la negación se escribe 'not', no '!'.", t.Linea);
        else
            izq = ExpSimple();

        while (true)
        {
            var op = Actual;
            if (op.Es("!=")) throw new ErrorLua("En Luau 'distinto de' se escribe '~=' (no '!=').", op.Linea);
            if (op.Es("&&") || op.Es("&")) throw new ErrorLua("En Luau 'y' se escribe 'and' (no '&&').", op.Linea);
            if (op.Es("||") || op.Es("|")) throw new ErrorLua("En Luau 'o' se escribe 'or' (no '||').", op.Linea);
            if (op.Tipo is not (TipoToken.Simbolo or TipoToken.PalabraClave) || !Prioridad.TryGetValue(op.Texto, out var pr) || pr.Izq <= limite)
                break;
            _p++;
            if (FinDeBloque() || Actual.Es(")"))
                throw new ErrorLua($"Después de '{op.Texto}' falta algo con lo que operar.", op.Linea);
            var der = SubExp(pr.Der);
            izq = new ExpBinaria(op.Texto, izq, der, op.Linea);
        }
        return izq;
    }

    private Exp ExpSimple()
    {
        var t = Actual;
        switch (t.Tipo)
        {
            case TipoToken.Numero:
            case TipoToken.Cadena:
                _p++;
                return new ExpLiteral(t.Valor, t.Linea);
            case TipoToken.Interpolada:
                _p++;
                var partes = new List<Exp>();
                foreach (var parte in (List<ParteInterpolada>)t.Valor!)
                    partes.Add(parte.Codigo is null
                        ? new ExpLiteral(parte.Texto, t.Linea)
                        : AnalizarExpresion(parte.Codigo, parte.Linea));
                return new ExpInterpolada(partes, t.Linea);
            case TipoToken.PalabraClave:
                switch (t.Texto)
                {
                    case "nil": _p++; return new ExpLiteral(null, t.Linea);
                    case "true": _p++; return new ExpLiteral(true, t.Linea);
                    case "false": _p++; return new ExpLiteral(false, t.Linea);
                    case "function":
                        _p++;
                        return CuerpoFuncion("", t.Linea, false);
                }
                break;
            case TipoToken.Simbolo:
                if (t.Es("{")) return Tabla();
                if (t.Es("...")) throw new ErrorLua("'...' es avanzado y este juego no lo usa.", t.Linea);
                break;
        }
        return ExpSufijo();
    }

    private Exp ExpPrimaria()
    {
        var t = Actual;
        if (t.Tipo == TipoToken.Nombre)
        {
            _p++;
            return new ExpVariable(t.Texto, t.Linea);
        }
        if (t.Es("("))
        {
            _p++;
            var e = Expresion();
            Esperar(")", $"Falta cerrar el paréntesis ')' que abriste en la línea {t.Linea}.");
            return new ExpParentesis(e, t.Linea);
        }
        if (t.Tipo == TipoToken.PalabraClave)
            throw new ErrorLua($"No esperaba la palabra '{t.Texto}' aquí.", t.Linea);
        if (t.Tipo == TipoToken.FinArchivo)
            throw new ErrorLua("El código se acaba antes de tiempo: falta algo al final.", Anterior.Linea);
        throw new ErrorLua($"No esperaba {Describir(t)} aquí. Esperaba un valor: un número, un texto entre comillas o una variable.", t.Linea);
    }

    private Exp ExpSufijo()
    {
        var e = ExpPrimaria();
        while (true)
        {
            var t = Actual;
            if (t.Es("."))
            {
                _p++;
                string campo = EsperarNombre("Después del punto va el nombre de un campo, por ejemplo jugador.vidas");
                e = new ExpIndice(e, new ExpLiteral(campo, t.Linea), t.Linea);
            }
            else if (t.Es("["))
            {
                _p++;
                var clave = Expresion();
                Esperar("]", "Falta cerrar el corchete ']'.");
                e = new ExpIndice(e, clave, t.Linea);
            }
            else if (t.Es(":"))
            {
                _p++;
                string metodo = EsperarNombre("Después de ':' va el nombre de un método, por ejemplo texto:upper()");
                if (!(Actual.Es("(") || Actual.Tipo == TipoToken.Cadena || Actual.Es("{")))
                    throw new ErrorLua($"Después de ':{metodo}' van paréntesis: :{metodo}()", Actual.Linea);
                e = new ExpMetodo(e, metodo, Argumentos(), t.Linea);
            }
            else if (t.Es("(") || t.Tipo is TipoToken.Cadena or TipoToken.Interpolada || t.Es("{"))
            {
                if (t.Es("(") && t.Linea != Anterior.Linea && e is not ExpParentesis)
                {
                    // Un paréntesis en la línea siguiente sigue siendo una llamada en Luau; lo aceptamos.
                }
                e = new ExpLlamada(e, Argumentos(), t.Linea);
            }
            else return e;
        }
    }

    private List<Exp> Argumentos()
    {
        var t = Actual;
        if (t.Tipo is TipoToken.Cadena or TipoToken.Interpolada) return new List<Exp> { ExpSimple() };
        if (t.Es("{")) return new List<Exp> { Tabla() };
        _p++; // (
        var args = new List<Exp>();
        if (!Actual.Es(")"))
        {
            args.Add(Expresion());
            while (true)
            {
                if (Acepta(",")) { args.Add(Expresion()); continue; }
                if (Actual.Tipo is TipoToken.Cadena or TipoToken.Numero or TipoToken.Nombre or TipoToken.Interpolada && Actual.Linea == t.Linea)
                    throw new ErrorLua("Falta una coma ',' entre los valores que pasas a la función.", Actual.Linea);
                break;
            }
        }
        if (!Acepta(")"))
            throw new ErrorLua($"Falta cerrar el paréntesis ')' de la línea {t.Linea}.", Actual.Tipo == TipoToken.FinArchivo || Actual.Linea != t.Linea ? t.Linea : Actual.Linea);
        return args;
    }

    private Exp Tabla()
    {
        var t = Avanzar(); // {
        var campos = new List<CampoTabla>();
        while (!Actual.Es("}"))
        {
            if (Actual.Es("["))
            {
                _p++;
                var clave = Expresion();
                Esperar("]", "Falta cerrar el corchete ']'.");
                Esperar("=", "Falta '=' después de la clave entre corchetes.");
                campos.Add(new CampoTabla(clave, Expresion()));
            }
            else if (Actual.Tipo == TipoToken.Nombre && _t[_p + 1].Es("="))
            {
                string nombre = Avanzar().Texto;
                _p++; // =
                campos.Add(new CampoTabla(new ExpLiteral(nombre, t.Linea), Expresion()));
            }
            else
            {
                if (Actual.Tipo == TipoToken.FinArchivo)
                    throw new ErrorLua($"Falta cerrar la llave '}}' de la tabla que empieza en la línea {t.Linea}.", t.Linea);
                campos.Add(new CampoTabla(null, Expresion()));
            }
            if (!Acepta(",") && !Acepta(";"))
            {
                if (Actual.Es("}")) break;
                if (Actual.Tipo == TipoToken.FinArchivo)
                    throw new ErrorLua($"Falta cerrar la llave '}}' de la tabla que empieza en la línea {t.Linea}.", t.Linea);
                throw new ErrorLua("Los elementos de una tabla se separan con comas: { \"rojo\", \"azul\" }", Actual.Linea);
            }
        }
        _p++; // }
        return new ExpTabla(campos, t.Linea);
    }
}
