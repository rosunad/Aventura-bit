# La Aventura de Bit

Juego de escritorio (WPF, .NET 10) para aprender a programar en **Luau**, el lenguaje de Roblox.
El jugador guía a Bit, un robot, por 7 mundos de puzzles. Primero entiende los conceptos y después escribe código de verdad.

## Cómo se juega

| Mundo | Tema | Qué se aprende |
|---|---|---|
| 1 · Isla del Comienzo | Instrucciones y funciones | Secuencia, llamar a funciones, `print`, comentarios, mayúsculas y minúsculas, depurar errores |
| 2 · Bosque de las Cajas | Variables y constantes | `local`, cambiar valores, camelCase, CONSTANTES, `x = x + 1` |
| 3 · Cueva de los Números | Tipos y operaciones | number, string, boolean y nil; `+ - * / %`; `..`; textos con `` `{}` ``; `+=` |
| 4 · Río de las Decisiones | `if` / `else` | Comparaciones, `elseif`, `and` / `or` / `not`, sangría |
| 5 · Montaña de los Bucles | `for`, `while` y `repeat` | Bucles, contar hacia atrás, bucles infinitos |
| 6 · Castillo de las Funciones | Funciones propias | Parámetros, `return`, nombres con verbo |
| 7 · Templo de las Tablas | Listas y objetos | `{}`, `#`, `table.insert`, `ipairs`, propiedades con punto |

Hay tres tipos de nivel:

- **Lección**: diapositivas con ejemplos, cuadros «En Roblox» y preguntas tipo test.
- **Ordenar**: líneas desordenadas que hay que colocar con ▲▼. En algunos niveles también hay que poner la sangría con ◀▶.
- **Código**: editor con colores en el que se escribe o se completa el programa. Los huecos se marcan con `___`.

Cada nivel de código da hasta 3 estrellas:

1. Cumplir los objetivos en todos los mapas del nivel.
2. Escribir el código limpio: sangría correcta, nombres en camelCase, `local` y constantes sin cambiar.
3. Escribir un programa corto (en los niveles de ordenar, acertar en uno o dos intentos).

Mientras se ejecuta el programa se ve la línea en curso, el panel de **cajas** con el valor de cada variable y la **consola** con lo que muestra `print`.
El modo **Paso a paso** (F10) se detiene antes de cada línea.
Los errores se explican en español y proponen correcciones, por ejemplo: «¿Querías decir `moverDerecha`?».

El progreso se guarda en `%APPDATA%\AventuraBit\progreso.json`.

## Ejecutar

```bash
dotnet run --project src/AventuraBit.App
```

O publicar un ejecutable:

```bash
dotnet publish src/AventuraBit.App -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publicar
```

## Estructura

- `src/AventuraBit.Motor`: lógica sin interfaz.
  - `Lua/`: intérprete propio de Luau, con lexer, parser, intérprete y biblioteca (`print`, `math`, `string`, `table`, `ipairs`…). Admite `+=`, `continue`, textos con `` ` `` y anotaciones de tipo. Los mensajes de error están pensados para niños.
  - `Mundo/`: el tablero de Bit y sus funciones (`moverDerecha`, `recogerGema`, `pintar`, `hayMuro`…).
  - `Juego/`: niveles (`Niveles1a3.cs`, `Niveles4a7.cs`), objetivos, evaluador de estrellas, revisor de estilo y progreso.
- `src/AventuraBit.App`: interfaz WPF (pantallas, editor AvalonEdit, animaciones).
- `tests/AventuraBit.Pruebas`: pruebas. Comprueban, entre otras cosas, que la solución de **cada nivel** consigue 3 estrellas y que el código inicial no supera el nivel.

## Añadir un nivel

Añade un `yield return new Nivel { ... }` en el mundo que corresponda. Hace falta:

- **Mapa**: `#` muro, `.` suelo, `~` agua, `R` Bit, `G` gema, `F` bandera, y letras minúsculas para el suelo ya pintado (`r` rojo, `a` amarillo, `v` verde, `z` azul…).
- **Varios mapas** (`Variantes`): obligan a usar `if` o bucles. Cada variante puede tener sus propios objetivos y variables globales del juego.
- **Objetivos**: con `O.Meta()`, `O.Gemas()`, `O.Imprime("...")`, `O.Variable("x", 5)`, `O.Usa(Construccion.For, "...")`, `O.Funcion("doble", [21], 42)`, etc.
- **Solucion**: las pruebas la ejecutan para validar el nivel.

Después ejecuta `dotnet test`.

Para revisar las pantallas sin abrir la ventana, ejecuta `AventuraBit.App.exe --capturas <carpeta>`, que guarda capturas PNG de varias pantallas.
