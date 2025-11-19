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
        public string UserName { get; set; }     

        public decimal Balance { get; set; } = 0;        
        
        public virtual ICollection<Transaction> Transactions { get; set; }
    }

}
