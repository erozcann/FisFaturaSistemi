using System.Collections.Generic;
using Newtonsoft.Json;

namespace FisFaturaUI.Models
{
    public class DashboardViewModel
    {
        public DashboardStatsViewModel Stats { get; set; } = new DashboardStatsViewModel();
        public string MonthlySummaryJson { get; set; } = "[]";
        public string InvoiceTypeSummaryJson { get; set; } = "[]";
        public List<FirmViewModel> Firms { get; set; } = new List<FirmViewModel>();

        public DashboardViewModel()
        {
            Stats = new DashboardStatsViewModel();
            MonthlySummaryJson = "[]";
            InvoiceTypeSummaryJson = "[]";
            Firms = new List<FirmViewModel>();
        }
    }

    public class DashboardStatsViewModel
    {
        public int TotalInvoices { get; set; }
        public int TotalFirms { get; set; }
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpense { get; set; }
    }

    public class MonthlySummaryViewModel
    {
        public string Month { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }
    
    public class InvoiceTypeSummaryViewModel
    {
        public string FaturaTuru { get; set; }
        public int Count { get; set; }
    }
} 