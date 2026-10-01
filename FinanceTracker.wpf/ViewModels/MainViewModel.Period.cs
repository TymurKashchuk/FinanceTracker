using System;
using System.Windows.Input;
using FinanceTracker.wpf.Helpers;
using FinanceTracker.wpf.Models;

namespace FinanceTracker.wpf.ViewModels
{
    public partial class MainViewModel
    {
        public string CurrentPeriodText => SelectedPeriod switch
        {
            PeriodType.Today => "Сьогодні",
            PeriodType.ThisWeek => "Тиждень",
            PeriodType.ThisMonth => "Місяць",
            PeriodType.AllTime => "Весь час",
            PeriodType.Custom => $"{CustomDateFrom:dd.MM} - {CustomDateTo:dd.MM}",
            _ => "Весь час"
        };

        public bool IsTodayPeriod => SelectedPeriod == PeriodType.Today;
        public bool IsWeekPeriod => SelectedPeriod == PeriodType.ThisWeek;
        public bool IsMonthPeriod => SelectedPeriod == PeriodType.ThisMonth;
        public bool IsAllTimePeriod => SelectedPeriod == PeriodType.AllTime;
        public bool IsCustomPeriod => SelectedPeriod == PeriodType.Custom;

        private PeriodType _selectedPeriod = PeriodType.ThisMonth;
        public PeriodType SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                _selectedPeriod = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentPeriodText));
                OnPropertyChanged(nameof(IsTodayPeriod));
                OnPropertyChanged(nameof(IsWeekPeriod));
                OnPropertyChanged(nameof(IsMonthPeriod));
                OnPropertyChanged(nameof(IsAllTimePeriod));
                OnPropertyChanged(nameof(IsCustomPeriod));
                _ = LoadAsync();
            }
        }

        private DateTime _customDateFrom = DateTime.Today.AddDays(-7);
        public DateTime CustomDateFrom
        {
            get => _customDateFrom;
            set
            {
                if (_customDateFrom != value)
                {
                    _customDateFrom = value;
                    if (_customDateFrom > _customDateTo)
                    {
                        _customDateTo = _customDateFrom;
                        OnPropertyChanged(nameof(CustomDateTo));
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentPeriodText));
                    if (SelectedPeriod == PeriodType.Custom) _ = LoadAsync();
                }
            }
        }

        private DateTime _customDateTo = DateTime.Today;
        public DateTime CustomDateTo
        {
            get => _customDateTo;
            set
            {
                if (_customDateTo != value)
                {
                    _customDateTo = value;
                    if (_customDateTo < _customDateFrom)
                    {
                        _customDateFrom = _customDateTo;
                        OnPropertyChanged(nameof(CustomDateFrom));
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentPeriodText));
                    if (SelectedPeriod == PeriodType.Custom) _ = LoadAsync();
                }
            }
        }

        public ICommand SetPeriodTodayCommand { get; private set; } = null!;
        public ICommand SetPeriodWeekCommand { get; private set; } = null!;
        public ICommand SetPeriodMonthCommand { get; private set; } = null!;
        public ICommand SetPeriodAllTimeCommand { get; private set; } = null!;
        public ICommand SetPeriodCustomCommand { get; private set; } = null!;
        public ICommand SetPeriodLast30DaysCommand => SetPeriodAllTimeCommand;

        private void InitializePeriodCommands()
        {
            SetPeriodTodayCommand = new RelayCommand(_ => SelectedPeriod = PeriodType.Today);
            SetPeriodWeekCommand = new RelayCommand(_ => SelectedPeriod = PeriodType.ThisWeek);
            SetPeriodMonthCommand = new RelayCommand(_ => SelectedPeriod = PeriodType.ThisMonth);
            SetPeriodAllTimeCommand = new RelayCommand(_ => SelectedPeriod = PeriodType.AllTime);
            SetPeriodCustomCommand = new RelayCommand(_ => SelectedPeriod = PeriodType.Custom);
        }

        private (DateTime? from, DateTime? to) GetPeriodDates()
            => DateTimeHelper.GetPeriodDates(SelectedPeriod, CustomDateFrom, CustomDateTo);
    }
}
