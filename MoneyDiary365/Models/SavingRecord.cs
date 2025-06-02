using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyDiary365.Models
{
    public class SavingRecord
    {
        public int Id { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "存款日期")]
        public DateTime SaveDate { get; set; }
        
        [Required]
        [Range(1, 365, ErrorMessage = "金額必須在 1 到 365 之間")]
        [Display(Name = "存款金額")]
        public int Amount { get; set; }
        
        [Display(Name = "備註")]
        public string? Note { get; set; }
        
        // 添加用戶關聯
        [Required]
        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
