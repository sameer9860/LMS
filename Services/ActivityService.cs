using LMS.Views.Data;

public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _db;

    public ActivityService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(ActivityLog log)
    {
        _db.ActivityLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
