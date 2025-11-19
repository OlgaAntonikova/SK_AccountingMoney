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
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
