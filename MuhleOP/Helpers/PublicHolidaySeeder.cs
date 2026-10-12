// Helpers/PublicHolidaySeeder.cs
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MuhleOP.Models;

namespace MuhleOP.Helpers
{
    public static class PublicHolidaySeeder
    {
        public static void Seed(ApplicationDbContext db)
        {
            if (db.PublicHolidays.Any()) return;

            var holidays = new List<PublicHoliday>();

            // Fixed-date holidays + observed Mondays when they fall on Sunday
            void AddFixed(int year, int month, int day, string name)
            {
                var d = new DateTime(year, month, day);
                holidays.Add(new PublicHoliday { HolidayDate = d, HolidayName = name, Year = year });
                if (d.DayOfWeek == DayOfWeek.Sunday)
                {
                    holidays.Add(new PublicHoliday
                    {
                        HolidayDate = d.AddDays(1),
                        HolidayName = name + " (observed)",
                        Year = year
                    });
                }
            }

            // Easter (Good Friday / Family Day) – approximate for demo; real systems use an algorithm
            // 2024: 29 Mar / 1 Apr; 2025: 18 Apr / 21 Apr; 2026: 3 Apr / 6 Apr
            var easter = new Dictionary<int, (DateTime GoodFriday, DateTime FamilyDay)>
            {
                { 2024, (new DateTime(2024, 3, 29), new DateTime(2024, 4, 1)) },
                { 2025, (new DateTime(2025, 4, 18), new DateTime(2025, 4, 21)) },
                { 2026, (new DateTime(2026, 4, 3),  new DateTime(2026, 4, 6)) }
            };

            foreach (var year in new[] { 2024, 2025, 2026 })
            {
                AddFixed(year, 1, 1, "New Year's Day");
                AddFixed(year, 3, 21, "Human Rights Day");
                holidays.Add(new PublicHoliday { HolidayDate = easter[year].GoodFriday, HolidayName = "Good Friday", Year = year });
                holidays.Add(new PublicHoliday { HolidayDate = easter[year].FamilyDay, HolidayName = "Family Day", Year = year });
                AddFixed(year, 4, 27, "Freedom Day");
                AddFixed(year, 5, 1, "Workers' Day");
                AddFixed(year, 6, 16, "Youth Day");
                AddFixed(year, 8, 9, "National Women's Day");
                AddFixed(year, 9, 24, "Heritage Day");
                AddFixed(year, 12, 16, "Day of Reconciliation");
                AddFixed(year, 12, 25, "Christmas Day");
                AddFixed(year, 12, 26, "Day of Goodwill");
            }

            db.PublicHolidays.AddRange(holidays);
            db.SaveChanges();
        }

        public static bool IsPublicHoliday(ApplicationDbContext db, DateTime date)
        {
            var d = date.Date;
            return db.PublicHolidays.Any(h => DbFunctions.TruncateTime(h.HolidayDate) == d);
        }
    }
}