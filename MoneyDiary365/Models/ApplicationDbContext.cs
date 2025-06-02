using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MoneyDiary365.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SavingRecord> SavingRecords { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // 配置 SavingRecord 實體
            builder.Entity<SavingRecord>(entity =>
            {
                // 確保 Amount 存為整數
                entity.Property(s => s.Amount)
                    .HasColumnType("int")
                    .IsRequired();

                // 配置與 ApplicationUser 的關係
                entity.HasOne(s => s.User)
                    .WithMany(u => u.SavingRecords)
                    .HasForeignKey(s => s.UserId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                // 添加唯一索引約束：每個用戶的金額必須唯一
                entity.HasIndex(s => new { s.UserId, s.Amount })
                    .IsUnique();
            });
        }
    }
}
