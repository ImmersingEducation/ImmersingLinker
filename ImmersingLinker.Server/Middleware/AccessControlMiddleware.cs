using System.Security.Cryptography;
using System.Text;
using ImmersingLinker.Core.Abstractions.AccessControl;
using ImmersingLinker.Core.Abstractions.Permission;

namespace ImmersingLinker.Server.Middleware;

public class AccessControlMiddleware
{
    private static readonly string[] _exemptPaths =
        ["/app/hello", "/scalar", "/openapi", "/permission/register"];

    private static readonly TimeSpan _timestampWindow = TimeSpan.FromMinutes(5);

    private readonly RequestDelegate _next;

    public AccessControlMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IPermissionService permissionService,
        IAccessControlService accessControlService)
    {
        var path = context.Request.Path.Value ?? "";

        if (IsExempt(path))
        {
            await _next(context);
            return;
        }

        if (!TryExtractHeaders(context, out var appId, out var timestamp, out var signature))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing or invalid authentication headers.");
            return;
        }

        if (!IsTimestampValid(timestamp))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Timestamp out of window.");
            return;
        }

        var app = permissionService.GetByAppId(appId);
        if (app is null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unknown application.");
            return;
        }

        var method = context.Request.Method;
        var expectedSignature = ComputeHmac(app.Secret, $"{timestamp}\n{method}\n{path}");

        if (!VerifySignature(signature, expectedSignature))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid signature.");
            return;
        }

        var ipString = context.Connection.RemoteIpAddress?.ToString();
        var result = accessControlService.CheckAccess(app.Application, ipString, method);

        if (result == AccessCheckResult.Denied)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Access denied.");
            return;
        }

        await _next(context);
    }

    private static bool IsExempt(string path)
    {
        return _exemptPaths.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryExtractHeaders(
        HttpContext context,
        out string appId,
        out long timestamp,
        out string signature)
    {
        appId = "";
        timestamp = 0;
        signature = "";

        var headers = context.Request.Headers;

        if (!headers.TryGetValue("X-App-Id", out var appIdValues) ||
            !headers.TryGetValue("X-App-Timestamp", out var tsValues) ||
            !headers.TryGetValue("X-App-Signature", out var sigValues))
            return false;

        appId = appIdValues.ToString();
        signature = sigValues.ToString();

        return long.TryParse(tsValues.ToString(), out timestamp);
    }

    private static bool IsTimestampValid(long timestamp)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Math.Abs(now - timestamp) <= (long)_timestampWindow.TotalSeconds;
    }

    private static string ComputeHmac(string secret, string message)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var hash = HMACSHA256.HashData(keyBytes, messageBytes);
        return Convert.ToHexStringLower(hash);
    }

    private static bool VerifySignature(string actual, string expected)
    {
        if (actual.Length != expected.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actual),
            Encoding.UTF8.GetBytes(expected));
    }
}
