using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AventuraBit.App.Controles;
using AventuraBit.Motor.Juego;

namespace AventuraBit.App.Pantallas;

/// <summary>Mapa con los mundos y sus niveles.</summary>
public sealed class PantallaMapa : UserControl
{
    private readonly MainWindow _ventana;

    public PantallaMapa(MainWindow ventana)
    {
        _ventana = ventana;
        var p = App.Progreso;
        var todos = Catalogo.Niveles;
        var siguiente = todos.FirstOrDefault(n => !p.Completado(n.Id) && p.Desbloqueado(n, todos));

        var raiz = new DockPanel { Margin = new Thickness(24, 16, 24, 0) };

        // Cabecera
        var cabecera = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        var casa = new Button { Style = Estilo("BotonBlanco"), Content = Graficos.IconoTexto(Ico.Casa, "Inicio", 16) };
        casa.Click += (_, _) => _ventana.IrInicio();
        cabecera.Children.Add(casa);
        var bitMini = new DibujoBit { Width = 58, Height = 62, Margin = new Thickness(16, 0, 8, 0) };
        bitMini.Loaded += (_, _) => bitMini.PonerAnimo(Animo.Feliz);
        cabecera.Children.Add(bitMini);
        var saludo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        saludo.Children.Add(new TextBlock { Text = $"¡Vamos, {p.NombreJugador}!", Style = Estilo<TextBlock>("Titulo"), FontSize = 30 });
        saludo.Children.Add(new TextBlock
        {
            Text = siguiente is null ? "¡Has completado todos los niveles! Eres un crack de Luau." : $"Siguiente reto: {siguiente.Etiqueta} · {siguiente.Titulo}",
            Style = Estilo<TextBlock>("Subtitulo"), FontSize = 16,
        });
        cabecera.Children.Add(saludo);
        var marcador = new Border
        {
            Background = Brushes.White, CornerRadius = new CornerRadius(20), Padding = new Thickness(16, 8, 18, 8),
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center,
            Effect = (System.Windows.Media.Effects.Effect)Application.Current.Resources["Sombra"],
        };
        var m = new StackPanel { Orientation = Orientation.Horizontal };
        m.Children.Add(Graficos.Estrella(30, true));
        m.Children.Add(new TextBlock
        {
            Text = $"  {p.TotalEstrellas} / {todos.Count * 3}", FontSize = 24, FontWeight = FontWeights.Black, VerticalAlignment = VerticalAlignment.Center,
        });
        marcador.Child = m;
        DockPanel.SetDock(marcador, Dock.Right);
        cabecera.Children.Insert(0, marcador);
        DockPanel.SetDock(cabecera, Dock.Top);
        raiz.Children.Add(cabecera);

        // Mundos
        var lista = new StackPanel { Margin = new Thickness(0, 0, 0, 30) };
        foreach (var mundo in Catalogo.Mundos)
            lista.Children.Add(TarjetaMundo(mundo, Catalogo.DelMundo(mundo.Numero).ToList(), siguiente));
        lista.Children.Add(TarjetaLibre());

        raiz.Children.Add(new ScrollViewer
        {
            Content = lista, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(0, 0, 8, 0),
        });
        Content = raiz;

        // Llevar el scroll al mundo del siguiente nivel
        Loaded += (_, _) =>
        {
            if (siguiente is null) return;
            int i = siguiente.Mundo - 1;
            if (i > 0 && i < lista.Children.Count)
                ((FrameworkElement)lista.Children[i]).BringIntoView();
        };
    }

    private static Style Estilo(string clave) => (Style)Application.Current.Resources[clave];
    private static Style Estilo<T>(string clave) => (Style)Application.Current.Resources[clave];

