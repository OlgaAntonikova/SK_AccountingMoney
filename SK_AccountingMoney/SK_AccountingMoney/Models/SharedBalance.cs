using System.ComponentModel.DataAnnotations;

namespace SK_AccountingMoney.Models
{
    public class SharedBalance
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Name (ex, "Main", "GroupBalance")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = "Main";

        /// <summary>
        /// Current shared balance
        /// </summary>
        public decimal Balance { get; set; } = 0;

        /// <summary>
        /// Date last update
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date creating
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
