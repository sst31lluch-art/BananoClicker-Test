using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BananaApp
{
    public static class BananaVisual
    {
        const string BodyPath = "M 8,64 C 8,88 34,104 68,100 C 102,96 128,76 134,44 C 136,34 130,28 124,34 C 112,48 92,64 66,68 C 40,72 18,60 12,44 C 10,38 6,42 6,50 C 6,56 8,60 8,64 Z";

        static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);
        static Color Lighten(Color c, double t) => Color.FromRgb((byte)(c.R + (255 - c.R) * t), (byte)(c.G + (255 - c.G) * t), (byte)(c.B + (255 - c.B) * t));
        static Color Darken(Color c, double t) => Color.FromRgb((byte)(c.R * (1 - t)), (byte)(c.G * (1 - t)), (byte)(c.B * (1 - t)));

        static Brush BodyBrush(BananaSkin s)
        {
            if (s.Pattern == "Rainbow")
            {
                var rb = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
                string[] cols = { "#FF0000", "#FF7F00", "#FFFF00", "#00FF00", "#0000FF", "#4B0082", "#9400D3" };
                for (int i = 0; i < cols.Length; i++) rb.GradientStops.Add(new GradientStop(C(cols[i]), i / 6.0));
                return rb;
            }
            var baseC = C(s.BaseColor);
            return new LinearGradientBrush(Lighten(baseC, 0.35), Darken(baseC, 0.25), 90);
        }

        public static Viewbox Create(BananaSkin skin, double size)
        {
            var canvas = new Canvas { Width = 140, Height = 110 };
            var geo = Geometry.Parse(BodyPath);

            canvas.Children.Add(new Path { Data = geo, Fill = BodyBrush(skin) });

            var tip = new SolidColorBrush(C(skin.TipColor));
            var t1 = new Ellipse { Width = 12, Height = 10, Fill = tip };
            Canvas.SetLeft(t1, 2); Canvas.SetTop(t1, 58);
            var t2 = new Ellipse { Width = 10, Height = 9, Fill = tip };
            Canvas.SetLeft(t2, 126); Canvas.SetTop(t2, 30);
            canvas.Children.Add(t1); canvas.Children.Add(t2);

            var clip = new Canvas { Clip = geo, Width = 140, Height = 110 };
            switch (skin.Pattern)
            {
                case "Dots":
                    foreach (var (x, y) in new[] { (30, 70), (60, 85), (95, 70), (115, 45) })
                        clip.Children.Add(MakeEllipse(x, y, 10, Darken(C(skin.BaseColor), 0.4)));
                    break;
                case "Stripes":
                    for (int i = 0; i < 5; i++)
                    {
                        var r = new Rectangle { Width = 8, Height = 120, Fill = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)) };
                        r.RenderTransform = new RotateTransform(-30, 20 + i * 25, 55);
                        Canvas.SetLeft(r, 15 + i * 25); Canvas.SetTop(r, 0);
                        clip.Children.Add(r);
                    }
                    break;
                case "Stars":
                    foreach (var (x, y) in new[] { (35, 65), (70, 80), (105, 50) })
                        clip.Children.Add(MakeStar(x, y));
                    break;
                case "Skull":
                    clip.Children.Add(MakeSkull());
                    break;
            }
            canvas.Children.Add(clip);
            return new Viewbox { Width = size, Child = canvas };
        }

        static Ellipse MakeEllipse(double x, double y, double r, Color c)
        {
            var e = new Ellipse { Width = r, Height = r, Fill = new SolidColorBrush(c) };
            Canvas.SetLeft(e, x); Canvas.SetTop(e, y);
            return e;
        }

        static Path MakeStar(double x, double y)
        {
            var p = new Path {
                Data = Geometry.Parse("M 0,-7 L 2,-2 7,-2 3,1.5 4.5,7 0,3.5 -4.5,7 -3,1.5 -7,-2 -2,-2 Z"),
                Fill = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255))
            };
            p.RenderTransform = new TranslateTransform(x, y);
            return p;
        }

        static Canvas MakeSkull()
        {
            var c = new Canvas();
            c.Children.Add(MakeEllipse(55, 55, 26, Colors.White));
            c.Children.Add(MakeEllipse(60, 62, 8, Colors.Black));
            c.Children.Add(MakeEllipse(76, 62, 8, Colors.Black));
            var teeth = new Rectangle { Width = 20, Height = 10, Fill = new SolidColorBrush(Colors.White) };
            Canvas.SetLeft(teeth, 60); Canvas.SetTop(teeth, 80);
            c.Children.Add(teeth);
            return c;
        }
    }
}