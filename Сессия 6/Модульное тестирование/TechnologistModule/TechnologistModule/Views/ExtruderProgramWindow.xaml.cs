using System;
using System.Threading.Tasks;
using System.Windows;
using TechnologistModule.Models;
using TechnologistModule.Services;

namespace TechnologistModule.Views
{
    public partial class ExtruderProgramWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly ExtruderProgram _program;
        private readonly bool _isEditMode;

        public ExtruderProgramWindow(ApiClient apiClient, ExtruderProgram program)
        {
            InitializeComponent();
            _apiClient = apiClient;
            _program = program ?? new ExtruderProgram();
            _isEditMode = program != null && program.Id > 0;

            Title = _isEditMode ? "✏️ Редактирование программы" : "➕ Новая программа";
            TitleText.Text = _isEditMode ? "✏️ Редактирование программы" : "🔧 Новая программа";
            SaveButton.Content = _isEditMode ? "Обновить" : "Сохранить";

            if (_isEditMode)
            {
                NameBox.Text = _program.Name;
                DescriptionBox.Text = _program.Description;
                ProgramDataBox.Text = _program.ProgramData;
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Введите название программы!");
                return;
            }

            _program.Name = NameBox.Text.Trim();
            _program.Description = DescriptionBox.Text.Trim();
            _program.ProgramData = ProgramDataBox.Text.Trim();

            SaveButton.IsEnabled = false;
            SaveButton.Content = "Сохранение...";

            bool success = _isEditMode
                ? await _apiClient.UpdateExtruderProgramAsync(_program.Id, _program) != null
                : await _apiClient.CreateExtruderProgramAsync(_program) != null;

            SaveButton.IsEnabled = true;
            SaveButton.Content = _isEditMode ? "Обновить" : "Сохранить";

            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Ошибка при сохранении!");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}