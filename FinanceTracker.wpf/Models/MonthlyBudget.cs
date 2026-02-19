using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTracker.wpf.Models
{
    public class MonthlyBudget
    {
        public int Id { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }                     

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public decimal PlannedAmount { get; set; }
    }
}
