// Models/PublicHoliday.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace MuhleOP.Models
{
    public class PublicHoliday
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime HolidayDate { get; set; }

        [Required]
        [StringLength(100)]
        public string HolidayName { get; set; }

        [Required]
        public int Year { get; set; }
    }
}