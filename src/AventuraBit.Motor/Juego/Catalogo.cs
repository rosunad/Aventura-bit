namespace AventuraBit.Motor.Juego;

public sealed record Comando(string Firma, string Descripcion, int DesdeMundo);

/// <summary>Todos los mundos y niveles del juego.</summary>
public static partial class Catalogo
{
    public static readonly IReadOnlyList<MundoInfo> Mundos = new[]
    {
        new MundoInfo { Numero = 1, Nombre = "Isla del Comienzo", Tema = "Instrucciones y funciones", Color = "#22C55E", ColorOscuro = "#15803D",
            Descripcion = "Da tus primeras órdenes a Bit: una detrás de otra." },
        new MundoInfo { Numero = 2, Nombre = "Bosque de las Cajas", Tema = "Variables y constantes", Color = "#F59E0B", ColorOscuro = "#B45309",
            Descripcion = "Guarda datos en cajas con nombre." },
        new MundoInfo { Numero = 3, Nombre = "Cueva de los Números", Tema = "Tipos y operaciones", Color = "#8B5CF6", ColorOscuro = "#6D28D9",
            Descripcion = "Suma, multiplica y une textos." },
        new MundoInfo { Numero = 4, Nombre = "Río de las Decisiones", Tema = "if, else y comparaciones", Color = "#0EA5E9", ColorOscuro = "#0369A1",
            Descripcion = "Haz que Bit decida qué hacer." },
        new MundoInfo { Numero = 5, Nombre = "Montaña de los Bucles", Tema = "for, while y repeat", Color = "#EF4444", ColorOscuro = "#B91C1C",
            Descripcion = "Repite sin cansarte." },
        new MundoInfo { Numero = 6, Nombre = "Castillo de las Funciones", Tema = "Funciones, parámetros y return", Color = "#EC4899", ColorOscuro = "#BE185D",
            Descripcion = "Inventa tus propias órdenes." },
        new MundoInfo { Numero = 7, Nombre = "Templo de las Tablas", Tema = "Listas y objetos", Color = "#14B8A6", ColorOscuro = "#0F766E",
            Descripcion = "Guarda muchas cosas en una sola variable." },
    };

    public static readonly IReadOnlyList<Comando> Comandos = new[]
    {
        new Comando("print(\"texto\")", "Muestra un mensaje en la consola", 1),
        new Comando("moverDerecha()", "Bit da un paso a la derecha", 1),
        new Comando("moverIzquierda()", "Un paso a la izquierda", 1),
        new Comando("moverArriba()", "Un paso hacia arriba", 1),
        new Comando("moverAbajo()", "Un paso hacia abajo", 1),
        new Comando("recogerGema()", "Recoge la gema donde está Bit", 1),
        new Comando("pintar(\"rojo\")", "Pinta la casilla de un color", 1),
        new Comando("local nombre = valor", "Crea una variable", 2),
        new Comando("moverDerecha(3)", "Da varios pasos de golpe", 2),
        new Comando("texto1 .. texto2", "Une dos textos", 3),
        new Comando("puntos += 10", "Suma a una variable", 3),
        new Comando("type(valor)", "Dice el tipo de un dato", 3),
        new Comando("tostring(valor)", "Convierte en texto", 3),
        new Comando("tonumber(\"5\")", "Convierte un texto en número", 3),
        new Comando("math.random(1, 6)", "Número al azar entre 1 y 6", 3),
        new Comando("if ... then ... end", "Hace algo solo si se cumple", 4),
        new Comando("hayGema()", "true si hay gema donde está Bit", 4),
        new Comando("hayMuro(\"derecha\")", "true si hay muro en esa dirección", 4),
        new Comando("hayAgua(\"abajo\")", "true si hay agua en esa dirección", 4),
        new Comando("colorSuelo()", "Color de la casilla de Bit (o nil)", 4),
        new Comando("decir(\"texto\")", "Bit lo dice en un bocadillo", 4),
        new Comando("for i = 1, 10 do ... end", "Repite 10 veces", 5),
        new Comando("while cond do ... end", "Repite mientras se cumpla", 5),
        new Comando("repeat ... until cond", "Repite hasta que se cumpla", 5),
        new Comando("enMeta()", "true si Bit está en la bandera", 5),
        new Comando("wait(1)", "Espera 1 segundo", 5),
        new Comando("local function nombre() ... end", "Crea tu propia función", 6),
        new Comando("return valor", "Devuelve un resultado", 6),
        new Comando("{ \"a\", \"b\" }", "Crea una lista", 7),
        new Comando("lista[1]", "Primer elemento de la lista", 7),
        new Comando("#lista", "Cuántos elementos tiene", 7),
        new Comando("table.insert(lista, valor)", "Añade al final de la lista", 7),
        new Comando("for i, v in ipairs(lista) do", "Recorre una lista", 7),
    };

    private static List<Nivel>? _niveles;

    public static IReadOnlyList<Nivel> Niveles => _niveles ??=
        Mundo1().Concat(Mundo2()).Concat(Mundo3()).Concat(Mundo4()).Concat(Mundo5()).Concat(Mundo6()).Concat(Mundo7()).ToList();

    public static IEnumerable<Nivel> DelMundo(int mundo) => Niveles.Where(n => n.Mundo == mundo);

    public static Nivel? Siguiente(Nivel actual)
    {
        var lista = Niveles.ToList();
        int i = lista.FindIndex(n => n.Id == actual.Id);
        return i >= 0 && i + 1 < lista.Count ? lista[i + 1] : null;
    }

    public static readonly Nivel ZonaLibre = new()
    {
        Id = "libre",
        Mundo = 0,
        Numero = 0,
        Titulo = "Zona libre",
        Tipo = TipoNivel.Codigo,
        EsLibre = true,
        Introduccion = "¡Aquí no hay retos! Prueba todo lo que has aprendido: mueve a Bit, pinta, recoge gemas, haz bucles... ¡Lo que quieras!",
        Variantes =
        {
            new Variante
            {
                Mapa = new[]
                {
                    "############",
                    "#R.....G...#",
                    "#.~~~...G..#",
                    "#.~~~..###.#",
                    "#..G.......#",
                    "#.....#..F.#",
                    "############",
                },
            },
        },
        CodigoInicial = """
            -- ¡Zona libre! Escribe aquí lo que quieras probar.
            print("¡Hola desde la zona libre!")

            for i = 1, 3 do
                moverDerecha()
                pintar("morado")
            end

            """,
    };
}
