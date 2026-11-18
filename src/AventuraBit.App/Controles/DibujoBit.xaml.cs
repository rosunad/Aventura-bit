using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AventuraBit.App.Controles;

public enum Animo { Normal, Feliz, Triste, Sorpresa }

/// <summary>Bit, el robot protagonista, dibujado con formas vectoriales.</summary>
public partial class DibujoBit : UserControl
{
    public DibujoBit()
    {
        InitializeComponent();
        Loaded += (_, _) => Parpadear();
    }

    public void Mirar(int dx, int dy)
    {
        Mirada.X = dx * 3;
        Mirada.Y = dy * 2;
    }

    public void PonerAnimo(Animo animo)
    {
        Boca.Data = Geometry.Parse(animo switch
        {
            Animo.Feliz => "M 38,51 Q 50,64 62,51",
            Animo.Triste => "M 41,58 Q 50,50 59,58",
            Animo.Sorpresa => "M 46,55 A 4,4 0 1 1 54,55 A 4,4 0 1 1 46,55",
            _ => "M 41,53 Q 50,60 59,53",
        });
        LuzAntena.Fill = new SolidColorBrush(animo switch
        {
            Animo.Feliz => Color.FromRgb(0x4A, 0xDE, 0x80),
            Animo.Triste => Color.FromRgb(0xF8, 0x71, 0x71),
            _ => Color.FromRgb(0xFA, 0xCC, 0x15),
        });
    }

    /// <summary>Bit se cae (al agua): se encoge y gira.</summary>
    public void Caer(TimeSpan duracion)
    {
        var a = new DoubleAnimation(1, 0, duracion) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseIn } };
        Escala.BeginAnimation(ScaleTransform.ScaleXProperty, a);
        Escala.BeginAnimation(ScaleTransform.ScaleYProperty, a);
        Giro.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 200, duracion));
    }

    /// <summary>Saltito de alegría.</summary>
    public void Saltar()
    {
        var a = new DoubleAnimation(1, 1.15, TimeSpan.FromMilliseconds(140)) { AutoReverse = true };
        Escala.BeginAnimation(ScaleTransform.ScaleYProperty, a);
    }

    public void Reiniciar()
    {
        Escala.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        Escala.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        Giro.BeginAnimation(RotateTransform.AngleProperty, null);
        Escala.ScaleX = Escala.ScaleY = 1;
        Giro.Angle = 0;
        Mirar(0, 0);
        PonerAnimo(Animo.Normal);
    }

    private void Parpadear()
    {
        var a = new DoubleAnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever, Duration = TimeSpan.FromSeconds(4) };
        a.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        a.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3.8))));
        a.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3.95))));
        var escala = new ScaleTransform(1, 1, 0, 40);
        Ojos.RenderTransform = new TransformGroup { Children = { escala, Mirada } };
        escala.BeginAnimation(ScaleTransform.ScaleYProperty, a);
    }
}
