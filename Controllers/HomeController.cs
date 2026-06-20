using Microsoft.AspNetCore.Mvc;
using LeadCapture.Data;
using LeadCapture.Models;
using Microsoft.AspNetCore.SignalR;
using LeadCapture.Hubs;
using Microsoft.EntityFrameworkCore;

namespace LeadCapture.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly IHubContext<LeadHub> _hub;

    public HomeController(AppDbContext db, IHubContext<LeadHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<IActionResult> Index()
    {
        var videoUrl = await _db.SiteSettings
            .Where(s => s.Key == "VideoUrl")
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        var galleryImages = await _db.SiteImages
            .OrderBy(i => i.SortOrder)
            .ToListAsync();

        ViewBag.VideoUrl = videoUrl;
        ViewBag.GalleryImages = galleryImages;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(LeadViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        string? photoPath = null;

        if (model.Photo != null && model.Photo.Length > 0)
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photos");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(model.Photo.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.Photo.CopyToAsync(stream);
            }

            photoPath = $"/uploads/photos/{fileName}";
        }

        var lead = new Lead
        {
            FullName = model.FullName,
            Phone = model.Phone,
            Email = model.Email,
            Age = model.Age,
            Height = model.Height,
            Weight = model.Weight,
            Talents = model.Talents,
            PhotoPath = photoPath,
            KvkkConsent = model.KvkkConsent,
            MarketingConsent = model.MarketingConsent,
            Source = Request.Query["utm_source"].FirstOrDefault()
                    ?? Request.Query["fbclid"].FirstOrDefault()
                    ?? "direct",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.FirstOrDefault()
        };

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("NewLead", new
        {
            id = lead.Id,
            fullName = lead.FullName,
            phone = lead.Phone,
            createdAt = lead.CreatedAt.ToString("dd.MM.yyyy HH:mm")
        });

        TempData["Success"] = "Başvurunuz başarıyla alındı! En kısa sürede sizinle iletişime geçeceğiz.";
        return RedirectToAction("Success");
    }

    public IActionResult Success()
    {
        return View();
    }

    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
