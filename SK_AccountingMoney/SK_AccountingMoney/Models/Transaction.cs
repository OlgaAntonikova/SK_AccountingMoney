using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SK_AccountingMoney.Models
{
    public class Transaction
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long UserId { get; set; }

        [Required]
        public decimal Amount { get; set; }        

        [MaxLength(500)]
        public required string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("UserId")]
        public virtual required User User { get; set; }

        [Required]
        [MaxLength(20)]
        public required string Type { get; set; } // "deposit" or "withdraw"
    }
}
