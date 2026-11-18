using AventuraBit.Motor.Mundo;

namespace AventuraBit.Motor.Juego;

public static partial class Catalogo
{
    // =====================================================================
    // MUNDO 4 · Río de las Decisiones · if, else y comparaciones
    // =====================================================================
    private static IEnumerable<Nivel> Mundo4()
    {
        yield return new Nivel
        {
            Id = "4-1", Mundo = 4, Numero = 1, Titulo = "Si pasa esto, haz aquello", Tipo = TipoNivel.Leccion,
            Diapositivas =
            {
                new("Comparar",
                    "Para comparar dos valores:\n\n==  igual        ~=  distinto\n<   menor        >   mayor\n<=  menor o igual   >=  mayor o igual\n\nEl resultado siempre es true (verdadero) o false (falso).\n¡Ojo! Un = guarda un valor; dos == comparan.",
                    """
                    print(5 > 3)        --> true
                    print(2 == 7)       --> false
                    print("a" ~= "b")   --> true
                    """),
                new("if ... then ... end",
                    "if significa «si». Lo que hay entre then y end solo se ejecuta si la condición es verdadera.",
                    """
                    local vidas = 0
                    if vidas == 0 then
                        print("Fin de la partida")
                    end
                    """),
                new("La sangría",
                    "Las líneas que están DENTRO del if se escriben más a la derecha, pulsando la tecla Tab al principio. Eso se llama SANGRÍA.\n\nAl ordenador le da igual, pero a las personas nos ayuda muchísimo a ver qué está dentro de qué. El end va siempre a la misma altura que su if.",
                    """
                    if hayGema() then
                        recogerGema()       -- dentro: con sangría
                        print("¡Una gema!")  -- dentro: con sangría
                    end                     -- alineado con el if
                    moverDerecha()          -- fuera del if
                    """),
                new("else y elseif",
                    "else significa «si no». elseif significa «si no, pero si...». Se comprueba de arriba abajo y solo se ejecuta el PRIMER bloque cuya condición sea verdadera.",
                    """
                    local puntos = 75
                    if puntos >= 90 then
                        print("¡Medalla de oro!")
                    elseif puntos >= 50 then
                        print("Medalla de plata")
                    else
                        print("Sigue practicando")
                    end
                    """),
                new("and, or, not",
                    "and: las dos cosas tienen que ser verdad.\nor: basta con que una sea verdad.\nnot: le da la vuelta (true pasa a false y al revés).",
                    """
                    if tieneLlave and puertaCerrada then
                        print("Abro la puerta")
                    end
                    if not hayMuro("derecha") then
                        moverDerecha()
                    end
                    """,
                    "En Roblox: if humanoid.Health <= 0 then ... end sirve para saber si un personaje se ha quedado sin vida."),
            },
            Preguntas =
            {
                new("¿Qué signo sirve para COMPARAR si dos cosas son iguales?",
                    new[] { "=", "==", "~=", "=>" }, 1,
                    "== compara. Un solo = guarda un valor en una variable."),
                new("¿Qué muestra este código?",
                    new[] { "grande", "pequeño", "grande y pequeño", "Nada" }, 1,
                    "4 no es mayor que 5, así que la condición es false y se ejecuta el else.",
                    """
                    local x = 4
                    if x > 5 then
                        print("grande")
                    else
                        print("pequeño")
                    end
                    """),
                new("¿Cuál está bien sangrado?",
                    new[] { "if x then\nprint(x)\nend", "if x then\n    print(x)\nend", "    if x then\nprint(x)\n    end" }, 1,
                    "Lo de dentro lleva sangría y el end se alinea con su if."),
            },
        };

        yield return new Nivel
        {
            Id = "4-2", Mundo = 4, Numero = 2, Titulo = "¿Hay gema?", Tipo = TipoNivel.Codigo,
            Introduccion = "A veces hay una gema y a veces no. Tu programa se probará en 2 mapas distintos.\n\nhayGema() devuelve true si hay una gema donde está Bit. Rellena el hueco para que solo intente recogerla si está.",
            Variantes =
            {
                new Variante { Nombre = "Con gema", Mapa = new[] { "####", "#RG#", "####" } },
                new Variante { Nombre = "Sin gema", Mapa = new[] { "####", "#R.#", "####" } },
            },
            Objetivos = { O.Gemas(), O.SinRecogerEnVacio() },
            CodigoInicial = """
                moverDerecha()
                if hayGema() ___
                    recogerGema()
                end
                """,
            Solucion = """
                moverDerecha()
                if hayGema() then
                    recogerGema()
                end
                """,
            Pistas = new[] { "Después de la condición del if va una palabra mágica... ¡then!" },
        };

        yield return new Nivel
        {
            Id = "4-3", Mundo = 4, Numero = 3, Titulo = "¿Muro o camino?", Tipo = TipoNivel.Codigo,
            Introduccion = "En un mapa hay un muro a la derecha de Bit y en el otro no. Usa hayMuro(\"derecha\") para decidir qué camino tomar.\n\nEscribe los movimientos dentro de cada parte del if, ¡con su sangría!",
            Variantes =
            {
                new Variante { Nombre = "Con muro", Mapa = new[]
                {
                    "#####",
                    "#R#F#",
                    "#...#",
                    "#####",
                } },
                new Variante { Nombre = "Sin muro", Mapa = new[]
                {
                    "#####",
                    "#R.F#",
                    "#####",
                    "#####",
                } },
            },
            Objetivos = { O.Meta(), O.Usa(Construccion.Else, "Usa if ... else") },
            CodigoInicial = """
                if hayMuro("derecha") then
                    -- camino de abajo

                else
                    -- camino recto

                end
                """,
            Solucion = """
                if hayMuro("derecha") then
                    moverAbajo()
                    moverDerecha(2)
                    moverArriba()
                else
                    moverDerecha(2)
                end
                """,
            Pistas = new[] { "Con muro: abajo, 2 a la derecha y arriba.", "Sin muro: 2 a la derecha." },
            LineasOptimas = 7,
        };

        yield return new Nivel
        {
            Id = "4-4", Mundo = 4, Numero = 4, Titulo = "El semáforo", Tipo = TipoNivel.Ordenar, OrdenarConSangria = true,
            Introduccion = "Bit está sobre una casilla de color. Si es roja se para, si es amarilla dice que va despacio y si es verde avanza.\n\n¡Esta vez también tienes que poner la sangría con las flechas ◀ ▶!",
            Variantes =
            {
                new Variante { Nombre = "Rojo", Mapa = new[] { "####", "#r.#", "####" }, Inicio = new Pos(1, 1),
                    Objetivos = { O.Dice("¡Me paro!"), O.Posicion(1, 1, "En rojo, Bit no se mueve") } },
                new Variante { Nombre = "Amarillo", Mapa = new[] { "####", "#a.#", "####" }, Inicio = new Pos(1, 1),
                    Objetivos = { O.Dice("Voy despacio"), O.Posicion(1, 1, "En amarillo, Bit no se mueve") } },
                new Variante { Nombre = "Verde", Mapa = new[] { "####", "#v.#", "####" }, Inicio = new Pos(1, 1),
                    Objetivos = { O.Posicion(2, 1, "En verde, Bit avanza"), O.NoDice() } },
            },
            Solucion = """
                local color = colorSuelo()
                if color == "rojo" then
                    decir("¡Me paro!")
                elseif color == "amarillo" then
                    decir("Voy despacio")
                else
                    moverDerecha()
                end
                """,
            Pistas = new[] { "Primero hay que saber el color: la línea con colorSuelo() va la primera.", "Las líneas con decir y moverDerecha van dentro, con sangría. if, elseif, else y end van sin sangría." },
        };

        yield return new Nivel
        {
            Id = "4-5", Mundo = 4, Numero = 5, Titulo = "Medallas", Tipo = TipoNivel.Codigo,
            Introduccion = "El juego te da una variable puntos (¡no la crees tú, ya existe!). Muestra con print:\n\n•  \"Oro\" si tiene 90 puntos o más\n•  \"Plata\" si tiene 50 o más\n•  \"Bronce\" en cualquier otro caso\n\nSe probará con 3 jugadores distintos.",
            Variantes =
            {
                new Variante { Nombre = "95 puntos", Globales = { ["puntos"] = 95 }, Objetivos = { O.ImprimeSolo("Oro") } },
                new Variante { Nombre = "50 puntos", Globales = { ["puntos"] = 50 }, Objetivos = { O.ImprimeSolo("Plata") } },
                new Variante { Nombre = "30 puntos", Globales = { ["puntos"] = 30 }, Objetivos = { O.ImprimeSolo("Bronce") } },
            },
            Objetivos = { O.Usa(Construccion.ElseIf, "Usa elseif") },
            CodigoInicial = """
                -- La variable puntos ya existe: ¡búscala en las cajas!

                """,
            Solucion = """
                if puntos >= 90 then
                    print("Oro")
                elseif puntos >= 50 then
                    print("Plata")
                else
                    print("Bronce")
                end
                """,
            Pistas = new[] { "Empieza comprobando lo más difícil: if puntos >= 90 then", "Con 50 puntos justos tiene que salir Plata: usa >= y no >." },
            LineasOptimas = 7,
        };

        yield return new Nivel
        {
            Id = "4-6", Mundo = 4, Numero = 6, Titulo = "El puente levadizo", Tipo = TipoNivel.Codigo,
            Introduccion = "Bit solo puede cruzar si tiene la llave Y ADEMÁS el puente está bajado. El juego te da las variables tieneLlave y puenteBajado (true o false).\n\nRellena el hueco con la palabra que significa «las dos cosas a la vez».",
            Variantes =
            {
                new Variante { Nombre = "Llave y puente", Mapa = new[] { "#######", "#R...F#", "#######" },
                    Globales = { ["tieneLlave"] = true, ["puenteBajado"] = true }, Objetivos = { O.Meta() } },
                new Variante { Nombre = "Sin puente", Mapa = new[] { "#######", "#R...F#", "#######" },
                    Globales = { ["tieneLlave"] = true, ["puenteBajado"] = false },
                    Objetivos = { O.Dice("No puedo pasar"), O.Posicion(1, 1, "Sin puente, Bit no se mueve") } },
                new Variante { Nombre = "Sin llave", Mapa = new[] { "#######", "#R...F#", "#######" },
                    Globales = { ["tieneLlave"] = false, ["puenteBajado"] = true },
                    Objetivos = { O.Dice("No puedo pasar"), O.Posicion(1, 1, "Sin llave, Bit no se mueve") } },
            },
            Objetivos = { O.Usa(Construccion.And, "Usa and") },
            CodigoInicial = """
                -- Bit solo puede cruzar si tiene la llave Y el puente está bajado
                if tieneLlave ___ puenteBajado then
                    moverDerecha(4)
                else
                    decir("No puedo pasar")
                end
                """,
            Solucion = """
                -- Bit solo puede cruzar si tiene la llave Y el puente está bajado
                if tieneLlave and puenteBajado then
                    moverDerecha(4)
                else
                    decir("No puedo pasar")
                end
                """,
            Pistas = new[] { "En Luau «y» se escribe en inglés: and" },
        };
    }

