using System.ComponentModel.DataAnnotations;


namespace SK_AccountingMoney.Models
{
    public class User
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long TelegramId { get; set; }

        [Required]
        [MaxLength(100)]
        public required string UserName { get; set; }              
        
        public virtual ICollection<Transaction> Transactions { get; set; } = [];
    }

}
