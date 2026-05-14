using LaboratoryModule.Models;
using LaboratoryModule.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LaboratoryModule.Views
{
    public partial class TestWindow : Window
    {
        private readonly ApiClient _apiClient;
        private LabTest _test;
        private readonly RawMaterialBatch _batch;
        private bool _isEditMode;
        private ObservableCollection<TestParameter> _parameters = new ObservableCollection<TestParameter>();
        private Dictionary<string, long> _executorDict = new Dictionary<string, long>();

        public TestWindow(ApiClient apiClient, LabTest test, RawMaterialBatch batch)
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
            ObjectInfoText.Text = $"{_batch.RawMaterialName} - {_batch.BatchNumber}";
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
            ObjectInfoText.Text = $"{_batch.RawMaterialName} - {_batch.BatchNumber}";
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

                    var newTest = await _apiClient.CreateLabTestAsync(_batch.Id, testType, executorId);
                    if (newTest == null || newTest.Id == 0)
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
                    MessageBox.Show("Ошибка при сохранении", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // Проверяем заполнение всех параметров
                var missingParams = _parameters.Where(p => p.MeasuredValue == null).ToList();
                if (missingParams.Any())
                {
                    MessageBox.Show($"Заполните параметры: {string.Join(", ", missingParams.Select(p => p.ParameterName))}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                UpdateResults();

                // Проверяем соответствие норме
                var failedParams = _parameters.Where(p => !p.IsPass).ToList();
                if (failedParams.Any())
                {
                    var msg = "Следующие параметры не соответствуют норме:\n";
                    foreach (var p in failedParams)
                    {
                        msg += $"{p.ParameterName}: {p.MeasuredValue} (норма: {p.StandardDisplay})\n";
                    }
                    var result = MessageBox.Show(msg + "\nПродолжить?",
                        "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.No)
                        return;
                }

                long testId;
                if (!_isEditMode)
                {
                    var testType = (TestTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Входной контроль";
                    var newTest = await _apiClient.CreateLabTestAsync(_batch.Id, testType, executorId);
                    if (newTest == null)
                    {
                        MessageBox.Show("Ошибка создания испытания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    testId = newTest.Id;
                    _test.Id = testId;
                    _test.TestNumber = newTest.TestNumber;
                    _isEditMode = true;
                }
                else
                {
                    testId = _test.Id;
                }

                // Сначала сохраняем черновик (параметры)
                var saveSuccess = await _apiClient.SaveTestDraftAsync(testId, _parameters.ToList());
                if (!saveSuccess)
                {
                    MessageBox.Show("Ошибка при сохранении параметров", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Затем завершаем испытание
                var allPass = _parameters.All(p => p.IsPass);
                var decision = allPass ? "approved" : "blocked";
                var decisionReason = allPass ? "" : "Несоответствие нормативам";
                var success = await _apiClient.CompleteTestAsync(testId, decision, decisionReason, executorId);
                if (success)
                {
                    MessageBox.Show($"Испытание завершено. Решение: {(allPass ? "✅ Допущено" : "❌ Заблокировано")}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
        // Валидация ввода (только цифры, запятая, точка)
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"[^0-9,.]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!System.Text.RegularExpressions.Regex.IsMatch(text, @"^[0-9,.]*$"))
                {
                    e.CancelCommand();
                }
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}