using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhleOP.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required]
        [Display(Name = "Title")]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Message")]
        [DataType(DataType.MultilineText)]
        public string Message { get; set; }

        [Display(Name = "Type")]
        public string Type { get; set; } // Application, Interview, Status, System, Project, Finance, Maintenance

        [Display(Name = "Is Read")]
        public bool IsRead { get; set; }

        [Display(Name = "Link")]
        [StringLength(500)]
        public string Link { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Read Date")]
        public DateTime? ReadDate { get; set; }

        [Display(Name = "Is Email Sent")]
        public bool IsEmailSent { get; set; }

        [Display(Name = "Email Sent Date")]
        public DateTime? EmailSentDate { get; set; }

        [Display(Name = "Priority")]
        public string Priority { get; set; } // Low, Normal, High, Urgent

        public Notification()
        {
            CreatedDate = DateTime.Now;
            IsRead = false;
            IsEmailSent = false;
            Priority = "Normal";
        }
    }
}