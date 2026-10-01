using System;
using FinanceTracker.wpf.Models;

namespace FinanceTracker.wpf.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime StartOfWeek(DateTime dt, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
        {
            var diff = (int)(dt.DayOfWeek - firstDayOfWeek);
            if (diff < 0) diff += 7;
            return dt.AddDays(-diff).Date;
        }

        public static DateTime EndOfWeek(DateTime dt, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
            => StartOfWeek(dt, firstDayOfWeek).AddDays(6).Date.AddDays(1).AddTicks(-1);

        public static DateTime StartOfMonth(DateTime dt) => new DateTime(dt.Year, dt.Month, 1);

        public static DateTime EndOfMonth(DateTime dt) => StartOfMonth(dt).AddMonths(1).AddTicks(-1);

        public static (DateTime? from, DateTime? to) GetPeriodDates(PeriodType period, DateTime customFrom, DateTime customTo)
        {
            return period switch
            {
                PeriodType.Today => (DateTime.Now.Date, DateTime.Now.Date.AddDays(1).AddTicks(-1)),
                PeriodType.ThisWeek => (StartOfWeek(DateTime.Now), EndOfWeek(DateTime.Now)),
                PeriodType.ThisMonth => (StartOfMonth(DateTime.Now), EndOfMonth(DateTime.Now)),
                PeriodType.AllTime => (null, null),
                PeriodType.Custom => (customFrom.Date, customTo.Date.AddDays(1).AddTicks(-1)),
                _ => (null, null)
            };
        }
    }
}
