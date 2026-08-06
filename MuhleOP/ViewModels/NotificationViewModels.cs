using System;
using System.Collections.Generic;

namespace MuhleOP.ViewModels
{
    public class ApplicationStatusViewModel
    {
        public int ApplicationId { get; set; }
        public string JobTitle { get; set; }
        public string Status { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string CoverLetter { get; set; }
        public bool IsShortlisted { get; set; }
        public bool InterviewScheduled { get; set; }
        public DateTime? InterviewDateTime { get; set; }
        public string InterviewNotes { get; set; }
        public double RankingScore { get; set; }
        public int SkillsMatchPercentage { get; set; }
        public List<ApplicationStatusHistoryViewModel> StatusHistory { get; set; }
    }

    public class ApplicationStatusHistoryViewModel
    {
        public string Status { get; set; }
        public string Notes { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangeDate { get; set; }
    }

    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public bool IsRead { get; set; }
        public string Link { get; set; }
        public DateTime CreatedDate { get; set; }
        public string TimeAgo { get; set; }
    }
}