using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using MuhleOP.Models;

namespace MuhleOP.Controllers
{
    // Shared notification bell for every role (Admin, Candidate, Project Manager,
    // Site Manager, Client, etc). _DashboardLayout.cshtml calls these three
    // endpoints from every dashboard page:
    //   GET  /Notifications/GetNotifications
    //   POST /Notifications/MarkNotificationRead
    //   POST /Notifications/MarkAllNotificationsRead
    // Previously these only existed on CandidateController (under /Candidate/...),
    // so every other role - and even Candidate pages that don't route through
    // CandidateController - got a 404 (returned as HTML, which broke the
    // frontend's JSON.parse and logged "Unexpected token '<'").
    [Authorize]
    public class NotificationsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Notifications/GetNotifications
        public JsonResult GetNotifications()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "User not authenticated" }, JsonRequestBehavior.AllowGet);
                }

                var userId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Invalid user ID" }, JsonRequestBehavior.AllowGet);
                }

                var notifications = db.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(10)
                    .ToList();

                var formattedNotifications = notifications.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type,
                    isRead = n.IsRead,
                    link = n.Link,
                    createdDate = n.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                    timeAgo = GetTimeAgo(n.CreatedDate)
                }).ToList();

                var unreadCount = db.Notifications.Count(n => n.UserId == userId && !n.IsRead);

                return Json(new { success = true, notifications = formattedNotifications, unreadCount }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Notifications/MarkNotificationRead
        [HttpPost]
        public async Task<JsonResult> MarkNotificationRead(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var userId = User.Identity.GetUserId();
                var notification = db.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == userId);

                if (notification != null)
                {
                    notification.IsRead = true;
                    notification.ReadDate = DateTime.Now;
                    await db.SaveChangesAsync();
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Notification not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Notifications/MarkAllNotificationsRead
        [HttpPost]
        public async Task<JsonResult> MarkAllNotificationsRead()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var userId = User.Identity.GetUserId();
                var notifications = db.Notifications.Where(n => n.UserId == userId && !n.IsRead);
                var count = notifications.Count();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadDate = DateTime.Now;
                }

                await db.SaveChangesAsync();
                return Json(new { success = true, count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GetTimeAgo(DateTime date)
        {
            var timeSpan = DateTime.Now - date;
            if (timeSpan.TotalSeconds < 60)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{Math.Floor(timeSpan.TotalMinutes)} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{Math.Floor(timeSpan.TotalHours)} hours ago";
            if (timeSpan.TotalDays < 7)
                return $"{Math.Floor(timeSpan.TotalDays)} days ago";
            return date.ToString("yyyy-MM-dd");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}