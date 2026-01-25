public class ActivityLogDisplayItem : ActivityLog
{
    public string? StudentName { get; set; }
    public string? CourseName { get; set; }
    public string? ResourceTitle { get; set; }
}

public class ActivityLogFilterViewModel
{
    public string? StudentId { get; set; }
    public string? CourseId { get; set; }
    public ActivityType? ActivityType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public List<ActivityLogDisplayItem>? Logs { get; set; }
}
