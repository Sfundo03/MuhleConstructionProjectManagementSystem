// HandoverPdfGenerator.cs
// Requires the iTextSharp NuGet package (Install-Package iTextSharp).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MuhleOP.Models;

namespace MuhleOP.Helpers
{
    public class HandoverSignOffMeta
    {
        public string SignerName { get; set; }
        public string SignerEmail { get; set; }
        public DateTime SignedAt { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Feedback { get; set; }
        public string SignOffNotes { get; set; }
    }

    public static class HandoverPdfGenerator
    {
        private static readonly BaseColor GoldColor = new BaseColor(245, 166, 35);
        private static readonly BaseColor DarkColor = new BaseColor(20, 24, 29);
        private static readonly BaseColor MutedColor = new BaseColor(110, 110, 110);

        /// <summary>
        /// Builds the signed handover certificate PDF, stamping in the client's
        /// electronic acceptance at the bottom.
        /// </summary>
        public static byte[] Generate(
            ProjectHandover handover,
            Project project,
            Client client,
            IEnumerable<HandoverDocument> documents,
            HandoverSignOffMeta signOff)
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
                document.Add(new Paragraph("PROJECT HANDOVER CERTIFICATE", titleFont));
                document.Add(new Paragraph($"Handover Type: {handover.HandoverType}", subtitleFont));
                document.Add(new Paragraph(" "));

                // Parties table
                var partiesTable = new PdfPTable(2) { WidthPercentage = 100 };
                partiesTable.SetWidths(new float[] { 1, 1 });
                AddPartyCell(partiesTable, "MUHLEOP", "MuhleOP", labelFont, bodyFont);
                AddPartyCell(partiesTable, "CLIENT", $"{client.CompanyName}\n{client.PrimaryContactName}\n{client.ContactEmail}", labelFont, bodyFont);
                document.Add(partiesTable);
                document.Add(new Paragraph(" "));

                // Project details
                document.Add(new Paragraph("PROJECT DETAILS", headingFont));
                var projectTable = new PdfPTable(2) { WidthPercentage = 100 };
                projectTable.SetWidths(new float[] { 1, 1 });
                AddDetailRow(projectTable, "Project Name", project.ProjectName, labelFont, bodyFont);
                AddDetailRow(projectTable, "Project Code", project.ProjectCode, labelFont, bodyFont);
                AddDetailRow(projectTable, "Project Location", project.ProjectLocation, labelFont, bodyFont);
                AddDetailRow(projectTable, "Start Date", project.StartDate.ToString("dd MMM yyyy"), labelFont, bodyFont);
                if (project.ActualEndDate.HasValue)
                {
                    AddDetailRow(projectTable, "Actual End Date", project.ActualEndDate.Value.ToString("dd MMM yyyy"), labelFont, bodyFont);
                }
                AddDetailRow(projectTable, "Project Manager", project.ProjectManager != null ? project.ProjectManager.FullName : "-", labelFont, bodyFont);
                if (project.SiteManager != null)
                {
                    AddDetailRow(projectTable, "Site Manager", project.SiteManager.FullName, labelFont, bodyFont);
                }
                document.Add(projectTable);
                document.Add(new Paragraph(" "));

                // Handover summary
                document.Add(new Paragraph("HANDOVER SUMMARY", headingFont));
                var summaryTable = new PdfPTable(2) { WidthPercentage = 100 };
                summaryTable.SetWidths(new float[] { 1, 1 });
                AddDetailRow(summaryTable, "Handover Type", handover.HandoverType, labelFont, bodyFont);
                AddDetailRow(summaryTable, "Handover Date", handover.HandoverDate.ToString("dd MMM yyyy HH:mm"), labelFont, bodyFont);
                AddDetailRow(summaryTable, "Final Inspection Date", handover.FinalInspectionDate.HasValue ? handover.FinalInspectionDate.Value.ToString("dd MMM yyyy") : "-", labelFont, bodyFont);
                document.Add(summaryTable);
                document.Add(new Paragraph(" "));
                AddSection(document, "Final Inspection Findings", handover.FinalInspectionFindings, headingFont, bodyFont);
                AddSection(document, "Completion Notes", handover.CompletionNotes, headingFont, bodyFont);
                AddSection(document, "Lessons Learned", handover.LessonsLearned, headingFont, bodyFont);
                AddSection(document, "Recommendations", handover.Recommendations, headingFont, bodyFont);

