using System;
using System.ComponentModel.DataAnnotations;

namespace Jointly.Models
{
    public class EventMedia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }
        
        public Event? Event { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string FileType { get; set; } = string.Empty; // Image or Video

        [MaxLength(100)]
        public string? UploadedBy { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = true;
    }
}
