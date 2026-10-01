using System;
using FinanceTracker.wpf.Models;

namespace FinanceTracker.wpf.Services
{
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string? ErrorMessage { get; }

        private ValidationResult(bool isValid, string? errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success() => new(true);
        public static ValidationResult Error(string message) => new(false, message);
    }

    public static class TransactionValidator
    {
        public static ValidationResult Validate(string? description, decimal amount, Account? account, Category? category, DateTime? date)
        {
            if (string.IsNullOrWhiteSpace(description))
                return ValidationResult.Error("Будь ласка, вкажіть опис операції");

            var trimmed = description.Trim();
            if (trimmed.Length < 2)
                return ValidationResult.Error("Опис операції занадто короткий (мінімум 2 символи)");

            if (trimmed.Length > 100)
                return ValidationResult.Error("Опис операції занадто довгий (максимум 100 символів)");

            if (amount <= 0)
                return ValidationResult.Error("Сума операції має бути більшою за 0 ₴");

            if (amount > 100_000_000)
                return ValidationResult.Error("Сума операції не може перевищувати 100 000 000 ₴");

            if (account == null)
                return ValidationResult.Error("Будь ласка, оберіть рахунок для операції");

            if (category == null)
                return ValidationResult.Error("Будь ласка, оберіть категорію витрати/доходу");

            var opDate = date ?? DateTime.Today;
            if (opDate.Date > DateTime.Today.AddYears(1))
                return ValidationResult.Error("Дата операції не може бути пізнішою за 1 рік уперед");

            if (opDate.Date < new DateTime(2000, 1, 1))
                return ValidationResult.Error("Дата операції не може бути ранішою за 01.01.2000");

            return ValidationResult.Success();
        }
    }
}
