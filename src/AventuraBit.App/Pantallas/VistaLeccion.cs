using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AventuraBit.App.Controles;
using AventuraBit.Motor.Juego;

namespace AventuraBit.App.Pantallas;

/// <summary>Lección: diapositivas explicativas y después unas preguntas.</summary>
public sealed class VistaLeccion : UserControl
{
    private readonly PantallaNivel _pantalla;
    private readonly Nivel _nivel;
    private readonly StackPanel _puntos = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) };
    private readonly ContentControl _contenido = new();
    private readonly StackPanel _botones = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
    private readonly DibujoBit _bit = new() { Width = 150, Height = 165, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 10, 24, 0) };
    private int _paso;
    private int _errores;
    private bool _respondida;

    public VistaLeccion(PantallaNivel pantalla, Nivel nivel)
    {
        _pantalla = pantalla;
        _nivel = nivel;

        var tarjeta = new Border { Style = (Style)Application.Current.Resources["Tarjeta"], Padding = new Thickness(30, 22, 30, 22), MaxWidth = 1100 };
        var dock = new DockPanel();
        tarjeta.Child = dock;
        DockPanel.SetDock(_puntos, Dock.Top);
        dock.Children.Add(_puntos);
        DockPanel.SetDock(_botones, Dock.Bottom);
        dock.Children.Add(_botones);
        var centro = new DockPanel();
        centro.Children.Add(_bit);
        centro.Children.Add(new ScrollViewer { Content = _contenido, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        dock.Children.Add(centro);
        Content = tarjeta;

        Mostrar();
    }

    internal void IrA(int paso)
    {
        _paso = paso;
        Mostrar();
    }

    private int TotalPasos => _nivel.Diapositivas.Count + _nivel.Preguntas.Count;

    private void Mostrar()
    {
        _puntos.Children.Clear();
        for (int i = 0; i < TotalPasos; i++)
        {
            bool esPregunta = i >= _nivel.Diapositivas.Count;
            _puntos.Children.Add(new Border
            {
                Width = i == _paso ? 34 : 14, Height = 14, CornerRadius = new CornerRadius(7), Margin = new Thickness(3, 0, 3, 0),
                Background = Graficos.Brocha(i < _paso ? "#22C55E" : i == _paso ? (esPregunta ? "#F59E0B" : "#3B82F6") : "#E2E8F0"),
            });
        }
        _bit.Reiniciar();
        if (_paso < _nivel.Diapositivas.Count) MostrarDiapositiva(_nivel.Diapositivas[_paso]);
        else MostrarPregunta(_nivel.Preguntas[_paso - _nivel.Diapositivas.Count]);
    }

    private void MostrarDiapositiva(Diapositiva d)
    {
        _bit.PonerAnimo(_paso == 0 ? Animo.Feliz : Animo.Normal);
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = d.Titulo, Style = (Style)Application.Current.Resources["Titulo"], FontSize = 32, Margin = new Thickness(0, 0, 0, 12), TextWrapping = TextWrapping.Wrap });
        sp.Children.Add(new TextBlock { Text = d.Texto, FontSize = 20, TextWrapping = TextWrapping.Wrap, LineHeight = 30 });
        if (d.Codigo is not null) sp.Children.Add(BloqueCodigo(d.Codigo));
        if (d.EnRoblox is not null) sp.Children.Add(CajaRoblox(d.EnRoblox));
        _contenido.Content = sp;
        Graficos.Aparecer(sp);

        _botones.Children.Clear();
        if (_paso > 0) _botones.Children.Add(Boton("BotonBlanco", Ico.Atras, "Anterior", () => { _paso--; Mostrar(); }));
        bool ultima = _paso == _nivel.Diapositivas.Count - 1;
        _botones.Children.Add(Boton(ultima ? "BotonNaranja" : "BotonAzul", Ico.Adelante, ultima ? "¡A las preguntas!" : "Siguiente", () => { _paso++; Mostrar(); }));
    }

    private void MostrarPregunta(Pregunta p)
    {
        _respondida = false;
        _bit.PonerAnimo(Animo.Sorpresa);
        var sp = new StackPanel();
        int numero = _paso - _nivel.Diapositivas.Count + 1;
        sp.Children.Add(new TextBlock { Text = $"Pregunta {numero} de {_nivel.Preguntas.Count}", Style = (Style)Application.Current.Resources["Subtitulo"], FontSize = 16 });
        sp.Children.Add(new TextBlock { Text = p.Enunciado, Style = (Style)Application.Current.Resources["Titulo"], FontSize = 28, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 8) });
        if (p.Codigo is not null) sp.Children.Add(BloqueCodigo(p.Codigo));

        var opciones = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
        bool opcionesDeCodigo = p.Opciones.Any(o => o.Contains('(') || o.Contains('"') || o.Contains('\n'));
        var explicacion = new TextBlock { FontSize = 19, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 14, 0, 0), FontWeight = FontWeights.SemiBold };
        for (int i = 0; i < p.Opciones.Length; i++)
        {
            int indice = i;
            string texto = p.Opciones[i];
            bool esCodigo = texto.Contains('\n');
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["BotonBlanco"], MinWidth = 220, Margin = new Thickness(0, 0, 12, 12),
                Content = new TextBlock
                {
                    Text = texto, FontSize = esCodigo ? 17 : 19, TextWrapping = TextWrapping.Wrap, MaxWidth = 380,
                    FontFamily = opcionesDeCodigo ? (FontFamily)Application.Current.Resources["FuenteCodigo"] : new FontFamily("Segoe UI"),
                    FontWeight = opcionesDeCodigo ? FontWeights.Normal : FontWeights.Bold,
                },
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };
            b.Click += (_, _) => Responder(p, indice, b, explicacion);
            opciones.Children.Add(b);
        }
        sp.Children.Add(opciones);
        sp.Children.Add(explicacion);
        _contenido.Content = sp;
        Graficos.Aparecer(sp);

        _botones.Children.Clear();
        _botones.Children.Add(Boton("BotonBlanco", Ico.Libro, "Repasar la explicación", () => { _paso = 0; Mostrar(); }));
    }

    private void Responder(Pregunta p, int indice, Button boton, TextBlock explicacion)
    {
        if (_respondida) return;
        if (indice == p.Correcta)
        {
            _respondida = true;
            boton.Background = Graficos.Brocha("#22C55E");
            boton.BorderBrush = Graficos.Brocha("#15803D");
            ((TextBlock)boton.Content).Foreground = Brushes.White;
            _bit.PonerAnimo(Animo.Feliz);
            _bit.Saltar();
            explicacion.Text = "¡Correcto! " + p.Explicacion;
            explicacion.Foreground = Graficos.Brocha("#15803D");
            Graficos.Aparecer(explicacion);
            _botones.Children.Clear();
            bool ultima = _paso == TotalPasos - 1;
            _botones.Children.Add(Boton("BotonJuego", Ico.Adelante, ultima ? "¡Terminar!" : "Siguiente pregunta", () =>
            {
                if (ultima) Terminar();
                else { _paso++; Mostrar(); }
            }));
        }
        else
        {
            _errores++;
            boton.IsEnabled = false;
            boton.Background = Graficos.Brocha("#FECACA");
            boton.BorderBrush = Graficos.Brocha("#EF4444");
            Graficos.Sacudir(boton);
            _bit.PonerAnimo(Animo.Triste);
            explicacion.Text = "¡Casi! Piénsalo otra vez. Si quieres, repasa la explicación.";
            explicacion.Foreground = Graficos.Brocha("#B91C1C");
        }
    }

    private void Terminar()
    {
        int estrellas = _errores == 0 ? 3 : _errores == 1 ? 2 : 1;
        var logros = new List<string> { "Has repasado toda la explicación." };
        logros.Add(_errores == 0 ? "¡Todas las preguntas a la primera!" : $"Has respondido todo bien ({_errores} fallo{(_errores > 1 ? "s" : "")} por el camino).");
        var consejos = _errores == 0 ? new List<string>() : new List<string> { "Repite la lección y acierta todo a la primera para las 3 estrellas." };
        _pantalla.MostrarResultado(estrellas, logros, consejos);
        // Si repite la lección, empieza de cero.
        _paso = 0;
        _errores = 0;
        Mostrar();
    }

    private static Button Boton(string estilo, string icono, string texto, Action accion)
    {
        var b = new Button { Style = (Style)Application.Current.Resources[estilo], Content = Graficos.IconoTexto(icono, texto, 18), Margin = new Thickness(8, 4, 0, 0) };
        b.Click += (_, _) => accion();
        return b;
    }

    public static FrameworkElement BloqueCodigo(string codigo)
    {
        var ed = EditorLua.Crear(soloLectura: true, tamano: 19);
        ed.Text = codigo;
        ed.Background = Graficos.Brocha("#F8FAFC");
        int lineas = codigo.Split('\n').Length;
        ed.Height = lineas * 19 * 1.32 + 18;
        ed.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        ed.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        ed.IsHitTestVisible = true;
        ed.TextArea.Caret.CaretBrush = Brushes.Transparent;
        return new Border
        {
            Background = Graficos.Brocha("#F8FAFC"), BorderBrush = Graficos.Brocha("#CBD5E1"), BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(14), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 16, 0, 0), Child = ed,
        };
    }

    private static FrameworkElement CajaRoblox(string texto)
    {
        var sp = new StackPanel();
        sp.Children.Add(new Border
        {
            Background = Graficos.Brocha("#E11D48"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 3, 10, 3),
            HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 8),
            Child = new TextBlock { Text = "EN ROBLOX", Foreground = Brushes.White, FontWeight = FontWeights.Black, FontSize = 13 },
        });
        sp.Children.Add(new TextBlock { Text = texto, Foreground = Brushes.White, FontSize = 18, TextWrapping = TextWrapping.Wrap, LineHeight = 26 });
        return new Border
        {
            Background = Graficos.Brocha("#1E293B"), CornerRadius = new CornerRadius(16), Padding = new Thickness(18, 14, 18, 16),
            Margin = new Thickness(0, 16, 0, 0), Child = sp,
        };
    }
}
