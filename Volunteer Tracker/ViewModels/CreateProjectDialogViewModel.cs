using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Volunteer_Tracker.ViewModels
{
    public partial class CreateProjectDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _shortDescription = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private DateTimeOffset? _startDate = DateTimeOffset.Now;

        [ObservableProperty]
        private DateTimeOffset? _endDate = DateTimeOffset.Now.AddMonths(1);

        [ObservableProperty]
        private string _maxPoints = "100";

        [ObservableProperty]
        private string _selectedCategory = "Волонтёрство";

        [ObservableProperty]
        private string _titleError = string.Empty;

        [ObservableProperty]
        private string _dateError = string.Empty;

        [ObservableProperty]
        private string _pointsError = string.Empty;

        public List<string> Categories { get; } = new()
        {
            "Волонтёрство",
            "Экология",
            "Социальный",
            "Образовательный",
            "Спортивный",
            "Культурный",
            "IT",
            "Другое"
        };

        public event EventHandler<(string Title, string ShortDescription, string Description, DateTime StartDate, DateTime EndDate, int MaxPoints, string Category)>? ProjectCreated;

        [RelayCommand]
        private void Cancel()
        {
            ProjectCreated?.Invoke(this, (string.Empty, string.Empty, string.Empty, DateTime.Now, DateTime.Now, 0, string.Empty));
        }

        [RelayCommand]
        private void Create()
        {
            bool hasError = false;

            TitleError = string.IsNullOrWhiteSpace(Title) ? "Введите название проекта" : string.Empty;
            if (!string.IsNullOrWhiteSpace(TitleError)) hasError = true;

            PointsError = string.Empty;
            if (!int.TryParse(MaxPoints, out int points) || points < 10 || points > 1000)
            {
                PointsError = "Введите число от 10 до 1000";
                hasError = true;
            }

            DateError = string.Empty;
            var today = DateTime.Today;

            if (StartDate == null || EndDate == null)
            {
                DateError = "Выберите даты";
                hasError = true;
            }
            else
            {
                // Проверка: дедлайн не может быть раньше сегодня
                if (EndDate.Value.Date < today)
                {
                    DateError = "Дедлайн не может быть раньше сегодняшнего дня";
                    hasError = true;
                }
                // Проверка: дедлайн не может быть раньше даты начала
                else if (EndDate.Value.Date < StartDate.Value.Date)
                {
                    DateError = "Дедлайн не может быть раньше даты начала";
                    hasError = true;
                }
                // Проверка: дата начала не может быть раньше сегодня
                else if (StartDate.Value.Date < today)
                {
                    DateError = "Дата начала не может быть раньше сегодняшнего дня";
                    hasError = true;
                }
            }

            if (hasError) return;

            ProjectCreated?.Invoke(this, (Title, ShortDescription, Description, StartDate!.Value.DateTime, EndDate!.Value.DateTime, points, SelectedCategory));
        }
    }
}