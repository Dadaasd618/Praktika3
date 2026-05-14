using System;
using System.Windows;
using System.Windows.Media;
using LaboratoryModule.Models;
using LaboratoryModule.Services;

namespace LaboratoryModule.Views
{
    public partial class ProtocolViewWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly LabTest _test;

        public ProtocolViewWindow(ApiClient apiClient, LabTest test)
        {
            InitializeComponent();
            _apiClient = apiClient;
            _test = test;

            TitleText.Text = $"📄 Протокол испытания №{test.TestNumber}";
            TestNumberText.Text = test.TestNumber;
            DateText.Text = test.TestedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";
            ObjectTypeText.Text = test.ObjectType == "raw_material" ? "Сырьё" : "Готовая продукция";
            ObjectNameText.Text = test.ObjectName ?? test.ObjectId.ToString();
            TestTypeText.Text = test.TestType;
            ExecutorText.Text = test.TestedByName;
            DecisionText.Text = test.Decision;

            var decisionColor = test.Decision == "approved" ? "#10B981" : "#EF4444";
            DecisionBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(decisionColor));

            ReasonText.Text = test.DecisionReason ?? "-";

            LoadParameters();
        }

        private async void LoadParameters()
        {
            try
            {
                var fullTest = await _apiClient.GetLabTestAsync(_test.Id);
                if (fullTest != null && fullTest.Parameters != null)
                {
                    foreach (var param in fullTest.Parameters)
                    {
                        // Вычисляем IsPass на основе измеренного значения и норм
                        if (param.MeasuredValue.HasValue)
                        {
                            bool minPass = !param.StandardMin.HasValue || param.MeasuredValue.Value >= param.StandardMin.Value;
                            bool maxPass = !param.StandardMax.HasValue || param.MeasuredValue.Value <= param.StandardMax.Value;
                            param.IsPass = minPass && maxPass;

                            param.ResultText = param.IsPass ? "✅ Соответствует" : "❌ Не соответствует";
                            param.ResultColor = param.IsPass ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                        }
                        else
                        {
                            param.IsPass = false;
                            param.ResultText = "❌ Не заполнено";
                            param.ResultColor = new SolidColorBrush(Colors.Red);
                        }
                    }
                    ParametersGrid.ItemsSource = fullTest.Parameters;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки параметров: {ex.Message}");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}