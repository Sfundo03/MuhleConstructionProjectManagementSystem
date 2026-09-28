namespace MuhleOP.Helpers
{
    using System;
    using System.Linq;
    using MuhleOP.Models;

    public static class InspectionTemplateSeeder
    {
        public static void Seed()
        {
            using (var db = new ApplicationDbContext())
            {
                var safetyItems = new[]
                {
                    ("Safety", "Are all workers wearing hard hats and high-vis vests?", "PPE", 1),
                    ("Safety", "Are fire extinguishers present and within inspection date?", "Fire Safety", 2),
                    ("Safety", "Is the site perimeter secured and signage visible?", "Site Security", 3),
                    ("Safety", "Are scaffolding and ladders inspected and tagged?", "Fall Protection", 4),
                    ("Safety", "Is first-aid equipment stocked and accessible?", "Health & Welfare", 5)
                };

                var qualityItems = new[]
                {
                    ("Quality", "Has concrete been sampled and tested per spec?", "Foundation", 1),
                    ("Quality", "Are structural connections torqued to specification?", "Framing", 2),
                    ("Quality", "Is rebar spacing and cover within tolerance?", "Foundation", 3),
                    ("Quality", "Are electrical conduits properly earthed?", "Electrical", 4),
                    ("Quality", "Have plumbing pressure tests been recorded?", "Plumbing", 5)
                };

                var progressItems = new[]
                {
                    ("Progress", "Is the current phase on schedule?", "Schedule", 1),
                    ("Progress", "Are blockers logged and assigned?", "Management", 2),
                    ("Progress", "Have daily outputs been recorded?", "Reporting", 3),
                    ("Progress", "Are materials on site sufficient for the next 5 days?", "Materials", 4),
                    ("Progress", "Have subcontractor milestones been confirmed?", "Subcontractors", 5)
                };

                var complianceItems = new[]
                {
                    ("Compliance", "Are statutory permits displayed on site?", "Permits", 1),
                    ("Compliance", "Is the site safety file up to date?", "Documentation", 2),
                    ("Compliance", "Have inductions been completed for all new workers?", "Training", 3),
                    ("Compliance", "Is waste disposal compliant with local regulations?", "Environmental", 4),
                    ("Compliance", "Are incident reports filed and closed?", "Safety Records", 5)
                };

                var allTemplates = safetyItems.Concat(qualityItems).Concat(progressItems).Concat(complianceItems);

                foreach (var t in allTemplates)
                {
                    var exists = db.InspectionTemplateItems.Any(x => x.InspectionType == t.Item1 && x.ItemLabel == t.Item2);
                    if (!exists)
                    {
                        db.InspectionTemplateItems.Add(new InspectionTemplateItem
                        {
                            InspectionType = t.Item1,
                            ItemLabel = t.Item2,
                            Category = t.Item3,
                            Order = t.Item4,
                            IsActive = true
                        });
                    }
                }

                db.SaveChanges();
            }
        }
    }
}