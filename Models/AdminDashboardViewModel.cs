namespace loginlogout.Models
{
    public class AdminDashboardViewModel
    {
        public string Period { get; set; } = "weekly";
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockProducts { get; set; }
        public decimal Revenue { get; set; }
        public IReadOnlyList<Order> RecentOrders { get; set; } = Array.Empty<Order>();
        public IReadOnlyList<AdminTopProductViewModel> TopProducts { get; set; } = Array.Empty<AdminTopProductViewModel>();
        public IReadOnlyList<AdminSalesTrendPointViewModel> SalesTrend { get; set; } = Array.Empty<AdminSalesTrendPointViewModel>();
    }

    public class AdminTopProductViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class AdminSalesTrendPointViewModel
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty;
        public int Orders { get; set; }
        public decimal Revenue { get; set; }
    }
}
