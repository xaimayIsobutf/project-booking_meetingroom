using System.Security.Claims;

namespace RoomBooking.Web.Infrastructure;

public static class DemoTenant
{
    public const string SessionKey = "DemoTenantId";

    public static int? TenantId(this HttpContext context)
    {
        if (int.TryParse(context.User.FindFirstValue("tenant_id"), out var claimId)) return claimId;
        return context.Session.GetInt32(SessionKey);
    }
}
