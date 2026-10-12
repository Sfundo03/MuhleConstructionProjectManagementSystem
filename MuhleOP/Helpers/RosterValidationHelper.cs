// ============ Helpers/RosterValidationHelper.cs ============
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using MuhleOP.Models;

namespace MuhleOP.Helpers
{
    public static class RosterValidationHelper
    {
        // ------------------------------------------------------------------
        // Config readers (with sensible defaults)
        // ------------------------------------------------------------------

        public static decimal MaxWeeklyOrdinaryHours =>
            decimal.TryParse(ConfigurationManager.AppSettings["Roster.MaxWeeklyOrdinaryHours"], out var v) ? v : 45m;

        public static decimal MaxWeeklyOvertimeHours =>
            decimal.TryParse(ConfigurationManager.AppSettings["Roster.MaxWeeklyOvertimeHours"], out var v) ? v : 10m;

        public static double MinDailyRestHours =>
            double.TryParse(ConfigurationManager.AppSettings["Roster.MinDailyRestHours"], out var v) ? v : 9.0;

        public static double MinWeeklyRestHours =>
            double.TryParse(ConfigurationManager.AppSettings["Roster.MinWeeklyRestHours"], out var v) ? v : 36.0;

        public static double MaxShiftHours =>
            double.TryParse(ConfigurationManager.AppSettings["Roster.MaxShiftHours"], out var v) ? v : 12.0;

        public static TimeSpan DayShiftEarliestStart
        {
            get
            {
                var s = ConfigurationManager.AppSettings["Roster.DayShiftEarliestStart"] ?? "05:00";
                return TimeSpan.TryParse(s, out var t) ? t : new TimeSpan(5, 0, 0);
            }
        }

        public static TimeSpan DayShiftLatestStart
        {
            get
            {
                var s = ConfigurationManager.AppSettings["Roster.DayShiftLatestStart"] ?? "10:00";
                return TimeSpan.TryParse(s, out var t) ? t : new TimeSpan(10, 0, 0);
            }
        }

