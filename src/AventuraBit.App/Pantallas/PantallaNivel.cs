using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AventuraBit.App.Controles;
using AventuraBit.Motor.Juego;

namespace AventuraBit.App.Pantallas;

/// <summary>Marco común de un nivel: cabecera, contenido (lección o código) y ventana de resultado.</summary>
public sealed class PantallaNivel : UserControl
{
    private readonly MainWindow _ventana;
    private readonly Nivel _nivel;
    private readonly Grid _capas = new();
    private Border? _resultado;

    public PantallaNivel(MainWindow ventana, Nivel nivel)
    {
        _ventana = ventana;
        _nivel = nivel;
        var mundo = Catalogo.Mundos.FirstOrDefault(m => m.Numero == nivel.Mundo);
        string color = mundo?.Color ?? "#8B5CF6", oscuro = mundo?.ColorOscuro ?? "#6D28D9";

        var raiz = new DockPanel { Margin = new Thickness(18, 12, 18, 14) };

        var cabecera = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
        var volver = new Button { Style = (Style)Application.Current.Resources["BotonBlanco"], Content = Graficos.IconoTexto(Ico.Atras, "Mapa", 16) };
        volver.Click += (_, _) => _ventana.IrMapa();
        cabecera.Children.Add(volver);
        var insignia = new Border
        {
            Background = Graficos.Brocha(color), BorderBrush = Graficos.Brocha(oscuro), BorderThickness = new Thickness(0, 0, 0, 4),
            CornerRadius = new CornerRadius(14), Padding = new Thickness(14, 6, 14, 6), Margin = new Thickness(14, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = nivel.EsLibre ? "Zona libre" : $"{mundo?.Nombre} · {nivel.Etiqueta}",
                Foreground = Brushes.White, FontWeight = FontWeights.Black, FontSize = 16,
            },
        };
        cabecera.Children.Add(insignia);
        cabecera.Children.Add(new TextBlock { Text = nivel.Titulo, Style = (Style)Application.Current.Resources["Titulo"], FontSize = 28, VerticalAlignment = VerticalAlignment.Center });
        DockPanel.SetDock(cabecera, Dock.Top);
        raiz.Children.Add(cabecera);

        FrameworkElement cuerpo = nivel.Tipo == TipoNivel.Leccion ? new VistaLeccion(this, nivel) : new VistaCodigo(this, nivel);
        raiz.Children.Add(cuerpo);

        _capas.Children.Add(raiz);
        Content = _capas;
    }

    /// <summary>Muestra la ventana de nivel superado y guarda las estrellas.</summary>
    public void MostrarResultado(int estrellas, IEnumerable<string> logros, IEnumerable<string> consejos)
    {
        int antes = App.Progreso.EstrellasDe(_nivel.Id);
        bool primeraVez = !App.Progreso.Completado(_nivel.Id);
        App.Progreso.Registrar(_nivel.Id, estrellas);
        App.GuardarProgreso();

        if (_resultado is not null) _capas.Children.Remove(_resultado);

        var tarjeta = new Border
        {
            Style = (Style)Application.Current.Resources["Tarjeta"], Width = 620, Padding = new Thickness(30, 24, 30, 24),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
        };
        var sp = new StackPanel();
        tarjeta.Child = sp;

        var bit = new DibujoBit { Width = 110, Height = 120 };
        bit.Loaded += (_, _) => { bit.PonerAnimo(Animo.Feliz); bit.Saltar(); };
        sp.Children.Add(bit);
        sp.Children.Add(new TextBlock
        {
            Text = estrellas == 3 ? "¡Perfecto!" : _nivel.Tipo == TipoNivel.Leccion ? "¡Lección completada!" : "¡Nivel superado!",
            Style = (Style)Application.Current.Resources["Titulo"], FontSize = 34, HorizontalAlignment = HorizontalAlignment.Center,
        });

        var filaEstrellas = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 6) };
        for (int i = 0; i < 3; i++)
        {
            var e = Graficos.Estrella(i == 1 ? 78 : 62, i < estrellas);
            e.Margin = new Thickness(6, i == 1 ? 0 : 14, 6, 0);
            filaEstrellas.Children.Add(e);
            Graficos.Aparecer(e, 250 + i * 280);
        }
        sp.Children.Add(filaEstrellas);
        if (!primeraVez && estrellas > antes)
            sp.Children.Add(new TextBlock { Text = "¡Has mejorado tu récord!", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Graficos.Brocha("#B45309"), HorizontalAlignment = HorizontalAlignment.Center });

        foreach (var l in logros)
            sp.Children.Add(Linea(Ico.Ok, l, "#15803D"));
        var lista = consejos.ToList();
        if (lista.Count > 0)
        {
            sp.Children.Add(new TextBlock { Text = "Para conseguir más estrellas:", FontWeight = FontWeights.Bold, FontSize = 16, Margin = new Thickness(0, 12, 0, 2) });
            foreach (var c in lista)
                sp.Children.Add(Linea(Ico.Bombilla, c, "#B45309"));
        }

        var botones = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 18, 0, 0) };
        var repetir = new Button { Style = (Style)Application.Current.Resources["BotonBlanco"], Content = Graficos.IconoTexto(Ico.Reiniciar, "Mejorarlo", 17) };
        repetir.Click += (_, _) => CerrarResultado();
        botones.Children.Add(repetir);
        var mapa = new Button { Style = (Style)Application.Current.Resources["BotonAzul"], Content = Graficos.IconoTexto(Ico.Casa, "Mapa", 17) };
        mapa.Click += (_, _) => _ventana.IrMapa();
        botones.Children.Add(mapa);
        var siguiente = Catalogo.Siguiente(_nivel);
        if (siguiente is not null)
        {
            var sig = new Button { Style = (Style)Application.Current.Resources["BotonJuego"], FontSize = 19, Content = Graficos.IconoTexto(Ico.Adelante, "Siguiente nivel", 19) };
            sig.Click += (_, _) => _ventana.AbrirNivel(siguiente);
            botones.Children.Add(sig);
            sig.Loaded += (_, _) => sig.Focus();
        }
        sp.Children.Add(botones);

        _resultado = new Border { Background = new SolidColorBrush(Color.FromArgb(0x99, 0x0F, 0x17, 0x2A)), Child = tarjeta };
        _capas.Children.Add(_resultado);
        Graficos.Aparecer(tarjeta);
    }

    public void CerrarResultado()
    {
        if (_resultado is null) return;
        _capas.Children.Remove(_resultado);
        _resultado = null;
    }

    private static FrameworkElement Linea(string icono, string texto, string color)
    {
        var dp = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
        var ic = Graficos.Icono(icono, 16, Graficos.Brocha(color));
        ic.VerticalAlignment = VerticalAlignment.Top;
        ic.Margin = new Thickness(0, 3, 10, 0);
        dp.Children.Add(ic);
        dp.Children.Add(new TextBlock { Text = texto, TextWrapping = TextWrapping.Wrap, FontSize = 15 });
        return dp;
    }
}
