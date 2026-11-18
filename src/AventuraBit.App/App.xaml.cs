using System.Windows;
using AventuraBit.Motor.Juego;

namespace AventuraBit.App;

public partial class App : Application
{
    public static Progreso Progreso { get; private set; } = new();

    public static string RutaProgreso { get; private set; } = Progreso.RutaPorDefecto;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length >= 2 && e.Args[0] == "--capturas")
        {
            // Modo de prueba: usa un progreso temporal y guarda imágenes de las pantallas.
            RutaProgreso = System.IO.Path.Combine(e.Args[1], "progreso-prueba.json");
            Progreso = new Progreso();
            _ = Capturas.Ejecutar(e.Args[1]);
            return;
        }
        Progreso = Progreso.Cargar(RutaProgreso);
        MainWindow = new MainWindow();
        MainWindow.Show();
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show("¡Uy! Algo ha fallado dentro del juego:\n\n" + args.Exception.Message, "La Aventura de Bit",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };
    }

    public static void GuardarProgreso()
    {
        try
        {
            Progreso.Guardar(RutaProgreso);
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
        {
            // Si no se puede guardar, se sigue jugando igualmente.
        }
    }
}
