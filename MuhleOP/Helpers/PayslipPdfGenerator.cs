// Helpers/PayslipPdfGenerator.cs
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MuhleOP.Models;

namespace MuhleOP.Helpers
{
    public static class PayslipPdfGenerator
    {
        private static readonly BaseColor Gold = new BaseColor(245, 166, 35);
        private static readonly BaseColor Dark = new BaseColor(20, 24, 29);
        private static readonly BaseColor Muted = new BaseColor(110, 110, 110);

        public static string Generate(Payroll payroll, Staff staff, string webRoot)
        {
            var dir = Path.Combine(webRoot, "App_Data", "Payslips");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var fileName = $"Payslip_{staff.EmployeeId}_{payroll.PayPeriodStart:yyyyMM}_{payroll.Id}.pdf";
            var fullPath = Path.Combine(dir, fileName);

            using (var fs = new FileStream(fullPath, FileMode.Create))
            using (var doc = new Document(PageSize.A4, 36, 36, 36, 36))
            using (var writer = PdfWriter.GetInstance(doc, fs))
            {
                doc.Open();
                var fontTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, Dark);
                var fontHead = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, Dark);
                var fontNorm = FontFactory.GetFont(FontFactory.HELVETICA, 9, Dark);
                var fontMuted = FontFactory.GetFont(FontFactory.HELVETICA, 8, Muted);
                var fontGold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, Gold);

                // ---- Company header ----
                var company = ConfigurationManager.AppSettings["Company.Name"] ?? "Muhle Building";
                var reg = ConfigurationManager.AppSettings["Company.RegistrationNumber"] ?? "2018/123456/07";
                var payeRef = ConfigurationManager.AppSettings["Company.PayeReference"] ?? "7123456789";
                var uifRef = ConfigurationManager.AppSettings["Company.UifReference"] ?? "U123456789";
                var sdlRef = ConfigurationManager.AppSettings["Company.SdlReference"] ?? "L123456789";
                var addr = ConfigurationManager.AppSettings["Company.PhysicalAddress"] ?? "123 Construction Road, Cape Town, 8001";

                doc.Add(new Paragraph(company, fontTitle));
                doc.Add(new Paragraph(addr, fontMuted));
                doc.Add(new Paragraph($"Reg: {reg}  |  PAYE: {payeRef}  |  UIF: {uifRef}  |  SDL: {sdlRef}", fontMuted));
                doc.Add(new Paragraph(" "));

                // ---- Employee block ----
                var tEmp = new PdfPTable(2) { WidthPercentage = 100 };
                tEmp.SetWidths(new float[] { 1, 1 });
                AddCell(tEmp, "Employee", fontHead, true);
                AddCell(tEmp, "Pay Period", fontHead, true);
                AddCell(tEmp, $"{staff.FullName}\nID: {staff.EmployeeId}\nRole: {staff.Role}", fontNorm, false);
                AddCell(tEmp, $"{payroll.PayPeriodStart:dd MMM yyyy} – {payroll.PayPeriodEnd:dd MMM yyyy}\nTax Year: {payroll.TaxYear}", fontNorm, false);
                doc.Add(tEmp);
                doc.Add(new Paragraph(" "));

                // ---- Earnings (SARS codes) ----
                doc.Add(new Paragraph("EARNINGS", fontGold));
                var tEarn = new PdfPTable(4) { WidthPercentage = 100 };
                tEarn.SetWidths(new float[] { 1.2f, 3f, 1.5f, 1.5f });
                AddHeaderRow(tEarn, new[] { "Code", "Description", "Hours / Qty", "Amount (R)" }, fontHead);
                AddEarnRow(tEarn, "3601", "Normal / Regular Pay", payroll.RegularHours, payroll.RegularPay, fontNorm);
                if (payroll.OvertimeHours > 0)
                    AddEarnRow(tEarn, "3605", "Overtime Pay (1.5×)", payroll.OvertimeHours, payroll.OvertimePay, fontNorm);
                if (payroll.SundayHours > 0)
                    AddEarnRow(tEarn, "3607", "Sunday Pay (2×)", payroll.SundayHours, payroll.SundayPay, fontNorm);
                if (payroll.HolidayHours > 0)
                    AddEarnRow(tEarn, "3607", "Public Holiday Pay (2×)", payroll.HolidayHours, payroll.HolidayPay, fontNorm);
                if (payroll.Bonus > 0)
                    AddEarnRow(tEarn, "3602", "Bonus", 0, payroll.Bonus, fontNorm);
                if ((payroll.OtherAdditions ?? 0) > 0)
                    AddEarnRow(tEarn, "3701", "Other Additions", 0, payroll.OtherAdditions.Value, fontNorm);
                AddEarnRow(tEarn, "", "GROSS PAY", 0, payroll.GrossPay, fontHead);
                doc.Add(tEarn);
                doc.Add(new Paragraph(" "));

