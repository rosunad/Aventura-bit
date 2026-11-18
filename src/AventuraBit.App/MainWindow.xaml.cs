using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using AventuraBit.App.Pantallas;
using AventuraBit.Motor.Juego;

namespace AventuraBit.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => DibujarNubes();
        IrInicio();
    }

    public void IrInicio() => Mostrar(new PantallaInicio(this));

    public void IrMapa() => Mostrar(new PantallaMapa(this));

    public void AbrirNivel(Nivel nivel) => Mostrar(new PantallaNivel(this, nivel));

    private void Mostrar(FrameworkElement pantalla)
    {
        Contenido.Content = pantalla;
        pantalla.Opacity = 0;
        pantalla.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)));
    }

    private void DibujarNubes()
    {
        var azar = new Random(7);
        for (int i = 0; i < 7; i++)
        {
            var nube = new Canvas { Opacity = 0.75 };
            double s = 0.6 + azar.NextDouble() * 0.9;
            foreach (var (x, y, w, h) in new[] { (0.0, 20.0, 90.0, 50.0), (35.0, 0.0, 80.0, 70.0), (85.0, 18.0, 90.0, 52.0) })
                nube.Children.Add(new Ellipse
                {
                    Width = w * s, Height = h * s, Fill = Brushes.White, Margin = new Thickness(x * s, y * s, 0, 0),
                });
            Canvas.SetTop(nube, 20 + azar.NextDouble() * 500);
            Nubes.Children.Add(nube);
            double ancho = Math.Max(ActualWidth, 1400);
            var a = new DoubleAnimation(-250, ancho + 50, TimeSpan.FromSeconds(80 + azar.NextDouble() * 80))
            {
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(-azar.NextDouble() * 120),
            };
            nube.BeginAnimation(Canvas.LeftProperty, a);
        }
    }
}
