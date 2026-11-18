using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using AventuraBit.Motor.Mundo;

namespace AventuraBit.App.Controles;

/// <summary>Dibuja el tablero de Bit y anima lo que pasa en él.</summary>
public sealed class VistaMundo : UserControl
{
    private const double T = 64;
    private readonly Canvas _lienzo = new();
    private readonly Canvas _capaPintura = new();
    private readonly Canvas _capaGemas = new();
    private readonly Canvas _capaBit = new();
    private readonly DibujoBit _bit = new() { Width = T, Height = T };
    private readonly Border _bocadillo;
    private readonly TextBlock _textoBocadillo;
    private readonly Dictionary<Pos, FrameworkElement> _gemas = new();
    private readonly Dictionary<Pos, Rectangle> _pinturas = new();
    private readonly TranslateTransform _empujon = new();
    private Pos _posBit;
    private int _versionBocadillo;

    public VistaMundo()
    {
        _textoBocadillo = new TextBlock
        {
            FontSize = 15, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, MaxWidth = 220,
            Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
        };
        _bocadillo = new Border
        {
            Background = Brushes.White, CornerRadius = new CornerRadius(14), Padding = new Thickness(10, 6, 10, 6),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)), BorderThickness = new Thickness(2.5),
            Child = _textoBocadillo, Visibility = Visibility.Collapsed,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 8, ShadowDepth = 2, Opacity = 0.25 },
        };
        _bit.RenderTransform = _empujon;
        _capaBit.Children.Add(_bit);
        _capaBit.Children.Add(_bocadillo);
        Content = new Viewbox { Stretch = Stretch.Uniform, Child = _lienzo, StretchDirection = StretchDirection.Both };
    }

    public void Cargar(EstadoMundo e)
    {
        _lienzo.Children.Clear();
        _capaPintura.Children.Clear();
        _capaGemas.Children.Clear();
        _gemas.Clear();
        _pinturas.Clear();
        _lienzo.Width = e.Ancho * T;
        _lienzo.Height = e.Alto * T;

        for (int y = 0; y < e.Alto; y++)
            for (int x = 0; x < e.Ancho; x++)
                DibujarCasilla(e, x, y);

        _lienzo.Children.Add(_capaPintura);
        foreach (var kv in e.Pintura) Pintar(kv.Key, kv.Value, animar: false);

        // La bandera encima de la pintura
        for (int y = 0; y < e.Alto; y++)
            for (int x = 0; x < e.Ancho; x++)
                if (e.Casillas[x, y] == TipoCasilla.Meta) DibujarBandera(x, y);

        _lienzo.Children.Add(_capaGemas);
        foreach (var g in e.Gemas) DibujarGema(g);

        if (_capaBit.Parent is Panel p) p.Children.Remove(_capaBit);
        _lienzo.Children.Add(_capaBit);
        _bit.Reiniciar();
        _bit.BeginAnimation(Canvas.LeftProperty, null);
        _bit.BeginAnimation(Canvas.TopProperty, null);
        _posBit = e.Bit;
        Canvas.SetLeft(_bit, e.Bit.X * T);
        Canvas.SetTop(_bit, e.Bit.Y * T);
        OcultarBocadillo();
    }

    // ---------------- Dibujo ----------------
    private void DibujarCasilla(EstadoMundo e, int x, int y)
    {
        var tipo = e.Casillas[x, y];
        var cesped = (x + y) % 2 == 0 ? Color.FromRgb(0x86, 0xEF, 0xAC) : Color.FromRgb(0x6E, 0xE7, 0x9B);
        switch (tipo)
        {
            case TipoCasilla.Agua:
            {
                var r = Rect(x, y, 0, new LinearGradientBrush(Color.FromRgb(0x7D, 0xD3, 0xFC), Color.FromRgb(0x0E, 0xA5, 0xE9), 90));
                _lienzo.Children.Add(r);
                var ola = new Path
                {
                    Data = Geometry.Parse("M 8,26 q 6,-6 12,0 t 12,0 t 12,0 t 12,0 M 2,44 q 6,-6 12,0 t 12,0 t 12,0 t 12,0"),
                    Stroke = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)), StrokeThickness = 3,
                    StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
                };
                Poner(ola, x * T, y * T);
                break;
            }
            case TipoCasilla.Muro:
            {
                _lienzo.Children.Add(Rect(x, y, 0, new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))));
                var piedra = new Rectangle
                {
                    Width = T - 4, Height = T - 4, RadiusX = 10, RadiusY = 10,
                    Fill = new LinearGradientBrush(Color.FromRgb(0xD6, 0xD3, 0xD1), Color.FromRgb(0x9C, 0x94, 0x8E), 90),
                    Stroke = new SolidColorBrush(Color.FromRgb(0x78, 0x71, 0x6C)), StrokeThickness = 3,
                };
                Poner(piedra, x * T + 2, y * T + 2);
                var grietas = new Path
                {
                    Data = Geometry.Parse("M 6,22 H 58 M 6,42 H 58 M 26,6 V 22 M 40,22 V 42 M 22,42 V 58"),
                    Stroke = new SolidColorBrush(Color.FromArgb(0x80, 0x78, 0x71, 0x6C)), StrokeThickness = 2.5,
                };
                Poner(grietas, x * T, y * T);
                break;
            }
            default:
            {
                _lienzo.Children.Add(Rect(x, y, 0, new SolidColorBrush(cesped)));
                if ((x * 7 + y * 3) % 5 == 0)
                {
                    var hierba = new Path
                    {
                        Data = Geometry.Parse("M 14,50 l 3,-8 l 3,8 M 44,20 l 3,-8 l 3,8"),
                        Stroke = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E)), StrokeThickness = 2,
                    };
                    Poner(hierba, x * T, y * T);
                }
                break;
            }
        }
    }

    private static Rectangle Rect(int x, int y, double margen, Brush relleno)
    {
        var r = new Rectangle { Width = T - 2 * margen + 0.5, Height = T - 2 * margen + 0.5, Fill = relleno };
        Canvas.SetLeft(r, x * T + margen);
        Canvas.SetTop(r, y * T + margen);
        return r;
    }

    private void Poner(UIElement e, double x, double y, Panel? capa = null)
    {
        Canvas.SetLeft(e, x);
        Canvas.SetTop(e, y);
        (capa ?? _lienzo).Children.Add(e);
    }

    private void DibujarBandera(int x, int y)
    {
        var g = new Canvas { Width = T, Height = T };
        g.Children.Add(new Ellipse { Width = 30, Height = 10, Fill = new SolidColorBrush(Color.FromArgb(0x55, 0, 0, 0)), Margin = new Thickness(17, 50, 0, 0) });
        g.Children.Add(new Rectangle { Width = 5, Height = 44, RadiusX = 2, RadiusY = 2, Fill = new SolidColorBrush(Color.FromRgb(0x78, 0x35, 0x0F)), Margin = new Thickness(22, 10, 0, 0) });
        var tela = new Path
        {
            Data = Geometry.Parse("M 27,10 L 52,18 L 27,28 Z"),
            Fill = new LinearGradientBrush(Color.FromRgb(0xF8, 0x71, 0x71), Color.FromRgb(0xDC, 0x26, 0x26), 0),
            Stroke = new SolidColorBrush(Color.FromRgb(0x99, 0x1B, 0x1B)), StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round,
        };
        var ondear = new SkewTransform(0, 0, 27, 18);
        tela.RenderTransform = ondear;
        ondear.BeginAnimation(SkewTransform.AngleYProperty,
            new DoubleAnimation(-6, 6, TimeSpan.FromSeconds(0.9)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() });
        g.Children.Add(tela);
        Poner(g, x * T, y * T);
    }

    private void DibujarGema(Pos p)
    {
        var g = new Canvas { Width = T, Height = T, RenderTransformOrigin = new Point(0.5, 0.5) };
        g.Children.Add(new Ellipse { Width = 26, Height = 8, Fill = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0)), Margin = new Thickness(19, 50, 0, 0) });
        var cuerpo = new Canvas();
        cuerpo.Children.Add(new Polygon
        {
            Points = PointCollection.Parse("32,10 48,26 32,48 16,26"),
            Fill = new LinearGradientBrush(Color.FromRgb(0xF0, 0xAB, 0xFC), Color.FromRgb(0x93, 0x33, 0xEA), 70),
            Stroke = new SolidColorBrush(Color.FromRgb(0x6B, 0x21, 0xA8)), StrokeThickness = 2.5, StrokeLineJoin = PenLineJoin.Round,
        });
        cuerpo.Children.Add(new Polygon { Points = PointCollection.Parse("32,14 40,26 32,26 24,26"), Fill = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)) });
        var flotar = new TranslateTransform();
        cuerpo.RenderTransform = flotar;
        flotar.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-3, 3, TimeSpan.FromSeconds(0.8 + (p.X % 3) * 0.15))
        {
            AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase(),
        });
        g.Children.Add(cuerpo);
        g.RenderTransform = new ScaleTransform(1, 1);
        Poner(g, p.X * T, p.Y * T, _capaGemas);
        _gemas[p] = g;
    }

    private void Pintar(Pos p, string color, bool animar, TimeSpan? duracion = null)
    {
        var hex = Colores.Hex.TryGetValue(color, out var h) ? h : "#94A3B8";
        var brocha = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        if (!_pinturas.TryGetValue(p, out var r))
        {
            r = new Rectangle
            {
                Width = T - 10, Height = T - 10, RadiusX = 12, RadiusY = 12,
                Stroke = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)), StrokeThickness = 3,
            };
            Poner(r, p.X * T + 5, p.Y * T + 5, _capaPintura);
            _pinturas[p] = r;
        }
        r.Fill = brocha;
        if (animar)
            r.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 0.92, duracion ?? TimeSpan.FromMilliseconds(250)));
        else
            r.Opacity = 0.92;
    }

    // ---------------- Animaciones ----------------
    public void Aplicar(EventoMundo e, TimeSpan duracion)
    {
        var d = TimeSpan.FromMilliseconds(Math.Max(60, duracion.TotalMilliseconds * 0.8));
        switch (e)
        {
            case EventoMover m:
            {
                int dx = m.Hasta.X - m.Desde.X, dy = m.Hasta.Y - m.Desde.Y;
                _bit.Mirar(dx, dy);
                _bit.PonerAnimo(Animo.Normal);
                var facil = new QuadraticEase { EasingMode = EasingMode.EaseInOut };
                _bit.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(m.Desde.X * T, m.Hasta.X * T, d) { EasingFunction = facil });
                _bit.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(m.Desde.Y * T, m.Hasta.Y * T, d) { EasingFunction = facil });
                _posBit = m.Hasta;
                ColocarBocadillo();
                break;
            }
            case EventoChoque c:
                _bit.Mirar(c.Dx, c.Dy);
                _bit.PonerAnimo(Animo.Sorpresa);
                _empujon.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(0, c.Dx * 12, TimeSpan.FromMilliseconds(Math.Min(120, d.TotalMilliseconds / 2))) { AutoReverse = true });
                _empujon.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(0, c.Dy * 12, TimeSpan.FromMilliseconds(Math.Min(120, d.TotalMilliseconds / 2))) { AutoReverse = true });
                Decir("¡Ay! Un muro", duracion);
                break;
            case EventoRecoger r:
                if (r.Exito && _gemas.TryGetValue(r.En, out var gema))
                {
                    _gemas.Remove(r.En);
                    var escala = (ScaleTransform)gema.RenderTransform;
                    var crecer = new DoubleAnimation(1, 1.8, d);
                    escala.BeginAnimation(ScaleTransform.ScaleXProperty, crecer);
                    escala.BeginAnimation(ScaleTransform.ScaleYProperty, crecer);
                    var desaparecer = new DoubleAnimation(1, 0, d);
                    desaparecer.Completed += (_, _) => _capaGemas.Children.Remove(gema);
                    gema.BeginAnimation(OpacityProperty, desaparecer);
                    _bit.PonerAnimo(Animo.Feliz);
                    _bit.Saltar();
                }
                else
                {
                    _bit.PonerAnimo(Animo.Triste);
                    Decir("Aquí no hay ninguna gema...", duracion);
                }
                break;
            case EventoPintar p:
                Pintar(p.En, p.Color, animar: true, d);
                _bit.PonerAnimo(Animo.Feliz);
                break;
            case EventoDecir dc:
                Decir(dc.Texto, TimeSpan.FromMilliseconds(Math.Max(1500, duracion.TotalMilliseconds * 3)));
                break;
            case EventoCaer:
                _bit.PonerAnimo(Animo.Triste);
                _bit.Caer(TimeSpan.FromMilliseconds(Math.Max(400, d.TotalMilliseconds)));
                Decir("¡Plof!", duracion);
                break;
        }
    }

    public void PonerAnimo(Animo animo)
    {
        _bit.PonerAnimo(animo);
        if (animo == Animo.Feliz) _bit.Saltar();
    }

    public void Decir(string texto, TimeSpan duracion)
    {
        _textoBocadillo.Text = texto;
        _bocadillo.Visibility = Visibility.Visible;
        ColocarBocadillo();
        int version = ++_versionBocadillo;
        var espera = TimeSpan.FromMilliseconds(Math.Max(900, duracion.TotalMilliseconds));
        _ = Task.Delay(espera).ContinueWith(_ =>
        {
            if (version == _versionBocadillo) _bocadillo.Visibility = Visibility.Collapsed;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void OcultarBocadillo()
    {
        _versionBocadillo++;
        _bocadillo.Visibility = Visibility.Collapsed;
    }

    private void ColocarBocadillo()
    {
        _bocadillo.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var tam = _bocadillo.DesiredSize;
        double x = _posBit.X * T + T / 2 - tam.Width / 2;
        x = Math.Clamp(x, 2, Math.Max(2, _lienzo.Width - tam.Width - 2));
        double y = _posBit.Y * T - tam.Height + 6;
        if (y < 2) y = _posBit.Y * T + T - 4;
        Canvas.SetLeft(_bocadillo, x);
        Canvas.SetTop(_bocadillo, y);
    }
}