    // =====================================================================
    // MUNDO 5 · Montaña de los Bucles · for, while y repeat
    // =====================================================================
    private static IEnumerable<Nivel> Mundo5()
    {
        yield return new Nivel
        {
            Id = "5-1", Mundo = 5, Numero = 1, Titulo = "Repetir sin cansarse", Tipo = TipoNivel.Leccion,
            Diapositivas =
            {
                new("¿Por qué repetir?",
                    "Si Bit tiene que dar 100 pasos recogiendo gemas, ¿vas a escribir 200 líneas? ¡Ni hablar! Los BUCLES repiten código por ti."),
                new("El bucle for",
                    "for repite un número de veces. i empieza en 1 y va subiendo de uno en uno hasta 5. En cada vuelta se ejecuta lo de dentro.\n\ni es una variable que cambia sola en cada vuelta: ¡mírala en las cajas cuando ejecutes!",
                    """
                    for i = 1, 5 do
                        print("Vuelta número", i)
                    end
                    """),
                new("Contar hacia atrás",
                    "Un tercer número indica el PASO. Con -1, cuenta hacia atrás.",
                    """
                    for i = 10, 1, -1 do
                        print(i)
                    end
                    print("¡Despegue!")
                    """),
                new("El bucle while",
                    "while significa «mientras». Repite mientras la condición sea verdadera.\n\n¡Cuidado! Si la condición no cambia nunca, el bucle no termina jamás: es un BUCLE INFINITO.",
                    """
                    while not hayMuro("derecha") do
                        moverDerecha()
                    end
                    """),
                new("repeat ... until",
                    "repeat primero hace y después comprueba: repite HASTA QUE la condición sea verdadera.",
                    """
                    repeat
                        moverDerecha()
                    until enMeta()
                    """,
                    "En Roblox es muy típico un bucle que se repite siempre con una espera: while true do ... task.wait(1) end. ¡La espera es obligatoria o el juego se congela!"),
            },
            Preguntas =
            {
                new("¿Cuántas veces se muestra «hola»?",
                    new[] { "1", "3", "4", "Infinitas" }, 1,
                    "i vale 1, luego 2, luego 3. Tres vueltas.",
                    """
                    for i = 1, 3 do
                        print("hola")
                    end
                    """),
                new("¿Qué es un bucle infinito?",
                    new[] { "Un bucle muy rápido", "Un bucle que nunca termina porque su condición no cambia", "Un bucle con muchas líneas", "Un bucle que cuenta hasta infinito y para" }, 1,
                    "Si nada cambia la condición, el bucle sigue para siempre."),
                new("¿Qué números se muestran?",
                    new[] { "1 2 3 ... 10", "2 4 6 8 10", "2 10", "10 8 6 4 2" }, 1,
                    "Empieza en 2 y va sumando 2 hasta llegar a 10.",
                    """
                    for i = 2, 10, 2 do
                        print(i)
                    end
                    """),
            },
        };

        yield return new Nivel
        {
            Id = "5-2", Mundo = 5, Numero = 2, Titulo = "El pasillo de gemas", Tipo = TipoNivel.Codigo,
            Introduccion = "¡7 gemas seguidas! En vez de escribir 14 líneas, usa un bucle for que repita 7 veces: moverse y recoger.",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "##########",
                    "#RGGGGGGG#",
                    "##########",
                } },
            },
            Objetivos = { O.Gemas(), O.Usa(Construccion.For, "Usa un bucle for") },
            CodigoInicial = """
                for i = 1, 7 do

                end
                """,
            Solucion = """
                for i = 1, 7 do
                    moverDerecha()
                    recogerGema()
                end
                """,
            Pistas = new[] { "Dentro del for (con sangría) pon moverDerecha() y recogerGema()." },
            LineasOptimas = 4,
        };

        yield return new Nivel
        {
            Id = "5-3", Mundo = 5, Numero = 3, Titulo = "Cuenta atrás", Tipo = TipoNivel.Ordenar, OrdenarConSangria = true,
            Introduccion = "¡El cohete de Bit va a despegar! Ordena y sangra el código para que cuente 5, 4, 3, 2, 1 y después muestre ¡Despegue!",
            Objetivos = { O.ImprimeLineas("5", "4", "3", "2", "1", "¡Despegue!") },
            Solucion = """
                for i = 5, 1, -1 do
                    print(i)
                    wait(0.5)
                end
                print("¡Despegue!")
                """,
            Pistas = new[] { "El for va el primero y su end cierra el bucle.", "¡Despegue! se dice una sola vez, así que va fuera del bucle (sin sangría)." },
        };

        yield return new Nivel
        {
            Id = "5-4", Mundo = 5, Numero = 4, Titulo = "La escalera", Tipo = TipoNivel.Codigo,
            Introduccion = "Bit baja una escalera con una gema en cada escalón. Busca el patrón que se repite y escríbelo dentro de un for. Al final, ¡a la bandera!",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "########",
                    "#R.#####",
                    "##G.####",
                    "###G.###",
                    "####G.##",
                    "#####GF#",
                    "########",
                } },
            },
            Objetivos = { O.Gemas(), O.Meta(), O.Usa(Construccion.For, "Usa un bucle for") },
            Solucion = """
                for i = 1, 4 do
                    moverDerecha()
                    moverAbajo()
                    recogerGema()
                end
                moverDerecha()
                """,
            Pistas = new[] { "Cada escalón es: derecha, abajo, recoger.", "Hay 4 escalones. Después del bucle falta un paso hasta la bandera." },
            LineasOptimas = 6,
        };

        yield return new Nivel
        {
            Id = "5-5", Mundo = 5, Numero = 5, Titulo = "Hasta el muro", Tipo = TipoNivel.Codigo,
            Introduccion = "Los pasillos tienen distinto largo en cada mapa, así que no sabes cuántos pasos dar. Usa un bucle while: mientras NO haya muro a la derecha, avanza.",
            Variantes =
            {
                new Variante { Nombre = "Mediano", Mapa = new[] { "#######", "#R...F#", "#######" } },
                new Variante { Nombre = "Largo", Mapa = new[] { "##########", "#R......F#", "##########" } },
                new Variante { Nombre = "Corto", Mapa = new[] { "#####", "#R.F#", "#####" } },
            },
            Objetivos = { O.Meta(), O.Usa(Construccion.While, "Usa un bucle while") },
            Solucion = """
                while not hayMuro("derecha") do
                    moverDerecha()
                end
                """,
            Pistas = new[] { "while not hayMuro(\"derecha\") do", "Dentro del while solo hace falta un paso." },
            LineasOptimas = 3,
        };

        yield return new Nivel
        {
            Id = "5-6", Mundo = 5, Numero = 6, Titulo = "Rojo, azul, rojo, azul...", Tipo = TipoNivel.Codigo,
            Introduccion = "Pinta las 8 casillas alternando colores: rojo, azul, rojo, azul...\n\nTruco: i % 2 da el resto de dividir entre 2. Si i % 2 == 1, i es impar (1, 3, 5...); si no, es par.",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "##########",
                    "#R.......#",
                    "##########",
                } },
            },
            Objetivos =
            {
                O.Pinta("Pinta las 8 casillas: rojo, azul, rojo, azul...",
                    (1, 1, "rojo"), (2, 1, "azul"), (3, 1, "rojo"), (4, 1, "azul"),
                    (5, 1, "rojo"), (6, 1, "azul"), (7, 1, "rojo"), (8, 1, "azul")),
                O.Usa(Construccion.For, "Usa un bucle for"),
                O.Usa(Construccion.If, "Usa un if dentro del bucle"),
            },
            Solucion = """
                for i = 1, 8 do
                    if i % 2 == 1 then
                        pintar("rojo")
                    else
                        pintar("azul")
                    end
                    moverDerecha()
                end
                """,
            Pistas = new[] { "Dentro del for, un if decide el color y después Bit avanza.", "El if va dentro del for, así que lo de dentro del if lleva DOS sangrías." },
            LineasOptimas = 8,
        };

        yield return new Nivel
        {
            Id = "5-7", Mundo = 5, Numero = 7, Titulo = "Gemas por el camino", Tipo = TipoNivel.Codigo,
            Introduccion = "Las gemas cambian de sitio en cada mapa. Avanza paso a paso recogiendo las gemas que encuentres hasta llegar a la bandera.\n\nPrueba repeat ... until enMeta().",
            Variantes =
            {
                new Variante { Nombre = "Mapa 1", Mapa = new[] { "#########", "#R.G.G.F#", "#########" } },
                new Variante { Nombre = "Mapa 2", Mapa = new[] { "#########", "#RG..G.F#", "#########" } },
                new Variante { Nombre = "Mapa 3", Mapa = new[] { "######", "#RGGF#", "######" } },
            },
            Objetivos =
            {
                O.Gemas(), O.Meta(), O.SinRecogerEnVacio(),
                O.UsaAlguno("Usa un bucle repeat o while", Construccion.Repeat, Construccion.While),
            },
            Solucion = """
                repeat
                    moverDerecha()
                    if hayGema() then
                        recogerGema()
                    end
                until enMeta()
                """,
            Pistas = new[] { "En cada vuelta: un paso a la derecha y, si hay gema, recogerla.", "El bucle termina con until enMeta()" },
            LineasOptimas = 6,
        };
    }

    // =====================================================================
    // MUNDO 6 · Castillo de las Funciones
    // =====================================================================
    private static IEnumerable<Nivel> Mundo6()
    {
        yield return new Nivel
        {
            Id = "6-1", Mundo = 6, Numero = 1, Titulo = "Crea tus propias órdenes", Tipo = TipoNivel.Leccion,
            Diapositivas =
            {
                new("¿Qué es una función?",
                    "Ya has usado muchas: print, moverDerecha, pintar... ¡Ahora vas a crear las tuyas!\n\nUna FUNCIÓN es un trozo de código con nombre que puedes usar todas las veces que quieras."),
                new("Definir y llamar",
                    "Definir la función NO la ejecuta: solo le enseña al ordenador lo que tiene que hacer. Se ejecuta cuando la LLAMAS con su nombre y paréntesis.",
                    """
                    local function saludar()
                        print("¡Hola!")
                        print("Soy Bit")
                    end

                    saludar()
                    saludar()
                    """),
                new("Parámetros",
                    "Los PARÁMETROS son datos que le pasas a la función entre los paréntesis. Dentro de la función funcionan como variables.",
                    """
                    local function saludarA(nombre)
                        print("¡Hola, " .. nombre .. "!")
                    end

                    saludarA("Ana")   --> ¡Hola, Ana!
                    saludarA("Leo")   --> ¡Hola, Leo!
                    """),
                new("return: devolver un resultado",
                    "return termina la función y DEVUELVE un valor a quien la llamó. Así la función puede calcular cosas por ti.",
                    """
                    local function doble(numero)
                        return numero * 2
                    end

                    local resultado = doble(21)
                    print(resultado)   --> 42
                    """),
                new("Buenos nombres",
                    "Las funciones también van en camelCase y su nombre suele empezar por un verbo: saltar, recogerTodo, calcularPuntos...",
                    """
                    local function alTocar(otraParte)
                        print("Me ha tocado", otraParte.Name)
                    end

                    script.Parent.Touched:Connect(alTocar)
                    """,
                    "Este es código de Roblox de verdad: conectas tu función a un evento y se ejecuta cada vez que algo toca la pieza."),
            },
            Preguntas =
            {
                new("Cuando escribes local function saltar() ... end, ¿qué pasa?",
                    new[] { "Bit salta en ese momento", "Se aprende la función, pero no se ejecuta hasta que la llames", "Da un error", "Bit salta dos veces" }, 1,
                    "Definir es enseñar. Para ejecutarla hay que llamarla: saltar()"),
                new("¿Qué muestra este código?",
                    new[] { "34", "7", "a + b", "Nada" }, 1,
                    "sumar(3, 4) devuelve 3 + 4 = 7, y print lo muestra.",
                    """
                    local function sumar(a, b)
                        return a + b
                    end
                    print(sumar(3, 4))
                    """),
            },
        };

        yield return new Nivel
        {
            Id = "6-2", Mundo = 6, Numero = 2, Titulo = "De dos en dos", Tipo = TipoNivel.Codigo,
            Introduccion = "La función recogerDos debería avanzar y recoger dos gemas, pero está a medias. Completa los huecos ___ y mira cómo se reutiliza en cada fila.",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "#####",
                    "#RGG#",
                    "#.GG#",
                    "#.GG#",
                    "#####",
                } },
            },
            Objetivos = { O.Gemas() },
            CodigoInicial = """
                local function recogerDos()
                    moverDerecha()
                    recogerGema()
                    ___
                    ___
                end

                recogerDos()
                moverAbajo()
                moverIzquierda(2)
                recogerDos()
                moverAbajo()
                moverIzquierda(2)
                recogerDos()
                """,
            Solucion = """
                local function recogerDos()
                    moverDerecha()
                    recogerGema()
                    moverDerecha()
                    recogerGema()
                end

                recogerDos()
                moverAbajo()
                moverIzquierda(2)
                recogerDos()
                moverAbajo()
                moverIzquierda(2)
                recogerDos()
                """,
            Pistas = new[] { "La segunda gema está una casilla más a la derecha. Repite lo mismo." },
        };

        yield return new Nivel
        {
            Id = "6-3", Mundo = 6, Numero = 3, Titulo = "Pinta filas", Tipo = TipoNivel.Codigo,
            Introduccion = "Crea una función pintarFila(color) que pinte las 4 casillas de una fila con ese color y deje a Bit al principio de la fila de abajo.\n\nDespués llámala 3 veces: rojo, amarillo y verde.",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "######",
                    "#R...#",
                    "#....#",
                    "#....#",
                    "######",
                } },
            },
            Objetivos =
            {
                O.Codigo(@"function\s+pintarFila\s*\(\s*\w+\s*\)", "Crea la función pintarFila con un parámetro"),
                O.Llama("pintarFila", "Llama a pintarFila 3 veces", 3),
                O.Pinta("Fila 1 roja, fila 2 amarilla, fila 3 verde",
                    (1, 1, "rojo"), (2, 1, "rojo"), (3, 1, "rojo"), (4, 1, "rojo"),
                    (1, 2, "amarillo"), (2, 2, "amarillo"), (3, 2, "amarillo"), (4, 2, "amarillo"),
                    (1, 3, "verde"), (2, 3, "verde"), (3, 3, "verde"), (4, 3, "verde")),
            },
            CodigoInicial = """
                local function pintarFila(color)
                    -- pinta 4 casillas y baja a la fila siguiente

                end

                pintarFila("rojo")
                """,
            Solucion = """
                local function pintarFila(color)
                    for i = 1, 3 do
                        pintar(color)
                        moverDerecha()
                    end
                    pintar(color)
                    moverAbajo()
                    moverIzquierda(3)
                end

                pintarFila("rojo")
                pintarFila("amarillo")
                pintarFila("verde")
                """,
            Pistas = new[] { "Dentro de la función usa pintar(color): color es el parámetro.", "Para volver al principio de la fila: moverAbajo() y moverIzquierda(3)." },
            LineasOptimas = 12,
        };

        yield return new Nivel
        {
            Id = "6-4", Mundo = 6, Numero = 4, Titulo = "El doble", Tipo = TipoNivel.Codigo,
            Introduccion = "Crea una función doble(numero) que DEVUELVA (con return) el doble del número. Después muestra con print el resultado de doble(21).\n\nEl juego probará tu función con otros números para ver si funciona siempre.",
            Objetivos =
            {
                O.Usa(Construccion.Return, "Usa return"),
                O.Funcion("doble", new object?[] { 21 }, 42),
                O.Funcion("doble", new object?[] { 5 }, 10),
                O.Funcion("doble", new object?[] { 0 }, 0),
                O.Imprime("42"),
            },
            Solucion = """
                local function doble(numero)
                    return numero * 2
                end

                print(doble(21))
                """,
            Pistas = new[] { "return numero * 2", "print(doble(21)) llama a la función y muestra lo que devuelve." },
            LineasOptimas = 4,
        };

        yield return new Nivel
        {
            Id = "6-5", Mundo = 6, Numero = 5, Titulo = "¿Es par?", Tipo = TipoNivel.Codigo,
            Introduccion = "Crea una función esPar(numero) que devuelva true si el número es par y false si es impar.\n\nRecuerda: numero % 2 es el resto de dividir entre 2. Y una comparación como (algo == 0) ya es true o false, ¡así que puedes devolverla directamente!",
            Objetivos =
            {
                O.Funcion("esPar", new object?[] { 4 }, true),
                O.Funcion("esPar", new object?[] { 7 }, false),
                O.Funcion("esPar", new object?[] { 0 }, true),
                O.Funcion("esPar", new object?[] { 13 }, false),
            },
            CodigoInicial = """
                local function esPar(numero)

                end

                print(esPar(10))
                """,
            Solucion = """
                local function esPar(numero)
                    return numero % 2 == 0
                end

                print(esPar(10))
                """,
            Pistas = new[] { "Un número es par si numero % 2 == 0", "return numero % 2 == 0" },
            LineasOptimas = 4,
        };
    }

    // =====================================================================
    // MUNDO 7 · Templo de las Tablas · Listas y objetos
    // =====================================================================
    private static IEnumerable<Nivel> Mundo7()
    {
        yield return new Nivel
        {
            Id = "7-1", Mundo = 7, Numero = 1, Titulo = "Listas y objetos", Tipo = TipoNivel.Leccion,
            Diapositivas =
            {
                new("Listas",
                    "Una TABLA puede guardar muchos valores en una sola variable, en orden. Se escribe entre llaves { } con los valores separados por comas.\n\nPara sacar un elemento se pone su posición entre corchetes. ¡En Luau se empieza a contar en 1! Y # te dice cuántos elementos hay.",
                    """
                    local colores = { "rojo", "verde", "azul" }
                    print(colores[1])   --> rojo
                    print(#colores)     --> 3
                    """),
                new("Añadir cosas",
                    "table.insert añade un valor al final de la lista.",
                    """
                    local mochila = {}
                    table.insert(mochila, "espada")
                    table.insert(mochila, "escudo")
                    print(#mochila)   --> 2
                    """),
                new("Recorrer una lista",
                    "ipairs recorre la lista en orden: en cada vuelta, i es la posición y fruta el valor.",
                    """
                    local frutas = { "manzana", "pera", "uva" }
                    for i, fruta in ipairs(frutas) do
                        print(i, fruta)
                    end
                    """),
                new("Objetos con propiedades",
                    "Una tabla también puede tener PROPIEDADES con nombre. Se leen y se cambian con un punto.",
                    """
                    local jugador = {
                        nombre = "Ana",
                        vidas = 3,
                    }
                    print(jugador.nombre)   --> Ana
                    jugador.vidas -= 1
                    """,
                    "¡En Roblox todo son objetos con propiedades! part.Color, part.Transparency, humanoid.WalkSpeed... Y para recorrer las piezas de una carpeta: for _, pieza in ipairs(carpeta:GetChildren()) do"),
            },
            Preguntas =
            {
                new("¿Qué muestra este código?",
                    new[] { "perro", "gato", "pez", "2" }, 1,
                    "Se cuenta desde 1: perro es el 1, gato el 2.",
                    """
                    local animales = { "perro", "gato", "pez" }
                    print(animales[2])
                    """),
                new("Si lista = { 5, 8, 2, 9 }, ¿cuánto vale #lista?",
                    new[] { "4", "9", "24", "5" }, 0,
                    "# cuenta cuántos elementos hay: 4."),
                new("¿Cómo se lee el color del coche?",
                    new[] { "coche[color]", "coche.color", "color.coche", "coche:color" }, 1,
                    "Las propiedades se leen con un punto: coche.color",
                    """
                    local coche = { color = "rojo" }
                    """),
            },
        };

        yield return new Nivel
        {
            Id = "7-2", Mundo = 7, Numero = 2, Titulo = "Pinta con la lista", Tipo = TipoNivel.Codigo,
            Introduccion = "Aquí tienes una lista con los colores del arcoíris. Recórrela con ipairs y pinta una casilla de cada color.",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "########",
                    "#R.....#",
                    "########",
                } },
            },
            Objetivos =
            {
                O.Usa(Construccion.Ipairs, "Recorre la lista con ipairs"),
                O.Pinta("Pinta el arcoíris",
                    (1, 1, "rojo"), (2, 1, "naranja"), (3, 1, "amarillo"), (4, 1, "verde"), (5, 1, "azul"), (6, 1, "morado")),
            },
            CodigoInicial = """
                local colores = { "rojo", "naranja", "amarillo", "verde", "azul", "morado" }

                """,
            Solucion = """
                local colores = { "rojo", "naranja", "amarillo", "verde", "azul", "morado" }

                for i, color in ipairs(colores) do
                    pintar(color)
                    moverDerecha()
                end
                """,
            Pistas = new[] { "for i, color in ipairs(colores) do", "Dentro: pintar(color) y moverDerecha()." },
            LineasOptimas = 5,
        };

        yield return new Nivel
        {
            Id = "7-3", Mundo = 7, Numero = 3, Titulo = "La ficha del jugador", Tipo = TipoNivel.Codigo,
            Introduccion = "jugador es un objeto con dos propiedades. Rellena el hueco para quitarle una vida usando su propiedad vidas.",
            CodigoInicial = """
                local jugador = {
                    nombre = "Bit",
                    vidas = 3,
                }

                -- Quita una vida al jugador
                jugador.vidas = ___

                print(jugador.nombre, "tiene", jugador.vidas, "vidas")
                """,
            Objetivos =
            {
                O.Codigo(@"jugador\.vidas\s*=\s*jugador\.vidas\s*-\s*1", "Usa jugador.vidas - 1"),
                O.Imprime("Bit tiene 2 vidas"),
            },
            Solucion = """
                local jugador = {
                    nombre = "Bit",
                    vidas = 3,
                }

                -- Quita una vida al jugador
                jugador.vidas = jugador.vidas - 1

                print(jugador.nombre, "tiene", jugador.vidas, "vidas")
                """,
            Pistas = new[] { "Es como vidas = vidas - 1, pero con jugador. delante." },
        };

        yield return new Nivel
        {
            Id = "7-4", Mundo = 7, Numero = 4, Titulo = "La mochila de gemas", Tipo = TipoNivel.Codigo,
            Introduccion = "Recorre el pasillo (5 pasos). Cada vez que encuentres una gema, recógela y guárdala en la mochila con table.insert(mochila, \"gema\").\n\nAl final muestra cuántas cosas hay en la mochila con print(#mochila).",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "########",
                    "#RG.G.G#",
                    "########",
                } },
            },
            Objetivos =
            {
                O.Gemas(), O.SinRecogerEnVacio(),
                O.Llama("table.insert", "Usa table.insert"),
                O.Longitud("mochila", 3),
                O.Imprime("3"),
            },
            CodigoInicial = """
                local mochila = {}

                """,
            Solucion = """
                local mochila = {}

                for i = 1, 5 do
                    moverDerecha()
                    if hayGema() then
                        recogerGema()
                        table.insert(mochila, "gema")
                    end
                end
                print(#mochila)
                """,
            Pistas = new[] { "Un for de 5 vueltas, con un if hayGema() dentro.", "Dentro del if: recogerGema() y table.insert(mochila, \"gema\")." },
            LineasOptimas = 9,
        };

        yield return new Nivel
        {
            Id = "7-5", Mundo = 7, Numero = 5, Titulo = "El gran reto final", Tipo = TipoNivel.Codigo,
            Introduccion = "¡El último desafío! Recoge las 18 gemas del templo y llega a la bandera. Usa todo lo que has aprendido: funciones, bucles e if.\n\nPara las 3 estrellas tendrás que escribir un programa muy cortito... ¿Sabías que a una función le puedes pasar OTRA función como parámetro?",
            Variantes =
            {
                new Variante { Mapa = new[]
                {
                    "##########",
                    "#RGGGGGG.#",
                    "########.#",
                    "#.GGGGGG.#",
                    "#.########",
                    "#.GGGGGGF#",
                    "##########",
                } },
            },
            Objetivos =
            {
                O.Gemas(), O.Meta(),
                O.Usa(Construccion.FuncionPropia, "Crea al menos una función"),
                O.Usa(Construccion.For, "Usa un bucle"),
            },
            Solucion = """
                local function recorrer(moverse)
                    for i = 1, 7 do
                        moverse()
                        if hayGema() then
                            recogerGema()
                        end
                    end
                end

                recorrer(moverDerecha)
                moverAbajo(2)
                recorrer(moverIzquierda)
                moverAbajo(2)
                recorrer(moverDerecha)
                """,
            Pistas = new[]
            {
                "Cada fila tiene 7 pasos. Puedes hacer una función para ir a la derecha recogiendo y otra para ir a la izquierda.",
                "Truco avanzado: function recorrer(moverse) ... moverse() ... end y luego recorrer(moverDerecha). ¡Sin paréntesis al pasarla!",
            },
            LineasOptimas = 13,
        };
    }
}
