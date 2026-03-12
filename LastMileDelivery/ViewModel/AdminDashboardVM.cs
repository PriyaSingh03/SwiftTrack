namespace LastMileDelivery.ViewModel
{
    public class AdminDashboardVM
    {
        public int TotalDeliveries { get; set; }
        public int ActiveAgents { get; set; }
        public int TotalCustomers { get; set; }
        public int PendingDeliveries { get; set; }

        // New properties for Performance
        public double SuccessRate { get; set; }
        public int AvgDeliveryTime { get; set; }

        // New properties for Distribution
        public double DeliveredPercentage { get; set; }
        public double InTransitPercentage { get; set; }
        public double PendingPercentage { get; set; }
        public double CancelledPercentage { get; set; }
    }
}
