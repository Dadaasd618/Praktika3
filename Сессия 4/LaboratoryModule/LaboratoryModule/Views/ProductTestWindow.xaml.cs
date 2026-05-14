using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Windows.Input;
using LaboratoryModule.Models;
using LaboratoryModule.Services;

namespace LaboratoryModule.Views
{
    public partial class ProductTestWindow : Window
    {
        private readonly ApiClient _apiClient;
        private LabTest _test;
        private readonly ProductionBatch _batch;
        private bool _isEditMode;
        private ObservableCollection<TestParameter> _parameters = new ObservableCollection<TestParameter>();
        private Dictionary<string, long> _executorDict = new Dictionary<string, long>();

        public ProductTestWindow(ApiClient apiClient, LabTest test, ProductionBatch batch)
        {
            InitializeComponent();
            _apiClient = apiClient;
            _test = test ?? new LabTest();
            _batch = batch;
            _isEditMode = test != null && test.Id > 0;

            if (_isEditMode)
            {
                TitleText.Text = $"🔬 Испытание №{_test.TestNumber}";
                LoadTestData();
            }
            else
            {
                TitleText.Text = "🔬 Новое испытание";
                InitializeNewTest();
            }

            Loaded += async (s, e) => await LoadExecutors();
        }

        private async Task LoadExecutors()
        {
            try
            {
                var users = await _apiClient.GetUsersAsync();
                if (users != null && users.Count > 0)
                {
                    var labUsers = users.Where(u => u.Role == "laboratory" || u.Role == "technologist").ToList();

                    _executorDict.Clear();
                    var executorNames = new List<string>();

                    foreach (var u in labUsers)
                    {
                        var name = u.FullName ?? u.Username;
                        executorNames.Add(name);
                        _executorDict[name] = u.Id;
                    }

                    ExecutorCombo.ItemsSource = executorNames;

                    if (_isEditMode && _test.TestedBy.HasValue)
                    {
                        var user = labUsers.FirstOrDefault(u => u.Id == _test.TestedBy.Value);
                        if (user != null)
                        {
                            var name = user.FullName ?? user.Username;
                            ExecutorCombo.SelectedItem = name;
                        }
                    }
                    else if (App.CurrentUser != null)
                    {
                        var name = App.CurrentUser.FullName ?? App.CurrentUser.Username;
                        if (executorNames.Contains(name))
                            ExecutorCombo.SelectedItem = name;
                        else
                            ExecutorCombo.SelectedItem = executorNames.FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private long GetSelectedExecutorId()
        {
            var selectedName = ExecutorCombo.SelectedItem as string;
            if (selectedName != null && _executorDict.ContainsKey(selectedName))
                return _executorDict[selectedName];
            return 0;
        }

        private void LoadTestData()
        {
            ObjectInfoText.Text = $"{_batch.ProductName} - {_batch.BatchNumber}";
            if (_test.Parameters != null && _test.Parameters.Any())
            {
                _parameters = new ObservableCollection<TestParameter>(_test.Parameters);
                UpdateParametersDisplay();
                ParametersGrid.ItemsSource = _parameters;
            }
            else
            {
                InitializeDefaultParameters();
            }

            if (_test.Status == "completed")
            {
                SaveDraftButton.IsEnabled = false;
                CompleteButton.IsEnabled = false;
                ParametersGrid.IsEnabled = false;
                TestTypeCombo.IsEnabled = false;
                ExecutorCombo.IsEnabled = false;
            }
        }

        private void InitializeNewTest()
        {
            ObjectInfoText.Text = $"{_batch.ProductName} - {_batch.BatchNumber}";
            InitializeDefaultParameters();
        }

        private void InitializeDefaultParameters()
        {
            _parameters.Clear();
            _parameters.Add(new TestParameter { ParameterName = "Концентрация", StandardMin = 97, StandardMax = 97, Unit = "%" });
            _parameters.Add(new TestParameter { ParameterName = "Влажность", StandardMax = 2.5m, Unit = "%" });
            _parameters.Add(new TestParameter { ParameterName = "pH", StandardMin = 6.5m, StandardMax = 7.0m, Unit = "" });
            UpdateParametersDisplay();
            ParametersGrid.ItemsSource = _parameters;
        }

        private void UpdateParametersDisplay()
        {
            foreach (var param in _parameters)
            {
                if (param.MeasuredValue.HasValue)
                {
                    param.IsPass = (!param.StandardMin.HasValue || param.MeasuredValue >= param.StandardMin) &&
                                   (!param.StandardMax.HasValue || param.MeasuredValue <= param.StandardMax);
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
        }

        private void UpdateResults()
        {
            foreach (var param in _parameters)
            {
                if (param.MeasuredValue.HasValue)
                {
                    param.IsPass = (!param.StandardMin.HasValue || param.MeasuredValue >= param.StandardMin) &&
                                   (!param.StandardMax.HasValue || param.MeasuredValue <= param.StandardMax);
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
            ParametersGrid.Items.Refresh();
        }

        // Валидация ввода (только цифры, запятая, точка)
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-9,.]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, @"^[0-9,.]*$"))
                {
                    e.CancelCommand();
                }
            }
        }

        private async void SaveDraft_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var executorId = GetSelectedExecutorId();
                if (executorId == 0 && App.CurrentUser != null)
                    executorId = App.CurrentUser.Id;

                if (executorId == 0)
                {
                    MessageBox.Show("Выберите исполнителя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                UpdateResults();

                if (!_isEditMode)
                {
                    var testType = (TestTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Входной контроль";

                    var newTest = await _apiClient.CreateProductLabTestAsync(_batch.Id, testType, executorId);
                    if (newTest == null)
                    {
                        MessageBox.Show("Ошибка создания испытания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    _test.Id = newTest.Id;
                    _test.TestNumber = newTest.TestNumber;
                    _isEditMode = true;
                    TitleText.Text = $"🔬 Испытание №{_test.TestNumber}";
                }

                var success = await _apiClient.SaveTestDraftAsync(_test.Id, _parameters.ToList());
                if (success)
                {
                    MessageBox.Show("Черновик сохранён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка при сохранении", "Ошибka", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CompleteTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var executorId = GetSelectedExecutorId();
                if (executorId == 0 && App.CurrentUser != null)
                    executorId = App.CurrentUser.Id;

                if (executorId == 0)
                {
                    MessageBox.Show("Выберите исполнителя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var missingParams = _parameters.Where(p => p.MeasuredValue == null).ToList();
                if (missingParams.Any())
                {
                    MessageBox.Show($"Заполните параметры: {string.Join(", ", missingParams.Select(p => p.ParameterName))}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                UpdateResults();

                if (!_isEditMode)
                {
                    var testType = (TestTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Входной контроль";
                    var newTest = await _apiClient.CreateProductLabTestAsync(_batch.Id, testType, executorId);
                    if (newTest == null)
                    {
                        MessageBox.Show("Ошибка создания испытания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    _test.Id = newTest.Id;
                    _test.TestNumber = newTest.TestNumber;
                    _isEditMode = true;
                }

                // Сохраняем параметры, НО НЕ ЗАВЕРШАЕМ испытание
                var saveSuccess = await _apiClient.SaveTestDraftAsync(_test.Id, _parameters.ToList());
                if (saveSuccess)
                {
                    MessageBox.Show("Параметры сохранены. Теперь можно завершить испытание кнопкой 'Завершить'",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка при сохранении параметров", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ApproveTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_test.Id == 0)
                {
                    MessageBox.Show("Сначала сохраните черновик!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var allPass = _parameters.All(p => p.IsPass);
                var decision = allPass ? "approved" : "blocked";
                var decisionReason = allPass ? "" : "Несоответствие нормативам";

                var success = await _apiClient.CompleteTestAsync(_test.Id, decision, decisionReason, GetSelectedExecutorId());
                if (success)
                {
                    MessageBox.Show($"Испытание завершено. Решение: {(allPass ? "✅ Допущено" : "❌ Заблокировано")}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Возвращаем ID партии и говорим, что нужно обновить
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Ошибка при завершении испытания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}