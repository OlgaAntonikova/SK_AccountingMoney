using Microsoft.EntityFrameworkCore;
using SK_AccountingMoney.Models;

namespace SK_AccountingMoney.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
           : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<SharedBalance> SharedBalances { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.TelegramId).IsUnique();                
            });
            
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 2);

                entity.HasOne(t => t.User)
                    .WithMany(u => u.Transactions)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SharedBalance>(entity =>
            {
                entity.Property(e => e.Balance).HasPrecision(18, 2);
                entity.HasIndex(e => e.Name).IsUnique();
            });
        }
    }
}
