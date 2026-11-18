using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AventuraBit.App.Controles;
using AventuraBit.Motor.Juego;
using AventuraBit.Motor.Mundo;
using ICSharpCode.AvalonEdit;

namespace AventuraBit.App.Pantallas;

/// <summary>Nivel de código: misión, editor (o líneas para ordenar), mundo, cajas y consola.</summary>
public sealed class VistaCodigo : UserControl
{
    private sealed class LineaOrden
    {
        public required string Texto { get; init; }
        public int Sangria { get; set; }
        public Border? Fila { get; set; }
    }

    private enum TipoMensaje { Info, Error, Exito, Aviso }

    private static readonly double[] Velocidades = { 900, 560, 340, 170, 60 };

    private readonly PantallaNivel _pantalla;
    private readonly Nivel _nivel;
    private readonly TextEditor? _editor;
    private readonly MarcadorLineas? _marcador;
    private readonly List<LineaOrden> _lineas = new();
    private readonly StackPanel _listaOrden = new();
    private readonly VistaMundo _mundo = new();
    private readonly TextBlock _nombreMapa = new() { FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Graficos.Brocha("#64748B"), VerticalAlignment = VerticalAlignment.Center };
    private readonly WrapPanel _cajas = new();
    private readonly StackPanel _consola = new();
    private readonly ScrollViewer _scrollConsola;
    private readonly Border _mensaje = new() { CornerRadius = new CornerRadius(16), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 10, 0, 0), Visibility = Visibility.Collapsed };
    private readonly StackPanel _objetivos = new();
    private readonly StackPanel _pistas = new();
    private readonly Button _botonPista;
    private readonly Button _botonSolucion;
    private readonly Button _ejecutar;
    private readonly Button _pasoAPaso;
    private readonly Button _parar;
    private readonly Button _reiniciar;
    private readonly Slider _velocidad = new() { Minimum = 0, Maximum = 4, Value = 2, IsSnapToTickEnabled = true, TickFrequency = 1, Width = 130, VerticalAlignment = VerticalAlignment.Center };
    private readonly Dictionary<Objetivo, TextBlock> _iconosObjetivo = new();
    private readonly DibujoBit _bitMision = new() { Width = 64, Height = 70, VerticalAlignment = VerticalAlignment.Top };

    private CancellationTokenSource? _cancelar;
    private TaskCompletionSource<bool>? _siguientePaso;
    private bool _modoPaso;
    private bool _ejecutando;
    private int _intentosFallidos;
    private bool _vioSolucion;
    private List<VariableVista> _ultimasVariables = new();

    public VistaCodigo(PantallaNivel pantalla, Nivel nivel)
    {
        _pantalla = pantalla;
        _nivel = nivel;

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 380 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.05, GridUnitType.Star), MinWidth = 340 });

        // ---------- Columna izquierda: misión ----------
        var mision = new StackPanel();
        var cab = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        cab.Children.Add(_bitMision);
        cab.Children.Add(new TextBlock { Text = "Tu misión", Style = Estilo("Titulo"), FontSize = 24, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });
        mision.Children.Add(cab);
        mision.Children.Add(new TextBlock { Text = nivel.Introduccion, TextWrapping = TextWrapping.Wrap, FontSize = 16.5, LineHeight = 24 });

        if (!nivel.EsLibre)
        {
            mision.Children.Add(Seccion("Objetivos"));
            mision.Children.Add(_objetivos);
            ConstruirObjetivos();

            if (nivel.VariantesEfectivas.Count > 1)
            {
                mision.Children.Add(Seccion($"Se prueba en {nivel.VariantesEfectivas.Count} mapas"));
                var mapas = new WrapPanel();
                for (int i = 0; i < nivel.VariantesEfectivas.Count; i++)
                {
                    int k = i;
                    var v = nivel.VariantesEfectivas[i];
                    var b = new Button { Style = Estilo("BotonFlecha"), Width = double.NaN, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(0, 0, 6, 6),
                        Content = new TextBlock { Text = v.Nombre ?? $"Mapa {i + 1}", FontWeight = FontWeights.Bold, Margin = new Thickness(8, 0, 8, 0) } };
                    b.Click += (_, _) => { if (!_ejecutando) VerVariante(k); };
                    mapas.Children.Add(b);
                }
                mision.Children.Add(mapas);
            }
        }

        if (nivel.Pistas.Length > 0)
        {
            mision.Children.Add(Seccion("Pistas"));
            mision.Children.Add(_pistas);
            _botonPista = new Button { Style = Estilo("BotonNaranja"), FontSize = 15, Content = Graficos.IconoTexto(Ico.Bombilla, $"Pedir una pista (0/{nivel.Pistas.Length})", 15), HorizontalAlignment = HorizontalAlignment.Left };
            _botonPista.Click += (_, _) => MostrarPista();
            mision.Children.Add(_botonPista);
        }
        else _botonPista = new Button();
        _botonSolucion = new Button { Style = Estilo("BotonBlanco"), FontSize = 14, Content = Graficos.IconoTexto(Ico.Ojo, "Ver una solución", 14), HorizontalAlignment = HorizontalAlignment.Left, Visibility = Visibility.Collapsed };
        _botonSolucion.Click += (_, _) => VerSolucion();
        mision.Children.Add(_botonSolucion);

        mision.Children.Add(Seccion("Comandos que conoces"));
        mision.Children.Add(ListaComandos());

        var tarjetaMision = Tarjeta(new ScrollViewer { Content = mision, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(0, 0, 6, 0) });
        tarjetaMision.Margin = new Thickness(0, 0, 14, 0);
        grid.Children.Add(tarjetaMision);

        // ---------- Columna central: código ----------
        var centro = new DockPanel();
        var cabCodigo = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        var consejo = new TextBlock
        {
            Text = nivel.Tipo == TipoNivel.Ordenar
                ? (nivel.OrdenarConSangria ? "▲▼ mueven la línea · ◀▶ cambian la sangría" : "▲▼ mueven la línea")
                : "Tab = sangría · Mayús+Tab = quitarla · F5 = ejecutar",
            Style = Estilo("Subtitulo"), FontSize = 13, VerticalAlignment = VerticalAlignment.Center,
        };
        DockPanel.SetDock(consejo, Dock.Right);
        cabCodigo.Children.Add(consejo);
        cabCodigo.Children.Add(new TextBlock { Text = nivel.Tipo == TipoNivel.Ordenar ? "Ordena el código" : "Tu código", Style = Estilo("Titulo"), FontSize = 22 });
        DockPanel.SetDock(cabCodigo, Dock.Top);
        centro.Children.Add(cabCodigo);

        // Barra de botones
        _ejecutar = new Button { Style = Estilo("BotonJuego"), FontSize = 19, Content = Graficos.IconoTexto(Ico.Jugar, "Ejecutar", 19), ToolTip = "Ejecuta el programa (F5)" };
        _ejecutar.Click += (_, _) => PulsarEjecutar();
        _pasoAPaso = new Button { Style = Estilo("BotonAzul"), Content = Graficos.IconoTexto(Ico.Pasos, "Paso a paso", 16), ToolTip = "Ejecuta una instrucción cada vez que pulses" };
        _pasoAPaso.Click += (_, _) => PulsarPaso();
        _parar = new Button { Style = Estilo("BotonRojo"), Content = Graficos.IconoTexto(Ico.Parar, "Parar", 16), Visibility = Visibility.Collapsed };
        _parar.Click += (_, _) => Parar();
        _reiniciar = new Button { Style = Estilo("BotonBlanco"), Content = Graficos.IconoTexto(Ico.Reiniciar, "Reiniciar", 15), ToolTip = "Vuelve a poner el código del principio" };
        _reiniciar.Click += (_, _) => ReiniciarCodigo();

        var barra = new DockPanel { Margin = new Thickness(0, 10, 0, 0) };
        var botones = new WrapPanel();
        botones.Children.Add(_ejecutar);
        botones.Children.Add(_pasoAPaso);
        botones.Children.Add(_parar);
        botones.Children.Add(_reiniciar);
        barra.Children.Add(botones);

        var abajo = new StackPanel();
        abajo.Children.Add(barra);
        abajo.Children.Add(_mensaje);
        DockPanel.SetDock(abajo, Dock.Bottom);
        centro.Children.Add(abajo);

        if (nivel.Tipo == TipoNivel.Ordenar)
        {
            PrepararOrden();
            centro.Children.Add(new ScrollViewer { Content = _listaOrden, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(0, 0, 6, 0) });
        }
        else
        {
            _editor = EditorLua.Crear();
            _editor.Text = App.Progreso.Codigo.TryGetValue(nivel.Id, out var guardado) && !string.IsNullOrWhiteSpace(guardado) ? guardado : nivel.CodigoInicial;
            _marcador = new MarcadorLineas(_editor);
            _editor.TextChanged += (_, _) => { if (!_ejecutando) _marcador.Marcar(0, 0); };
            centro.Children.Add(new Border
            {
                BorderBrush = Graficos.Brocha("#CBD5E1"), BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(14),
                Padding = new Thickness(2), Background = Brushes.White, Child = _editor, ClipToBounds = true,
            });
            Loaded += (_, _) =>
            {
                _editor.Focus();
                _editor.CaretOffset = _editor.Text.Length;
            };
        }
        var tarjetaCodigo = Tarjeta(centro);
        tarjetaCodigo.Margin = new Thickness(0, 0, 14, 0);
        Grid.SetColumn(tarjetaCodigo, 1);
        grid.Children.Add(tarjetaCodigo);

        // ---------- Columna derecha: mundo, cajas y consola ----------
        var derecha = new Grid();
        derecha.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 200 });
        derecha.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        derecha.RowDefinitions.Add(new RowDefinition { Height = new GridLength(170) });

        var mundoDock = new DockPanel();
        var cabMundo = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
        var vel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, ToolTip = "Velocidad de Bit" };
        vel.Children.Add(new TextBlock { Text = "Lento", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Graficos.Brocha("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
        vel.Children.Add(_velocidad);
        vel.Children.Add(new TextBlock { Text = "Rápido", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Graficos.Brocha("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) });
        DockPanel.SetDock(vel, Dock.Right);
        cabMundo.Children.Add(vel);
        _nombreMapa.Margin = new Thickness(10, 0, 0, 0);
        DockPanel.SetDock(_nombreMapa, Dock.Right);
        var titMundo = new StackPanel { Orientation = Orientation.Horizontal };
        titMundo.Children.Add(new TextBlock { Text = "El mundo de Bit", Style = Estilo("Titulo"), FontSize = 20 });
        titMundo.Children.Add(_nombreMapa);
        cabMundo.Children.Add(titMundo);
        DockPanel.SetDock(cabMundo, Dock.Top);
        mundoDock.Children.Add(cabMundo);
        mundoDock.Children.Add(new Border { CornerRadius = new CornerRadius(14), ClipToBounds = true, Child = _mundo });
        var tarjetaMundo = Tarjeta(mundoDock);
        tarjetaMundo.Padding = new Thickness(14);
        derecha.Children.Add(tarjetaMundo);

        var cajasDock = new DockPanel();
        var cabCajas = new TextBlock { Text = "Cajas (variables)", Style = Estilo("Titulo"), FontSize = 17, Margin = new Thickness(0, 0, 0, 6) };
        DockPanel.SetDock(cabCajas, Dock.Top);
        cajasDock.Children.Add(cabCajas);
        cajasDock.Children.Add(new ScrollViewer { Content = _cajas, MaxHeight = 160, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        var tarjetaCajas = Tarjeta(cajasDock);
        tarjetaCajas.Padding = new Thickness(14, 10, 14, 10);
        tarjetaCajas.Margin = new Thickness(0, 12, 0, 12);
        tarjetaCajas.MinHeight = 90;
        Grid.SetRow(tarjetaCajas, 1);
        derecha.Children.Add(tarjetaCajas);

        _scrollConsola = new ScrollViewer { Content = _consola, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var consolaDock = new DockPanel();
        var cabConsola = new TextBlock { Text = "Consola · lo que muestra print", Foreground = Graficos.Brocha("#94A3B8"), FontWeight = FontWeights.Bold, FontSize = 13, Margin = new Thickness(0, 0, 0, 4) };
        DockPanel.SetDock(cabConsola, Dock.Top);
        consolaDock.Children.Add(cabConsola);
        consolaDock.Children.Add(_scrollConsola);
        var tarjetaConsola = new Border
        {
            Background = Graficos.Brocha("#0F172A"), CornerRadius = new CornerRadius(18), Padding = new Thickness(14, 10, 14, 10),
            Effect = (System.Windows.Media.Effects.Effect)Application.Current.Resources["Sombra"], Child = consolaDock,
        };
        Grid.SetRow(tarjetaConsola, 2);
        derecha.Children.Add(tarjetaConsola);
        Grid.SetColumn(derecha, 2);
        grid.Children.Add(derecha);

        Content = grid;
        VerVariante(0);
        PintarCajas(new List<VariableVista>());
        _bitMision.Loaded += (_, _) => _bitMision.PonerAnimo(Animo.Feliz);

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.F5 || (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control))
            {
                PulsarEjecutar();
                e.Handled = true;
            }
            else if (e.Key == Key.F10)
            {
                PulsarPaso();
                e.Handled = true;
            }
        };
    }

    // ---------------- Construcción ----------------
    private static Style Estilo(string clave) => (Style)Application.Current.Resources[clave];

    private static Border Tarjeta(UIElement hijo) => new() { Style = Estilo("Tarjeta"), Child = hijo };

    private static TextBlock Seccion(string texto) => new()
    {
        Text = texto.ToUpperInvariant(), FontSize = 13, FontWeight = FontWeights.Black, Foreground = Graficos.Brocha("#94A3B8"),
        Margin = new Thickness(0, 16, 0, 6),
    };

    private void ConstruirObjetivos()
    {
        _objetivos.Children.Clear();
        _iconosObjetivo.Clear();
        foreach (var o in _nivel.Objetivos) _objetivos.Children.Add(FilaObjetivo(o, o.Descripcion));
        foreach (var v in _nivel.VariantesEfectivas)
            foreach (var o in v.Objetivos)
                _objetivos.Children.Add(FilaObjetivo(o, $"{v.Nombre}: {o.Descripcion}"));
    }

    private FrameworkElement FilaObjetivo(Objetivo o, string texto)
    {
        var dp = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };
        var icono = Graficos.Icono("", 16, Graficos.Brocha("#CBD5E1"));
        icono.VerticalAlignment = VerticalAlignment.Top;
        icono.Margin = new Thickness(0, 2, 8, 0);
        _iconosObjetivo[o] = icono;
        dp.Children.Add(icono);
        dp.Children.Add(new TextBlock { Text = texto, TextWrapping = TextWrapping.Wrap, FontSize = 15, FontWeight = FontWeights.SemiBold });
        return dp;
    }

    private void MarcarObjetivos(Calificacion c)
    {
        foreach (var (o, icono) in _iconosObjetivo)
        {
            var estados = c.Resultados.SelectMany(r => r.Objetivos).Where(x => x.Objetivo == o).ToList();
            if (estados.Count == 0) { PonerIcono(icono, null); continue; }
            PonerIcono(icono, estados.All(x => x.Cumplido));
        }
    }

    private static void PonerIcono(TextBlock icono, bool? ok)
    {
        icono.Text = ok switch { true => Ico.Ok, false => Ico.Cancelar, null => "" };
        icono.Foreground = Graficos.Brocha(ok switch { true => "#16A34A", false => "#DC2626", null => "#CBD5E1" });
        if (ok is not null) Graficos.Latir(icono);
    }

    private FrameworkElement ListaComandos()
    {
        var sp = new StackPanel();
        int hasta = _nivel.EsLibre ? 99 : _nivel.Mundo;
        foreach (var c in Catalogo.Comandos.Where(c => c.DesdeMundo <= hasta))
        {
            var fila = new StackPanel { Margin = new Thickness(0, 0, 0, 6), Background = Brushes.Transparent, Cursor = _editor is null ? null : Cursors.Hand };
            fila.Children.Add(new TextBlock
            {
                Text = c.Firma, FontFamily = (FontFamily)Application.Current.Resources["FuenteCodigo"], FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = Graficos.Brocha("#2563EB"), TextWrapping = TextWrapping.Wrap,
            });
            fila.Children.Add(new TextBlock { Text = c.Descripcion, FontSize = 13, Foreground = Graficos.Brocha("#64748B"), TextWrapping = TextWrapping.Wrap });
            fila.ToolTip = "Doble clic para escribirlo en tu código";
            fila.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ClickCount == 2 && _editor is not null && !_editor.IsReadOnly)
                {
                    _editor.Document.Insert(_editor.CaretOffset, c.Firma);
                    _editor.Focus();
                }
            };
            sp.Children.Add(fila);
        }
        return sp;
    }

    // ---------------- Ordenar ----------------
    private void PrepararOrden()
    {
        var originales = _nivel.Solucion.Replace("\r", "").Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l =>
            {
                int espacios = l.Length - l.TrimStart(' ').Length;
                return new LineaOrden { Texto = l.Trim(), Sangria = _nivel.OrdenarConSangria ? 0 : espacios / 4 };
            }).ToList();

        var azar = new Random();
        List<LineaOrden> mezcla;
        int intentos = 0;
        do
        {
            mezcla = originales.OrderBy(_ => azar.Next()).ToList();
        } while (intentos++ < 20 && mezcla.Select(l => l.Texto).SequenceEqual(originales.Select(l => l.Texto)));
        _lineas.Clear();
        _lineas.AddRange(mezcla);
        PintarOrden();
    }

    private void PintarOrden()
    {
        _listaOrden.Children.Clear();
        for (int i = 0; i < _lineas.Count; i++)
        {
            int k = i;
            var l = _lineas[i];
            var dp = new DockPanel();
            var botones = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            botones.Children.Add(Flecha(Ico.Arriba, "Subir", i > 0, () => Mover(k, -1)));
            botones.Children.Add(Flecha(Ico.Abajo, "Bajar", i < _lineas.Count - 1, () => Mover(k, 1)));
            if (_nivel.OrdenarConSangria)
            {
                botones.Children.Add(Flecha(Ico.Izquierda, "Quitar sangría", l.Sangria > 0, () => Sangrar(k, -1)));
                botones.Children.Add(Flecha(Ico.Derecha, "Poner sangría", l.Sangria < 4, () => Sangrar(k, 1)));
            }
            DockPanel.SetDock(botones, Dock.Right);
            dp.Children.Add(botones);
            dp.Children.Add(new TextBlock
            {
                Text = (i + 1).ToString(), Width = 26, FontFamily = (FontFamily)Application.Current.Resources["FuenteCodigo"],
                Foreground = Graficos.Brocha("#A0AEC0"), FontSize = 16, VerticalAlignment = VerticalAlignment.Center,
            });
            var codigo = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            for (int s = 0; s < l.Sangria; s++)
                codigo.Children.Add(new Border { Width = 26, Height = 26, Margin = new Thickness(0, 0, 6, 0), CornerRadius = new CornerRadius(6), Background = Graficos.Brocha("#E0F2FE") });
            var texto = Graficos.CodigoColoreado(l.Texto, 17);
            texto.VerticalAlignment = VerticalAlignment.Center;
            texto.TextWrapping = TextWrapping.Wrap;
            var celda = new DockPanel();
            DockPanel.SetDock(codigo, Dock.Left);
            celda.Children.Add(codigo);
            celda.Children.Add(texto);
            dp.Children.Add(celda);
            var fila = new Border
            {
                Background = Brushes.White, BorderBrush = Graficos.Brocha("#CBD5E1"), BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12), Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 0, 7), Child = dp,
            };
            l.Fila = fila;
            _listaOrden.Children.Add(fila);
        }
    }

    private Button Flecha(string icono, string ayuda, bool activo, Action accion)
    {
        var b = new Button { Style = Estilo("BotonFlecha"), Content = Graficos.Icono(icono, 14, Graficos.Brocha("#334155")), ToolTip = ayuda, IsEnabled = activo && !_ejecutando };
        b.Click += (_, _) => accion();
        return b;
    }

    private void Mover(int i, int d)
    {
        int j = i + d;
        if (j < 0 || j >= _lineas.Count) return;
        (_lineas[i], _lineas[j]) = (_lineas[j], _lineas[i]);
        OcultarMensaje();
        PintarOrden();
        Graficos.Latir(_lineas[j].Fila!);
    }

    private void Sangrar(int i, int d)
    {
        _lineas[i].Sangria = Math.Clamp(_lineas[i].Sangria + d, 0, 4);
        OcultarMensaje();
        PintarOrden();
    }

    private void MarcarFilaOrden(int ejecutando, int error)
    {
        for (int i = 0; i < _lineas.Count; i++)
        {
            var f = _lineas[i].Fila;
            if (f is null) continue;
            bool esError = i + 1 == error, esActual = i + 1 == ejecutando;
            f.Background = Graficos.Brocha(esError ? "#FEE2E2" : esActual ? "#FEF9C3" : "#FFFFFF");
            f.BorderBrush = Graficos.Brocha(esError ? "#EF4444" : esActual ? "#EAB308" : "#CBD5E1");
        }
        int linea = error > 0 ? error : ejecutando;
        if (linea > 0 && linea <= _lineas.Count) _lineas[linea - 1].Fila?.BringIntoView();
    }

    // ---------------- Código ----------------
    private string CodigoActual() => _editor?.Text
        ?? string.Join("\n", _lineas.Select(l => new string(' ', 4 * l.Sangria) + l.Texto));

    private (int Ejecutando, int Error) _marca;

    private void Marcar(int ejecutando, int error)
    {
        _marca = (ejecutando, error);
        if (_marcador is not null) _marcador.Marcar(ejecutando, error);
        else MarcarFilaOrden(ejecutando, error);
    }

    private void ReiniciarCodigo()
    {
        if (_ejecutando) return;
        if (_editor is not null)
        {
            if (_editor.Text != _nivel.CodigoInicial &&
                MessageBox.Show("¿Seguro que quieres borrar tu código y volver a empezar?", "Empezar de nuevo",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
                return;
            _editor.Text = _nivel.CodigoInicial;
            App.Progreso.Codigo.Remove(_nivel.Id);
            App.GuardarProgreso();
        }
        else PrepararOrden();
        Marcar(0, 0);
        OcultarMensaje();
        VerVariante(0);
        _consola.Children.Clear();
        PintarCajas(new List<VariableVista>());
    }

    private void VerVariante(int i)
    {
        var v = _nivel.VariantesEfectivas[i];
        _mundo.Cargar(EstadoMundo.DesdeMapa(v.Mapa, v.Inicio));
        _nombreMapa.Text = _nivel.VariantesEfectivas.Count > 1 ? (v.Nombre ?? $"Mapa {i + 1}") : "";
        if (v.Globales.Count > 0)
            PintarCajas(v.Globales.Select(g => new VariableVista(g.Key, Motor.Lua.LuaUtil.Representar(O.Normalizar(g.Value)),
                Motor.Lua.LuaUtil.TipoAmigable(O.Normalizar(g.Value)), false, true)).ToList());
    }

    private void MostrarPista()
    {
        int n = _pistas.Children.Count;
        if (n >= _nivel.Pistas.Length) return;
        var tb = new Border
        {
            Background = Graficos.Brocha("#FEF3C7"), CornerRadius = new CornerRadius(12), Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 0, 0, 6),
            Child = new TextBlock { Text = _nivel.Pistas[n], TextWrapping = TextWrapping.Wrap, FontSize = 15 },
        };
        _pistas.Children.Add(tb);
        Graficos.Aparecer(tb);
        n++;
        _botonPista.Content = Graficos.IconoTexto(Ico.Bombilla, n >= _nivel.Pistas.Length ? "No hay más pistas" : $"Otra pista ({n}/{_nivel.Pistas.Length})", 15);
        _botonPista.IsEnabled = n < _nivel.Pistas.Length;
    }

    private void VerSolucion()
    {
        if (!_vioSolucion &&
            MessageBox.Show("Si ves la solución, en este intento solo podrás conseguir 1 estrella. ¿La quieres ver?", "Ver una solución",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        _vioSolucion = true;
        var ventana = new Window
        {
            Title = "Una solución posible", Width = 720, SizeToContent = SizeToContent.Height, MaxHeight = 760,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Window.GetWindow(this), ResizeMode = ResizeMode.NoResize,
            Background = Graficos.Brocha("#F1F5F9"),
        };
        var sp = new StackPanel { Margin = new Thickness(24) };
        sp.Children.Add(new TextBlock
        {
            Text = _editor is null
                ? "Este es el orden correcto. Fíjate también en la sangría de cada línea."
                : "Esta es una forma de resolverlo (hay más). Escríbela tú en el editor, ¡así se aprende de verdad!",
            FontSize = 17, TextWrapping = TextWrapping.Wrap,
        });
        sp.Children.Add(VistaLeccion.BloqueCodigo(_nivel.Solucion));
        var cerrar = new Button { Style = Estilo("BotonAzul"), Content = "¡Entendido!", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        cerrar.Click += (_, _) => ventana.Close();
        sp.Children.Add(cerrar);
        ventana.Content = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        ventana.ShowDialog();
    }

    // ---------------- Mensajes ----------------
    private void MostrarMensaje(TipoMensaje tipo, string titulo, string texto)
    {
        var (fondo, borde, color, icono) = tipo switch
        {
            TipoMensaje.Error => ("#FEF2F2", "#FCA5A5", "#B91C1C", Ico.Cancelar),
            TipoMensaje.Exito => ("#F0FDF4", "#86EFAC", "#15803D", Ico.Ok),
            TipoMensaje.Aviso => ("#FFFBEB", "#FCD34D", "#B45309", Ico.Bombilla),
            _ => ("#EFF6FF", "#93C5FD", "#1D4ED8", Ico.Info),
        };
        _mensaje.Background = Graficos.Brocha(fondo);
        _mensaje.BorderBrush = Graficos.Brocha(borde);
        _mensaje.BorderThickness = new Thickness(2);
        var dp = new DockPanel();
        var bit = new DibujoBit { Width = 52, Height = 56, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 0, 12, 0) };
        bit.Loaded += (_, _) => bit.PonerAnimo(tipo switch { TipoMensaje.Error => Animo.Triste, TipoMensaje.Exito => Animo.Feliz, TipoMensaje.Aviso => Animo.Sorpresa, _ => Animo.Normal });
        dp.Children.Add(bit);
        var sp = new StackPanel();
        var cab = new StackPanel { Orientation = Orientation.Horizontal };
        cab.Children.Add(Graficos.Icono(icono, 16, Graficos.Brocha(color)));
        cab.Children.Add(new TextBlock { Text = " " + titulo, FontWeight = FontWeights.Black, FontSize = 17, Foreground = Graficos.Brocha(color) });
        sp.Children.Add(cab);
        sp.Children.Add(new TextBlock { Text = texto, TextWrapping = TextWrapping.Wrap, FontSize = 16, Margin = new Thickness(0, 4, 0, 0), LineHeight = 23 });
        dp.Children.Add(sp);
        _mensaje.Child = dp;
        _mensaje.Visibility = Visibility.Visible;
        if (tipo == TipoMensaje.Error) Graficos.Sacudir(_mensaje);
    }

    private void OcultarMensaje() => _mensaje.Visibility = Visibility.Collapsed;

    // ---------------- Cajas (variables) ----------------
    private void PintarCajas(List<VariableVista> vars)
    {
        if (vars.Select(v => (v.Nombre, v.Valor)).SequenceEqual(_ultimasVariables.Select(v => (v.Nombre, v.Valor))) && _cajas.Children.Count > 0)
            return;
        var anteriores = _ultimasVariables.ToDictionary(v => v.Nombre, v => v.Valor);
        _ultimasVariables = vars;
        _cajas.Children.Clear();
        if (vars.Count == 0)
        {
            _cajas.Children.Add(new TextBlock
            {
                Text = "Todavía no hay variables. Cuando crees una con local, aparecerá aquí su caja.",
                Foreground = Graficos.Brocha("#94A3B8"), FontSize = 14, TextWrapping = TextWrapping.Wrap,
            });
            return;
        }
        foreach (var v in vars)
        {
            var caja = Caja(v);
            _cajas.Children.Add(caja);
            if (!anteriores.TryGetValue(v.Nombre, out var antes) || antes != v.Valor)
                Graficos.Latir(caja);
        }
    }

    private static FrameworkElement Caja(VariableVista v)
    {
        var (c1, c2, etiqueta) = v.EsDelJuego ? ("#2DD4BF", "#0D9488", "del juego")
            : v.EsConstante ? ("#A78BFA", "#7C3AED", "constante")
            : v.Tipo == "función" ? ("#F9A8D4", "#DB2777", "función")
            : ("#FBBF24", "#D97706", v.Tipo);
        var sp = new StackPanel { Margin = new Thickness(0, 0, 10, 8), MinWidth = 90 };
        sp.Children.Add(new Border
        {
            Background = Graficos.Brocha("#FFFBEB"), BorderBrush = Graficos.Brocha(c2), BorderThickness = new Thickness(2, 2, 2, 0),
            CornerRadius = new CornerRadius(8, 8, 0, 0), Padding = new Thickness(8, 2, 8, 2), HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock { Text = v.Nombre, FontFamily = (FontFamily)Application.Current.Resources["FuenteCodigo"], FontWeight = FontWeights.Bold, FontSize = 14 },
        });
        string valor = v.Valor.Length > 28 ? v.Valor[..27] + "…" : v.Valor;
        sp.Children.Add(new Border
        {
            Background = new LinearGradientBrush(((SolidColorBrush)Graficos.Brocha(c1)).Color, ((SolidColorBrush)Graficos.Brocha(c2)).Color, 90),
            CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 8, 12, 6), MinHeight = 44, ToolTip = v.Valor,
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = valor, FontFamily = (FontFamily)Application.Current.Resources["FuenteCodigo"], FontSize = 18, FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center },
                    new TextBlock { Text = etiqueta, FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(0xDD, 0xFF, 0xFF, 0xFF)),
                        HorizontalAlignment = HorizontalAlignment.Center },
                },
            },
        });
        return sp;
    }

    private void Imprimir(string texto, bool error = false)
    {
        var tb = new TextBlock
        {
            Text = (error ? "✖ " : "> ") + texto, FontFamily = (FontFamily)Application.Current.Resources["FuenteCodigo"], FontSize = 15,
            Foreground = Graficos.Brocha(error ? "#FCA5A5" : "#E2E8F0"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 1),
        };
        _consola.Children.Add(tb);
        if (_consola.Children.Count > 300) _consola.Children.RemoveAt(0);
        _scrollConsola.ScrollToEnd();
    }

    // ---------------- Ejecución ----------------
    internal bool Ejecutando => _ejecutando;

    internal void EjecutarParaPrueba(bool paso = false)
    {
        _velocidad.Value = 4;
        if (paso) PulsarPaso();
        else _ = Ejecutar(paso: false);
    }

    internal void PasoParaPrueba() => PulsarPaso();

    private void PulsarEjecutar()
    {
        if (_ejecutando)
        {
            // Durante el paso a paso, "Ejecutar" sigue hasta el final
            _modoPaso = false;
            _siguientePaso?.TrySetResult(true);
            return;
        }
        _ = Ejecutar(paso: false);
    }

    private void PulsarPaso()
    {
        if (_ejecutando)
        {
            _modoPaso = true;
            _siguientePaso?.TrySetResult(true);
            return;
        }
        _ = Ejecutar(paso: true);
    }

    private void Parar()
    {
        _cancelar?.Cancel();
        _siguientePaso?.TrySetResult(false);
    }

    private void EstadoEjecutando(bool si)
    {
        _ejecutando = si;
        if (_editor is not null) _editor.IsReadOnly = si;
        _reiniciar.IsEnabled = !si;
        _parar.Visibility = si ? Visibility.Visible : Visibility.Collapsed;
        _ejecutar.Content = Graficos.IconoTexto(Ico.Jugar, si ? "Hasta el final" : "Ejecutar", 19);
        _pasoAPaso.Content = Graficos.IconoTexto(Ico.Pasos, si ? "Siguiente paso" : "Paso a paso", 16);
        if (_editor is null)
        {
            PintarOrden();
            MarcarFilaOrden(_marca.Ejecutando, _marca.Error);
        }
    }

    private static Task<T> EnHiloGrande<T>(Func<T> f)
    {
        var tcs = new TaskCompletionSource<T>();
        var hilo = new Thread(() =>
        {
            try { tcs.SetResult(f()); }
            catch (Exception e) { tcs.SetException(e); }
        }, 32 * 1024 * 1024) { IsBackground = true };
        hilo.Start();
        return tcs.Task;
    }

    private async Task Ejecutar(bool paso)
    {
        string codigo = CodigoActual();
        if (_editor is not null)
        {
            App.Progreso.Codigo[_nivel.Id] = codigo;
            App.GuardarProgreso();
        }

        _modoPaso = paso;
        _cancelar = new CancellationTokenSource();
        var token = _cancelar.Token;
        EstadoEjecutando(true);
        OcultarMensaje();
        Marcar(0, 0);
        _consola.Children.Clear();
        _ultimasVariables = new();
        foreach (var icono in _iconosObjetivo.Values) PonerIcono(icono, null);

        try
        {
            int fallos = _intentosFallidos;
            var c = await EnHiloGrande(() => Evaluador.Calificar(_nivel, codigo, fallos));
            var r = c.Mostrar;
            VerVariante(r.IndiceVariante);
            PintarCajas(new List<VariableVista>());

            if (r.ErrorDeSintaxis)
            {
                Marcar(0, r.Error!.Linea);
                Imprimir($"Línea {r.Error.Linea}: {r.Error.Message}", error: true);
                MostrarMensaje(TipoMensaje.Error, $"Hay un problema en la línea {r.Error.Linea}", r.Error.Message);
                _mundo.PonerAnimo(Animo.Triste);
                Fallo();
                return;
            }

            bool completo = await Reproducir(r, token);
            if (!completo)
            {
                MostrarMensaje(TipoMensaje.Info, "Programa parado", "Has parado el programa. Cuando quieras, vuelve a ejecutarlo.");
                return;
            }

            if (r.Error is not null)
            {
                Marcar(0, r.Error.Linea);
                Imprimir($"Línea {r.Error.Linea}: {r.Error.Message}", error: true);
                bool esJuego = r.Error is Motor.Lua.ErrorJuego;
                MostrarMensaje(TipoMensaje.Error, esJuego ? "¡Oh, no!" : $"Error en la línea {r.Error.Linea}", r.Error.Message + VarianteTexto(r));
                _mundo.PonerAnimo(Animo.Triste);
                MarcarObjetivos(c);
                Fallo();
                return;
            }

            Marcar(0, 0);
            MarcarObjetivos(c);
            if (_nivel.EsLibre)
            {
                MostrarMensaje(TipoMensaje.Exito, "¡Programa terminado!", "Bit ha hecho todo lo que le has dicho. ¡Sigue experimentando!");
                _mundo.PonerAnimo(Animo.Feliz);
                return;
            }

            if (c.Superado)
            {
                _mundo.PonerAnimo(Animo.Feliz);
                MostrarMensaje(TipoMensaje.Exito, "¡Lo has conseguido!", "Todos los objetivos cumplidos.");
                await Task.Delay(700);
                int estrellas = _vioSolucion ? 1 : c.Estrellas;
                var consejos = c.Consejos.ToList();
                if (_vioSolucion && c.Estrellas > 1)
                    consejos.Add("Has visto la solución. Vuelve a intentarlo sin mirarla para ganar más estrellas.");
                _pantalla.MostrarResultado(estrellas, c.Logros, consejos);
            }
            else
            {
                var pendientes = r.Objetivos.Where(o => !o.Cumplido).Select(o => "•  " + o.Objetivo.Descripcion).ToList();
                MostrarMensaje(TipoMensaje.Aviso, "¡Casi! Aún falta algo",
                    "El programa funciona, pero no cumple todo:\n" + string.Join("\n", pendientes) + VarianteTexto(r));
                _mundo.PonerAnimo(Animo.Sorpresa);
                Fallo();
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(TipoMensaje.Error, "¡Uy! Algo ha fallado dentro del juego", ex.Message);
        }
        finally
        {
            _siguientePaso = null;
            EstadoEjecutando(false);
        }
    }

    private string VarianteTexto(ResultadoEjecucion r) =>
        _nivel.VariantesEfectivas.Count > 1 ? $"\n(Ha fallado en el mapa «{r.Variante.Nombre ?? $"Mapa {r.IndiceVariante + 1}"}».)" : "";

    private void Fallo()
    {
        _intentosFallidos++;
        if (_intentosFallidos >= 3 && !string.IsNullOrEmpty(_nivel.Solucion))
            _botonSolucion.Visibility = Visibility.Visible;
    }

    /// <summary>Anima los fotogramas de la ejecución. Devuelve false si se ha parado.</summary>
    private async Task<bool> Reproducir(ResultadoEjecucion r, CancellationToken token)
    {
        var fotos = r.Fotogramas;
        int lineasSolas = fotos.Count(f => f.Evento is null);
        foreach (var f in fotos)
        {
            if (token.IsCancellationRequested) return false;
            double base_ = Velocidades[(int)_velocidad.Value];

            Marcar(f.Linea, 0);
            PintarCajas(f.Variables.ToList());
            double espera;
            switch (f.Evento)
            {
                case null:
                    espera = base_ * 0.45 * Math.Min(1, 120.0 / Math.Max(1, lineasSolas));
                    break;
                case EventoImprimir imp:
                    Imprimir(imp.Texto);
                    espera = base_ * 0.7;
                    break;
                case EventoEsperar e:
                    espera = Math.Min(2000, base_ * 2 * e.Segundos);
                    break;
                default:
                    _mundo.Aplicar(f.Evento, TimeSpan.FromMilliseconds(base_));
                    espera = base_;
                    break;
            }

            if (_modoPaso && f.Evento is null)
            {
                _siguientePaso = new TaskCompletionSource<bool>();
                MostrarMensaje(TipoMensaje.Info, $"Paso a paso · línea {f.Linea}",
                    "La línea amarilla es la que Bit va a ejecutar ahora. Mira las cajas y el mundo, y pulsa «Siguiente paso» (F10) para ejecutarla, o «Hasta el final» para ir seguido.");
                if (!await _siguientePaso.Task) return false;
                OcultarMensaje();
            }
            else if (espera >= 1)
            {
                try { await Task.Delay(TimeSpan.FromMilliseconds(espera), token); }
                catch (TaskCanceledException) { return false; }
            }
        }
        if (_modoPaso) OcultarMensaje();
        return true;
    }
}
