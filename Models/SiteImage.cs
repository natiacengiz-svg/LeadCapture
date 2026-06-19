using System.ComponentModel.DataAnnotations;

namespace LeadCapture.Models;

public class SiteImage
{
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
