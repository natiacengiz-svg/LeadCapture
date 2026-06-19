using System.ComponentModel.DataAnnotations;

namespace LeadCapture.Models;

public class SiteSetting
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Value { get; set; }
}
