using Microsoft.AspNetCore.Mvc;
using Jointly.Models;
using Jointly.Data;
using Jointly.Filters;
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
                .Where(e => e.UserId == userId && e.IsActive)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View(events);
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
        public async Task<IActionResult> Edit(int id, Event model)
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

            eventItem.Title = model.Title;
            eventItem.Description = model.Description;
            eventItem.EventDate = model.EventDate;
            eventItem.Location = model.Location;
            eventItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Etkinlik başarıyla güncellendi!";
            return RedirectToAction("Index");
        }

        // POST: Dashboard/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (eventItem == null)
            {
                return NotFound();
            }

            // Soft delete
            eventItem.IsActive = false;
            eventItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Etkinlik başarıyla silindi!";
            return RedirectToAction("Index");
        }
    }
}
