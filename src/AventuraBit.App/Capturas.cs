using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AventuraBit.App.Pantallas;
using AventuraBit.Motor.Juego;

namespace AventuraBit.App;

/// <summary>Modo de prueba (--capturas carpeta): recorre pantallas y las guarda como PNG.</summary>
internal static class Capturas
{
    public static async Task Ejecutar(string carpeta)
    {
        Directory.CreateDirectory(carpeta);
        var v = new MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual, Left = -20000, Top = 0, ShowActivated = false,
            Width = 1440, Height = 880,
        };
        Application.Current.MainWindow = v;
        v.Show();
        try
        {
            await Esperar(600);
            Guardar(v, carpeta, "01-inicio");

            App.Progreso.NombreJugador = "Leo";
            foreach (var n in Catalogo.Niveles.Take(9)) App.Progreso.Registrar(n.Id, n.Numero % 3 + 1);
            v.IrMapa();
            await Esperar(600);
            Guardar(v, carpeta, "02-mapa");

            v.AbrirNivel(Nivel("1-1"));
            await Esperar(700);
            Guardar(v, carpeta, "03-leccion");
            var leccion = Buscar<VistaLeccion>((DependencyObject)v.Content)!;
            leccion.IrA(2);
            await Esperar(700);
            Guardar(v, carpeta, "03b-diapositiva");
            leccion.IrA(6);
            await Esperar(700);
            Guardar(v, carpeta, "03c-pregunta");

            v.AbrirNivel(Nivel("1-5"));
            await Esperar(500);
            var vp = Buscar<VistaCodigo>((DependencyObject)v.Content)!;
            vp.EjecutarParaPrueba(paso: true);
            await Esperar(600);
            vp.PasoParaPrueba();
            await Esperar(300);
            vp.PasoParaPrueba();
            await Esperar(500);
            Guardar(v, carpeta, "03d-paso-a-paso");

            await Codigo(v, carpeta, "04-codigo-1-4", "1-4", Nivel("1-4").Solucion, esperarFin: true);
            await Codigo(v, carpeta, "05-cajas-3-4", "3-4", Nivel("3-4").Solucion.Replace("puntos += 10\nprint", "puntos += 10\n print"), esperarFin: true, cerrarResultado: true);
            await Codigo(v, carpeta, "06-error", "1-6", Nivel("1-6").CodigoInicial.Replace("moverDerecha(\n", "moverDerecha()\n"), esperarFin: true);

            v.AbrirNivel(Nivel("4-4"));
            await Esperar(700);
            Guardar(v, carpeta, "07-ordenar");

            await Codigo(v, carpeta, "08-bucle", "5-6", Nivel("5-6").Solucion, esperarFin: false, ms: 2500);
        }
        catch (Exception e)
        {
            File.WriteAllText(Path.Combine(carpeta, "error.txt"), e.ToString());
        }
        Application.Current.Shutdown();
    }

    private static Nivel Nivel(string id) => Catalogo.Niveles.Single(n => n.Id == id);

    private static async Task Codigo(MainWindow v, string carpeta, string nombre, string id, string codigo, bool esperarFin, bool cerrarResultado = false, int ms = 0)
    {
        App.Progreso.Codigo[id] = codigo;
        v.AbrirNivel(Nivel(id));
        await Esperar(500);
        var pantalla = (PantallaNivel)((System.Windows.Controls.ContentControl)v.FindName("Contenido")).Content;
        var vista = Buscar<VistaCodigo>(pantalla)!;
        vista.EjecutarParaPrueba();
        if (esperarFin)
        {
            for (int i = 0; i < 200 && vista.Ejecutando; i++) await Esperar(100);
            await Esperar(cerrarResultado ? 300 : 1400);
            if (cerrarResultado) pantalla.CerrarResultado();
            await Esperar(200);
        }
        else await Esperar(ms);
        Guardar(v, carpeta, nombre);
    }

    private static T? Buscar<T>(DependencyObject raiz) where T : DependencyObject
    {
        if (raiz is T t) return t;
        int n = VisualTreeHelper.GetChildrenCount(raiz);
        for (int i = 0; i < n; i++)
            if (Buscar<T>(VisualTreeHelper.GetChild(raiz, i)) is { } r) return r;
        return null;
    }

    private static Task Esperar(int ms) => Task.Delay(ms);

    private static void Guardar(Window v, string carpeta, string nombre)
    {
        var contenido = (FrameworkElement)v.Content;
        contenido.UpdateLayout();
        var rtb = new RenderTargetBitmap((int)contenido.ActualWidth, (int)contenido.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(contenido);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(Path.Combine(carpeta, nombre + ".png"));
        enc.Save(fs);
    }
}
