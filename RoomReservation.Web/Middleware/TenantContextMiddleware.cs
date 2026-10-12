using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RoomReservation.Infrastructure.Persistence;
using RoomReservation.Application;

namespace RoomReservation.Api.Middleware;

public class TenantContextMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext http, AppDbContext db)
    {
        // Authentication and account-management actions manage their own
        // transactions. Skip tenant wrapping before reading the organization
        // claim so a stale cookie cannot route these requests into the outer
        // transaction and cause a nested-transaction failure during login.
        if (http.Request.Path.StartsWithSegments("/Account"))
        {
            await next(http);
            return;
        }

        var value = http.User.FindFirstValue("organization_id");
        var userValue = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out _) && DisplaySessionToken.TryRead(http.Request.Cookies[DisplaySessionToken.CookieName], http.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Key"] ?? "", out var displaySession))
            value = displaySession.OrganizationId.ToString();
        if (!Guid.TryParse(value, out var id))
        {
            // The account-selection POST owns its transaction because it must
            // update the unassigned user's tenant at the same time as the
            // RLS settings are changed.
            if (http.Request.Path.StartsWithSegments("/Account/ChooseOrganization") || http.Request.Path.StartsWithSegments("/choose-organization"))
            {
                await next(http);
                return;
            }

            if (http.User.Identity?.IsAuthenticated == true
                && !http.Request.Path.StartsWithSegments("/Account")
                && !http.Request.Path.StartsWithSegments("/public")
                && !http.Request.Path.StartsWithSegments("/room-display")
                && !http.Request.Path.StartsWithSegments("/RoomDisplay")
                && !http.Request.Path.StartsWithSegments("/health")
                && !http.Request.Path.StartsWithSegments("/version"))
            {
                http.Response.Redirect("/Account/ChooseOrganization");
                return;
            }

            // Account flows own their database transaction when needed (for
            // example Google OAuth callback and organization selection). Do
            // not wrap those requests in a second transaction here.
            if (http.Request.Path.StartsWithSegments("/Account"))
            {
                await next(http);
                return;
            }

            if (http.User.Identity?.IsAuthenticated != true)
            {
                await next(http);
                return;
            }

            await using var userTransaction = await db.Database.BeginTransactionAsync();
            try
            {
                if (Guid.TryParse(userValue, out var currentUserId))
                    await db.Database.ExecuteSqlRawAsync("select set_config('app.current_user_id', {0}, true)", currentUserId.ToString());
                await next(http);
                await userTransaction.CommitAsync();
            }
            finally { }
            return;
        }

        TenantContext.OrganizationId = id;
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // Keep the tenant setting on the same connection as all request queries.
            // A pooled connection can otherwise lose SET LOCAL between commands.
            await db.Database.ExecuteSqlRawAsync("select set_config('app.current_org_id', {0}, true)", id.ToString());
            if (Guid.TryParse(userValue, out var currentUserId))
                await db.Database.ExecuteSqlRawAsync("select set_config('app.current_user_id', {0}, true)", currentUserId.ToString());
            await next(http);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            TenantContext.OrganizationId = null;
        }
    }
}
