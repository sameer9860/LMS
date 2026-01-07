public enum ActivityType
{
    Login = 1,
    Logout = 2,
    ViewMaterial = 3,
    DownloadMaterial = 4,
    StartAssignment = 5,
    SubmitAssignment = 6,
    JoinLiveClass = 7,
    LeaveLiveClass = 8,
    PostForum = 9,
    GradeViewed = 10
}

public class ActivityLog
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string? CourseId { get; set; }
    public string? ResourceId { get; set; }
    public ActivityType ActivityType { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public int? DurationSeconds { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? MetadataJson { get; set; }
}

