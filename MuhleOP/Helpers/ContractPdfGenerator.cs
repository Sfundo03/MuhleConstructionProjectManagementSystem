// ContractPdfGenerator.cs
// Requires the iTextSharp NuGet package (Install-Package iTextSharp).
using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MuhleOP.Models;

namespace MuhleOP.Helpers
{
    public static class ContractPdfGenerator
    {
        private static readonly BaseColor GoldColor = new BaseColor(245, 166, 35);
        private static readonly BaseColor DarkColor = new BaseColor(20, 24, 29);
        private static readonly BaseColor MutedColor = new BaseColor(110, 110, 110);

        /// <summary>
        /// Builds the contract PDF. Pass a signature image (PNG bytes) and the
        /// ContractSignature record to stamp a "Signed" block at the bottom;
        /// omit both to produce the unsigned draft/preview PDF.
        /// </summary>
        public static byte[] Generate(Contract contract, Client client, byte[] signatureImageBytes = null, ContractSignature signature = null)
        {
            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 50, 50, 60, 60);
                PdfWriter.GetInstance(document, stream);
                document.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, DarkColor);
                var subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, MutedColor);
                var headingFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13, GoldColor);
                var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, DarkColor);
                var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, DarkColor);
                var smallMuted = FontFactory.GetFont(FontFactory.HELVETICA, 8, MutedColor);

                // Header
                document.Add(new Paragraph("SERVICE CONTRACT", titleFont));
                document.Add(new Paragraph($"Contract Number: {contract.ContractNumber}", subtitleFont));
                document.Add(new Paragraph(" "));

                var statusLabel = contract.IsSigned ? "SIGNED" : contract.Status?.ToUpper() ?? "DRAFT";
                var statusPara = new Paragraph($"Status: {statusLabel}", labelFont);
                document.Add(statusPara);
                document.Add(new Paragraph(" "));

                // Parties table
                var partiesTable = new PdfPTable(2) { WidthPercentage = 100 };
                partiesTable.SetWidths(new float[] { 1, 1 });
                AddPartyCell(partiesTable, "SERVICE PROVIDER", "MuhleOP", labelFont, bodyFont);
                AddPartyCell(partiesTable, "CLIENT", $"{client.CompanyName}\n{client.PrimaryContactName}\n{client.ContactEmail}", labelFont, bodyFont);
                document.Add(partiesTable);
                document.Add(new Paragraph(" "));

                // Key details table
                var detailsTable = new PdfPTable(2) { WidthPercentage = 100 };
                detailsTable.SetWidths(new float[] { 1, 1 });
                AddDetailRow(detailsTable, "Project Name", contract.ProjectName, labelFont, bodyFont);
                AddDetailRow(detailsTable, "Contract Type", contract.ContractType, labelFont, bodyFont);
                AddDetailRow(detailsTable, "Contract Value", contract.ContractValue.ToString("C"), labelFont, bodyFont);
                AddDetailRow(detailsTable, "Start Date", contract.StartDate.ToString("dd MMM yyyy"), labelFont, bodyFont);
                AddDetailRow(detailsTable, "End Date", contract.EndDate.ToString("dd MMM yyyy"), labelFont, bodyFont);
                document.Add(detailsTable);
                document.Add(new Paragraph(" "));

                AddSection(document, "1. Project Scope", contract.ProjectScope, headingFont, bodyFont);
                AddSection(document, "2. Deliverables", contract.Deliverables, headingFont, bodyFont);
                AddSection(document, "3. Payment Terms", contract.PaymentTerms, headingFont, bodyFont);
                AddSection(document, "4. Payment & Invoicing Schedule", contract.PaymentSchedule, headingFont, bodyFont);
                AddSection(document, "5. Milestones", contract.Milestones, headingFont, bodyFont);
                AddSection(document, "6. Project Timeline & Deadlines", contract.Timeline, headingFont, bodyFont);
                AddSection(document, "7. Penalty Clauses", contract.PenaltyClauses, headingFont, bodyFont);
                AddSection(document, "8. Termination Conditions", contract.TerminationConditions, headingFont, bodyFont);
                if (!string.IsNullOrWhiteSpace(contract.SpecialConditions))
                {
                    AddSection(document, "9. Special Conditions", contract.SpecialConditions, headingFont, bodyFont);
                }

                // Signature block
                document.Add(new Paragraph(" "));
                var line = new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(0.5f, 100, MutedColor, Element.ALIGN_CENTER, 1)));
                document.Add(line);
                document.Add(new Paragraph(" "));
                document.Add(new Paragraph("SIGNATURE", headingFont));

                if (signature != null && signatureImageBytes != null && signatureImageBytes.Length > 0)
                {
                    var sigImage = Image.GetInstance(signatureImageBytes);
                    sigImage.ScaleToFit(180, 70);
                    document.Add(sigImage);

                    document.Add(new Paragraph($"Signed by: {signature.SignerName}", bodyFont));
                    document.Add(new Paragraph($"Email: {signature.SignerEmail}", bodyFont));
                    document.Add(new Paragraph($"Date: {signature.SignedDate:dd MMM yyyy HH:mm}", bodyFont));
                    document.Add(new Paragraph($"Signature Type: {signature.SignatureType}", bodyFont));
                    document.Add(new Paragraph($"Verification Token: {signature.VerificationToken}", smallMuted));
                    document.Add(new Paragraph($"IP Address: {signature.IpAddress}", smallMuted));
                }
                else
                {
                    document.Add(new Paragraph(" "));
                    document.Add(new Paragraph("This is a DRAFT copy for review. It has not yet been signed.", bodyFont));
                    document.Add(new Paragraph(" "));
                    document.Add(new Paragraph("_____________________________", bodyFont));
                    document.Add(new Paragraph("Client Signature", smallMuted));
                }

                document.Add(new Paragraph(" "));
                document.Add(new Paragraph($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}", smallMuted));

                document.Close();
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Masks a bank account for display: keeps first 4 and last 4 digits, middle becomes ••••.
        /// Short values are returned as-is or fully masked.
        /// </summary>
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

        private static void AddSection(Document document, string heading, string body, Font headingFont, Font bodyFont)
        {
            document.Add(new Paragraph(heading, headingFont));
            document.Add(new Paragraph(string.IsNullOrWhiteSpace(body) ? "-" : body, bodyFont));
            document.Add(new Paragraph(" "));
        }
    }
}