using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jointly.Models
{
    public class EventVoiceNote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(100)]
        public string? SenderName { get; set; }

        public int Duration { get; set; } // in seconds

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = true;

        // Navigation property
        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }
    }
}
