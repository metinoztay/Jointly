using Microsoft.AspNetCore.Mvc;
using Jointly.Models;
using Jointly.Data;
using Jointly.Filters;
using Jointly.Services;
using Jointly.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Jointly.Controllers
{
    [AuthorizeSession]
    public class DashboardController : Controller
    {
        private readonly JointlyDbContext _context;

        public DashboardController(JointlyDbContext context)
        {
            _context = context;
        }

        // GET: Dashboard
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var events = await _context.Events
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.IsActive)
                .ThenByDescending(e => e.CreatedAt)
                .ToListAsync();

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View(events);
        }

        // GET: Dashboard/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var eventItem = await _context.Events
                .Include(e => e.EventMedia)
                .Include(e => e.EventMessages)
                .Include(e => e.EventVoiceNotes)
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (eventItem == null)
            {
                return NotFound();
            }

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View(eventItem);
        }

        // GET: Dashboard/DownloadAllMedia/5
        public async Task<IActionResult> DownloadAllMedia(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var eventItem = await _context.Events
                .Include(e => e.EventMedia)
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (eventItem == null || eventItem.EventMedia == null || !eventItem.EventMedia.Any())
            {
                return NotFound();
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    int counter = 1;
                    foreach (var media in eventItem.EventMedia)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", media.FilePath.TrimStart('/').Replace("/", "\\"));
                        
                        if (System.IO.File.Exists(filePath))
                        {
                            var fileExtension = Path.GetExtension(filePath);
                            var fileName = $"{counter:D3}_{Path.GetFileName(filePath)}";
                            
                            var zipEntry = archive.CreateEntry(fileName, System.IO.Compression.CompressionLevel.Fastest);
                            using (var zipStream = zipEntry.Open())
                            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                            {
                                await fileStream.CopyToAsync(zipStream);
                            }
                            counter++;
                        }
                    }
                }

                memoryStream.Position = 0;
                var zipFileName = $"{eventItem.Title.Replace(" ", "_")}_medyalar.zip";
                return File(memoryStream.ToArray(), "application/zip", zipFileName);
            }
        }

        // GET: Dashboard/DownloadAllVoiceNotes/5
        public async Task<IActionResult> DownloadAllVoiceNotes(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var eventItem = await _context.Events
                .Include(e => e.EventVoiceNotes)
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (eventItem == null || eventItem.EventVoiceNotes == null || !eventItem.EventVoiceNotes.Any())
            {
                return NotFound();
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    int counter = 1;
                    foreach (var voice in eventItem.EventVoiceNotes.OrderBy(v => v.CreatedAt))
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", voice.FilePath.TrimStart('/').Replace("/", "\\"));
                        
                        if (System.IO.File.Exists(filePath))
                        {
                            var fileExtension = Path.GetExtension(filePath);
                            var senderName = !string.IsNullOrEmpty(voice.SenderName) ? voice.SenderName.Replace(" ", "_") : "Anonim";
                            var fileName = $"{counter:D3}_{senderName}{fileExtension}";
                            
                            var zipEntry = archive.CreateEntry(fileName, System.IO.Compression.CompressionLevel.Fastest);
                            using (var zipStream = zipEntry.Open())
                            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                            {
                                await fileStream.CopyToAsync(zipStream);
                            }
                            counter++;
                        }
                    }
                }

                memoryStream.Position = 0;
                var zipFileName = $"{eventItem.Title.Replace(" ", "_")}_sesli_mesajlar.zip";
                return File(memoryStream.ToArray(), "application/zip", zipFileName);
            }
        }

        // GET: Dashboard/DownloadAllMessages/5
        public async Task<IActionResult> DownloadAllMessages(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var eventItem = await _context.Events
                .Include(e => e.EventMessages)
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (eventItem == null || eventItem.EventMessages == null || !eventItem.EventMessages.Any())
            {
                return NotFound();
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Etkinlik: {eventItem.Title}");
            sb.AppendLine($"Tarih: {eventItem.EventDate:dd MMMM yyyy, HH:mm}");
            sb.AppendLine($"Konum: {eventItem.Location}");
            sb.AppendLine();
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine($"MESAJLAR (Toplam: {eventItem.EventMessages.Count})");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();

            int counter = 1;
            foreach (var message in eventItem.EventMessages.OrderBy(m => m.CreatedAt))
            {
                sb.AppendLine($"[{counter}] {message.CreatedAt.ToLocalTime():dd MMM yyyy, HH:mm}");
                sb.AppendLine($"Gönderen: {(string.IsNullOrEmpty(message.SenderName) ? "Anonim" : message.SenderName)}");
                sb.AppendLine();
                sb.AppendLine(message.Message);
                sb.AppendLine();
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine();
                counter++;
            }

            var content = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"{eventItem.Title.Replace(" ", "_")}_mesajlar.txt";
            
            return File(content, "text/plain", fileName);
        }

        // GET: Dashboard/DownloadEventCard/5
        public async Task<IActionResult> DownloadEventCard(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (eventItem == null)
            {
                return NotFound();
            }

            // Generate Event URL
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var eventUrl = $"{baseUrl}/Event/{eventItem.QRCode}";

            // Generate QR Code bytes
            var qrCodeBytes = QRCodeHelper.GenerateQRCodeBytes(eventUrl);

            // Generate PDF
            var pdfService = new EventCardPdfService();
            var pdfBytes = pdfService.GenerateEventCardPdf(eventItem, eventUrl, qrCodeBytes);

            var fileName = $"{eventItem.Title.Replace(" ", "_")}_Event_Card.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // POST: Dashboard/DeleteMedia/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            
            // Find the media item and verify ownership through the event
            var mediaItem = await _context.EventMedia
                .Include(m => m.Event)
                .FirstOrDefaultAsync(m => m.Id == id && m.Event.UserId == userId);

            if (mediaItem == null)
            {
                return NotFound();
            }

            var eventId = mediaItem.EventId;

            // Delete the physical file
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", mediaItem.FilePath.TrimStart('/').Replace("/", "\\"));
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    // Log error but continue with database deletion
                    Console.WriteLine($"Error deleting file: {ex.Message}");
                }
            }

            // Delete from database
            _context.EventMedia.Remove(mediaItem);
            await _context.SaveChangesAsync();

            // Return JSON for AJAX requests
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.ContentType?.Contains("application/json") == true)
            {
                return Json(new { success = true, message = "Medya başarıyla silindi." });
            }

            TempData["SuccessMessage"] = "Medya başarıyla silindi.";
            return RedirectToAction("Details", new { id = eventId });
        }

        // POST: Dashboard/DeleteMessage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            
            // Find the message and verify ownership through the event
            var messageItem = await _context.EventMessages
                .Include(m => m.Event)
                .FirstOrDefaultAsync(m => m.Id == id && m.Event.UserId == userId);

            if (messageItem == null)
            {
                return NotFound();
            }

            var eventId = messageItem.EventId;

            // Delete from database
            _context.EventMessages.Remove(messageItem);
            await _context.SaveChangesAsync();

            // Return JSON for AJAX requests
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.ContentType?.Contains("application/json") == true)
            {
                return Json(new { success = true, message = "Mesaj başarıyla silindi." });
            }

            TempData["SuccessMessage"] = "Mesaj başarıyla silindi.";
            return RedirectToAction("Details", new { id = eventId });
        }

        // POST: Dashboard/DeleteVoiceNote/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVoiceNote(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            
            // Find the voice note and verify ownership through the event
            var voiceNote = await _context.EventVoiceNotes
                .Include(v => v.Event)
                .FirstOrDefaultAsync(v => v.Id == id && v.Event.UserId == userId);

            if (voiceNote == null)
            {
                return NotFound();
            }

            var eventId = voiceNote.EventId;

            // Delete the physical file
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", voiceNote.FilePath.TrimStart('/').Replace("/", "\\"));
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    // Log error but continue with database deletion
                    Console.WriteLine($"Error deleting file: {ex.Message}");
                }
            }

            // Delete from database
            _context.EventVoiceNotes.Remove(voiceNote);
            await _context.SaveChangesAsync();

            // Return JSON for AJAX requests
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.ContentType?.Contains("application/json") == true)
            {
                return Json(new { success = true, message = "Sesli mesaj başarıyla silindi." });
            }

            TempData["SuccessMessage"] = "Sesli mesaj başarıyla silindi.";
            return RedirectToAction("Details", new { id = eventId });
        }

        // GET: Dashboard/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Dashboard/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event model, IFormFile? headerImage)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            // Validate header image
            if (headerImage == null || headerImage.Length == 0)
            {
                ModelState.AddModelError("headerImage", "Header fotoğrafı seçmeniz gereklidir");
                return View(model);
            }

            // Handle file upload
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "events");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{headerImage.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await headerImage.CopyToAsync(fileStream);
            }

            model.HeaderImagePath = $"/uploads/events/{uniqueFileName}";

            // Generate unique QR code
            model.QRCode = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
            model.UserId = userId.Value;
            model.CreatedAt = DateTime.UtcNow;
            model.IsActive = true;

            _context.Events.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Etkinlik başarıyla oluşturuldu!";
            return RedirectToAction("Index");
        }

        // GET: Dashboard/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (eventItem == null)
            {
                return NotFound();
            }

            return View(eventItem);
        }

        // POST: Dashboard/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event model, IFormFile? headerImage)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (eventItem == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Handle file upload if new image provided
            if (headerImage != null && headerImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "events");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{headerImage.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await headerImage.CopyToAsync(fileStream);
                }

                // Delete old image if exists
                if (!string.IsNullOrEmpty(eventItem.HeaderImagePath))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", eventItem.HeaderImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                eventItem.HeaderImagePath = $"/uploads/events/{uniqueFileName}";
            }

            eventItem.Title = model.Title;
            eventItem.Description = model.Description;
            eventItem.EventDate = model.EventDate;
            eventItem.Location = model.Location;
            eventItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Etkinlik başarıyla güncellendi!";
            return RedirectToAction("Index");
        }

        // POST: Dashboard/EndEvent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndEvent(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (eventItem == null)
            {
                return NotFound();
            }

            eventItem.IsActive = false;
            eventItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Etkinlik başarıyla sonlandırıldı!";
            return RedirectToAction("Index");
        }

        // POST: Dashboard/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var eventItem = await _context.Events
                .Include(e => e.EventMedia)
                .Include(e => e.EventVoiceNotes)
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (eventItem == null)
            {
                return NotFound();
            }

            // Delete the entire event folder (includes all media and voice notes)
            var eventFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "events", id.ToString());
            if (Directory.Exists(eventFolderPath))
            {
                try
                {
                    Directory.Delete(eventFolderPath, recursive: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting event folder: {ex.Message}");
                }
            }

            // Delete header image file if exists
            if (!string.IsNullOrEmpty(eventItem.HeaderImagePath))
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", eventItem.HeaderImagePath.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    try
                    {
                        System.IO.File.Delete(imagePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting header image: {ex.Message}");
                    }
                }
            }

            // Hard delete from database (cascade delete will handle related records)
            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Etkinlik başarıyla silindi!";
            return RedirectToAction("Index");
        }
    }
}
