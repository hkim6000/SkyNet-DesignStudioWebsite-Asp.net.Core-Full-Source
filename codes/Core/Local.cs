using System.Net;
using Microsoft.AspNetCore.Http;

namespace DesignStudio.Core
{
    public static class Local
    {
        public static IHttpContextAccessor? Accessor { get; set; }

        public static bool Request()
        {
            HttpContext? h = Accessor?.HttpContext;
            if (h == null)
            {
                return false;
            }
            IPAddress? ip = h.Connection.RemoteIpAddress;
            string host = h.Request.Host.Host;
            bool named = host is "localhost" or "127.0.0.1" or "[::1]" or "::1";
            return ip != null && IPAddress.IsLoopback(ip) && named && !h.Request.Headers.ContainsKey("X-Forwarded-For");
        }

        public static string Caller()
        {
            HttpContext? h = Accessor?.HttpContext;
            string ip = h?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            return ip.Length == 0 ? "unknown" : ip;
        }
    }
}
