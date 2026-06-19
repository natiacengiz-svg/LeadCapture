using System.ComponentModel.DataAnnotations;

namespace LeadCapture.Models;

public class LeadViewModel
{
    [Required(ErrorMessage = "Ad soyad zorunludur")]
    [Display(Name = "Adınız ve Soyadınız")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon numarası zorunludur")]
    [Display(Name = "Telefon Numaranız")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "E-posta Adresiniz (isteğe bağlı)")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    public string? Email { get; set; }

    [Display(Name = "Yaşınız")]
    [Range(18, 65, ErrorMessage = "Yaş 18-65 arasında olmalıdır")]
    public int? Age { get; set; }

    [Display(Name = "Boyunuz (cm)")]
    [Range(140, 220, ErrorMessage = "Boy 140-220 cm arasında olmalıdır")]
    public int? Height { get; set; }

    [Display(Name = "Kilonuz (kg)")]
    [Range(35, 200, ErrorMessage = "Kilo 35-200 kg arasında olmalıdır")]
    public int? Weight { get; set; }

    [Display(Name = "Yetenekleriniz")]
    [MaxLength(1000)]
    public string? Talents { get; set; }

    [Display(Name = "Fotoğrafınız")]
    public IFormFile? Photo { get; set; }

    [Required(ErrorMessage = "KVKK aydınlatma metnini kabul etmelisiniz")]
    [Display(Name = "KVKK Aydınlatma Metni'ni okudum ve kabul ediyorum")]
    public bool KvkkConsent { get; set; }

    [Display(Name = "Yeni projelerden haberdar olmak için ileti almayı kabul ediyorum")]
    public bool MarketingConsent { get; set; }
}
