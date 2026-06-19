using System.ComponentModel.DataAnnotations;

namespace LeadCapture.Models;

public class Lead
{
    public int Id { get; set; }

    public bool IsViewed { get; set; }

    [Display(Name = "Fotoğraf")]
    public string? PhotoPath { get; set; }

    [Required(ErrorMessage = "Ad soyad zorunludur")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon numarası zorunludur")]
    [Display(Name = "Telefon")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    public string? Email { get; set; }

    [Display(Name = "Yaş")]
    [Range(18, 65, ErrorMessage = "Yaş 18-65 arasında olmalıdır")]
    public int? Age { get; set; }

    [Display(Name = "Boy (cm)")]
    [Range(140, 220, ErrorMessage = "Boy 140-220 cm arasında olmalıdır")]
    public int? Height { get; set; }

    [Display(Name = "Kilo (kg)")]
    [Range(35, 200, ErrorMessage = "Kilo 35-200 kg arasında olmalıdır")]
    public int? Weight { get; set; }

    [Display(Name = "Yetenekler")]
    [MaxLength(1000)]
    public string? Talents { get; set; }

    [Display(Name = "KVKK")]
    public bool KvkkConsent { get; set; }

    [Display(Name = "Pazarlama İzni")]
    public bool MarketingConsent { get; set; }

    [Display(Name = "Tarih")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Source { get; set; }
}
