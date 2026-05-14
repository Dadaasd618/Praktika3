using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LaboratoryModule.Services
{
    public class CaptchaService
    {
        private static readonly Random _random = new Random();
        private string _currentCode = "";

        public BitmapSource GenerateCaptchaImage(int width = 280, int height = 90)
        {
            _currentCode = GenerateRandomCode(6);
            return CreateCaptchaImage(_currentCode, width, height);
        }

        public bool VerifyCode(string input)
        {
            return !string.IsNullOrEmpty(_currentCode) &&
                   _currentCode.Equals(input, StringComparison.OrdinalIgnoreCase);
        }

        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            char[] code = new char[length];
            for (int i = 0; i < length; i++)
                code[i] = chars[_random.Next(chars.Length)];
            return new string(code);
        }

        private BitmapSource CreateCaptchaImage(string code, int width, int height)
        {
            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                context.DrawRectangle(new SolidColorBrush(Color.FromRgb(245, 247, 250)), null, new Rect(0, 0, width, height));

                for (int i = 0; i < 50; i++)
                {
                    var pen = new Pen(new SolidColorBrush(Color.FromRgb(200, 200, 220)), 1);
                    context.DrawLine(pen,
                        new Point(_random.Next(width), _random.Next(height)),
                        new Point(_random.Next(width), _random.Next(height)));
                }

                for (int i = 0; i < 300; i++)
                {
                    var brush = new SolidColorBrush(Color.FromRgb(100, 100, 150));
                    context.DrawRectangle(brush, null,
                        new Rect(_random.Next(width), _random.Next(height), 1.5, 1.5));
                }

                double x = 20;
                double y = 55;

                foreach (char c in code)
                {
                    var color = Color.FromRgb(
                        (byte)_random.Next(40, 180),
                        (byte)_random.Next(40, 140),
                        (byte)_random.Next(40, 180));

                    var brush = new SolidColorBrush(color);
                    var fontSize = 38 + _random.Next(-5, 5);
                    var fontFamily = new FontFamily("Arial");
                    var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

                    var formattedText = new FormattedText(
                        c.ToString(),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        fontSize,
                        brush,
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                    var angle = _random.Next(-20, 20);
                    context.PushTransform(new RotateTransform(angle, x + 15, y));
                    context.DrawText(formattedText, new Point(x, y));
                    context.Pop();

                    x += 35;
                }

                var borderPen = new Pen(new SolidColorBrush(Colors.Gray), 1.5);
                context.DrawRectangle(null, borderPen, new Rect(0, 0, width - 1, height - 1));
            }

            var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);
            return renderBitmap;
        }
    }
}