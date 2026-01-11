using System;
using System.ComponentModel.DataAnnotations;

namespace Jointly.Models
{
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [MaxLength(300)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? HeaderImagePath { get; set; }

        [MaxLength(100)]
        public string QRCode { get; set; } = string.Empty;

        public int UserId { get; set; }
        
        public User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
