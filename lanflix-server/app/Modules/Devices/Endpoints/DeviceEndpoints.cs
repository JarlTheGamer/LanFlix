using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lanflix.Modules.Devices;

public static class DeviceModule
{
    public static IServiceCollection AddDevicesModule(this IServiceCollection services)
        => services.AddScoped<DeviceService>();

    public static IEndpointRouteBuilder MapDevicesModule(this IEndpointRouteBuilder endpoints)
    {
        var devices = endpoints.MapGroup("/api/v2/devices").WithTags("Devices").RequireAuthorization();
        devices.MapPost("/register", async (RegisterDeviceRequest request, ClaimsPrincipal user, HttpContext context, DeviceService service, CancellationToken ct) =>
            Results.Ok(await service.RegisterAsync(request, user, context.Connection.RemoteIpAddress?.ToString(), DateTime.UtcNow, ct)));
        devices.MapGet("/{id}", async (string id, DeviceService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } device ? Results.Ok(device) : Results.NotFound());
        devices.MapGet("/", async (DeviceService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)))
            .RequireAuthorization("ServerManage");
        devices.MapDelete("/{id}", async (string id, DeviceService service, CancellationToken ct) =>
            await service.RemoveAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization("ServerManage");
        return endpoints;
    }
}
