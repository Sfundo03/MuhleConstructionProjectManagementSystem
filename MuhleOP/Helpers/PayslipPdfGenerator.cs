using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MuhleOP.Models;

namespace MuhleOP.Helpers
{
    public static class PayslipPdfGenerator
    {
        private static readonly BaseColor GoldColor = new BaseColor(245, 166, 35);
        private static readonly BaseColor DarkColor = new BaseColor(20, 24, 29);
        private static readonly BaseColor MutedColor = new BaseColor(110, 110, 110);

        /// <summary>
        /// Generates the official electronic payslip PDF for a paid payroll record.
        /// </summary>
        public static byte[] GeneratePayslip(Payroll payroll, Staff staff)
        {
            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 50, 50, 60, 60);
                PdfWriter.GetInstance(document, stream);
                document.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, DarkColor);
                var subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, MutedColor);
                var headingFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, GoldColor);
                var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, DarkColor);
                var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, DarkColor);
                var smallMuted = FontFactory.GetFont(FontFactory.HELVETICA, 8, MutedColor);

                // Header
                document.Add(new Paragraph("PAYSLIP — MuhleOps", titleFont));
                document.Add(new Paragraph($"Period: {payroll.PayPeriodStart:MMMM yyyy} ({payroll.PayPeriodStart:dd MMM yyyy} – {payroll.PayPeriodEnd:dd MMM yyyy})", subtitleFont));
                document.Add(new Paragraph(" "));

                // Employer / Employee block
                var partiesTable = new PdfPTable(2) { WidthPercentage = 100 };
                partiesTable.SetWidths(new float[] { 1, 1 });
                AddPartyCell(partiesTable, "EMPLOYER", "Muhle Building\nCape Town, South Africa", labelFont, bodyFont);
                AddPartyCell(partiesTable, "EMPLOYEE", $"{staff.FullName}\nID: {(string.IsNullOrEmpty(staff.EmployeeId) ? "—" : staff.EmployeeId)}\nRole: {staff.Role}\nEmail: {staff.Email}", labelFont, bodyFont);
                document.Add(partiesTable);
                document.Add(new Paragraph(" "));

                // Earnings table
                document.Add(new Paragraph("EARNINGS", headingFont));
                var earningsTable = new PdfPTable(2) { WidthPercentage = 100 };
                earningsTable.SetWidths(new float[] { 3, 1 });
                AddDetailRow(earningsTable, $"Regular Hours ({payroll.RegularHours:N2} hrs @ R {payroll.HourlyRate:N2}/hr)", $"R {payroll.RegularPay:N2}", labelFont, bodyFont);
                AddDetailRow(earningsTable, $"Overtime Hours ({payroll.OvertimeHours:N2} hrs @ R {payroll.OvertimeRate:N2}/hr)", $"R {payroll.OvertimePay:N2}", labelFont, bodyFont);
                if (payroll.Bonus > 0)
                {
                    AddDetailRow(earningsTable, "Bonus", $"R {payroll.Bonus:N2}", labelFont, bodyFont);
                }
                if (payroll.OtherAdditions.HasValue && payroll.OtherAdditions.Value > 0)
                {
                    AddDetailRow(earningsTable, $"Other Additions {(string.IsNullOrWhiteSpace(payroll.AdditionNotes) ? "" : "— " + payroll.AdditionNotes)}", $"R {payroll.OtherAdditions.Value:N2}", labelFont, bodyFont);
                }
                AddDetailRow(earningsTable, "GROSS PAY", $"R {payroll.GrossPay:N2}", labelFont, bodyFont);
                document.Add(earningsTable);
                document.Add(new Paragraph(" "));

                // Deductions table
                document.Add(new Paragraph("DEDUCTIONS & TAX", headingFont));
                var deductionsTable = new PdfPTable(2) { WidthPercentage = 100 };
                deductionsTable.SetWidths(new float[] { 3, 1 });
                AddDetailRow(deductionsTable, "UIF (1% capped)", $"R {payroll.Deductions:N2}", labelFont, bodyFont);
                AddDetailRow(deductionsTable, "PAYE Tax (18%)", $"R {payroll.Tax:N2}", labelFont, bodyFont);
                if (payroll.MedicalAidContribution.HasValue && payroll.MedicalAidContribution.Value > 0)
                {
                    AddDetailRow(deductionsTable, "Medical Aid Contribution", $"R {payroll.MedicalAidContribution.Value:N2}", labelFont, bodyFont);
                }
                if (payroll.PensionContribution.HasValue && payroll.PensionContribution.Value > 0)
                {
                    AddDetailRow(deductionsTable, "Pension Contribution", $"R {payroll.PensionContribution.Value:N2}", labelFont, bodyFont);
                }
                if (payroll.OtherDeductions.HasValue && payroll.OtherDeductions.Value > 0)
                {
                    AddDetailRow(deductionsTable, $"Other Deductions {(string.IsNullOrWhiteSpace(payroll.DeductionNotes) ? "" : "— " + payroll.DeductionNotes)}", $"R {payroll.OtherDeductions.Value:N2}", labelFont, bodyFont);
                }
                if (payroll.AdvanceRecovery.HasValue && payroll.AdvanceRecovery.Value > 0)
                {
                    AddDetailRow(deductionsTable, "Advance Recovery", $"R {payroll.AdvanceRecovery.Value:N2}", labelFont, bodyFont);
                }

                var totalDeductionsCalc = payroll.Deductions + payroll.Tax +
                    (payroll.MedicalAidContribution ?? 0m) +
                    (payroll.PensionContribution ?? 0m) +
                    (payroll.OtherDeductions ?? 0m) +
                    (payroll.AdvanceRecovery ?? 0m);

                AddDetailRow(deductionsTable, "TOTAL DEDUCTIONS", $"R {totalDeductionsCalc:N2}", labelFont, bodyFont);
                document.Add(deductionsTable);
                document.Add(new Paragraph(" "));

                // Net Pay section
                var netTable = new PdfPTable(2) { WidthPercentage = 100 };
                netTable.SetWidths(new float[] { 3, 1 });
                AddDetailRow(netTable, "NET PAY", $"R {payroll.NetPay:N2}", headingFont, headingFont);
                document.Add(netTable);
                document.Add(new Paragraph(" "));

                // Payment Details
                document.Add(new Paragraph("PAYMENT DETAILS", headingFont));
                var paymentTable = new PdfPTable(2) { WidthPercentage = 100 };
                paymentTable.SetWidths(new float[] { 1, 1 });
                AddDetailRow(paymentTable, "Payment Method", payroll.PaymentMethod ?? "Bank Transfer", labelFont, bodyFont);
                AddDetailRow(paymentTable, "Payment Date", payroll.PaymentDate.HasValue ? payroll.PaymentDate.Value.ToString("dd MMM yyyy") : "—", labelFont, bodyFont);
                AddDetailRow(paymentTable, "Bank Account", MaskBankAccount(payroll.BankAccount ?? staff.BankAccount), labelFont, bodyFont);
                document.Add(paymentTable);

                document.Add(new Paragraph(" "));
                var line = new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(0.5f, 100, MutedColor, Element.ALIGN_CENTER, 1)));
                document.Add(line);
                document.Add(new Paragraph(" "));
                document.Add(new Paragraph($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}", smallMuted));
                document.Add(new Paragraph("This is a system-generated payslip and does not require a signature.", smallMuted));

                document.Close();
                return stream.ToArray();
            }
        }

        private static string MaskBankAccount(string account)
        {
            if (string.IsNullOrWhiteSpace(account))
                return "—";

            var cleaned = account.Trim().Replace(" ", "");
            if (cleaned.Length <= 4)
                return new string('•', cleaned.Length);
            if (cleaned.Length <= 8)
                return cleaned.Substring(0, 2) + new string('•', cleaned.Length - 4) + cleaned.Substring(cleaned.Length - 2);

            return cleaned.Substring(0, 4) + "••••" + cleaned.Substring(cleaned.Length - 4);
        }

        private static void AddPartyCell(PdfPTable table, string label, string value, Font labelFont, Font bodyFont)
        {
            var cell = new PdfPCell { Border = Rectangle.NO_BORDER, Padding = 6 };
            cell.AddElement(new Paragraph(label, labelFont));
            cell.AddElement(new Paragraph(value, bodyFont));
            table.AddCell(cell);
        }

        private static void AddDetailRow(PdfPTable table, string label, string value, Font labelFont, Font bodyFont)
        {
            var labelCell = new PdfPCell(new Phrase(label, labelFont)) { Border = Rectangle.BOTTOM_BORDER, PaddingBottom = 6, PaddingTop = 4 };
            var valueCell = new PdfPCell(new Phrase(value ?? "", bodyFont)) { Border = Rectangle.BOTTOM_BORDER, PaddingBottom = 6, PaddingTop = 4 };
            table.AddCell(labelCell);
            table.AddCell(valueCell);
        }
    }
}