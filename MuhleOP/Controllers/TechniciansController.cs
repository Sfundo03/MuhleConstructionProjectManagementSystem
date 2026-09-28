using System.Linq;
using System.Web.Mvc;
using MuhleOP.Models;
using MuhleOP.ViewModels;

namespace MuhleOP.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TechniciansController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index(string search = null)
        {
            var query = db.EquipmentTechnicians.Where(t => t.IsActive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(t => t.FullName.ToLower().Contains(term) || t.Email.ToLower().Contains(term));
            }
            return View(query.OrderBy(t => t.FullName).ToList());
        }

        public ActionResult Create() => View(new EquipmentTechnicianFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EquipmentTechnicianFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var tech = new EquipmentTechnician
                {
                    FullName = model.FullName.Trim(),
                    Email = model.Email.Trim(),
                    Phone = model.Phone?.Trim(),
                    Specialization = model.Specialization?.Trim(),
                    IsActive = true
                };
                db.EquipmentTechnicians.Add(tech);
                db.SaveChanges();
                TempData["SuccessMessage"] = $"Technician '{tech.FullName}' added successfully.";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var tech = db.EquipmentTechnicians.Find(id);
            if (tech == null || !tech.IsActive)
            {
                TempData["ErrorMessage"] = "Technician not found.";
                return RedirectToAction("Index");
            }
            var model = new EquipmentTechnicianFormViewModel
            {
                Id = tech.Id,
                FullName = tech.FullName,
                Email = tech.Email,
                Phone = tech.Phone,
                Specialization = tech.Specialization,
                IsActive = tech.IsActive
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EquipmentTechnicianFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var tech = db.EquipmentTechnicians.Find(model.Id);
                if (tech == null)
                {
                    TempData["ErrorMessage"] = "Technician not found.";
                    return RedirectToAction("Index");
                }
                tech.FullName = model.FullName.Trim();
                tech.Email = model.Email.Trim();
                tech.Phone = model.Phone?.Trim();
                tech.Specialization = model.Specialization?.Trim();
                db.SaveChanges();
                TempData["SuccessMessage"] = $"Technician '{tech.FullName}' updated successfully.";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var tech = db.EquipmentTechnicians.Find(id);
            if (tech != null)
            {
                tech.IsActive = false;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Technician deactivated successfully.";
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}