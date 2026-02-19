using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTracker.wpf.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsIncome { get; set; }                 

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

}
