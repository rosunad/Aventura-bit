using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace AventuraBit.App.Controles;

/// <summary>Pequeñas piezas visuales reutilizables: estrellas, iconos, animaciones.</summary>
public static class Graficos
{
    private static readonly Geometry GeometriaEstrella = CrearEstrella();

    private static Geometry CrearEstrella()
    {
        var g = new StreamGeometry();
        using (var c = g.Open())
        {
            for (int i = 0; i < 10; i++)
            {
                double ang = -Math.PI / 2 + i * Math.PI / 5;
                double r = i % 2 == 0 ? 50 : 22;
                var p = new Point(50 + r * Math.Cos(ang), 52 + r * Math.Sin(ang));
                if (i == 0) c.BeginFigure(p, true, true);
                else c.LineTo(p, true, true);
            }
        }
        g.Freeze();
        return g;
    }

    public static FrameworkElement Estrella(double tamano, bool llena)
    {
        var path = new Path
        {
            Data = GeometriaEstrella,
            Stretch = Stretch.Uniform,
            Width = tamano,
            Height = tamano,
            StrokeThickness = Math.Max(1.5, tamano / 14),
            StrokeLineJoin = PenLineJoin.Round,
            Fill = llena
                ? new LinearGradientBrush(Color.FromRgb(0xFD, 0xE0, 0x47), Color.FromRgb(0xF5, 0x9E, 0x0B), 90)
                : new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
            Stroke = llena ? new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09)) : new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1, 1),
        };
        return path;
    }

    public static StackPanel FilaEstrellas(int cuantas, double tamano, int total = 3)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        for (int i = 0; i < total; i++)
        {
            var e = Estrella(tamano, i < cuantas);
            e.Margin = new Thickness(1, 0, 1, 0);
            sp.Children.Add(e);
        }
        return sp;
    }

    public static TextBlock Icono(string glifo, double tamano, Brush? color = null) => new()
    {
        Text = glifo,
        FontFamily = new FontFamily("Segoe MDL2 Assets"),
        FontSize = tamano,
        Foreground = color ?? Brushes.White,
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Center,
    };

    /// <summary>Contenido de botón con icono y texto.</summary>
    public static StackPanel IconoTexto(string glifo, string texto, double tamano = 17)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        var ic = Icono(glifo, tamano - 1);
        ic.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("Foreground")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(Button), 1),
        });
        ic.Margin = new Thickness(0, 0, 8, 0);
        sp.Children.Add(ic);
        var tb = new TextBlock { Text = texto, VerticalAlignment = VerticalAlignment.Center, FontSize = tamano, FontWeight = FontWeights.Bold };
        tb.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("Foreground")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(Button), 1),
        });
        sp.Children.Add(tb);
        return sp;
    }

    public static SolidColorBrush Brocha(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;

    private static readonly HashSet<string> FuncionesJuego = new()
    {
        "moverDerecha", "moverIzquierda", "moverArriba", "moverAbajo", "recogerGema", "pintar", "decir",
        "hayMuro", "hayAgua", "hayGema", "enMeta", "colorSuelo", "gemasRecogidas",
    };

    private static readonly HashSet<string> FuncionesBase = new()
    {
        "print", "wait", "esperar", "task", "tostring", "tonumber", "type", "typeof", "ipairs", "pairs", "math", "string", "table",
    };

    /// <summary>Una línea de código con los mismos colores que el editor.</summary>
    public static TextBlock CodigoColoreado(string linea, double tamano)
    {
        var tb = new TextBlock { FontFamily = (FontFamily)Application.Current.Resources["FuenteCodigo"], FontSize = tamano };
        List<Motor.Lua.Token> tokens;
        try { tokens = Motor.Lua.Lexer.Analizar(linea); }
        catch (Motor.Lua.ErrorLua) { tb.Text = linea; return tb; }

        int pos = 0;
        foreach (var t in tokens.Where(t => t.Tipo != Motor.Lua.TipoToken.FinArchivo))
        {
            int ini = Math.Clamp(t.Columna - 1, pos, linea.Length);
            if (ini > pos) tb.Inlines.Add(new System.Windows.Documents.Run(linea[pos..ini]));
            var (color, negrita) = t.Tipo switch
            {
                Motor.Lua.TipoToken.Cadena or Motor.Lua.TipoToken.Interpolada => ("#16A34A", false),
                Motor.Lua.TipoToken.Numero => ("#EA580C", false),
                Motor.Lua.TipoToken.PalabraClave when t.Texto is "true" or "false" or "nil" => ("#DC2626", true),
                Motor.Lua.TipoToken.PalabraClave => ("#C026D3", true),
                Motor.Lua.TipoToken.Nombre when FuncionesJuego.Contains(t.Texto) => ("#2563EB", true),
                Motor.Lua.TipoToken.Nombre when FuncionesBase.Contains(t.Texto) => ("#0891B2", false),
                _ => ("#1E293B", false),
            };
            int fin = Math.Min(linea.Length, ini + t.Texto.Length);
            tb.Inlines.Add(new System.Windows.Documents.Run(linea[ini..fin])
            {
                Foreground = Brocha(color), FontWeight = negrita ? FontWeights.Bold : FontWeights.Normal,
            });
            pos = fin;
        }
        if (pos < linea.Length)
            tb.Inlines.Add(new System.Windows.Documents.Run(linea[pos..]) { Foreground = Brocha("#94A3B8"), FontStyle = FontStyles.Italic });
        return tb;
    }

    /// <summary>Hace "pop": crece desde cero con un rebote.</summary>
    public static void Aparecer(FrameworkElement e, double retrasoMs = 0)
    {
        var st = new ScaleTransform(0, 0);
        e.RenderTransformOrigin = new Point(0.5, 0.5);
        e.RenderTransform = st;
        var a = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(450))
        {
            BeginTime = TimeSpan.FromMilliseconds(retrasoMs),
            EasingFunction = new BackEase { Amplitude = 0.6, EasingMode = EasingMode.EaseOut },
        };
        st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
    }

    /// <summary>Sacude un elemento de lado a lado (para "¡no!").</summary>
    public static void Sacudir(FrameworkElement e)
    {
        var t = new TranslateTransform();
        e.RenderTransform = t;
        var a = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(400) };
        double[] xs = { 0, -10, 10, -8, 8, -4, 0 };
        for (int i = 0; i < xs.Length; i++)
            a.KeyFrames.Add(new LinearDoubleKeyFrame(xs[i], KeyTime.FromPercent(i / (double)(xs.Length - 1))));
        t.BeginAnimation(TranslateTransform.XProperty, a);
    }

    /// <summary>Latido: crece un poco y vuelve.</summary>
    public static void Latir(FrameworkElement e)
    {
        var st = new ScaleTransform(1, 1);
        e.RenderTransformOrigin = new Point(0.5, 0.5);
        e.RenderTransform = st;
        var a = new DoubleAnimation(1, 1.18, TimeSpan.FromMilliseconds(160)) { AutoReverse = true };
        st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
    }
}

/// <summary>Iconos de Segoe MDL2 Assets.</summary>
public static class Ico
{
    public const string Jugar = "";
    public const string Parar = "";
    public const string Reiniciar = "";
    public const string Atras = "";
    public const string Adelante = "";
    public const string Bombilla = "";
    public const string Codigo = "";
    public const string Libro = "";
    public const string Ordenar = "";
    public const string Candado = "";
    public const string Ok = "";
    public const string Cancelar = "";
    public const string Pasos = "";
    public const string Casa = "";
    public const string Ojo = "";
    public const string Arriba = "";
    public const string Abajo = "";
    public const string Izquierda = "";
    public const string Derecha = "";
    public const string Estrella = "";
    public const string Info = "";
}
