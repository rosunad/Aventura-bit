using System.Globalization;
using System.Text;

namespace AventuraBit.Motor.Lua;

/// <summary>Funciones estándar de Luau que conoce el juego (print, math, string, table...).</summary>
public static class Biblioteca
{
    public static void Registrar(Interprete it)
    {
        it.RegistrarNativa("print", a =>
        {
            it.AlImprimir?.Invoke(string.Join(" ", a.Select(LuaUtil.ATexto)));
            return LuaUtil.Nada;
        });
        Func<object?[], object?[]> esperar = a =>
        {
            double s = a.Length > 0 && a[0] is double d ? d : 0.03;
            it.AlEsperar?.Invoke(Math.Clamp(s, 0, 5));
            return new object?[] { s };
        };
        it.RegistrarNativa("wait", esperar);
        it.RegistrarNativa("esperar", esperar);
        var task = new LuaTabla();
        task.Asignar("wait", new FuncionNativa("task.wait", esperar));
        it.RegistrarValor("task", task);

        it.RegistrarNativa("tostring", a => new object?[] { LuaUtil.ATexto(Arg(a, 0)) });
        it.RegistrarNativa("tonumber", a =>
        {
            var v = Arg(a, 0);
            if (v is double) return new[] { v };
            if (v is string s && double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return new object?[] { d };
            return new object?[] { null };
        });
        it.RegistrarNativa("type", a => new object?[] { LuaUtil.TipoDe(Arg(a, 0)) });
        it.RegistrarNativa("typeof", a => new object?[] { LuaUtil.TipoDe(Arg(a, 0)) });
        it.RegistrarNativa("error", a => throw new ErrorLua(LuaUtil.ATexto(Arg(a, 0)), 0));
        it.RegistrarNativa("assert", a =>
        {
            if (!LuaUtil.EsVerdadero(Arg(a, 0)))
                throw new ErrorLua(a.Length > 1 ? LuaUtil.ATexto(a[1]) : "assert: la comprobación ha fallado.", 0);
            return a;
        });
        it.RegistrarNativa("ipairs", a =>
        {
            var t = Arg(a, 0) as LuaTabla ?? throw new ErrorLua("ipairs(...) necesita una tabla (una lista).", 0);
            var iter = new FuncionNativa("ipairs", args =>
            {
                double i = (args.Length > 1 && args[1] is double d ? d : 0) + 1;
                var v = t.Obtener(i);
                return v is null ? new object?[] { null } : new[] { (object?)i, v };
            });
            return new object?[] { iter, t, 0.0 };
        });
        it.RegistrarNativa("pairs", a =>
        {
            var t = Arg(a, 0) as LuaTabla ?? throw new ErrorLua("pairs(...) necesita una tabla.", 0);
            return new object?[] { Interprete.IteradorPares(t), t, null };
        });

        it.RegistrarValor("math", Matematicas(it));
        it.RegistrarValor("string", Textos());
        it.RegistrarValor("table", Tablas(it));
    }

    private static object? Arg(object?[] a, int i) => i < a.Length ? a[i] : null;

    private static double Num(object?[] a, int i, string funcion)
    {
        var v = Arg(a, i);
        if (v is double d) return d;
        throw new ErrorLua($"{funcion} necesita un número{(a.Length > 1 ? $" en la posición {i + 1}" : "")}, pero recibió {LuaUtil.TipoAmigable(v)}.", 0);
    }

    private static string Txt(object?[] a, int i, string funcion)
    {
        var v = Arg(a, i);
        if (v is string s) return s;
        if (v is double d) return LuaUtil.NumeroATexto(d);
        throw new ErrorLua($"{funcion} necesita un texto, pero recibió {LuaUtil.TipoAmigable(v)}.", 0);
    }

    private static void Nativa(LuaTabla t, string prefijo, string nombre, Func<object?[], object?[]> f) =>
        t.Asignar(nombre, new FuncionNativa(prefijo + "." + nombre, f));

    private static object?[] Uno(object? v) => new[] { v };

    private static LuaTabla Matematicas(Interprete it)
    {
        var m = new LuaTabla();
        Nativa(m, "math", "floor", a => Uno(Math.Floor(Num(a, 0, "math.floor"))));
        Nativa(m, "math", "ceil", a => Uno(Math.Ceiling(Num(a, 0, "math.ceil"))));
        Nativa(m, "math", "abs", a => Uno(Math.Abs(Num(a, 0, "math.abs"))));
        Nativa(m, "math", "sqrt", a => Uno(Math.Sqrt(Num(a, 0, "math.sqrt"))));
        Nativa(m, "math", "round", a => Uno(Math.Round(Num(a, 0, "math.round"), MidpointRounding.AwayFromZero)));
        Nativa(m, "math", "sign", a => Uno((double)Math.Sign(Num(a, 0, "math.sign"))));
        Nativa(m, "math", "max", a => Uno(a.Select((_, i) => Num(a, i, "math.max")).Max()));
        Nativa(m, "math", "min", a => Uno(a.Select((_, i) => Num(a, i, "math.min")).Min()));
        Nativa(m, "math", "clamp", a => Uno(Math.Clamp(Num(a, 0, "math.clamp"), Num(a, 1, "math.clamp"), Num(a, 2, "math.clamp"))));
        Nativa(m, "math", "random", a =>
        {
            if (a.Length == 0) return Uno(it.Azar.NextDouble());
            int min = 1, max;
            if (a.Length == 1) max = (int)Num(a, 0, "math.random");
            else { min = (int)Num(a, 0, "math.random"); max = (int)Num(a, 1, "math.random"); }
            if (max < min) throw new ErrorLua("En math.random(a, b), 'a' no puede ser mayor que 'b'.", 0);
            return Uno((double)it.Azar.Next(min, max + 1));
        });
        m.Asignar("pi", Math.PI);
        m.Asignar("huge", double.PositiveInfinity);
        return m;
    }

    private static LuaTabla? _textos;

    private static LuaTabla Textos()
    {
        var s = new LuaTabla();
        Nativa(s, "string", "len", a => Uno((double)Txt(a, 0, "string.len").Length));
        Nativa(s, "string", "upper", a => Uno(Txt(a, 0, "string.upper").ToUpperInvariant()));
        Nativa(s, "string", "lower", a => Uno(Txt(a, 0, "string.lower").ToLowerInvariant()));
        Nativa(s, "string", "reverse", a => Uno(new string(Txt(a, 0, "string.reverse").Reverse().ToArray())));
        Nativa(s, "string", "rep", a =>
        {
            var t = Txt(a, 0, "string.rep");
            int n = (int)Num(a, 1, "string.rep");
            if (n * (long)t.Length > 10000) throw new ErrorLua("string.rep: ¡ese texto sería demasiado largo!", 0);
            return Uno(string.Concat(Enumerable.Repeat(t, Math.Max(0, n))));
        });
        Nativa(s, "string", "sub", a =>
        {
            var t = Txt(a, 0, "string.sub");
            int len = t.Length;
            int i = (int)(Arg(a, 1) is double d1 ? d1 : 1);
            int j = (int)(Arg(a, 2) is double d2 ? d2 : -1);
            if (i < 0) i = Math.Max(len + i + 1, 1); else if (i == 0) i = 1;
            if (j < 0) j = len + j + 1; else if (j > len) j = len;
            return Uno(i > j ? "" : t.Substring(i - 1, j - i + 1));
        });
        Nativa(s, "string", "split", a =>
        {
            var t = Txt(a, 0, "string.split");
            var sep = Arg(a, 1) is string sp ? sp : ",";
            var tabla = new LuaTabla();
            int k = 1;
            foreach (var parte in sep.Length == 0 ? t.Select(c => c.ToString()) : t.Split(sep))
                tabla.Asignar((double)k++, parte);
            return Uno(tabla);
        });
        Nativa(s, "string", "find", a =>
        {
            var t = Txt(a, 0, "string.find");
            var buscado = Txt(a, 1, "string.find");
            int pos = t.IndexOf(buscado, StringComparison.Ordinal);
            return pos < 0 ? Uno(null) : new object?[] { (double)(pos + 1), (double)(pos + buscado.Length) };
        });
        Nativa(s, "string", "format", a => Uno(Formatear(Txt(a, 0, "string.format"), a.Skip(1).ToArray())));
        _textos = s;
        return s;
    }

    public static object? MetodoTexto(Interprete it, string metodo) => (_textos ?? Textos()).Obtener(metodo);

    private static string Formatear(string formato, object?[] args)
    {
        var sb = new StringBuilder();
        int k = 0;
        for (int i = 0; i < formato.Length; i++)
        {
            char c = formato[i];
            if (c != '%' || i + 1 >= formato.Length) { sb.Append(c); continue; }
            int ini = ++i;
            while (i < formato.Length && (char.IsDigit(formato[i]) || formato[i] == '.' || formato[i] == '-')) i++;
            if (i >= formato.Length) break;
            string mods = formato[ini..i];
            char tipo = formato[i];
            if (tipo == '%') { sb.Append('%'); continue; }
            var v = k < args.Length ? args[k++] : null;
            switch (tipo)
            {
                case 'd' or 'i':
                    sb.Append(v is double d ? ((long)d).ToString(CultureInfo.InvariantCulture) : LuaUtil.ATexto(v));
                    break;
                case 'f':
                    int dec = mods.Contains('.') && int.TryParse(mods[(mods.IndexOf('.') + 1)..], out var n) ? n : 6;
                    sb.Append(v is double f ? f.ToString("F" + dec, CultureInfo.InvariantCulture) : LuaUtil.ATexto(v));
                    break;
                default:
                    sb.Append(LuaUtil.ATexto(v));
                    break;
            }
        }
        return sb.ToString();
    }

    private static LuaTabla Tablas(Interprete it)
    {
        var t = new LuaTabla();
        Nativa(t, "table", "insert", a =>
        {
            var tabla = Arg(a, 0) as LuaTabla ?? throw new ErrorLua("table.insert necesita una tabla como primer valor: table.insert(lista, valor)", 0);
            if (a.Length >= 3) tabla.Insertar((int)Num(a, 1, "table.insert"), a[2]);
            else tabla.Insertar(tabla.Longitud + 1, Arg(a, 1));
            return LuaUtil.Nada;
        });
        Nativa(t, "table", "remove", a =>
        {
            var tabla = Arg(a, 0) as LuaTabla ?? throw new ErrorLua("table.remove necesita una tabla.", 0);
            int pos = a.Length >= 2 ? (int)Num(a, 1, "table.remove") : tabla.Longitud;
            return Uno(tabla.Quitar(pos));
        });
        Nativa(t, "table", "concat", a =>
        {
            var tabla = Arg(a, 0) as LuaTabla ?? throw new ErrorLua("table.concat necesita una tabla.", 0);
            string sep = Arg(a, 1) is string s ? s : "";
            var partes = new List<string>();
            for (int i = 1; i <= tabla.Longitud; i++) partes.Add(LuaUtil.ATexto(tabla.Obtener((double)i)));
            return Uno(string.Join(sep, partes));
        });
        Nativa(t, "table", "find", a =>
        {
            var tabla = Arg(a, 0) as LuaTabla ?? throw new ErrorLua("table.find necesita una tabla.", 0);
            var buscado = Arg(a, 1);
            for (int i = 1; i <= tabla.Longitud; i++)
                if (LuaUtil.SonIguales(tabla.Obtener((double)i), buscado)) return Uno((double)i);
            return Uno(null);
        });
        Nativa(t, "table", "sort", a =>
        {
            var tabla = Arg(a, 0) as LuaTabla ?? throw new ErrorLua("table.sort necesita una tabla.", 0);
            var comparador = Arg(a, 1);
            var lista = new List<object?>();
            for (int i = 1; i <= tabla.Longitud; i++) lista.Add(tabla.Obtener((double)i));
            Comparison<object?> cmp = comparador is IFuncionLua f
                ? (x, y) => LuaUtil.EsVerdadero(it.LlamarFuncion(f, new[] { x, y }).FirstOrDefault()) ? -1
                    : LuaUtil.EsVerdadero(it.LlamarFuncion(f, new[] { y, x }).FirstOrDefault()) ? 1 : 0
                : (x, y) => (x, y) switch
                {
                    (double p, double q) => p.CompareTo(q),
                    (string p, string q) => string.CompareOrdinal(p, q),
                    _ => throw new ErrorLua("table.sort solo sabe ordenar listas de números o de textos.", 0),
                };
            lista.Sort(cmp);
            for (int i = 0; i < lista.Count; i++) tabla.Asignar((double)(i + 1), lista[i]);
            return LuaUtil.Nada;
        });
        return t;
    }
}
