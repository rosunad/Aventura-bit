using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;

namespace AventuraBit.App.Controles;

/// <summary>Editor de código con colores para Luau y marcas de línea (ejecutando / error).</summary>
public static class EditorLua
{
    private const string Xshd = """
        <SyntaxDefinition name="Luau" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
          <Color name="Comentario" foreground="#94A3B8" fontStyle="italic" />
          <Color name="Texto" foreground="#16A34A" />
          <Color name="Numero" foreground="#EA580C" />
          <Color name="Clave" foreground="#C026D3" fontWeight="bold" />
          <Color name="Valor" foreground="#DC2626" fontWeight="bold" />
          <Color name="Juego" foreground="#2563EB" fontWeight="bold" />
          <Color name="Base" foreground="#0891B2" />
          <RuleSet>
            <Span color="Comentario" multiline="true" begin="--\[\[" end="\]\]" />
            <Span color="Comentario" begin="--" />
            <Span color="Texto"><Begin>"</Begin><End>"</End><RuleSet><Span begin="\\" end="." /></RuleSet></Span>
            <Span color="Texto"><Begin>'</Begin><End>'</End><RuleSet><Span begin="\\" end="." /></RuleSet></Span>
            <Span color="Texto"><Begin>`</Begin><End>`</End></Span>
            <Span color="Texto" multiline="true" begin="\[\[" end="\]\]" />
            <Keywords color="Clave">
              <Word>and</Word><Word>break</Word><Word>do</Word><Word>else</Word><Word>elseif</Word><Word>end</Word>
              <Word>for</Word><Word>function</Word><Word>if</Word><Word>in</Word><Word>local</Word><Word>not</Word>
              <Word>or</Word><Word>repeat</Word><Word>return</Word><Word>then</Word><Word>until</Word><Word>while</Word>
              <Word>continue</Word>
            </Keywords>
            <Keywords color="Valor"><Word>true</Word><Word>false</Word><Word>nil</Word></Keywords>
            <Keywords color="Juego">
              <Word>moverDerecha</Word><Word>moverIzquierda</Word><Word>moverArriba</Word><Word>moverAbajo</Word>
              <Word>recogerGema</Word><Word>pintar</Word><Word>decir</Word><Word>hayMuro</Word><Word>hayAgua</Word>
              <Word>hayGema</Word><Word>enMeta</Word><Word>colorSuelo</Word><Word>gemasRecogidas</Word>
            </Keywords>
            <Keywords color="Base">
              <Word>print</Word><Word>wait</Word><Word>esperar</Word><Word>task</Word><Word>tostring</Word><Word>tonumber</Word>
              <Word>type</Word><Word>typeof</Word><Word>ipairs</Word><Word>pairs</Word><Word>math</Word><Word>string</Word>
              <Word>table</Word><Word>error</Word><Word>assert</Word><Word>self</Word>
            </Keywords>
            <Rule color="Numero">\b0[xX][0-9a-fA-F_]+|\b\d[\d_]*(\.\d+)?([eE][+-]?\d+)?</Rule>
          </RuleSet>
        </SyntaxDefinition>
        """;

    private static IHighlightingDefinition? _definicion;

    public static IHighlightingDefinition Definicion
    {
        get
        {
            if (_definicion is null)
            {
                using var lector = XmlReader.Create(new StringReader(Xshd));
                _definicion = HighlightingLoader.Load(lector, HighlightingManager.Instance);
            }
            return _definicion;
        }
    }

    public static TextEditor Crear(bool soloLectura = false, double tamano = 18)
    {
        var ed = new TextEditor
        {
            FontFamily = (FontFamily)Application.Current.Resources["FuenteCodigo"],
            FontSize = tamano,
            SyntaxHighlighting = Definicion,
            ShowLineNumbers = !soloLectura,
            IsReadOnly = soloLectura,
            WordWrap = false,
            Padding = new Thickness(6),
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
            LineNumbersForeground = new SolidColorBrush(Color.FromRgb(0xA0, 0xAE, 0xC0)),
            HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
        };
        ed.Options.ConvertTabsToSpaces = true;
        ed.Options.IndentationSize = 4;
        ed.Options.EnableHyperlinks = false;
        ed.Options.EnableEmailHyperlinks = false;
        ed.Options.HighlightCurrentLine = !soloLectura;
        if (!soloLectura)
        {
            // Puntitos para ver los espacios de la sangría
            ed.Options.ShowSpaces = true;
            ed.Options.ShowTabs = true;
            ed.TextArea.TextView.NonPrintableCharacterBrush = new SolidColorBrush(Color.FromRgb(0xD5, 0xDD, 0xE8));
            ed.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
            ed.TextArea.TextView.CurrentLineBorder = new Pen(Brushes.Transparent, 0);
        }
        ed.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(0x55, 0x3B, 0x82, 0xF6));
        ed.TextArea.SelectionForeground = null;
        ed.TextArea.SelectionBorder = null;
        return ed;
    }
}

/// <summary>Pinta el fondo de la línea que se está ejecutando y de la línea con error.</summary>
public sealed class MarcadorLineas : IBackgroundRenderer
{
    private readonly TextEditor _editor;

    public MarcadorLineas(TextEditor editor)
    {
        _editor = editor;
        editor.TextArea.TextView.BackgroundRenderers.Add(this);
    }

    public int LineaEjecutando { get; private set; }
    public int LineaError { get; private set; }

    public KnownLayer Layer => KnownLayer.Background;

    public void Marcar(int ejecutando, int error)
    {
        LineaEjecutando = ejecutando;
        LineaError = error;
        _editor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        int linea = error > 0 ? error : ejecutando;
        if (linea > 0 && linea <= _editor.Document.LineCount) _editor.ScrollToLine(linea);
    }

    public void Draw(TextView textView, DrawingContext dc)
    {
        Dibujar(textView, dc, LineaEjecutando, Color.FromRgb(0xFE, 0xF0, 0x8A), Color.FromRgb(0xEA, 0xB3, 0x08));
        Dibujar(textView, dc, LineaError, Color.FromRgb(0xFE, 0xCA, 0xCA), Color.FromRgb(0xEF, 0x44, 0x44));
    }

    private void Dibujar(TextView tv, DrawingContext dc, int linea, Color fondo, Color borde)
    {
        if (linea <= 0 || linea > _editor.Document.LineCount) return;
        var vl = tv.VisualLines.FirstOrDefault(v =>
            v.FirstDocumentLine.LineNumber <= linea && v.LastDocumentLine.LineNumber >= linea);
        if (vl is null) return;
        double y = vl.VisualTop - tv.ScrollOffset.Y;
        dc.DrawRectangle(new SolidColorBrush(fondo), null, new Rect(0, y, Math.Max(tv.ActualWidth, 10), vl.Height));
        dc.DrawRectangle(new SolidColorBrush(borde), null, new Rect(0, y, 4, vl.Height));
    }
}