    private FrameworkElement TarjetaMundo(MundoInfo mundo, List<Nivel> niveles, Nivel? siguiente)
    {
        var p = App.Progreso;
        bool abierto = p.Desbloqueado(niveles[0], Catalogo.Niveles);
        int estrellas = niveles.Sum(n => p.EstrellasDe(n.Id));

        var tarjeta = new Border
        {
            CornerRadius = new CornerRadius(28), Padding = new Thickness(22, 18, 22, 18), Margin = new Thickness(0, 8, 0, 8),
            Background = new LinearGradientBrush(((SolidColorBrush)Graficos.Brocha(mundo.Color)).Color, ((SolidColorBrush)Graficos.Brocha(mundo.ColorOscuro)).Color, 20),
            Effect = (System.Windows.Media.Effects.Effect)Application.Current.Resources["Sombra"],
            Opacity = abierto ? 1 : 0.6,
        };
        var dock = new DockPanel();
        tarjeta.Child = dock;

        var izquierda = new StackPanel { Width = 290, VerticalAlignment = VerticalAlignment.Center };
        var fila = new StackPanel { Orientation = Orientation.Horizontal };
        var numero = new Border
        {
            Width = 54, Height = 54, CornerRadius = new CornerRadius(27), Background = Brushes.White,
            Child = new TextBlock { Text = mundo.Numero.ToString(), FontSize = 28, FontWeight = FontWeights.Black, Foreground = Graficos.Brocha(mundo.ColorOscuro),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
        };
        fila.Children.Add(numero);
        var textos = new StackPanel { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        textos.Children.Add(new TextBlock { Text = mundo.Nombre, FontSize = 22, FontWeight = FontWeights.Black, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, MaxWidth = 220 });
        textos.Children.Add(new TextBlock { Text = mundo.Tema, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF)) });
        fila.Children.Add(textos);
        izquierda.Children.Add(fila);
        izquierda.Children.Add(new TextBlock
        {
            Text = abierto ? mundo.Descripcion : "Supera el mundo anterior para abrir este.",
            Foreground = Brushes.White, FontSize = 14, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 10, 0, 4),
        });
        var resumen = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2, 4, 0, 0) };
        resumen.Children.Add(Graficos.Estrella(20, true));
        resumen.Children.Add(new TextBlock { Text = $" {estrellas} / {niveles.Count * 3}", Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 15, VerticalAlignment = VerticalAlignment.Center });
        izquierda.Children.Add(resumen);
        dock.Children.Add(izquierda);

        var botones = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        foreach (var n in niveles) botones.Children.Add(BotonNivel(n, mundo, n == siguiente));
        dock.Children.Add(botones);
        return tarjeta;
    }

    private FrameworkElement BotonNivel(Nivel n, MundoInfo mundo, bool esSiguiente)
    {
        var p = App.Progreso;
        bool abierto = p.Desbloqueado(n, Catalogo.Niveles);
        bool hecho = p.Completado(n.Id);

        var contenido = new StackPanel();
        string icono = n.Tipo switch { TipoNivel.Leccion => Ico.Libro, TipoNivel.Ordenar => Ico.Ordenar, _ => Ico.Codigo };
        contenido.Children.Add(Graficos.Icono(abierto ? icono : Ico.Candado, 22, Graficos.Brocha(abierto ? mundo.ColorOscuro : "#94A3B8")));
        contenido.Children.Add(new TextBlock
        {
            Text = n.Etiqueta, FontSize = 22, FontWeight = FontWeights.Black, HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Graficos.Brocha(abierto ? "#1E293B" : "#94A3B8"),
        });
        contenido.Children.Add(Graficos.FilaEstrellas(p.EstrellasDe(n.Id), 16));

        var b = new Button
        {
            Style = Estilo("BotonBlanco"), Width = 104, Height = 108, Padding = new Thickness(4), Margin = new Thickness(6),
            Content = contenido, IsEnabled = abierto,
            ToolTip = $"{n.Titulo}\n{(n.Tipo switch { TipoNivel.Leccion => "Lección", TipoNivel.Ordenar => "Ordena el código", _ => "Escribe código" })}",
        };
        if (hecho) b.BorderBrush = Graficos.Brocha("#FBBF24");
        b.Click += (_, _) => _ventana.AbrirNivel(n);

        var contenedor = new StackPanel();
        contenedor.Children.Add(b);
        contenedor.Children.Add(new TextBlock
        {
            Text = n.Titulo, Width = 112, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center,
            Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, FontSize = 13, Margin = new Thickness(0, -2, 0, 4),
        });
        if (esSiguiente)
        {
            var st = new ScaleTransform(1, 1);
            b.RenderTransformOrigin = new Point(0.5, 0.5);
            b.RenderTransform = st;
            var a = new DoubleAnimation(1, 1.08, TimeSpan.FromSeconds(0.7)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
        }
        return contenedor;
    }

    private FrameworkElement TarjetaLibre()
    {
        var tarjeta = new Border
        {
            CornerRadius = new CornerRadius(28), Padding = new Thickness(22, 18, 22, 18), Margin = new Thickness(0, 8, 0, 8),
            Background = new LinearGradientBrush(Color.FromRgb(0x64, 0x74, 0x8B), Color.FromRgb(0x33, 0x41, 0x55), 20),
            Effect = (System.Windows.Media.Effects.Effect)Application.Current.Resources["Sombra"],
        };
        var dock = new DockPanel();
        var boton = new Button { Style = Estilo("BotonMorado"), FontSize = 20, Content = Graficos.IconoTexto(Ico.Codigo, "Entrar", 20), VerticalAlignment = VerticalAlignment.Center };
        boton.Click += (_, _) => _ventana.AbrirNivel(Catalogo.ZonaLibre);
        DockPanel.SetDock(boton, Dock.Right);
        dock.Children.Add(boton);
        var textos = new StackPanel();
        textos.Children.Add(new TextBlock { Text = "Zona libre", FontSize = 24, FontWeight = FontWeights.Black, Foreground = Brushes.White });
        textos.Children.Add(new TextBlock { Text = "Sin retos ni estrellas: un mapa para experimentar con todo lo que aprendas.", FontSize = 15, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap });
        dock.Children.Add(textos);
        tarjeta.Child = dock;
        return tarjeta;
    }
}
