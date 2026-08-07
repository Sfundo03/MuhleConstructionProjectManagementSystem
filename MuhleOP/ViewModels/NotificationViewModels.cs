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
        public string InterviewResult { get; set; }
        public string InterviewFeedback { get; set; }
        public bool IsHired { get; set; }
        public bool IsInterviewCompleted { get; set; }
        public double RankingScore { get; set; }
        public int SkillsMatchPercentage { get; set; }
        public int? QuizScore { get; set; }
        public bool? QuizPassed { get; set; }
        public string QuizResult { get; set; }
        public List<ApplicationStatusHistoryViewModel> StatusHistory { get; set; }

        public ApplicationStatusViewModel()
        {
            StatusHistory = new List<ApplicationStatusHistoryViewModel>();
            InterviewResult = "Pending";
            IsHired = false;
            IsInterviewCompleted = false;
            QuizResult = "Not Taken";
        }
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
        public string Priority { get; set; }
        public string IconClass => GetIconClass();
        public string TypeIcon => GetTypeIcon();

        private string GetIconClass()
        {
            switch (Type)
            {
                case "Application": return "application";
                case "Interview": return "interview";
                case "Status": return "status";
                case "System": return "system";
                case "Project": return "project";
                case "Finance": return "finance";
                case "Maintenance": return "maintenance";
                default: return "system";
            }
        }

        private string GetTypeIcon()
        {
            switch (Type)
            {
                case "Application": return "file-alt";
                case "Interview": return "calendar-check";
                case "Status": return "clock";
                case "Project": return "project-diagram";
                case "Finance": return "coins";
                case "Maintenance": return "tools";
                case "System":
                default: return "bell";
            }
        }
    }
}