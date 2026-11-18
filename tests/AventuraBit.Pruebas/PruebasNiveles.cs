using AventuraBit.Motor.Juego;

namespace AventuraBit.Pruebas;

public class PruebasNiveles
{
    public static IEnumerable<object[]> NivelesConCodigo() =>
        Catalogo.Niveles.Where(n => n.Tipo != TipoNivel.Leccion).Select(n => new object[] { n.Id });

    public static IEnumerable<object[]> Lecciones() =>
        Catalogo.Niveles.Where(n => n.Tipo == TipoNivel.Leccion).Select(n => new object[] { n.Id });

    private static Nivel Nivel(string id) => Catalogo.Niveles.Single(n => n.Id == id);

    [Theory]
    [MemberData(nameof(NivelesConCodigo))]
    public void LaSolucionConsigueTresEstrellas(string id)
    {
        var nivel = Nivel(id);
        var c = Evaluador.Calificar(nivel, nivel.Solucion);
        var fallo = c.Resultados.FirstOrDefault(r => !r.Superado);
        var detalle = fallo is null ? "" :
            $"Variante {fallo.IndiceVariante}: error={fallo.Error?.Message} (línea {fallo.Error?.Linea}); objetivos fallidos: " +
            string.Join(" | ", fallo.Objetivos.Where(o => !o.Cumplido).Select(o => o.Objetivo.Descripcion)) +
            "; salida: " + string.Join(" / ", fallo.Salida);
        Assert.True(c.Superado, detalle);
        Assert.True(c.Estrellas == 3, $"Estrellas {c.Estrellas}. Consejos: {string.Join(" | ", c.Consejos)}");
    }

    [Theory]
    [MemberData(nameof(NivelesConCodigo))]
    public void ElCodigoInicialNoSuperaElNivel(string id)
    {
        var nivel = Nivel(id);
        if (nivel.Tipo == TipoNivel.Ordenar) return;
        var c = Evaluador.Calificar(nivel, nivel.CodigoInicial);
        Assert.False(c.Superado);
    }

    [Theory]
    [MemberData(nameof(Lecciones))]
    public void LasPreguntasEstanBienFormadas(string id)
    {
        var nivel = Nivel(id);
        Assert.NotEmpty(nivel.Diapositivas);
        Assert.NotEmpty(nivel.Preguntas);
        foreach (var p in nivel.Preguntas)
            Assert.InRange(p.Correcta, 0, p.Opciones.Length - 1);
    }

    [Fact]
    public void LosIdsSonUnicosYOrdenados()
    {
        var ids = Catalogo.Niveles.Select(n => n.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(Catalogo.Niveles, n => Assert.Equal($"{n.Mundo}-{n.Numero}", n.Id));
    }

    [Fact]
    public void LaZonaLibreSeEjecuta()
    {
        var r = Ejecutor.Ejecutar(Catalogo.ZonaLibre, Catalogo.ZonaLibre.CodigoInicial, 0);
        Assert.Null(r.Error);
    }
}
