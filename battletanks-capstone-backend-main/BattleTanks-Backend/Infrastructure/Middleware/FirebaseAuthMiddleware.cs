using System.Security.Claims;
using BattleTanks_Backend.Domain.Interfaces;
using BattleTanks_Backend.Application.Interfaces;
using BattleTanks_Backend.Application.Services;

namespace BattleTanks_Backend.Infrastructure.Middleware;

public class FirebaseAuthMiddleware
{
    private readonly RequestDelegate _next;

    public FirebaseAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IPlayerRepository playerRepository, IAuthService authService, RedisCacheService redisCache)
    {
        var endpoint = context.GetEndpoint();
        var requiresAuth = endpoint?.Metadata.GetMetadata<RequireFirebaseAuthAttribute>() != null;

        if (!requiresAuth)
        {
            await _next(context);
            return;
        }

        string? firebaseUid = null;
        bool authenticatedViaHeader = false;

        var headerUserId = context.Request.Headers["X-Firebase-Uid"].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerUserId))
        {
            firebaseUid = headerUserId;
            authenticatedViaHeader = true;
            Console.WriteLine($"Auth via X-Firebase-Uid header: {firebaseUid}");
        }

        if (string.IsNullOrEmpty(firebaseUid))
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();

                try
                {
                    firebaseUid = await authService.VerifyTokenAsync(token);
                    if (!string.IsNullOrEmpty(firebaseUid))
                    {
                        Console.WriteLine($"Auth via Firebase ID Token: {firebaseUid}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Firebase token validation failed: {ex.Message}");
                }
            }
        }

        if (string.IsNullOrEmpty(firebaseUid))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "No autenticado. Usa header X-Firebase-Uid o Bearer token.",
                hint = "Para testing: Header 'X-Firebase-Uid: uid'. Para producción: 'Authorization: Bearer <firebase_id_token>'"
            });
            return;
        }

        var cachedSession = await redisCache.GetCachedSessionAsync(firebaseUid);

        if (cachedSession != null)
        {
            Console.WriteLine($"[Redis Cache] Sesión encontrada en caché para {firebaseUid}");

            var claims = new List<Claim>
            {
                new Claim("FirebaseUid", firebaseUid),
                new Claim("PlayerId", cachedSession["PlayerId"]),
                new Claim("Username", cachedSession["Username"]),
                new Claim("Email", cachedSession["Email"]),
                new Claim("AuthMethod", authenticatedViaHeader ? "Header" : "FirebaseToken")
            };

            var identity = new ClaimsIdentity(claims, "FirebaseAuth");
            context.User = new ClaimsPrincipal(identity);

            context.Items["PlayerId"] = Guid.Parse(cachedSession["PlayerId"]);
            context.Items["FirebaseUid"] = firebaseUid;
            context.Items["Username"] = cachedSession["Username"];

            await _next(context);
            return;
        }

        var player = await playerRepository.GetByFirebaseUidAsync(firebaseUid);

        if (player == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Usuario no registrado. Registra tu cuenta primero." });
            return;
        }

        await redisCache.CacheSessionAsync(firebaseUid, new Dictionary<string, string>
        {
            ["PlayerId"] = player.Id.ToString(),
            ["Username"] = player.Username,
            ["Email"] = player.Email
        });

        var playerClaims = new List<Claim>
        {
            new Claim("FirebaseUid", firebaseUid),
            new Claim("PlayerId", player.Id.ToString()),
            new Claim("Username", player.Username),
            new Claim("Email", player.Email),
            new Claim("AuthMethod", authenticatedViaHeader ? "Header" : "FirebaseToken")
        };

        var playerIdentity = new ClaimsIdentity(playerClaims, "FirebaseAuth");
        context.User = new ClaimsPrincipal(playerIdentity);

        context.Items["PlayerId"] = player.Id;
        context.Items["FirebaseUid"] = firebaseUid;
        context.Items["Username"] = player.Username;

        await _next(context);
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireFirebaseAuthAttribute : Attribute
{
}

public static class FirebaseAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseFirebaseAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FirebaseAuthMiddleware>();
    }
}

