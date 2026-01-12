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
                .Include(e => e.EventMessages)
                .Include(e => e.EventVoiceNotes)
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

        // POST: Event/SubmitMessage
        [HttpPost]
        [Route("Event/SubmitMessage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitMessage(int eventId, string message, string? senderName)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Lütfen bir mesaj yazın";
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

            var eventMessage = new Jointly.Models.EventMessage
            {
                EventId = eventId,
                Message = message,
                SenderName = senderName,
                CreatedAt = DateTime.UtcNow,
                IsApproved = true
            };

            _context.EventMessages.Add(eventMessage);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mesajınız başarıyla eklendi!";
            return Redirect($"/Event/{eventItem.QRCode}");
        }

        // POST: Event/UploadVoiceNote
        [HttpPost]
        [Route("Event/UploadVoiceNote")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadVoiceNote(int eventId, IFormFile audioFile, string? senderName, int duration = 0)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Lütfen bir ses kaydı seçin";
                var eventForRedirect = await _context.Events.FindAsync(eventId);
                if (eventForRedirect != null)
                {
                    return Redirect($"/Event/{eventForRedirect.QRCode}");
                }
                return NotFound();
            }

            // Validate file size (5MB max for audio)
            if (audioFile.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Ses dosyası boyutu 5MB'dan küçük olmalıdır";
                var eventForRedirect = await _context.Events.FindAsync(eventId);
                return Redirect($"/Event/{eventForRedirect.QRCode}");
            }

            // Validate file type
            var allowedExtensions = new[] { ".webm", ".mp3", ".wav", ".ogg", ".m4a" };
            var extension = Path.GetExtension(audioFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] = "Sadece ses dosyaları yüklenebilir";
                var eventForRedirect = await _context.Events.FindAsync(eventId);
                return Redirect($"/Event/{eventForRedirect.QRCode}");
            }

            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem == null)
            {
                return NotFound();
            }

            try
            {
                // Save file
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "events", eventId.ToString(), "voice");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await audioFile.CopyToAsync(fileStream);
                }

                // Save to database
                var voiceNote = new Jointly.Models.EventVoiceNote
                {
                    EventId = eventId,
                    FilePath = $"/uploads/events/{eventId}/voice/{uniqueFileName}",
                    SenderName = senderName,
                    Duration = duration,
                    CreatedAt = DateTime.UtcNow,
                    IsApproved = true
                };

                _context.EventVoiceNotes.Add(voiceNote);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Ses kaydınız başarıyla yüklendi!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Yükleme hatası: {ex.Message}";
            }

            return Redirect($"/Event/{eventItem.QRCode}");
        }
    }
}
