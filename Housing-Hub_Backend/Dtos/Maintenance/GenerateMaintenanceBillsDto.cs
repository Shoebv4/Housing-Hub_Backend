using System;

namespace HousingHubBackend.Dtos.Maintenance
{
    public class GenerateMaintenanceBillsDto
    {
        public int? WingId { get; set; }
        public DateOnly? PeriodStart { get; set; }
        public DateOnly? PeriodEnd { get; set; }
        public DateOnly? DueDate { get; set; }
    }

    public class GenerateBillsResultDto
    {
        public int GeneratedCount { get; set; }
        public int SkippedCount { get; set; }
        public int IgnoredCount { get; set; }
        public double TotalAmount { get; set; }
    }
}
