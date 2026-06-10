namespace PMEHCRM.Models.ViewModels
{
    public class TicketManagementViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string? TicketStatus { get; set; }
        public string? Channel { get; set; }
        public string? CallingNumber { get; set; }
        public string? SrOrTicket { get; set; }
        public List<TicketManagement> Records { get; set; }
    }
}
