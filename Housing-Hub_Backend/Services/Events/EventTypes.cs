namespace HousingHubBackend.Services.Events
{
    public static class EventTypes
    {
        public const string VisitorCreated = "visitor.created";
        public const string VisitorExited = "visitor.exited";
        public const string BookingCreated = "booking.created";
        public const string BookingUpdated = "booking.updated";
        public const string BookingDeleted = "booking.deleted";
        public const string AnnouncementCreated = "announcement.created";
        public const string BillCreated = "bill.created";
        public const string BillUpdated = "bill.updated";
        public const string ComplaintCreated = "complaint.created";
        public const string ComplaintUpdated = "complaint.updated";
    }
}
