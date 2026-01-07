public class ActivityLogFilterViewModel
{
    public string? StudentId { get; set; }
    public string? CourseId { get; set; }
    public ActivityType? ActivityType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public List<ActivityLog>? Logs { get; set; }
}
