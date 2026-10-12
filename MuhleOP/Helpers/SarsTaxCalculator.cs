// Helpers/SarsTaxCalculator.cs
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using MuhleOP.Models;

namespace MuhleOP.Helpers
{
    public class PayrollTaxResult
    {
        public decimal GrossPay { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal PayeBeforeRebate { get; set; }
        public decimal PrimaryRebateMonthly { get; set; }
        public decimal MedicalTaxCredit { get; set; }
        public decimal Paye { get; set; }                 // final PAYE (code 4001)
        public decimal UifEmployee { get; set; }          // code 4003
        public decimal UifEmployer { get; set; }
        public decimal Sdl { get; set; }                  // employer only
        public decimal RetirementDeduction { get; set; }  // s11F capped
        public decimal NetPay { get; set; }
        public string TaxYear { get; set; }
    }

    public static class SarsTaxCalculator
    {
        // 2024/25 annual brackets (under-65)
        private static readonly List<(decimal Upper, decimal BaseTax, decimal Rate, decimal Threshold)> Brackets2024_25 =
            new List<(decimal, decimal, decimal, decimal)>
            {
                (237100m, 0m, 0.18m, 0m),
                (370500m, 42678m, 0.26m, 237100m),
                (512800m, 77362m, 0.31m, 370500m),
                (673000m, 121475m, 0.36m, 512800m),
                (857900m, 179147m, 0.39m, 673000m),
                (1817000m, 251258m, 0.41m, 857900m),
                (decimal.MaxValue, 644489m, 0.45m, 1817000m)
            };

        private const decimal PrimaryRebateAnnual = 17235m;
        private const decimal SecondaryRebateAnnual = 9444m;   // 65+
        private const decimal TertiaryRebateAnnual = 3145m;    // 75+
        private const decimal UifMonthlyCap = 177.12m;
        private const decimal UifRate = 0.01m;
        private const decimal SdlRate = 0.01m;
        private const decimal MedicalMain = 364m;
        private const decimal MedicalFirstDep = 364m;
        private const decimal MedicalAdditional = 246m;
        private const decimal RetirementPct = 0.275m;
        private const decimal RetirementAnnualCap = 350000m;

        public static PayrollTaxResult CalculatePayrollTaxes(Payroll payroll, Staff staff)
        {
            var taxYear = ConfigurationManager.AppSettings["Payroll.TaxYear"] ?? "2024/2025";
            payroll.TaxYear = taxYear;

            // 1. Gross already computed by CalculateTotals (Regular + OT + Sunday + Holiday + Bonus + OtherAdditions)
            var gross = payroll.GrossPay;

            // 2. Retirement (s11F) – monthly
            var pension = payroll.PensionContribution ?? 0m;
            var retirementCapMonthly = RetirementAnnualCap / 12m;
            var retirementLimit = Math.Min(gross * RetirementPct, retirementCapMonthly);
            var retirementDeduction = Math.Min(pension, retirementLimit);
            payroll.RetirementDeduction = Math.Round(retirementDeduction, 2);

            var taxableMonthly = Math.Max(0m, gross - retirementDeduction);
            var taxableAnnual = taxableMonthly * 12m;

            // 3. Annual PAYE from brackets
            decimal annualTax = 0m;
            foreach (var b in Brackets2024_25)
            {
                if (taxableAnnual <= b.Upper)
                {
                    annualTax = b.BaseTax + (taxableAnnual - b.Threshold) * b.Rate;
                    break;
                }
            }

            // Age-based rebates (default under-65)
            int age = 40; // safe default
            if (staff != null)
            {
                // If DateOfBirth ever added: age = ...
            }
            decimal rebateAnnual = PrimaryRebateAnnual;
            if (age >= 75) rebateAnnual += SecondaryRebateAnnual + TertiaryRebateAnnual;
            else if (age >= 65) rebateAnnual += SecondaryRebateAnnual;

            var taxBeforeCredit = Math.Max(0m, (annualTax - rebateAnnual) / 12m);
            payroll.PayeBeforeRebate = Math.Round(taxBeforeCredit, 2);

            // 4. Medical tax credit (s6A)
            int deps = payroll.Dependants;
            decimal medCredit = MedicalMain;
            if (deps >= 1) medCredit += MedicalFirstDep;
            if (deps > 1) medCredit += (deps - 1) * MedicalAdditional;
            payroll.MedicalTaxCredit = Math.Round(medCredit, 2);

            var paye = Math.Max(0m, taxBeforeCredit - medCredit);
            payroll.Tax = Math.Round(paye, 2);

            // 5. UIF employee + employer
            var uifEmp = Math.Min(gross * UifRate, UifMonthlyCap);
            var uifEr = Math.Min(gross * UifRate, UifMonthlyCap);
            payroll.UifEmployee = Math.Round(uifEmp, 2);
            payroll.UifEmployer = Math.Round(uifEr, 2);
            payroll.Deductions = payroll.UifEmployee; // keep legacy field for UI compatibility

            // 6. SDL (employer only)
            var companyAnnual = ParseDecimal(ConfigurationManager.AppSettings["Payroll.CompanyAnnualPayroll"], 1200000m);
            var sdlThreshold = ParseDecimal(ConfigurationManager.AppSettings["Payroll.SdlThreshold"], 500000m);
            payroll.Sdl = companyAnnual > sdlThreshold
                ? Math.Round(gross * SdlRate, 2)
                : 0m;

            // 7. Net
            var otherDeds = (payroll.MedicalAidContribution ?? 0m)
                          + (payroll.PensionContribution ?? 0m)
                          + (payroll.OtherDeductions ?? 0m)
                          + (payroll.AdvanceRecovery ?? 0m);

            var net = gross - paye - uifEmp - otherDeds;
            payroll.NetPay = Math.Round(Math.Max(0m, net), 2);

            return new PayrollTaxResult
            {
                GrossPay = gross,
                TaxableIncome = taxableMonthly,
                PayeBeforeRebate = payroll.PayeBeforeRebate,
                PrimaryRebateMonthly = Math.Round(rebateAnnual / 12m, 2),
                MedicalTaxCredit = payroll.MedicalTaxCredit,
                Paye = payroll.Tax,
                UifEmployee = payroll.UifEmployee,
                UifEmployer = payroll.UifEmployer,
                Sdl = payroll.Sdl,
                RetirementDeduction = payroll.RetirementDeduction,
                NetPay = payroll.NetPay,
                TaxYear = taxYear
            };
        }

        private static decimal ParseDecimal(string raw, decimal fallback)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            decimal d;
            return decimal.TryParse(raw, out d) ? d : fallback;
        }
    }
}