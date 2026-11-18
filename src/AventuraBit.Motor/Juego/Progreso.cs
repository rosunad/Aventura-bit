using System.Text.Json;

namespace AventuraBit.Motor.Juego;

/// <summary>Lo que se guarda entre partidas: nombre, estrellas y código de cada nivel.</summary>
public sealed class Progreso
{
    public string NombreJugador { get; set; } = "";
    public Dictionary<string, int> Estrellas { get; set; } = new();
    public Dictionary<string, string> Codigo { get; set; } = new();

    public int TotalEstrellas => Estrellas.Values.Sum();

    public int EstrellasDe(string id) => Estrellas.TryGetValue(id, out var e) ? e : 0;

    public bool Completado(string id) => Estrellas.ContainsKey(id);

    public void Registrar(string id, int estrellas)
    {
        if (!Estrellas.TryGetValue(id, out var antes) || estrellas > antes)
            Estrellas[id] = estrellas;
    }

    /// <summary>Un nivel se abre cuando el anterior está completado.</summary>
    public bool Desbloqueado(Nivel nivel, IReadOnlyList<Nivel> todos)
    {
        int i = todos.ToList().FindIndex(n => n.Id == nivel.Id);
        return i <= 0 || Completado(todos[i - 1].Id) || nivel.EsLibre;
    }

    private static readonly JsonSerializerOptions Opciones = new() { WriteIndented = true };

    public static string RutaPorDefecto =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AventuraBit", "progreso.json");

    public static Progreso Cargar(string ruta)
    {
        try
        {
            if (File.Exists(ruta))
                return JsonSerializer.Deserialize<Progreso>(File.ReadAllText(ruta)) ?? new Progreso();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // Si el fichero está estropeado, se empieza de cero.
        }
        return new Progreso();
    }

    public void Guardar(string ruta)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        File.WriteAllText(ruta, JsonSerializer.Serialize(this, Opciones));
    }
}
