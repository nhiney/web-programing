using System;
using System.Collections.Generic;

namespace WebBanGIay.Models
{
    public class DashboardViewModel
    {
        // Summary Cards
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        
        public int NewOrdersCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public int ShippingOrdersCount { get; set; }
        public int CompletedOrdersCount { get; set; }
        public int CancelledOrdersCount { get; set; }

        // Charts Data
        public List<string> RevenueLabels { get; set; } = new List<string>();
        public List<decimal> RevenueData { get; set; } = new List<decimal>();

        public List<string> TopProductLabels { get; set; } = new List<string>();
        public List<int> TopProductData { get; set; } = new List<int>();

        public List<string> CategoryLabels { get; set; } = new List<string>();
        public List<int> CategoryData { get; set; } = new List<int>();

        public List<string> OrderStatusLabels { get; set; } = new List<string>();
        public List<int> OrderStatusData { get; set; } = new List<int>();

        public List<string> MonthlyOrderLabels { get; set; } = new List<string>();
        public List<int> MonthlyOrderData { get; set; } = new List<int>();
    }
}
