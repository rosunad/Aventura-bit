using AventuraBit.Motor.Mundo;

namespace AventuraBit.Motor.Juego;

public enum TipoNivel
{
    /// <summary>Explicación con diapositivas y preguntas.</summary>
    Leccion,

    /// <summary>Ordenar (y sangrar) líneas de código ya escritas.</summary>
    Ordenar,

    /// <summary>Escribir o completar código en el editor.</summary>
    Codigo,
}

public sealed record Diapositiva(string Titulo, string Texto, string? Codigo = null, string? EnRoblox = null);

public sealed record Pregunta(string Enunciado, string[] Opciones, int Correcta, string Explicacion, string? Codigo = null);

/// <summary>Un mapa en el que se prueba el programa. Un nivel puede tener varios para obligar a usar if o bucles.</summary>
public sealed class Variante
{
    public string[] Mapa { get; init; } = Nivel.MapaPorDefecto;
    public Pos? Inicio { get; init; }

    /// <summary>Variables que el juego crea antes de ejecutar el código del niño.</summary>
    public Dictionary<string, object?> Globales { get; init; } = new();

    public List<Objetivo> Objetivos { get; init; } = new();
    public string? Nombre { get; init; }
}

public sealed class MundoInfo
{
    public required int Numero { get; init; }
    public required string Nombre { get; init; }
    public required string Tema { get; init; }
    public required string Color { get; init; }
    public required string ColorOscuro { get; init; }
    public required string Descripcion { get; init; }
}

public sealed class Nivel
{
    public static readonly string[] MapaPorDefecto =
    {
        "~~~~~~~",
        "~.....~",
        "~..R..~",
        "~.....~",
        "~~~~~~~",
    };

    public required string Id { get; init; }
    public required int Mundo { get; init; }
    public required int Numero { get; init; }
    public required string Titulo { get; init; }
    public required TipoNivel Tipo { get; init; }

    /// <summary>Lo que dice Bit al empezar el nivel.</summary>
    public string Introduccion { get; init; } = "";

    public List<Diapositiva> Diapositivas { get; init; } = new();
    public List<Pregunta> Preguntas { get; init; } = new();

    public List<Variante> Variantes { get; init; } = new();
    public List<Objetivo> Objetivos { get; init; } = new();
    public string CodigoInicial { get; init; } = "";
    public string Solucion { get; init; } = "";
    public string[] Pistas { get; init; } = Array.Empty<string>();
    public int? LineasOptimas { get; init; }

    /// <summary>En los niveles de ordenar, las líneas llegan sin sangría y el niño tiene que ponerla.</summary>
    public bool OrdenarConSangria { get; init; }

    /// <summary>Nivel libre, sin objetivos.</summary>
    public bool EsLibre { get; init; }

    public IReadOnlyList<Variante> VariantesEfectivas =>
        Variantes.Count > 0 ? Variantes : new[] { new Variante { Mapa = MapaPorDefecto } };

    public string Etiqueta => $"{Mundo}-{Numero}";
}