                // ---- Deductions ----
                doc.Add(new Paragraph("DEDUCTIONS", fontGold));
                var tDed = new PdfPTable(3) { WidthPercentage = 100 };
                tDed.SetWidths(new float[] { 1.2f, 4f, 1.5f });
                AddHeaderRow(tDed, new[] { "Code", "Description", "Amount (R)" }, fontHead);
                AddDedRow(tDed, "4001", "PAYE", payroll.Tax, fontNorm);
                AddDedRow(tDed, "4003", "UIF (Employee)", payroll.UifEmployee, fontNorm);
                if ((payroll.MedicalAidContribution ?? 0) > 0)
                    AddDedRow(tDed, "4006", "Medical Aid", payroll.MedicalAidContribution.Value, fontNorm);
                if ((payroll.PensionContribution ?? 0) > 0)
                    AddDedRow(tDed, "4007", "Pension / Retirement Fund", payroll.PensionContribution.Value, fontNorm);
                if ((payroll.OtherDeductions ?? 0) > 0)
                    AddDedRow(tDed, "4091", "Other Deductions", payroll.OtherDeductions.Value, fontNorm);
                if ((payroll.AdvanceRecovery ?? 0) > 0)
                    AddDedRow(tDed, "4095", "Advance Recovery", payroll.AdvanceRecovery.Value, fontNorm);
                doc.Add(tDed);
                doc.Add(new Paragraph(" "));

                // ---- Employer contributions ----
                doc.Add(new Paragraph("EMPLOYER CONTRIBUTIONS (not deducted from salary)", fontGold));
                var tEr = new PdfPTable(2) { WidthPercentage = 100 };
                AddCell(tEr, "UIF Employer (1% capped R177.12)", fontNorm, false);
                AddCell(tEr, payroll.UifEmployer.ToString("N2"), fontNorm, false);
                AddCell(tEr, "SDL (1%)", fontNorm, false);
                AddCell(tEr, payroll.Sdl.ToString("N2"), fontNorm, false);
                doc.Add(tEr);
                doc.Add(new Paragraph(" "));

                // ---- Net ----
                var netPara = new Paragraph($"NET PAY: R {payroll.NetPay:N2}", fontTitle);
                netPara.Alignment = Element.ALIGN_RIGHT;
                doc.Add(netPara);
                doc.Add(new Paragraph($"Payment date: {(payroll.PaymentDate.HasValue ? payroll.PaymentDate.Value.ToString("dd MMM yyyy") : "—")}  |  Bank: {Mask(payroll.BankAccount ?? staff.BankAccount)}", fontMuted) { Alignment = Element.ALIGN_RIGHT });

                // ---- Footer disclaimer ----
                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph(
                    "This payslip is generated by MuhleOps for demonstration purposes. Tax figures are based on SARS 2024/25 tables and may not reflect your final tax liability. Consult a registered tax practitioner for filing.",
                    fontMuted));

                doc.Close();
            }

            return "/App_Data/Payslips/" + fileName;
        }

        private static void AddCell(PdfPTable t, string text, Font f, bool header)
        {
            var c = new PdfPCell(new Phrase(text, f)) { Border = header ? Rectangle.BOTTOM_BORDER : Rectangle.NO_BORDER, Padding = 4 };
            t.AddCell(c);
        }
        private static void AddHeaderRow(PdfPTable t, string[] headers, Font f)
        {
            foreach (var h in headers)
            {
                var c = new PdfPCell(new Phrase(h, f)) { BackgroundColor = new BaseColor(245, 245, 245), Padding = 4 };
                t.AddCell(c);
            }
        }
        private static void AddEarnRow(PdfPTable t, string code, string desc, decimal hours, decimal amt, Font f)
        {
            t.AddCell(new PdfPCell(new Phrase(code, f)) { Padding = 3 });
            t.AddCell(new PdfPCell(new Phrase(desc, f)) { Padding = 3 });
            t.AddCell(new PdfPCell(new Phrase(hours > 0 ? hours.ToString("N2") : "", f)) { Padding = 3, HorizontalAlignment = Element.ALIGN_RIGHT });
            t.AddCell(new PdfPCell(new Phrase(amt.ToString("N2"), f)) { Padding = 3, HorizontalAlignment = Element.ALIGN_RIGHT });
        }
        private static void AddDedRow(PdfPTable t, string code, string desc, decimal amt, Font f)
        {
            t.AddCell(new PdfPCell(new Phrase(code, f)) { Padding = 3 });
            t.AddCell(new PdfPCell(new Phrase(desc, f)) { Padding = 3 });
            t.AddCell(new PdfPCell(new Phrase(amt.ToString("N2"), f)) { Padding = 3, HorizontalAlignment = Element.ALIGN_RIGHT });
        }
        private static string Mask(string acc)
        {
            if (string.IsNullOrWhiteSpace(acc)) return "—";
            var c = acc.Trim().Replace(" ", "");
            if (c.Length <= 4) return new string('•', c.Length);
            return c.Substring(0, 4) + "••••" + c.Substring(c.Length - 4);
        }
    }
}