using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using LeadCapture.Data;
using LeadCapture.Models;

namespace LeadCapture.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (username == "admin" && password == "admin123")
        {
            var identity = new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "admin") },
                "CookieAuth");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("CookieAuth", principal);
            return RedirectToAction("Index");
        }

        ViewBag.Error = "Kullanıcı adı veya şifre hatalı";
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("CookieAuth");
        return RedirectToAction("Login");
    }

    [Authorize]
    public async Task<IActionResult> Index(string? search, string? sort)
    {
        var query = _db.Leads.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            query = query.Where(l =>
                l.FullName.ToLower().Contains(search) ||
                l.Phone.Contains(search) ||
                (l.Email != null && l.Email.ToLower().Contains(search)) ||
                (l.Talents != null && l.Talents.ToLower().Contains(search))
            );
        }

        query = sort switch
        {
            "name" => query.OrderBy(l => l.FullName),
            "name_desc" => query.OrderByDescending(l => l.FullName),
            "date" => query.OrderBy(l => l.CreatedAt),
            _ => query.OrderByDescending(l => l.CreatedAt)
        };

        if (string.IsNullOrEmpty(search))
        {
            var unviewed = await query.Where(l => !l.IsViewed).ToListAsync();
            foreach (var l in unviewed)
            {
                l.IsViewed = true;
            }
            await _db.SaveChangesAsync();
        }

        var leads = await query.ToListAsync();
        ViewBag.Search = search;
        ViewBag.Sort = sort;
        return View(leads);
    }

    [Authorize]
    public async Task<IActionResult> Export(string? search)
    {
        var query = _db.Leads.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            query = query.Where(l =>
                l.FullName.ToLower().Contains(search) ||
                l.Phone.Contains(search) ||
                (l.Email != null && l.Email.ToLower().Contains(search))
            );
        }

        var leads = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Leads");
        ws.Cell(1, 1).Value = "ID";
        ws.Cell(1, 2).Value = "Ad Soyad";
        ws.Cell(1, 3).Value = "Telefon";
        ws.Cell(1, 4).Value = "E-posta";
        ws.Cell(1, 5).Value = "Yaş";
        ws.Cell(1, 6).Value = "Boy";
        ws.Cell(1, 7).Value = "Kilo";
        ws.Cell(1, 8).Value = "Yetenekler";
        ws.Cell(1, 9).Value = "KVKK";
        ws.Cell(1, 10).Value = "Pazarlama";
        ws.Cell(1, 11).Value = "Tarih";
        ws.Cell(1, 12).Value = "Kaynak";

        var header = ws.Row(1);
        header.Style.Font.Bold = true;

        for (int i = 0; i < leads.Count; i++)
        {
            var l = leads[i];
            ws.Cell(i + 2, 1).Value = l.Id;
            ws.Cell(i + 2, 2).Value = l.FullName;
            ws.Cell(i + 2, 3).Value = l.Phone;
            ws.Cell(i + 2, 4).Value = l.Email;
            ws.Cell(i + 2, 5).Value = l.Age;
            ws.Cell(i + 2, 6).Value = l.Height;
            ws.Cell(i + 2, 7).Value = l.Weight;
            ws.Cell(i + 2, 8).Value = l.Talents;
            ws.Cell(i + 2, 9).Value = l.KvkkConsent ? "Evet" : "Hayır";
            ws.Cell(i + 2, 10).Value = l.MarketingConsent ? "Evet" : "Hayır";
            ws.Cell(i + 2, 11).Value = l.CreatedAt.ToString("dd.MM.yyyy HH:mm");
            ws.Cell(i + 2, 12).Value = l.Source;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "leads.xlsx");
    }

    [Authorize]
    public async Task<IActionResult> VideoSettings()
    {
        var videoUrl = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == "VideoUrl");
        return View(videoUrl ?? new SiteSetting { Key = "VideoUrl" });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> VideoSettings(string videoUrl)
    {
        var setting = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == "VideoUrl");
        if (setting == null)
        {
            setting = new SiteSetting { Key = "VideoUrl", Value = videoUrl };
            _db.SiteSettings.Add(setting);
        }
        else
        {
            setting.Value = videoUrl;
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = "Video URL güncellendi.";
        return RedirectToAction("VideoSettings");
    }

    [Authorize]
    public async Task<IActionResult> Gallery()
    {
        var images = await _db.SiteImages.OrderBy(i => i.SortOrder).ThenByDescending(i => i.CreatedAt).ToListAsync();
        return View(images);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> GalleryUpload(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            TempData["Error"] = "Lütfen en az bir dosya seçin.";
            return RedirectToAction("Gallery");
        }

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "gallery");
        Directory.CreateDirectory(uploadsDir);

        var maxSort = await _db.SiteImages.MaxAsync(i => (int?)i.SortOrder) ?? 0;

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            maxSort++;
            _db.SiteImages.Add(new SiteImage
            {
                FilePath = $"/uploads/gallery/{fileName}",
                SortOrder = maxSort
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"{files.Count} fotoğraf yüklendi.";
        return RedirectToAction("Gallery");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> GalleryDelete(int id)
    {
        var image = await _db.SiteImages.FindAsync(id);
        if (image != null)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _db.SiteImages.Remove(image);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Gallery");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> GalleryReorder([FromForm] List<int> ids)
    {
        for (int i = 0; i < ids.Count; i++)
        {
            var image = await _db.SiteImages.FindAsync(ids[i]);
            if (image != null)
                image.SortOrder = i;
        }
        await _db.SaveChangesAsync();
        return RedirectToAction("Gallery");
    }
}
