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

        [Required]
        [Display(Name = "Title")]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Message")]
        [DataType(DataType.MultilineText)]
        public string Message { get; set; }

        [Display(Name = "Type")]
        public string Type { get; set; } // Application, Interview, Status, System

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

        public Notification()
        {
            CreatedDate = DateTime.Now;
            IsRead = false;
            IsEmailSent = false;
        }
    }

    public class ApplicationStatusHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ApplicationId { get; set; }

        [ForeignKey("ApplicationId")]
        public virtual JobApplication JobApplication { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "Changed By")]
        public string ChangedBy { get; set; }

        [Display(Name = "Change Date")]
        public DateTime ChangeDate { get; set; }

        public ApplicationStatusHistory()
        {
            ChangeDate = DateTime.Now;
        }
    }
}