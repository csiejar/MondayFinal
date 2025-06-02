using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MoneyDiary365.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Display(Name = "全名")]
        public string? FullName { get; set; }
        
        // 建立與 SavingRecord 的關聯
        public virtual ICollection<SavingRecord> SavingRecords { get; set; } = new List<SavingRecord>();
    }
}
