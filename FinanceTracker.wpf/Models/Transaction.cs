using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTracker.wpf.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public decimal Amount { get; set; }              
        public DateTime Date { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;

        public bool IsIncome { get; set; }              
    }
}
