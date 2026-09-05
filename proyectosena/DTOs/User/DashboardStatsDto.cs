// =============================================
// DTO: DashboardStatsDto
// Aggregated numbers for the administrator dashboard
// =============================================

namespace proyectosena.DTOs.User
{
    public class DashboardStatsDto
    {
        public int TotalRequests { get; set; }

        // Breakdown by current status
        public int Pending { get; set; }
        public int Assigned { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Rejected { get; set; }

        // Activity
        public int RequestsLast30Days { get; set; }

        // Active people in the system
        public int ActiveManagers { get; set; }
        public int ActiveCitizens { get; set; }
    }
}
