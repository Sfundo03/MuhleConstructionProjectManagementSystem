using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MuhleOP.Controllers
{
    [Authorize]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private ApplicationUserManager _userManager;

        public StaffController()
        {
        }

        public StaffController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }

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

        // GET: Staff/Hire
        [Authorize(Roles = "Admin")]
        public ActionResult Hire()
        {
            System.Diagnostics.Debug.WriteLine("=== HIRE STAFF PAGE LOADED ===");
            return View();
        }

        // POST: Staff/Hire
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Hire(CreateStaffViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("=== HIRE STAFF POST STARTED ===");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await UserManager.FindByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "A user with this email already exists.");
                        return View(model);
                    }

                    string password = GenerateRandomPassword();

                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        EmailConfirmed = true
                    };

                    var result = await UserManager.CreateAsync(user, password);
                    System.Diagnostics.Debug.WriteLine($"User creation result: {result.Succeeded}");

                    if (result.Succeeded)
                    {
                        var roleResult = await UserManager.AddToRoleAsync(user.Id, model.Role);

                        var staff = new Staff
                        {
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            Email = model.Email,
                            Role = model.Role,
                            PhoneNumber = model.PhoneNumber,
                            Department = model.Department,
                            UserId = user.Id,
                            HireDate = DateTime.Now,
                            IsActive = true
                        };

                        db.Staff.Add(staff);
                        await db.SaveChangesAsync();

                        // Check if hiring from candidate
                        if (!string.IsNullOrEmpty(Request.Form["ApplicationId"]))
                        {
                            int applicationId = int.Parse(Request.Form["ApplicationId"]);
                            var application = await db.JobApplications.FindAsync(applicationId);
                            if (application != null)
                            {
                                application.IsHired = true;
                                application.Status = "Hired";
                                await db.SaveChangesAsync();

                                await CreateNotificationForCandidate(
                                    application.CandidateId,
                                    "You've Been Hired!",
                                    $"Congratulations! You have been hired for the position of '{application.JobPost?.JobTitle ?? "the position"}'. Welcome to the team!",
                                    "System",
                                    $"/Candidate/ApplicationStatus/{application.Id}"
                                );
                            }
                        }

                        await CreateNotification(
                            User.Identity.GetUserId(),
                            "New Staff Member Hired",
                            $"You have successfully hired {model.FirstName} {model.LastName} as a {model.Role}.",
                            "System",
                            "/Staff/Index"
                        );

                        TempData["SuccessMessage"] = $"{model.FirstName} {model.LastName} has been hired successfully! Login details have been sent to their email.";
                        return RedirectToAction("Index");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while hiring staff: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: Staff/Index
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            var staffMembers = db.Staff
                .OrderByDescending(s => s.HireDate)
                .ToList();

            var viewModels = staffMembers.Select(s => new StaffViewModel
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                FullName = s.FullName,
                Email = s.Email,
                Role = s.Role,
                PhoneNumber = s.PhoneNumber,
                EmployeeId = s.EmployeeId,
                Department = s.Department,
                HireDate = s.HireDate,
                IsActive = s.IsActive,
                ProfilePicturePath = s.ProfilePicturePath
            }).ToList();

            var viewModel = new StaffListViewModel
            {
                StaffMembers = viewModels,
                TotalStaff = viewModels.Count,
                ActiveStaff = viewModels.Count(s => s.IsActive),
                InactiveStaff = viewModels.Count(s => !s.IsActive)
            };

            return View(viewModel);
        }

        // GET: Staff/Details/5
        [Authorize(Roles = "Admin")]
        public ActionResult Details(int id)
        {
            var staff = db.Staff.Find(id);
            if (staff == null)
            {
                return HttpNotFound();
            }

            var viewModel = new StaffViewModel
            {
                Id = staff.Id,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                FullName = staff.FullName,
                Email = staff.Email,
                Role = staff.Role,
                PhoneNumber = staff.PhoneNumber,
                EmployeeId = staff.EmployeeId,
                Department = staff.Department,
                HireDate = staff.HireDate,
                IsActive = staff.IsActive,
                ProfilePicturePath = staff.ProfilePicturePath
            };

            return View(viewModel);
        }

        // POST: Staff/ToggleActive/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ToggleActive(int id)
        {
            var staff = db.Staff.Find(id);
            if (staff == null)
            {
                return Json(new { success = false, message = "Staff member not found" });
            }

            staff.IsActive = !staff.IsActive;
            await db.SaveChangesAsync();

            return Json(new { success = true, isActive = staff.IsActive });
        }

        // GET: Staff/HireFromCandidate/5
        [Authorize(Roles = "Admin")]
        public ActionResult HireFromCandidate(int id, int? applicationId)
        {
            var candidate = db.Candidates.Find(id);
            if (candidate == null)
            {
                TempData["ErrorMessage"] = "Candidate not found.";
                return RedirectToAction("Index", "JobPost");
            }

            var viewModel = new CreateStaffViewModel
            {
                FirstName = candidate.FirstName,
                LastName = candidate.LastName,
                Email = candidate.Email,
                PhoneNumber = candidate.PhoneNumber
            };

            ViewBag.CandidateId = id;
            ViewBag.ApplicationId = applicationId;
            return View("Hire", viewModel);
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new StringBuilder();
            for (int i = 0; i < 12; i++)
            {
                password.Append(chars[random.Next(chars.Length)]);
            }
            return password.ToString();
        }

        private async Task CreateNotification(string userId, string title, string message, string type, string link)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                db.Notifications.Add(notification);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating notification: {ex.Message}");
            }
        }

        private async Task CreateNotificationForCandidate(int candidateId, string title, string message, string type, string link)
        {
            try
            {
                var candidate = db.Candidates.Find(candidateId);
                if (candidate == null) return;

                var notification = new Notification
                {
                    UserId = candidate.UserId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                db.Notifications.Add(notification);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating notification: {ex.Message}");
            }
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