                var warrantyTable = new PdfPTable(2) { WidthPercentage = 100 };
                warrantyTable.SetWidths(new float[] { 1, 1 });
                AddDetailRow(warrantyTable, "Warranty Period", handover.WarrantyPeriodDays.HasValue ? $"{handover.WarrantyPeriodDays} days" : "-", labelFont, bodyFont);
                AddDetailRow(warrantyTable, "Warranty Start", handover.WarrantyStartDate.HasValue ? handover.WarrantyStartDate.Value.ToString("dd MMM yyyy") : "-", labelFont, bodyFont);
                AddDetailRow(warrantyTable, "Warranty End", handover.WarrantyEndDate.HasValue ? handover.WarrantyEndDate.Value.ToString("dd MMM yyyy") : "-", labelFont, bodyFont);
                document.Add(warrantyTable);
                document.Add(new Paragraph(" "));

                // Handover documents
                document.Add(new Paragraph("HANDOVER DOCUMENTS", headingFont));
                var docList = documents != null ? documents.ToList() : new List<HandoverDocument>();
                if (!docList.Any())
                {
                    document.Add(new Paragraph("-", bodyFont));
                }
                else
                {
                    foreach (var doc in docList)
                    {
                        document.Add(new Paragraph(
                            $"{doc.DocumentName} ({doc.DocumentType}) — uploaded {doc.UploadDate:dd MMM yyyy} by {doc.UploadedBy}",
                            bodyFont));
                    }
                }
                document.Add(new Paragraph(" "));

                // Client acceptance
                var line = new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(0.5f, 100, MutedColor, Element.ALIGN_CENTER, 1)));
                document.Add(line);
                document.Add(new Paragraph(" "));
                document.Add(new Paragraph("CLIENT ACCEPTANCE", headingFont));
                document.Add(new Paragraph("The undersigned client hereby accepts the project as completed on the date and time below.", bodyFont));
                document.Add(new Paragraph(" "));
                document.Add(new Paragraph($"Signed by: {signOff.SignerName}", bodyFont));
                document.Add(new Paragraph($"Email: {signOff.SignerEmail}", bodyFont));
                document.Add(new Paragraph($"Signed at: {signOff.SignedAt:dd MMM yyyy HH:mm}", bodyFont));
                document.Add(new Paragraph("Signature type: Electronic", bodyFont));
                document.Add(new Paragraph($"IP Address: {(string.IsNullOrEmpty(signOff.IpAddress) ? "—" : signOff.IpAddress)}", smallMuted));
                document.Add(new Paragraph($"User Agent: {(string.IsNullOrEmpty(signOff.UserAgent) ? "—" : signOff.UserAgent)}", smallMuted));
                document.Add(new Paragraph($"Client Feedback: {(string.IsNullOrWhiteSpace(signOff.Feedback) ? "—" : signOff.Feedback)}", bodyFont));
                document.Add(new Paragraph($"Sign-off Notes: {(string.IsNullOrWhiteSpace(signOff.SignOffNotes) ? "—" : signOff.SignOffNotes)}", bodyFont));

