using AventuraBit.Motor.Mundo;

namespace AventuraBit.Motor.Juego;

public static partial class Catalogo
{
    // =====================================================================
    // MUNDO 1 · Isla del Comienzo · Instrucciones y funciones
    // =====================================================================
    private static IEnumerable<Nivel> Mundo1()
    {
        yield return new Nivel
        {
            Id = "1-1", Mundo = 1, Numero = 1, Titulo = "¡Hola, soy Bit!", Tipo = TipoNivel.Leccion,
            Diapositivas =
            {
                new("¡Hola! Soy Bit",
                    "Soy un robot y solo hago exactamente lo que me dices. Para darme órdenes hay que escribir un PROGRAMA: una lista de instrucciones.\n\nEl lenguaje que vamos a usar se llama Luau. ¡Es el mismo que se usa para programar juegos en Roblox!"),
                new("De arriba abajo",
                    "Cada línea es una instrucción. El ordenador las ejecuta una detrás de otra, de arriba abajo, sin saltarse ninguna.",
                    """
                    moverDerecha()
                    moverDerecha()
                    recogerGema()
                    """),
                new("Llamar a una función",
                    "moverDerecha es una FUNCIÓN: una orden con nombre. Para usarla escribimos su nombre seguido de paréntesis ( ). ¡Sin paréntesis no pasa nada!\n\nprint es la función que muestra mensajes en la consola. Lo que va entre comillas es un texto.",
                    """
                    print("¡Hola, mundo!")
                    """,
                    "En Roblox Studio, print escribe en la ventana Output. Es la forma más rápida de saber qué está haciendo tu script."),
                new("Mayúsculas y minúsculas",
                    "Para el ordenador, moverDerecha y moverderecha NO son lo mismo. Fíjate bien en cómo se escribe cada nombre.\n\nEse estilo, con la primera palabra en minúscula y las siguientes empezando en mayúscula, se llama camelCase, porque las mayúsculas parecen las jorobas de un camello.",
                    """
                    moverDerecha()   -- bien
                    moverderecha()   -- mal: falta la D mayúscula
                    MoverDerecha()   -- mal: la M no va en mayúscula
                    """),
                new("Comentarios",
                    "Las líneas que empiezan por -- son COMENTARIOS. El ordenador no las lee: sirven para dejar notas a las personas.",
                    """
                    -- Esto es un comentario: Bit no lo ejecuta
                    print("Esto sí se ejecuta")
                    """),
            },
            Preguntas =
            {
                new("¿En qué orden se ejecutan las instrucciones de un programa?",
                    new[] { "Todas a la vez", "De arriba abajo, una detrás de otra", "De abajo arriba", "En el orden que quiera el ordenador" }, 1,
                    "¡Eso es! El ordenador empieza por la primera línea y va bajando."),
                new("¿Cuál de estas líneas llama bien a la función?",
                    new[] { "moverDerecha", "moverDerecha()", "MoverDerecha()", "mover Derecha()" }, 1,
                    "Nombre exacto (con camelCase) y paréntesis al final."),
            },
        };

        yield return new Nivel
        {
            Id = "1-2", Mundo = 1, Numero = 2, Titulo = "Tu primer mensaje", Tipo = TipoNivel.Codigo,
            Introduccion = "¡Vamos a escribir tu primer programa! Usa print para que en la consola aparezca:  Hola, Bit",
            CodigoInicial = """
                -- Escribe tu código debajo de esta línea

                """,
            Objetivos = { O.Imprime("Hola, Bit") },
            Solucion = """
                print("Hola, Bit")
                """,
            Pistas = new[]
            {
                "La función print necesita paréntesis: print( )",
                "El texto va dentro de los paréntesis y entre comillas: \"Hola, Bit\"",
            },
            LineasOptimas = 1,
        };

        yield return new Nivel
        {
            Id = "1-3", Mundo = 1, Numero = 3, Titulo = "Camino a la bandera", Tipo = TipoNivel.Ordenar,
            Introduccion = "Las instrucciones están desordenadas. Ponlas en orden con las flechas ▲ ▼ para llevar a Bit hasta la bandera.",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "########",
                    "#R...###",
                    "####.###",
                    "####..F#",
                    "########",
                } },
            },
            Objetivos = { O.Meta() },
            Solucion = """
                moverDerecha()
                moverDerecha()
                moverDerecha()
                moverAbajo()
                moverAbajo()
                moverDerecha()
                moverDerecha()
                """,
            Pistas = new[] { "Primero Bit tiene que ir a la derecha hasta el final del pasillo.", "Cuenta las casillas: 3 a la derecha, 2 abajo y 2 a la derecha." },
        };

        yield return new Nivel
        {
            Id = "1-4", Mundo = 1, Numero = 4, Titulo = "Recoge las gemas", Tipo = TipoNivel.Codigo,
            Introduccion = "Bit tiene que coger todas las gemas y terminar en la bandera. Para coger una gema, Bit tiene que estar encima y usar recogerGema().",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "#######",
                    "#RG..G#",
                    "#####.#",
                    "#F...G#",
                    "#######",
                } },
            },
            Objetivos = { O.Gemas(), O.Meta() },
            CodigoInicial = """
                moverDerecha()
                recogerGema()

                """,
            Solucion = """
                moverDerecha()
                recogerGema()
                moverDerecha()
                moverDerecha()
                moverDerecha()
                recogerGema()
                moverAbajo()
                moverAbajo()
                recogerGema()
                moverIzquierda()
                moverIzquierda()
                moverIzquierda()
                moverIzquierda()
                """,
            Pistas = new[]
            {
                "Ya tienes la primera gema. Ahora ve 3 casillas a la derecha.",
                "Después baja 2 casillas, recoge la gema y ve a la izquierda hasta la bandera.",
            },
        };

        yield return new Nivel
        {
            Id = "1-5", Mundo = 1, Numero = 5, Titulo = "Pinta la bandera", Tipo = TipoNivel.Codigo,
            Introduccion = "Dentro de los paréntesis podemos darle información a una función. pintar(\"rojo\") pinta de rojo la casilla donde está Bit.\n\nPinta estas 4 casillas así: rojo, amarillo, amarillo, rojo.",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "######",
                    "#R...#",
                    "######",
                } },
            },
            Objetivos = { O.Pinta("Pinta: rojo, amarillo, amarillo, rojo", (1, 1, "rojo"), (2, 1, "amarillo"), (3, 1, "amarillo"), (4, 1, "rojo")) },
            CodigoInicial = """
                pintar("rojo")
                moverDerecha()

                """,
            Solucion = """
                pintar("rojo")
                moverDerecha()
                pintar("amarillo")
                moverDerecha()
                pintar("amarillo")
                moverDerecha()
                pintar("rojo")
                """,
            Pistas = new[] { "Pinta, muévete, pinta, muévete... ¡como un patrón!", "El color va entre comillas: pintar(\"amarillo\")" },
        };

        yield return new Nivel
        {
            Id = "1-6", Mundo = 1, Numero = 6, Titulo = "Caza errores", Tipo = TipoNivel.Codigo,
            Introduccion = "¡Este programa tiene 3 errores! Ejecútalo, lee con calma lo que te digo cuando algo falla y arréglalo. Los programadores de verdad se pasan mucho tiempo cazando errores (bugs).",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "#######",
                    "#R.G..#",
                    "#####F#",
                    "#######",
                } },
            },
            Objetivos = { O.Gemas(), O.Meta() },
            CodigoInicial = """
                -- Este programa tiene 3 errores. ¡Encuéntralos!
                moverDerecha()
                MoverDerecha()
                recogergema()
                moverDerecha()
                moverDerecha(
                moverAbajo()
                """,
            Solucion = """
                moverDerecha()
                moverDerecha()
                recogerGema()
                moverDerecha()
                moverDerecha()
                moverAbajo()
                """,
            Pistas = new[]
            {
                "Mira bien los paréntesis: cada ( necesita su ).",
                "Fíjate en las mayúsculas: ¿moverDerecha o MoverDerecha? ¿recogerGema o recogergema?",
            },
        };
    }

    // =====================================================================
    // MUNDO 2 · Bosque de las Cajas · Variables y constantes
    // =====================================================================
    private static IEnumerable<Nivel> Mundo2()
    {
        yield return new Nivel
        {
            Id = "2-1", Mundo = 2, Numero = 1, Titulo = "Cajas con nombre", Tipo = TipoNivel.Leccion,
            Diapositivas =
            {
                new("¿Qué es una variable?",
                    "Una VARIABLE es como una caja con una etiqueta: dentro guardas un dato y la etiqueta es su nombre. Así puedes usar ese dato más tarde.\n\nlocal crea la caja, puntos es la etiqueta y 0 es lo que guardamos dentro. El signo = significa «guarda esto en la caja».",
                    """
                    local puntos = 0
                    """),
                new("Cambiar lo que hay dentro",
                    "Puedes cambiar el contenido de la caja cuando quieras. Para cambiarlo ya no se pone local, porque la caja ya existe.",
                    """
                    local vidas = 3
                    print(vidas)     --> 3
                    vidas = 2
                    print(vidas)     --> 2
                    """),
                new("Sumar a una variable",
                    "Esto parece raro pero es muy útil: coge lo que hay en la caja, súmale 1 y guarda el resultado otra vez en la misma caja.",
                    """
                    local gemas = 0
                    gemas = gemas + 1
                    print(gemas)     --> 1
                    """),
                new("Ponerle buen nombre",
                    "Los nombres no pueden tener espacios, ni tildes, ni ñ, ni empezar por un número. En Roblox se escriben en camelCase: la primera palabra en minúscula y las siguientes empezando con mayúscula.",
                    """
                    local nombreJugador = "Ana"    -- bien
                    local vidasRestantes = 3       -- bien
                    local Nombre_jugador = "Ana"   -- mal
                    local nombre jugador = "Ana"   -- error: tiene un espacio
                    """),
                new("Constantes",
                    "Una CONSTANTE es un valor que no va a cambiar nunca mientras funciona el programa. Por costumbre se escriben en MAYÚSCULAS, con guiones bajos entre palabras. Así, cualquiera que lea el código sabe que no hay que cambiarlas.",
                    """
                    local VIDAS_MAXIMAS = 3
                    local VELOCIDAD = 16
                    """,
                    "En Roblox, la velocidad normal de un personaje es 16. Muchos scripts empiezan con constantes como local VELOCIDAD_CORRER = 32."),
                new("print con varias cosas",
                    "Si a print le das varias cosas separadas por comas, las muestra seguidas con un espacio entre ellas.",
                    """
                    local gemas = 5
                    print("Tengo", gemas, "gemas")   --> Tengo 5 gemas
                    """),
            },
            Preguntas =
            {
                new("¿Qué nombre de variable sigue el estilo camelCase?",
                    new[] { "nombrejugador", "NombreJugador", "nombreJugador", "nombre_jugador" }, 2,
                    "camelCase: primera palabra en minúscula y cada palabra nueva con mayúscula."),
                new("¿Qué se muestra en la consola?",
                    new[] { "10", "15", "monedas", "25" }, 1,
                    "La caja empezó con 10, pero luego guardamos 15 dentro. Lo que había antes se pierde.",
                    """
                    local monedas = 10
                    monedas = 15
                    print(monedas)
                    """),
                new("¿Cómo se escribe normalmente una constante?",
                    new[] { "velocidadMaxima", "VELOCIDAD_MAXIMA", "Velocidad Maxima", "velocidad-maxima" }, 1,
                    "Las constantes van en MAYÚSCULAS con guiones bajos."),
            },
        };

        yield return new Nivel
        {
            Id = "2-2", Mundo = 2, Numero = 2, Titulo = "Tu nombre en una caja", Tipo = TipoNivel.Codigo,
            Introduccion = "Crea una variable llamada nombre que guarde tu nombre (es un texto, así que va entre comillas). Después muéstrala con print.\n\nCuando ejecutes, mira el panel de Cajas: ¡verás tu variable!",
            CodigoInicial = """
                -- 1. Crea la variable nombre

                -- 2. Muéstrala con print

                """,
            Objetivos =
            {
                O.Usa(Construccion.Local, "Crea la variable con local"),
                O.VariableTipo("nombre", "string", "un texto"),
                O.Codigo(@"print\s*\([^)]*\bnombre\b", "Usa la variable dentro de print"),
                O.ImprimeVariable("nombre"),
            },
            Solucion = """
                local nombre = "Bit"
                print(nombre)
                """,
            Pistas = new[] { "Así se crea: local nombre = \"Tu nombre\"", "Para mostrarla no lleva comillas: print(nombre). Si pones comillas, se muestra la palabra «nombre»." },
            LineasOptimas = 2,
        };

        yield return new Nivel
        {
            Id = "2-3", Mundo = 2, Numero = 3, Titulo = "Contador de gemas", Tipo = TipoNivel.Ordenar,
            Introduccion = "Ordena el programa para que Bit recoja las dos gemas y las vaya contando en la variable gemas. Al final tiene que mostrar: Tengo 2 gemas.\n\nRecuerda: la caja tiene que existir antes de usarla.",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "#####",
                    "#RGG#",
                    "#####",
                } },
            },
            Objetivos = { O.Gemas(), O.Variable("gemas", 2), O.Imprime("Tengo 2 gemas") },
            Solucion = """
                local gemas = 0
                moverDerecha()
                recogerGema()
                gemas = gemas + 1
                moverDerecha()
                recogerGema()
                gemas = gemas + 1
                print("Tengo", gemas, "gemas")
                """,
            Pistas = new[] { "La línea con local va la primera: crea la caja.", "Después de cada recogerGema() suma 1 a la caja. El print va al final." },
        };

        yield return new Nivel
        {
            Id = "2-4", Mundo = 2, Numero = 4, Titulo = "El puente largo", Tipo = TipoNivel.Codigo,
            Introduccion = "Las funciones de moverse aceptan un número: moverDerecha(3) da 3 pasos seguidos.\n\nEl puente mide 6 casillas. Guarda ese número en una variable llamada largoPuente y úsala para cruzar: moverDerecha(largoPuente).",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "~~~~~~~~~",
                    "~R.....F~",
                    "~~~~~~~~~",
                } },
            },
            Objetivos =
            {
                O.Meta(),
                O.Variable("largoPuente", 6),
                O.Codigo(@"moverDerecha\s*\(\s*largoPuente\s*\)", "Usa moverDerecha(largoPuente)"),
            },
            CodigoInicial = "",
            Solucion = """
                local largoPuente = 6
                moverDerecha(largoPuente)
                """,
            Pistas = new[] { "Primero crea la caja: local largoPuente = 6", "Cuidado con las mayúsculas: largoPuente lleva la P mayúscula." },
            LineasOptimas = 2,
        };

        yield return new Nivel
        {
            Id = "2-5", Mundo = 2, Numero = 5, Titulo = "Tu color favorito", Tipo = TipoNivel.Codigo,
            Introduccion = "Crea una constante COLOR_FAVORITO con tu color favorito (por ejemplo \"azul\") y pinta las 5 casillas usando pintar(COLOR_FAVORITO).\n\nSi mañana cambias de color favorito, solo tendrás que cambiar UNA línea. ¡Esa es la magia de las constantes!\n\nColores: rojo, azul, verde, amarillo, naranja, morado, rosa, blanco, negro, gris.",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "#######",
                    "#R....#",
                    "#######",
                } },
            },
            Objetivos =
            {
                O.VariableTipo("COLOR_FAVORITO", "string", "un color"),
                O.Codigo(@"pintar\s*\(\s*COLOR_FAVORITO\s*\)", "Usa pintar(COLOR_FAVORITO)"),
                O.PintaConVariable("COLOR_FAVORITO", "Pinta las 5 casillas con tu color", (1, 1), (2, 1), (3, 1), (4, 1), (5, 1)),
            },
            Solucion = """
                local COLOR_FAVORITO = "azul"

                pintar(COLOR_FAVORITO)
                moverDerecha()
                pintar(COLOR_FAVORITO)
                moverDerecha()
                pintar(COLOR_FAVORITO)
                moverDerecha()
                pintar(COLOR_FAVORITO)
                moverDerecha()
                pintar(COLOR_FAVORITO)
                """,
            Pistas = new[] { "local COLOR_FAVORITO = \"verde\"", "Dentro de pintar( ) pon el nombre de la constante, sin comillas." },
        };

        yield return new Nivel
        {
            Id = "2-6", Mundo = 2, Numero = 6, Titulo = "¡Ay, un cactus!", Tipo = TipoNivel.Codigo,
            Introduccion = "Rellena el hueco ___ para que Bit pierda una vida. Tienes que usar la propia variable: quitarle 1 a lo que ya tenía.",
            CodigoInicial = """
                local vidas = 3
                print("Vidas:", vidas)

                -- ¡Bit choca con un cactus! Quítale una vida
                vidas = ___
                print("Vidas:", vidas)
                """,
            Objetivos =
            {
                O.ImprimeLineas("Vidas: 3", "Vidas: 2"),
                O.Variable("vidas", 2),
                O.Codigo(@"vidas\s*=\s*vidas\s*-\s*1", "Resta 1 a la variable: vidas - 1"),
            },
            Solucion = """
                local vidas = 3
                print("Vidas:", vidas)

                -- ¡Bit choca con un cactus! Quítale una vida
                vidas = vidas - 1
                print("Vidas:", vidas)
                """,
            Pistas = new[] { "Recuerda lo de gemas = gemas + 1. Aquí es parecido, pero restando." },
        };
    }

    // =====================================================================
    // MUNDO 3 · Cueva de los Números · Tipos y operaciones
    // =====================================================================
    private static IEnumerable<Nivel> Mundo3()
    {
        yield return new Nivel
        {
            Id = "3-1", Mundo = 3, Numero = 1, Titulo = "Números, textos y más", Tipo = TipoNivel.Leccion,
            Diapositivas =
            {
                new("Tipos de datos",
                    "Cada dato tiene un TIPO:\n\n•  number (número): 7, 3.5, -2\n•  string (texto): \"hola\". Siempre entre comillas.\n•  boolean (booleano): true (verdadero) o false (falso)\n•  nil: nada. La caja está vacía.",
                    """
                    local edad = 10
                    local nombre = "Leo"
                    local tieneEspada = true
                    local mascota = nil
                    """),
                new("Operaciones",
                    "+ suma, - resta, * multiplica, / divide y % da el resto de una división.\n\nComo en mates: primero se multiplica y se divide, y después se suma y se resta. Los paréntesis van primero.",
                    """
                    print(2 + 3 * 4)     --> 14
                    print((2 + 3) * 4)   --> 20
                    print(10 % 3)        --> 1
                    """),
                new("Unir textos",
                    "Para unir textos se usan dos puntos seguidos: .. (¡no el +!).\n\nTambién puedes meter variables dentro de un texto si lo escribes entre acentos graves ` y pones la variable entre llaves { }.",
                    """
                    local nombre = "Bit"
                    print("Hola, " .. nombre)   --> Hola, Bit
                    print(`Hola, {nombre}`)     --> Hola, Bit
                    """),
                new("Atajos",
                    "Luau tiene atajos para cambiar una variable: += suma, -= resta, *= multiplica.",
                    """
                    local puntos = 10
                    puntos += 5      -- igual que: puntos = puntos + 5
                    print(puntos)    --> 15
                    """,
                    "En Roblox lo verás muchísimo, por ejemplo leaderstats.Coins.Value += 1 cada vez que un jugador coge una moneda."),
                new("¿De qué tipo es?",
                    "La función type te dice el tipo de un dato. ¡Ojo! \"5\" entre comillas es un texto, no un número.",
                    """
                    print(type(5))       --> number
                    print(type("5"))     --> string
                    print(tonumber("5") + 1)   --> 6
                    """),
            },
            Preguntas =
            {
                new("¿De qué tipo es \"25\" (con comillas)?",
                    new[] { "number (número)", "string (texto)", "boolean", "nil" }, 1,
                    "Todo lo que va entre comillas es un texto, aunque parezca un número."),
                new("¿Qué muestra este código?",
                    new[] { "10", "8", "12", "232" }, 1,
                    "Primero la multiplicación: 3 * 2 = 6. Luego 2 + 6 = 8.",
                    """
                    print(2 + 3 * 2)
                    """),
                new("¿Cómo se unen dos textos en Luau?",
                    new[] { "\"a\" + \"b\"", "\"a\" .. \"b\"", "\"a\" & \"b\"", "\"a\" * \"b\"" }, 1,
                    "Con dos puntos: .."),
            },
        };

        yield return new Nivel
        {
            Id = "3-2", Mundo = 3, Numero = 2, Titulo = "La calculadora de Bit", Tipo = TipoNivel.Codigo,
            Introduccion = "Bit tenía 12 gemas y ha encontrado 7 más.\n\nCrea las variables gemasAntes (12) y gemasNuevas (7). Después crea total sumando las dos variables y muéstralo con print.",
            Objetivos =
            {
                O.Variable("gemasAntes", 12),
                O.Variable("gemasNuevas", 7),
                O.Codigo(@"total\s*=\s*gemas(Antes|Nuevas)\s*\+\s*gemas(Antes|Nuevas)", "Calcula total sumando las dos variables"),
                O.Variable("total", 19),
                O.ImprimeVariable("total"),
            },
            Solucion = """
                local gemasAntes = 12
                local gemasNuevas = 7
                local total = gemasAntes + gemasNuevas
                print(total)
                """,
            Pistas = new[] { "local total = gemasAntes + gemasNuevas", "No escribas 19 a mano: ¡que lo calcule el ordenador!" },
            LineasOptimas = 4,
        };

        yield return new Nivel
        {
            Id = "3-3", Mundo = 3, Numero = 3, Titulo = "Une los textos", Tipo = TipoNivel.Codigo,
            Introduccion = "Rellena los huecos ___ con el operador que une textos para que se muestre:\nMe llamo Bit\nTengo 10 años",
            CodigoInicial = """
                local nombre = "Bit"
                local edad = 10

                print("Me llamo " ___ nombre)
                print("Tengo " .. edad ___ " años")
                """,
            Objetivos = { O.ImprimeLineas("Me llamo Bit", "Tengo 10 años"), O.Usa(Construccion.Concatenar, "Usa .. para unir") },
            Solucion = """
                local nombre = "Bit"
                local edad = 10

                print("Me llamo " .. nombre)
                print("Tengo " .. edad .. " años")
                """,
            Pistas = new[] { "Fíjate en la segunda línea: ya tiene un ejemplo del operador." },
        };

        yield return new Nivel
        {
            Id = "3-4", Mundo = 3, Numero = 4, Titulo = "Puntos por gema", Tipo = TipoNivel.Codigo,
            Introduccion = "Cada gema vale 10 puntos. Empieza con local puntos = 0 y, cada vez que recojas una gema, suma 10 con el atajo +=.\n\nAl final, muestra los puntos con print.",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "######",
                    "#RGGG#",
                    "######",
                } },
            },
            Objetivos =
            {
                O.Gemas(),
                O.Usa(Construccion.MasIgual, "Usa el atajo +="),
                O.Variable("puntos", 30),
                O.ImprimeVariable("puntos"),
            },
            CodigoInicial = """
                local puntos = 0

                """,
            Solucion = """
                local puntos = 0

                moverDerecha()
                recogerGema()
                puntos += 10
                moverDerecha()
                recogerGema()
                puntos += 10
                moverDerecha()
                recogerGema()
                puntos += 10
                print("Puntos:", puntos)
                """,
            Pistas = new[] { "puntos += 10 suma 10 a lo que ya tenía la caja." },
        };

        yield return new Nivel
        {
            Id = "3-5", Mundo = 3, Numero = 5, Titulo = "El jardín de Bit", Tipo = TipoNivel.Codigo,
            Introduccion = "Bit quiere saber cuántas baldosas tiene su jardín.\n\nCrea ancho = 4 y alto = 3, calcula area multiplicando ancho * alto y muestra exactamente:\nEl jardín tiene 12 baldosas",
            Objetivos =
            {
                O.Codigo(@"\bancho\s*\*\s*alto\b|\balto\s*\*\s*ancho\b", "Calcula el área multiplicando ancho * alto"),
                O.Variable("area", 12),
                O.Imprime("El jardín tiene 12 baldosas"),
            },
            Solucion = """
                local ancho = 4
                local alto = 3
                local area = ancho * alto
                print("El jardín tiene " .. area .. " baldosas")
                """,
            Pistas = new[] { "Para el mensaje une textos y la variable: \"El jardín tiene \" .. area .. \" baldosas\"", "¡Cuidado con los espacios dentro de las comillas!" },
            LineasOptimas = 4,
        };
    }
}
