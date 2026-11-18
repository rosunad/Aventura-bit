namespace AventuraBit.Motor.Juego;

public sealed class Calificacion
{
    public required bool Superado { get; init; }
    public required int Estrellas { get; init; }
    public required ResultadoEjecucion Mostrar { get; init; }
    public required IReadOnlyList<ResultadoEjecucion> Resultados { get; init; }
    public List<string> Logros { get; } = new();
    public List<string> Consejos { get; } = new();
}

/// <summary>Ejecuta el código en todos los mapas del nivel y reparte las estrellas.</summary>
public static class Evaluador
{
    public static Calificacion Calificar(Nivel nivel, string codigo, int intentosFallidos = 0, bool grabar = true)
    {
        var resultados = new List<ResultadoEjecucion>();
        for (int i = 0; i < nivel.VariantesEfectivas.Count; i++)
        {
            var r = Ejecutor.Ejecutar(nivel, codigo, i, grabar);
            resultados.Add(r);
            if (r.ErrorDeSintaxis) break; // el mismo error saldría en todos
        }

        var fallo = resultados.FirstOrDefault(r => !r.Superado);
        if (fallo is not null || nivel.EsLibre)
        {
            return new Calificacion
            {
                Superado = false,
                Estrellas = 0,
                Mostrar = fallo ?? resultados[0],
                Resultados = resultados,
            };
        }

        var c = new Calificacion { Superado = true, Estrellas = 0, Mostrar = resultados[0], Resultados = resultados };
        int estrellas = 1;
        c.Logros.Add("¡Has cumplido todos los objetivos!");

        var it = resultados[0].Interprete!;
        var consejos = RevisorEstilo.Revisar(codigo, resultados[0].Programa!, it.NombresDelJuego)
            .Select(x => x.Mensaje)
            .Concat(resultados.SelectMany(r => r.Interprete!.Avisos.Select(a => a.Mensaje)))
            .Distinct()
            .ToList();
        if (consejos.Count == 0)
        {
            estrellas++;
            c.Logros.Add("Código bien escrito: sangría correcta y nombres en camelCase.");
        }
        else
        {
            c.Consejos.AddRange(consejos);
        }

        if (nivel.Tipo == TipoNivel.Ordenar)
        {
            if (intentosFallidos <= 1)
            {
                estrellas++;
                c.Logros.Add(intentosFallidos == 0 ? "¡A la primera!" : "Casi a la primera.");
            }
            else c.Consejos.Add("Para la tercera estrella, intenta acertar el orden en uno o dos intentos.");
        }
        else
        {
            int lineas = Analisis.LineasDeCodigo(codigo);
            if (nivel.LineasOptimas is not { } optimas || lineas <= optimas)
            {
                estrellas++;
                c.Logros.Add(nivel.LineasOptimas is null ? "¡Programa perfecto!" : $"Programa cortito: {lineas} líneas.");
            }
            else
            {
                c.Consejos.Add($"Tu programa tiene {lineas} líneas de código. Se puede hacer en {optimas} o menos. ¿Puedes acortarlo?");
            }
        }

        return new Calificacion
        {
            Superado = true,
            Estrellas = estrellas,
            Mostrar = resultados[0],
            Resultados = resultados,
        }.ConLogros(c);
    }

    private static Calificacion ConLogros(this Calificacion destino, Calificacion origen)
    {
        destino.Logros.AddRange(origen.Logros);
        destino.Consejos.AddRange(origen.Consejos);
        return destino;
    }
}
