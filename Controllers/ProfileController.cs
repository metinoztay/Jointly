using Microsoft.AspNetCore.Mvc;
using Jointly.Data;
using Jointly.Models;
using Jointly.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Jointly.Controllers
{
    [AuthorizeSession]
    public class ProfileController : Controller
    {
        private readonly JointlyDbContext _context;

        public ProfileController(JointlyDbContext context)
        {
            _context = context;
        }

        // GET: Profile
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Profile/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(UpdateProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(new { success = false, message = string.Join(", ", errors) });
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Oturum süresi dolmuş" });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı" });
            }

            // Check if email is already used by another user
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.Id != userId.Value);
            
            if (existingUser != null)
            {
                return BadRequest(new { success = false, message = "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor" });
            }

            // Update user info
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;

            try
            {
                await _context.SaveChangesAsync();
                
                // Update session if needed
                HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
                
                return Ok(new { success = true, message = "Profil bilgileriniz başarıyla güncellendi!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Güncelleme sırasında bir hata oluştu: " + ex.Message });
            }
        }

        // POST: Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(new { success = false, message = string.Join(", ", errors) });
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Oturum süresi dolmuş" });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı" });
            }

            // Verify current password
            var hashedCurrentPassword = HashPassword(model.CurrentPassword);
            if (user.PasswordHash != hashedCurrentPassword)
            {
                return BadRequest(new { success = false, message = "Mevcut şifreniz yanlış" });
            }

            // Hash new password
            user.PasswordHash = HashPassword(model.NewPassword);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Şifreniz başarıyla değiştirildi!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Şifre değiştirme sırasında bir hata oluştu: " + ex.Message });
            }
        }

        // Helper method for password hashing
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
