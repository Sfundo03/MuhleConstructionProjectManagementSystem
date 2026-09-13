using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Models;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MuhleOP.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard");
            }
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        public ActionResult Dashboard()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            ViewBag.UserRole = user != null ? GetUserRole(user.Id) : "User";
            ViewBag.UserName = User.Identity.Name;

            if (User.IsInRole("Admin"))
            {
                ViewBag.TotalJobPosts = db.JobPosts.Count();
                ViewBag.TotalApplications = db.JobApplications.Count();
                ViewBag.TotalCandidates = db.Candidates.Count();
                ViewBag.TotalStaff = db.Staff.Count();
                ViewBag.TotalClients = db.Clients.Count();
                ViewBag.TotalProjects = db.Projects.Count();
                ViewBag.PendingApplications = db.JobApplications.Count(a => a.Status == "Pending");
                ViewBag.ShortlistedCount = db.JobApplications.Count(a => a.IsShortlisted);
                ViewBag.InterviewScheduledCount = db.JobApplications.Count(a => a.InterviewScheduled);
                ViewBag.HiredCount = db.JobApplications.Count(a => a.IsHired);
                ViewBag.ActiveProjects = db.Projects.Count(p => p.Status == "In Progress" || p.Status == "Initiated");
                ViewBag.CompletedProjects = db.Projects.Count(p => p.Status == "Completed");
                ViewBag.RecentApplications = db.JobApplications
                    .OrderByDescending(a => a.ApplicationDate)
                    .Take(5)
                    .ToList();
                ViewBag.RecentProjects = db.Projects
                    .OrderByDescending(p => p.CreatedDate)
                    .Take(5)
                    .ToList();

                // Equipment maintenance dashboard widgets
                ViewBag.EquipmentDueCount = db.Equipment.Count(e => e.IsActive && e.NextMaintenanceDate != null && e.NextMaintenanceDate <= DateTime.Today.AddDays(14));
                ViewBag.OverdueEquipmentCount = db.Equipment.Count(e => e.IsActive && e.NextMaintenanceDate != null && e.NextMaintenanceDate < DateTime.Today);
            }
            else if (User.IsInRole("Candidate"))
            {
                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);
                if (candidate != null)
                {
                    ViewBag.TotalApplications = db.JobApplications.Count(a => a.CandidateId == candidate.Id);
                    ViewBag.PendingApplications = db.JobApplications.Count(a => a.CandidateId == candidate.Id && a.Status == "Pending");
                    ViewBag.ShortlistedApplications = db.JobApplications.Count(a => a.CandidateId == candidate.Id && a.IsShortlisted);
                    ViewBag.InterviewScheduledCount = db.JobApplications.Count(a => a.CandidateId == candidate.Id && a.InterviewScheduled);
                    ViewBag.HiredCount = db.JobApplications.Count(a => a.CandidateId == candidate.Id && a.IsHired);
                    ViewBag.RecentApplications = db.JobApplications
                        .Where(a => a.CandidateId == candidate.Id)
                        .OrderByDescending(a => a.ApplicationDate)
                        .Take(5)
                        .ToList();
                    ViewBag.AvailableJobs = db.JobPosts
                        .Where(j => j.IsPublished && !j.IsClosed && j.ClosingDate >= DateTime.Now)
                        .OrderByDescending(j => j.DatePosted)
                        .Take(5)
                        .ToList();
                }
            }
            else if (User.IsInRole("Project Manager") || User.IsInRole("Site Manager"))
            {
                var staff = db.Staff.FirstOrDefault(s => s.UserId == user.Id);
                if (staff != null)
                {
                    ViewBag.MyProjects = db.Projects
                        .Where(p => p.ProjectManagerId == staff.Id || p.SiteManagerId == staff.Id)
                        .ToList();
                    ViewBag.MyTasks = db.DailyTasks
                        .Where(t => t.AssignedToStaffId == staff.Id && !t.IsCompleted)
                        .OrderBy(t => t.TaskDate)
                        .Take(10)
                        .ToList();
                }
            }
            else if (User.IsInRole("Client"))
            {
                // Was previously missing entirely, so a Client fell through to
                // GetUserRole() returning "User" and Dashboard()/Dashboard.cshtml
                // had nothing to render for them - hence no buttons/contracts,
                // even though notifications (which are role-agnostic) worked fine.
                var client = db.Clients.FirstOrDefault(c => c.UserId == user.Id);
                if (client != null)
                {
                    var contracts = db.Contracts
                        .Where(c => c.ClientId == client.Id)
                        .ToList();

                    ViewBag.ClientRecord = client;
                    ViewBag.TotalContracts = contracts.Count;
                    ViewBag.PendingSignatureCount = contracts.Count(c => c.Status == ContractStatus.Sent && !c.IsSigned);
                    ViewBag.ActiveContractsCount = contracts.Count(c => c.Status == ContractStatus.Active);
                    ViewBag.SignedContractsCount = contracts.Count(c => c.IsSigned);
                    ViewBag.RecentContracts = contracts
                        .OrderByDescending(c => c.CreatedDate)
                        .Take(5)
                        .ToList();

                    ViewBag.TotalProjects = db.Projects.Count(p => p.ClientId == client.Id);
                    ViewBag.ActiveProjects = db.Projects.Count(p => p.ClientId == client.Id &&
                        (p.Status == "In Progress" || p.Status == "Initiated"));
                }
            }

            return View();
        }

        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        private string GetUserRole(string userId)
        {
            if (UserManager.IsInRole(userId, "Admin")) return "Admin";
            if (UserManager.IsInRole(userId, "Candidate")) return "Candidate";
            if (UserManager.IsInRole(userId, "Project Manager")) return "Project Manager";
            if (UserManager.IsInRole(userId, "Site Manager")) return "Site Manager";
            if (UserManager.IsInRole(userId, "Finance Officer")) return "Finance Officer";
            if (UserManager.IsInRole(userId, "Crew Member")) return "Crew Member";
            if (UserManager.IsInRole(userId, "Procurement Officer")) return "Procurement Officer";
            if (UserManager.IsInRole(userId, "Client")) return "Client";
            return "User";
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
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