// ContractTemplateGenerator.cs
// Fills in the contract's text fields from fixed templates based on
// contract type / value / dates. Purely offline/deterministic - no external
// calls. The admin can edit every field afterwards.
using System;
using MuhleOP.Models;

namespace MuhleOP.Helpers
{
    public class ContractDefaults
    {
        public string ProjectScope { get; set; }
        public string Deliverables { get; set; }
        public string PaymentTerms { get; set; }
        public string PaymentSchedule { get; set; }
        public string Milestones { get; set; }
        public string Timeline { get; set; }
        public string PenaltyClauses { get; set; }
        public string TerminationConditions { get; set; }
    }

    public static class ContractTemplateGenerator
    {
        public static ContractDefaults Generate(string clientCompanyName, string projectName, string contractType,
            decimal contractValue, DateTime startDate, DateTime endDate)
        {
            var months = Math.Max(1, (int)Math.Round((endDate - startDate).TotalDays / 30.0));
            var deposit = Math.Round(contractValue * 0.30m, 2);
            var midPayment = Math.Round(contractValue * 0.40m, 2);
            var finalPayment = contractValue - deposit - midPayment;
            var company = string.IsNullOrWhiteSpace(clientCompanyName) ? "the Client" : clientCompanyName;
            var project = string.IsNullOrWhiteSpace(projectName) ? "the project" : projectName;

            string paymentTerms;
            switch (contractType)
            {
                case "Time and Materials":
                    paymentTerms = $"MuhleOP shall invoice {company} monthly based on actual hours worked and materials " +
                        $"used, at the rates agreed in this contract. Invoices are payable within 14 days of the invoice date. " +
                        $"The estimated total contract value is {contractValue:C}, subject to actual hours and materials.";
                    break;
                case "Cost Plus":
                    paymentTerms = $"MuhleOP shall invoice {company} for actual project costs plus an agreed management fee. " +
                        $"The estimated contract value is {contractValue:C}. All costs must be supported by receipts/records and " +
                        $"invoices are payable within 14 days of the invoice date.";
                    break;
                default: // Fixed Price
                    paymentTerms = $"This is a Fixed Price contract with a total value of {contractValue:C}. " +
                        $"Payment is due according to the schedule below. All invoices are payable within 14 days of the invoice date. " +
                        $"Late payments may incur interest at 2% per month on the outstanding balance.";
                    break;
            }

            var paymentSchedule =
                $"Deposit (30%): {deposit:C} - due on contract signature.\n" +
                $"Progress Payment (40%): {midPayment:C} - due at project midpoint / on completion of major milestones.\n" +
                $"Final Payment (30%): {finalPayment:C} - due on final delivery/completion and client sign-off.\n" +
                $"All amounts are exclusive of VAT unless stated otherwise on the invoice.";

            var milestones =
                $"Milestone 1 - Project kickoff and planning: {startDate:dd MMM yyyy}\n" +
                $"Milestone 2 - Mid-project review and progress sign-off: {startDate.AddDays((endDate - startDate).TotalDays / 2):dd MMM yyyy}\n" +
                $"Milestone 3 - Final delivery and handover: {endDate:dd MMM yyyy}";

            var timeline =
                $"Project duration: approximately {months} month(s), from {startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}. " +
                $"Any delays caused by {company} providing information, approvals, or site access will extend the timeline accordingly. " +
                $"MuhleOP will notify {company} promptly of any anticipated delays to the above dates.";

            var penaltyClauses =
                $"Should MuhleOP fail to meet an agreed milestone date through its own fault (excluding delays caused by {company} " +
                $"or events outside MuhleOP's reasonable control), a penalty of 0.5% of the contract value per week of delay may apply, " +
                $"up to a maximum of 5% of the total contract value. Penalties do not apply to delays caused by {company}, force majeure, " +
                $"or scope changes requested after signature.";

            var terminationConditions =
                $"Either party may terminate this contract with 30 days' written notice. MuhleOP may terminate immediately if {company} " +
                $"fails to make payment within 30 days of the due date. On termination, {company} shall pay for all work completed and " +
                $"materials committed up to the termination date. Confidential information and deliverables already paid for remain with " +
                $"the paying party as applicable.";

            var deliverables =
                $"MuhleOP will deliver {project} in accordance with the agreed scope, including all documentation, sign-off reports, " +
                $"and handover materials required for {company} to accept and make use of the completed work.";

            var projectScope =
                $"MuhleOP will provide services for {project} on behalf of {company}, as described in this contract. " +
                $"The scope covers planning, execution, quality checks, and handover of the work described in the Deliverables " +
                $"section below. Any work outside this scope will require a written change order and may affect the contract value and timeline.";

            return new ContractDefaults
            {
                ProjectScope = projectScope,
                Deliverables = deliverables,
                PaymentTerms = paymentTerms,
                PaymentSchedule = paymentSchedule,
                Milestones = milestones,
                Timeline = timeline,
                PenaltyClauses = penaltyClauses,
                TerminationConditions = terminationConditions
            };
        }
    }
}