        private static HashSet<DateTime> PublicHolidays
        {
            get
            {
                var raw = ConfigurationManager.AppSettings["Roster.PublicHolidays"] ?? "";
                var set = new HashSet<DateTime>();
                foreach (var part in raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (DateTime.TryParseExact(part.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                        set.Add(d.Date);
                }
                return set;
            }
        }

        // ------------------------------------------------------------------
        // Core calculations
        // ------------------------------------------------------------------

        public static bool IsNightShift(TimeSpan startTime)
        {
            return startTime < DayShiftEarliestStart || startTime > DayShiftLatestStart;
        }

        public static decimal CalculateShiftHours(TimeSpan start, TimeSpan end)
        {
            if (end <= start)
                return 0m; // invalid / overnight not supported for day shifts
            return Math.Round((decimal)(end - start).TotalHours, 2);
        }

        public static bool IsSunday(DateTime date) => date.DayOfWeek == DayOfWeek.Sunday;

        public static bool IsPublicHoliday(DateTime date) => PublicHolidays.Contains(date.Date);

        // ------------------------------------------------------------------
        // Role → certification map (expand as needed)
        // ------------------------------------------------------------------

        public static List<string> RequiredCertificationsForRole(string shiftRole)
        {
            // TODO: Expand this map as new specialised roles are introduced.
            if (string.IsNullOrWhiteSpace(shiftRole))
                return new List<string>();

            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Crane Operator", new List<string> { "Crane Operator Certification", "Crane Operator" } },
                { "Forklift Operator", new List<string> { "Forklift Certification", "Forklift Operator" } },
                { "Electrician", new List<string> { "Electrical Trade Certificate", "Electrician" } },
                { "Scaffolding Erector", new List<string> { "Scaffolding Certification" } },
                { "Site Supervisor", new List<string> { "Site Supervisor Certificate", "Safety Officer" } },
                { "General Labour", new List<string>() }, // no special cert required
                { "Labourer", new List<string>() }
            };

            return map.ContainsKey(shiftRole) ? map[shiftRole] : new List<string>();
        }

        public static List<string> ParseCertifications(string certificationsCsv)
        {
            if (string.IsNullOrWhiteSpace(certificationsCsv))
                return new List<string>();
            return certificationsCsv
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .ToList();
        }

        // ------------------------------------------------------------------
        // Individual rule validators
        // ------------------------------------------------------------------

        public static void ValidateShiftDuration(TimeSpan start, TimeSpan end, List<string> blockers, List<string> warnings)
        {
            var hours = CalculateShiftHours(start, end);
            if (hours <= 0)
                blockers.Add("Shift end time must be after start time.");
            if ((double)hours > MaxShiftHours)
                blockers.Add($"Shift duration ({hours:N1} h) exceeds the maximum of {MaxShiftHours} hours (BCEA).");
            if (IsNightShift(start))
                blockers.Add($"Shift start time {start:hh\\:mm} is outside the permitted day-shift window ({DayShiftEarliestStart:hh\\:mm}–{DayShiftLatestStart:hh\\:mm}). Night shifts are not allowed.");
        }

        public static void ValidateSundayAndHoliday(DateTime shiftDate, List<string> warnings)
        {
            if (IsSunday(shiftDate))
                warnings.Add("This is a Sunday shift. PM acknowledgement is required before publish.");
            if (IsPublicHoliday(shiftDate))
                warnings.Add($"This shift falls on a public holiday ({shiftDate:dd MMM yyyy}). PM acknowledgement is required before publish.");
        }

        public static void ValidateWeeklyHours(
            decimal ordinaryHoursSoFar,
            decimal overtimeHoursSoFar,
            decimal proposedHours,
            List<string> blockers,
            List<string> warnings,
            out bool isOvertime)
        {
            isOvertime = false;
            var projectedOrdinary = ordinaryHoursSoFar + proposedHours;
            if (projectedOrdinary > MaxWeeklyOrdinaryHours)
            {
                isOvertime = true;
                var overtimePortion = projectedOrdinary - MaxWeeklyOrdinaryHours;
                var projectedOvertime = overtimeHoursSoFar + overtimePortion;
                if (projectedOvertime > MaxWeeklyOvertimeHours)
                {
                    blockers.Add($"Assigning this shift would exceed the maximum weekly overtime of {MaxWeeklyOvertimeHours} h (BCEA s10). Current overtime: {overtimeHoursSoFar:N1} h.");
                }
                else
                {
                    warnings.Add($"This assignment pushes the crew member into overtime ({projectedOvertime:N1} h of overtime this week). PM approval is required.");
                }
            }
        }

        public static void ValidateDailyRest(
            DateTime previousShiftEnd,
            DateTime thisShiftStart,
            List<string> blockers)
        {
            var rest = thisShiftStart - previousShiftEnd;
            if (rest.TotalHours < MinDailyRestHours)
            {
                blockers.Add($"Insufficient daily rest: only {rest.TotalHours:N1} h between shifts (minimum {MinDailyRestHours} h required – BCEA s15).");
            }
        }

        public static void ValidateLeaveOverlap(bool hasApprovedLeave, DateTime date, List<string> blockers)
        {
            if (hasApprovedLeave)
                blockers.Add($"Crew member has approved leave covering {date:dd MMM yyyy}.");
        }

        public static void ValidateCertificationMatch(
            string shiftRole,
            string staffCertificationsCsv,
            List<string> blockers,
            List<string> warnings,
            bool allowOverride)
        {
            var required = RequiredCertificationsForRole(shiftRole);
            if (!required.Any()) return;

            var held = ParseCertifications(staffCertificationsCsv);
            var missing = required.Where(r => !held.Any(h => h.IndexOf(r, StringComparison.OrdinalIgnoreCase) >= 0
                                                          || r.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            if (missing.Any())
            {
                var msg = $"Missing required certification(s) for role '{shiftRole}': {string.Join(", ", missing)}.";
                if (allowOverride)
                    warnings.Add(msg + " Override reason will be required on publish.");
                else
                    blockers.Add(msg);
            }
        }

        public static void ValidateProjectAssignment(
            bool isActivelyAssigned,
            List<string> blockers)
        {
            if (!isActivelyAssigned)
                blockers.Add("Staff member is not actively assigned to this project for the shift date.");
        }

        public static void ValidateNoOverlap(
            bool hasOverlappingShift,
            List<string> blockers)
        {
            if (hasOverlappingShift)
                blockers.Add("Staff member already has an overlapping shift on this date/time (any project).");
        }
    }
}