using System.Linq;
using System.Web.Mvc;
using MuhleOP.Models;
using MuhleOP.ViewModels;

namespace MuhleOP.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PartsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index(string search = null)
        {
            var query = db.EquipmentParts.Where(p => p.IsActive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.PartName.ToLower().Contains(term) || (p.PartCode != null && p.PartCode.ToLower().Contains(term)));
            }
            return View(query.OrderBy(p => p.PartName).ToList());
        }

        public ActionResult Create() => View(new EquipmentPartFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EquipmentPartFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var part = new EquipmentPart
                {
                    PartName = model.PartName.Trim(),
                    PartCode = string.IsNullOrWhiteSpace(model.PartCode) ? "PART-" + System.DateTime.Today.ToString("yyyyMMdd") + "-" + new System.Random().Next(1000, 9999) : model.PartCode.Trim(),
                    Unit = string.IsNullOrWhiteSpace(model.Unit) ? "each" : model.Unit.Trim(),
                    QuantityOnHand = model.QuantityOnHand,
                    MinimumStockLevel = model.MinimumStockLevel,
                    UnitCost = model.UnitCost ?? 0m,
                    IsActive = true
                };
                db.EquipmentParts.Add(part);
                db.SaveChanges();
                TempData["SuccessMessage"] = $"Part '{part.PartName}' created successfully.";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var part = db.EquipmentParts.Find(id);
            if (part == null || !part.IsActive)
            {
                TempData["ErrorMessage"] = "Part not found.";
                return RedirectToAction("Index");
            }
            var model = new EquipmentPartFormViewModel
            {
                Id = part.Id,
                PartName = part.PartName,
                PartCode = part.PartCode,
                Unit = part.Unit,
                QuantityOnHand = part.QuantityOnHand,
                MinimumStockLevel = part.MinimumStockLevel,
                UnitCost = part.UnitCost,
                IsActive = part.IsActive
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EquipmentPartFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var part = db.EquipmentParts.Find(model.Id);
                if (part == null)
                {
                    TempData["ErrorMessage"] = "Part not found.";
                    return RedirectToAction("Index");
                }
                part.PartName = model.PartName.Trim();
                part.PartCode = model.PartCode?.Trim();
                part.Unit = model.Unit?.Trim();
                part.QuantityOnHand = model.QuantityOnHand;
                part.MinimumStockLevel = model.MinimumStockLevel;
                part.UnitCost = model.UnitCost ?? 0m;
                db.SaveChanges();
                TempData["SuccessMessage"] = $"Part '{part.PartName}' updated successfully.";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var part = db.EquipmentParts.Find(id);
            if (part != null)
            {
                part.IsActive = false;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Part deactivated successfully.";
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