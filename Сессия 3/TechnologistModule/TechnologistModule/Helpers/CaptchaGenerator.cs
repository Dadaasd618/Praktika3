using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TechnologistModule.Helpers
{
    public static class CaptchaGenerator
    {
        private static readonly Random _random = new Random();
        private static string _currentCode = "";

        public static string CurrentCode => _currentCode;

        public static BitmapSource GenerateCaptchaImage(int width = 280, int height = 90)
        {
            _currentCode = GenerateRandomCode(6);

            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                // Фон
                context.DrawRectangle(new SolidColorBrush(Color.FromRgb(245, 247, 250)), null, new Rect(0, 0, width, height));

                // Шум (линии)
                for (int i = 0; i < 40; i++)
                {
                    var pen = new Pen(new SolidColorBrush(Color.FromRgb(200, 200, 220)), 1);
                    context.DrawLine(pen,
                        new Point(_random.Next(width), _random.Next(height)),
                        new Point(_random.Next(width), _random.Next(height)));
                }

                // Точки
                for (int i = 0; i < 200; i++)
                {
                    var brush = new SolidColorBrush(Color.FromRgb(100, 100, 150));
                    context.DrawRectangle(brush, null,
                        new Rect(_random.Next(width), _random.Next(height), 1.5, 1.5));
                }

                // Рисуем символы
                double x = 20;
                double y = 55;
                var random = new Random();

                foreach (char c in _currentCode)
                {
                    // Случайный цвет
                    var color = Color.FromRgb(
                        (byte)_random.Next(40, 180),
                        (byte)_random.Next(40, 140),
                        (byte)_random.Next(40, 180));

                    var brush = new SolidColorBrush(color);

                    // Случайный шрифт
                    var fontFamily = new FontFamily("Arial");
                    var fontSize = 38 + _random.Next(-5, 5);
                    var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

                    var formattedText = new FormattedText(
                        c.ToString(),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        fontSize,
                        brush,
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                    // Случайный наклон
                    var angle = _random.Next(-20, 20);
                    context.PushTransform(new RotateTransform(angle, x + 15, y));
                    context.DrawText(formattedText, new Point(x, y));
                    context.Pop();

                    x += 35;
                }

                // Добавляем рамку
                var borderPen = new Pen(new SolidColorBrush(Colors.Gray), 1.5);
                context.DrawRectangle(null, borderPen, new Rect(0, 0, width - 1, height - 1));
            }

            // Рендерим в bitmap
            var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);

            return renderBitmap;
        }

        public static bool VerifyCode(string input)
        {
            return !string.IsNullOrEmpty(_currentCode) &&
                   _currentCode.Equals(input, StringComparison.OrdinalIgnoreCase);
        }

        private static string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var code = new char[length];
            for (int i = 0; i < length; i++)
                code[i] = chars[_random.Next(chars.Length)];
            return new string(code);
        }
    }
}