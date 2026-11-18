using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AventuraBit.App.Controles;

namespace AventuraBit.App.Pantallas;

/// <summary>Portada: Bit saluda y pide el nombre la primera vez.</summary>
public sealed class PantallaInicio : UserControl
{
    private readonly MainWindow _ventana;
    private readonly TextBox _nombre;

    public PantallaInicio(MainWindow ventana)
    {
        _ventana = ventana;
        var p = App.Progreso;

        var bit = new DibujoBit { Width = 230, Height = 250, Margin = new Thickness(0, 0, 0, 10) };
        var flotar = new TranslateTransform();
        bit.RenderTransform = flotar;
        flotar.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-8, 8, TimeSpan.FromSeconds(1.6))
        {
            AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        });
        bit.Loaded += (_, _) => { bit.PonerAnimo(Animo.Feliz); bit.Mirar(0, 1); };

        var titulo = new TextBlock
        {
            Text = "La Aventura de Bit",
            FontFamily = new FontFamily("Segoe UI Black"),
            FontSize = 64,
            Foreground = Graficos.Brocha("#1E3A8A"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.White, BlurRadius = 0, ShadowDepth = 4, Direction = 300, Opacity = 1 },
        };
        var subtitulo = new TextBlock
        {
            Text = "Aprende a programar en Luau, el lenguaje de Roblox",
            FontSize = 22, FontWeight = FontWeights.SemiBold, Foreground = Graficos.Brocha("#1E40AF"),
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 30),
        };

        _nombre = new TextBox
        {
            FontSize = 26, FontWeight = FontWeights.Bold, Width = 340, Padding = new Thickness(14, 8, 14, 8),
            HorizontalContentAlignment = HorizontalAlignment.Center, MaxLength = 20,
            BorderBrush = Graficos.Brocha("#3B82F6"), BorderThickness = new Thickness(3),
        };
        _nombre.KeyDown += (_, e) => { if (e.Key == Key.Enter) Empezar(); };

        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(bit);
        panel.Children.Add(titulo);
        panel.Children.Add(subtitulo);

        var tarjeta = new Border { Style = (Style)Application.Current.Resources["Tarjeta"], Padding = new Thickness(36, 24, 36, 24), HorizontalAlignment = HorizontalAlignment.Center };
        var interior = new StackPanel();
        tarjeta.Child = interior;

        if (string.IsNullOrWhiteSpace(p.NombreJugador))
        {
            interior.Children.Add(new TextBlock
            {
                Text = "¡Hola! Soy Bit. ¿Cómo te llamas?", FontSize = 24, FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 14),
            });
            interior.Children.Add(_nombre);
            var empezar = new Button { Style = (Style)Application.Current.Resources["BotonJuego"], FontSize = 24, Margin = new Thickness(0, 16, 0, 0),
                Content = Graficos.IconoTexto(Ico.Jugar, "¡Empezar la aventura!", 22) };
            empezar.Click += (_, _) => Empezar();
            interior.Children.Add(empezar);
            Loaded += (_, _) => _nombre.Focus();
        }
        else
        {
            interior.Children.Add(new TextBlock
            {
                Text = $"¡Hola de nuevo, {p.NombreJugador}!", FontSize = 28, FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 6),
            });
            var est = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) };
            est.Children.Add(Graficos.Estrella(28, true));
            est.Children.Add(new TextBlock { Text = $" {p.TotalEstrellas} estrellas conseguidas", FontSize = 20, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            interior.Children.Add(est);
            var seguir = new Button { Style = (Style)Application.Current.Resources["BotonJuego"], FontSize = 24,
                Content = Graficos.IconoTexto(Ico.Jugar, "¡Seguir jugando!", 22) };
            seguir.Click += (_, _) => _ventana.IrMapa();
            interior.Children.Add(seguir);
            var otro = new Button
            {
                Content = $"No soy {p.NombreJugador}", Margin = new Thickness(0, 12, 0, 0), HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Graficos.Brocha("#64748B"),
                FontSize = 14, Cursor = Cursors.Hand,
            };
            otro.Click += (_, _) => CambiarJugador();
            interior.Children.Add(otro);
        }
        panel.Children.Add(tarjeta);
        Content = panel;
    }

    private void Empezar()
    {
        var n = _nombre.Text.Trim();
        if (n.Length == 0)
        {
            Graficos.Sacudir(_nombre);
            _nombre.Focus();
            return;
        }
        App.Progreso.NombreJugador = n;
        App.GuardarProgreso();
        _ventana.IrMapa();
    }

    private void CambiarJugador()
    {
        var r = MessageBox.Show(
            "¿Quieres empezar una partida nueva? Se borrarán las estrellas y el código guardado de este jugador.",
            "Nueva partida", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (r != MessageBoxResult.Yes) return;
        App.Progreso.NombreJugador = "";
        App.Progreso.Estrellas.Clear();
        App.Progreso.Codigo.Clear();
        App.GuardarProgreso();
        _ventana.IrInicio();
    }
}
