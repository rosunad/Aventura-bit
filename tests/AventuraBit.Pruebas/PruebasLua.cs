using AventuraBit.Motor.Juego;
using AventuraBit.Motor.Lua;

namespace AventuraBit.Pruebas;

public class PruebasLua
{
    private static ResultadoEjecucion Ejecutar(string codigo) =>
        Ejecutor.Ejecutar(Catalogo.ZonaLibre, codigo, 0);

    private static List<string> Salida(string codigo)
    {
        var r = Ejecutar(codigo);
        Assert.True(r.Error is null, r.Error?.Message);
        return r.Salida;
    }

    [Fact]
    public void AritmeticaYPrioridad() =>
        Assert.Equal(new[] { "14", "20", "1", "2.5", "8" }, Salida("print(2 + 3 * 4)\nprint((2 + 3) * 4)\nprint(10 % 3)\nprint(5 / 2)\nprint(2 ^ 3)"));

    [Fact]
    public void TextosEInterpolacion() =>
        Assert.Equal(new[] { "Hola, Bit", "Bit tiene 3 vidas", "Tengo 5 gemas" },
            Salida("local n = \"Bit\"\nlocal v = 3\nprint(\"Hola, \" .. n)\nprint(`{n} tiene {v} vidas`)\nprint(\"Tengo\", 5, \"gemas\")"));

    [Fact]
    public void BuclesYControl() =>
        Assert.Equal(new[] { "3", "2", "1", "10", "1 a", "2 b", "x", "y", "6" },
            Salida("""
                for i = 3, 1, -1 do
                    print(i)
                end
                local s = 0
                local i = 0
                while true do
                    i += 1
                    if i > 4 then break end
                    s += i
                end
                print(s)
                for k, v in ipairs({ "a", "b" }) do
                    print(k, v)
                end
                local t = { "x", "y" }
                for _, v in t do
                    print(v)
                end
                local n = 0
                repeat
                    n += 1
                    if n % 2 == 0 then continue end
                until n >= 6
                print(n)
                """));

    [Fact]
    public void FuncionesYCierres() =>
        Assert.Equal(new[] { "1", "2", "120", "HOLA", "3" },
            Salida("""
                local function contador()
                    local c = 0
                    return function()
                        c += 1
                        return c
                    end
                end
                local f = contador()
                print(f())
                print(f())
                local function fact(n: number): number
                    if n <= 1 then return 1 end
                    return n * fact(n - 1)
                end
                print(fact(5))
                print(("hola"):upper())
                local jugador = { vidas = 3 }
                function jugador:getVidas()
                    return self.vidas
                end
                print(jugador:getVidas())
                """));

    [Theory]
    [InlineData("moverderecha()", "moverDerecha")]
    [InlineData("print(puntso)\n", "No existe la variable")]
    [InlineData("if 1 = 1 then end", "==")]
    [InlineData("if true\n  print(1)\nend", "then")]
    [InlineData("for i = 1, 3 do\n  print(i)\n", "Falta 'end'")]
    [InlineData("print(\"hola\" + 1)", "..")]
    [InlineData("local año = 3", "anio")]
    [InlineData("while true do end", "bucle infinito")]
    [InlineData("print(\"hola)", "comilla")]
    [InlineData("if x != 2 then end", "~=")]
    [InlineData("moverDerecha", "paréntesis")]
    [InlineData("local x = nil\nprint(x + 1)", "nil")]
    [InlineData("pintar(\"fucsia\")", "No conozco el color")]
    public void ErroresAmigables(string codigo, string fragmento)
    {
        var r = Ejecutar(codigo);
        Assert.NotNull(r.Error);
        Assert.Contains(fragmento, r.Error!.Message);
        Assert.True(r.Error.Linea > 0);
    }

    [Fact]
    public void ElRevisorDetectaSangriaYNombres()
    {
        var codigo = "local Mi_Variable = 1\nif true then\nprint(1)\nend\n";
        var programa = Parser.Analizar(codigo);
        var consejos = RevisorEstilo.Revisar(codigo, programa);
        Assert.Contains(consejos, c => c.Linea == 3);
        Assert.Contains(consejos, c => c.Mensaje.Contains("miVariable"));
    }

    [Fact]
    public void ElRevisorAceptaCodigoLimpio()
    {
        var codigo = """
            local VIDAS_MAXIMAS = 3
            local jugador = {
                nombre = "Ana",
            }
            local function saltar(altura)
                if altura > 2 then
                    print("alto")
                elseif altura > 1 then
                    print("medio")
                else
                    print("bajo")
                end
            end
            script = nil
            """;
        var consejos = RevisorEstilo.Revisar(codigo, Parser.Analizar(codigo), new[] { "script" });
        Assert.Empty(consejos);
    }

    [Fact]
    public void AvisaAlCambiarUnaConstante()
    {
        var r = Ejecutar("local MAXIMO = 3\nMAXIMO = 4");
        Assert.Single(r.Interprete!.Avisos);
    }
}