                document.Add(new Paragraph(" "));
                document.Add(new Paragraph($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}", smallMuted));

                document.Close();
                return stream.ToArray();
            }
        }

        public static byte[] GenerateAcceptanceLetter(
            ProjectHandover handover,
            Project project,
            Client client,
            HandoverSignOffMeta signOff,
            IEnumerable<SnagItem> snags)
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

                document.Add(new Paragraph("CLIENT ACCEPTANCE LETTER", titleFont));
                document.Add(new Paragraph($"Project: {project.ProjectName} ({project.ProjectCode})", subtitleFont));
                document.Add(new Paragraph(" "));

                var totalSnags = snags?.Count() ?? 0;
                var openSnags = snags?.Count(s => s.Status != "Verified") ?? 0;
                var criticalOpen = snags?.Count(s => s.Severity == "Critical" && s.Status != "Verified") ?? 0;

                document.Add(new Paragraph("PROJECT ACCEPTANCE STATEMENT", headingFont));
                document.Add(new Paragraph($"This letter confirms that {client.CompanyName}, represented by {signOff.SignerName}, has formally reviewed and accepted the handover for project '{project.ProjectName}' as completed.", bodyFont));
                document.Add(new Paragraph(" "));

                document.Add(new Paragraph($"Snag Summary: {totalSnags} snags were recorded, of which {openSnags} remain open ({criticalOpen} critical).", bodyFont));
                document.Add(new Paragraph(" "));

                document.Add(new Paragraph("SIGN-OFF DETAILS", headingFont));
                var detailsTable = new PdfPTable(2) { WidthPercentage = 100 };
                detailsTable.SetWidths(new float[] { 1, 1 });
                AddDetailRow(detailsTable, "Signer Name", signOff.SignerName, labelFont, bodyFont);
                AddDetailRow(detailsTable, "Signer Email", signOff.SignerEmail, labelFont, bodyFont);
                AddDetailRow(detailsTable, "Sign-off Date", signOff.SignedAt.ToString("dd MMM yyyy HH:mm"), labelFont, bodyFont);
                AddDetailRow(detailsTable, "IP Address", string.IsNullOrEmpty(signOff.IpAddress) ? "—" : signOff.IpAddress, labelFont, bodyFont);
                document.Add(detailsTable);
                document.Add(new Paragraph(" "));

                if (!string.IsNullOrWhiteSpace(signOff.Feedback))
                {
                    AddSection(document, "Client Feedback", signOff.Feedback, headingFont, bodyFont);
                }

                document.Add(new Paragraph(" "));
                var line = new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(0.5f, 100, MutedColor, Element.ALIGN_CENTER, 1)));
                document.Add(line);
                document.Add(new Paragraph(" "));
                document.Add(new Paragraph($"User Agent: {(string.IsNullOrEmpty(signOff.UserAgent) ? "—" : signOff.UserAgent)}", smallMuted));
                document.Add(new Paragraph($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}", smallMuted));

                document.Close();
                return stream.ToArray();
            }
        }

        public static byte[] GenerateClosureReport(
            ProjectHandover handover,
            Project project,
            Client client,
            ProjectBudget budget,
            IEnumerable<ProjectVariation> variations,
            IEnumerable<SnagItem> snags,
            IEnumerable<HandoverDocument> documents)
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

                document.Add(new Paragraph("PROJECT CLOSURE REPORT", titleFont));
                document.Add(new Paragraph($"Project: {project.ProjectName} ({project.ProjectCode}) | Client: {client.CompanyName}", subtitleFont));
                document.Add(new Paragraph(" "));

                // 1. Schedule Summary
                document.Add(new Paragraph("1. Schedule Summary", headingFont));
                var estEnd = project.EstimatedEndDate.HasValue ? project.EstimatedEndDate.Value.ToString("dd MMM yyyy") : "—";
                var actEnd = project.ActualEndDate.HasValue ? project.ActualEndDate.Value.ToString("dd MMM yyyy") : "—";
                string scheduleStatus = "On Schedule";
                if (project.EstimatedEndDate.HasValue && project.ActualEndDate.HasValue)
                {
                    int daysDiff = (project.ActualEndDate.Value - project.EstimatedEndDate.Value).Days;
                    if (daysDiff > 0) scheduleStatus = $"{daysDiff} days late";
                    else if (daysDiff < 0) scheduleStatus = $"{Math.Abs(daysDiff)} days early";
                }
                var schedTable = new PdfPTable(2) { WidthPercentage = 100 };
                schedTable.SetWidths(new float[] { 1, 1 });
                AddDetailRow(schedTable, "Planned End Date", estEnd, labelFont, bodyFont);
                AddDetailRow(schedTable, "Actual End Date", actEnd, labelFont, bodyFont);
                AddDetailRow(schedTable, "Schedule Variance", scheduleStatus, labelFont, bodyFont);
                document.Add(schedTable);
                document.Add(new Paragraph(" "));

                // 2. Financial Summary
                document.Add(new Paragraph("2. Financial Summary", headingFont));
                var finTable = new PdfPTable(2) { WidthPercentage = 100 };
                finTable.SetWidths(new float[] { 1, 1 });
                decimal contractVal = project.Contract != null ? project.Contract.ContractValue : 0m;
                decimal estCost = budget != null ? budget.EstimatedCost : 0m;
                decimal actCost = budget != null ? budget.SpentAmount : 0m;
                AddDetailRow(finTable, "Contract Value", contractVal.ToString("C"), labelFont, bodyFont);
                AddDetailRow(finTable, "Estimated Budget", estCost.ToString("C"), labelFont, bodyFont);
                AddDetailRow(finTable, "Actual Spent", actCost.ToString("C"), labelFont, bodyFont);
                document.Add(finTable);
                document.Add(new Paragraph(" "));

                // 3. Variation Summary
                document.Add(new Paragraph("3. Variation Summary", headingFont));
                var varList = variations != null ? variations.ToList() : new List<ProjectVariation>();
                if (!varList.Any())
                {
                    document.Add(new Paragraph("No variations recorded.", bodyFont));
                }
                else
                {
                    foreach (var v in varList)
                    {
                        document.Add(new Paragraph($"{v.VariationNumber} ({v.VariationType}) - Status: {v.Status} - Cost Impact: {v.ImpactOnCost?.ToString("C") ?? "—"}", bodyFont));
                    }
                }
                document.Add(new Paragraph(" "));

                // 4. Snag Summary
                document.Add(new Paragraph("4. Snag Summary", headingFont));
                var snagList = snags != null ? snags.ToList() : new List<SnagItem>();
                int totalSnags = snagList.Count;
                int verifiedSnags = snagList.Count(s => s.Status == "Verified");
                document.Add(new Paragraph($"Total Snags: {totalSnags} | Verified/Resolved: {verifiedSnags} | Remaining Open: {totalSnags - verifiedSnags}", bodyFont));
                document.Add(new Paragraph(" "));

                // 5. Handover Package
                document.Add(new Paragraph("5. Handover Package Documents", headingFont));
                var docList = documents != null ? documents.ToList() : new List<HandoverDocument>();
                if (!docList.Any())
                {
                    document.Add(new Paragraph("No documents attached.", bodyFont));
                }
                else
                {
                    foreach (var d in docList)
                    {
                        document.Add(new Paragraph($"{d.DocumentName} [{d.DocumentType}]", bodyFont));
                    }
                }
                document.Add(new Paragraph(" "));

                // 6. Client Acceptance
                document.Add(new Paragraph("6. Client Acceptance", headingFont));
                document.Add(new Paragraph($"Status: {handover.ClientSignOffStatus} on {handover.ClientSignOffDate?.ToString("dd MMM yyyy HH:mm") ?? "—"}", bodyFont));
                document.Add(new Paragraph($"IP Address: {handover.ClientSignatureIpAddress ?? "—"}", smallMuted));

                document.Add(new Paragraph(" "));
                document.Add(new Paragraph($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}", smallMuted));

                document.Close();
                return stream.ToArray();
            }
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