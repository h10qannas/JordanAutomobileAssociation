using System.ComponentModel.DataAnnotations;

namespace JAA.Models
{
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Value { get; set; } = string.Empty;
    }
}
