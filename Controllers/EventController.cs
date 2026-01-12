using Microsoft.AspNetCore.Mvc;
using Jointly.Data;
using Microsoft.EntityFrameworkCore;

namespace Jointly.Controllers
{
    public class EventController : Controller
    {
        private readonly JointlyDbContext _context;

        public EventController(JointlyDbContext context)
        {
            _context = context;
        }

        // GET: Event/{qrCode}
        [Route("Event/{id}")]
        public async Task<IActionResult> Index(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var eventItem = await _context.Events
                .Include(e => e.User)
                .Include(e => e.EventMedia)
                .FirstOrDefaultAsync(e => e.QRCode == id);

            if (eventItem == null)
            {
                return NotFound();
            }

            return View(eventItem);
        }

        // POST: Event/UploadMedia
        [HttpPost]
        [Route("Event/UploadMedia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMedia(int eventId, List<IFormFile> files, string? uploaderName)
        {
            if (files == null || files.Count == 0)
            {
                TempData["ErrorMessage"] = "Lütfen en az bir dosya seçin";
                var eventForRedirect = await _context.Events.FindAsync(eventId);
                if (eventForRedirect != null)
                {
                    return Redirect($"/Event/{eventForRedirect.QRCode}");
                }
                return NotFound();
            }

            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem == null)
            {
                return NotFound();
            }

            int uploadedCount = 0;
            var errors = new List<string>();

            foreach (var file in files)
            {
                // Validate file size (10MB max)
                if (file.Length > 10 * 1024 * 1024)
                {
                    errors.Add($"{file.FileName}: Dosya boyutu 10MB'dan küçük olmalıdır");
                    continue;
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    errors.Add($"{file.FileName}: Sadece resim ve video dosyaları yüklenebilir");
                    continue;
                }

                try
                {
                    // Save file
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "events", eventId.ToString(), "media");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    // Save to database
                    var fileType = extension == ".mp4" || extension == ".mov" ? "Video" : "Image";
                    var media = new Jointly.Models.EventMedia
                    {
                        EventId = eventId,
                        FilePath = $"/uploads/events/{eventId}/media/{uniqueFileName}",
                        FileType = fileType,
                        UploadedBy = uploaderName,
                        UploadedAt = DateTime.UtcNow,
                        IsApproved = true
                    };

                    _context.EventMedia.Add(media);
                    uploadedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{file.FileName}: Yükleme hatası - {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            if (uploadedCount > 0)
            {
                TempData["SuccessMessage"] = $"{uploadedCount} medya başarıyla yüklendi!";
            }

            if (errors.Any())
            {
                TempData["ErrorMessage"] = string.Join("<br>", errors);
            }

            return Redirect($"/Event/{eventItem.QRCode}");
        }
    }
}
