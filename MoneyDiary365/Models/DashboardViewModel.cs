using System.Collections.Generic;

namespace MoneyDiary365.Models
{
    public class DashboardViewModel
    {
        public DashboardViewModel()
        {
            NewSaving = new SavingRecord();
            RecentSavings = new List<SavingRecord>();
            CurrentUserId = string.Empty;
        }
        
        public string CurrentUserId { get; set; }

        public decimal TotalSavings { get; set; }
        public int TotalDays { get; set; }
        public double ProgressPercentage { get; set; }
        public SavingRecord NewSaving { get; set; }
        public List<SavingRecord> RecentSavings { get; set; }
    }
}
