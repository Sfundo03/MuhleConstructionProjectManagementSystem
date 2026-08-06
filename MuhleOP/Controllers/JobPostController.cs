using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using MuhleOP.Models;
using MuhleOP.ViewModels;

namespace MuhleOP.Controllers
{
    [Authorize]
    public class JobPostController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: JobPost
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            var jobPosts = db.JobPosts
                .OrderByDescending(j => j.DatePosted)
                .ToList();

            var viewModels = jobPosts.Select(j => new JobPostViewModel
            {
                Id = j.Id,
                JobTitle = j.JobTitle,
                JobDescription = j.JobDescription,
                Requirements = j.Requirements,
                ClosingDate = j.ClosingDate,
                JobType = j.JobType,
                ExperienceLevel = j.ExperienceLevel,
                Location = j.Location,
                SalaryRange = j.SalaryRange,
                NumberOfOpenings = j.NumberOfOpenings,
                Department = j.Department,
                IsPublished = j.IsPublished,
                DatePosted = j.DatePosted,
                PostedBy = j.PostedBy,
                ApplicationCount = db.JobApplications.Count(a => a.JobPostId == j.Id)
            }).ToList();

            return View(viewModels);
        }

        // GET: JobPost/Details/5
        [Authorize(Roles = "Admin")]
        public ActionResult Details(int id)
        {
            var jobPost = db.JobPosts.Find(id);
            if (jobPost == null)
            {
                return HttpNotFound();
            }

            var viewModel = new JobPostViewModel
            {
                Id = jobPost.Id,
                JobTitle = jobPost.JobTitle,
                JobDescription = jobPost.JobDescription,
                Requirements = jobPost.Requirements,
                ClosingDate = jobPost.ClosingDate,
                JobType = jobPost.JobType,
                ExperienceLevel = jobPost.ExperienceLevel,
                Location = jobPost.Location,
                SalaryRange = jobPost.SalaryRange,
                NumberOfOpenings = jobPost.NumberOfOpenings,
                Department = jobPost.Department,
                IsPublished = jobPost.IsPublished,
                DatePosted = jobPost.DatePosted,
                PostedBy = jobPost.PostedBy,
                ApplicationCount = db.JobApplications.Count(a => a.JobPostId == jobPost.Id)
            };

            return View(viewModel);
        }

        // GET: JobPost/Create
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: JobPost/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(JobPostViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var jobPost = new JobPost
                {
                    JobTitle = viewModel.JobTitle,
                    JobDescription = viewModel.JobDescription,
                    Requirements = viewModel.Requirements,
                    ClosingDate = viewModel.ClosingDate,
                    JobType = viewModel.JobType,
                    ExperienceLevel = viewModel.ExperienceLevel,
                    Location = viewModel.Location,
                    SalaryRange = viewModel.SalaryRange,
                    NumberOfOpenings = viewModel.NumberOfOpenings,
                    Department = viewModel.Department,
                    IsPublished = viewModel.IsPublished,
                    DatePosted = DateTime.Now,
                    PostedBy = User.Identity.Name
                };

                db.JobPosts.Add(jobPost);
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Job post created successfully!";
                return RedirectToAction("Index");
            }

            return View(viewModel);
        }

        // GET: JobPost/Edit/5
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            var jobPost = db.JobPosts.Find(id);
            if (jobPost == null)
            {
                return HttpNotFound();
            }

            var viewModel = new JobPostViewModel
            {
                Id = jobPost.Id,
                JobTitle = jobPost.JobTitle,
                JobDescription = jobPost.JobDescription,
                Requirements = jobPost.Requirements,
                ClosingDate = jobPost.ClosingDate,
                JobType = jobPost.JobType,
                ExperienceLevel = jobPost.ExperienceLevel,
                Location = jobPost.Location,
                SalaryRange = jobPost.SalaryRange,
                NumberOfOpenings = jobPost.NumberOfOpenings,
                Department = jobPost.Department,
                IsPublished = jobPost.IsPublished,
                DatePosted = jobPost.DatePosted,
                PostedBy = jobPost.PostedBy
            };

            return View(viewModel);
        }

        // POST: JobPost/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Edit(JobPostViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var jobPost = await db.JobPosts.FindAsync(viewModel.Id);
                if (jobPost == null)
                {
                    return HttpNotFound();
                }

                jobPost.JobTitle = viewModel.JobTitle;
                jobPost.JobDescription = viewModel.JobDescription;
                jobPost.Requirements = viewModel.Requirements;
                jobPost.ClosingDate = viewModel.ClosingDate;
                jobPost.JobType = viewModel.JobType;
                jobPost.ExperienceLevel = viewModel.ExperienceLevel;
                jobPost.Location = viewModel.Location;
                jobPost.SalaryRange = viewModel.SalaryRange;
                jobPost.NumberOfOpenings = viewModel.NumberOfOpenings;
                jobPost.Department = viewModel.Department;
                jobPost.IsPublished = viewModel.IsPublished;

                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Job post updated successfully!";
                return RedirectToAction("Index");
            }

            return View(viewModel);
        }

        // GET: JobPost/Delete/5
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            var jobPost = db.JobPosts.Find(id);
            if (jobPost == null)
            {
                return HttpNotFound();
            }

            var viewModel = new JobPostViewModel
            {
                Id = jobPost.Id,
                JobTitle = jobPost.JobTitle,
                JobDescription = jobPost.JobDescription,
                Requirements = jobPost.Requirements,
                ClosingDate = jobPost.ClosingDate,
                JobType = jobPost.JobType,
                ExperienceLevel = jobPost.ExperienceLevel,
                Location = jobPost.Location,
                SalaryRange = jobPost.SalaryRange,
                NumberOfOpenings = jobPost.NumberOfOpenings,
                Department = jobPost.Department,
                IsPublished = jobPost.IsPublished,
                DatePosted = jobPost.DatePosted,
                PostedBy = jobPost.PostedBy
            };

            return View(viewModel);
        }

        // POST: JobPost/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var jobPost = await db.JobPosts.FindAsync(id);
            if (jobPost != null)
            {
                // Check if there are any applications for this job post
                var hasApplications = db.JobApplications.Any(a => a.JobPostId == id);
                if (hasApplications)
                {
                    TempData["ErrorMessage"] = "Cannot delete this job post because it has applications.";
                    return RedirectToAction("Index");
                }

                db.JobPosts.Remove(jobPost);
                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Job post deleted successfully!";
            }

            return RedirectToAction("Index");
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