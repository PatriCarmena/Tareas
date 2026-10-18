using OrderDesk.Web.Models;

namespace OrderDesk.Web.Services;

public static class ActivitySchedule
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

    public static DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public static DateTime ToUtc(DateTime local)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, Zone);
    }

    public static (DateTime Start, DateTime End) TodayUtc(DateTime nowUtc)
    {
        var today = ToLocal(nowUtc).Date;
        return (ToUtc(today), ToUtc(today.AddDays(1)));
    }

    public static string EffectiveStatus(CustomerActivity activity, DateTime nowUtc) =>
        activity.Status == ActivityStatus.Completed ? "Realizada" : activity.ScheduledAtUtc < nowUtc ? "Vencida" : "Pendiente";

    public static string TypeLabel(ActivityType type) => type switch
    {
        ActivityType.Call => "Llamada",
        ActivityType.Email => "Correo",
        ActivityType.Meeting => "Reunión",
        _ => "Tarea"
    };

    public static string PriorityLabel(ActivityPriority priority) => priority switch
    {
        ActivityPriority.Low => "Baja",
        ActivityPriority.High => "Alta",
        _ => "Normal"
    };
}
