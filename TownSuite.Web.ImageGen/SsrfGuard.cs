using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace TownSuite.Web.ImageGen;

/// <summary>
/// Server-Side Request Forgery (SSRF) protection for the image proxy.
///
/// The proxy fetches an arbitrary, attacker-supplied URL. Without controls this can be
/// abused to reach cloud metadata endpoints (e.g. 169.254.169.254), loopback, and
/// internal/cluster-only services, and to scan the internal network.
///
/// This guard enforces two layers:
///   1. <see cref="ValidateUrl"/> rejects non-http(s) schemes up front.
///   2. <see cref="ConnectCallback"/> is wired into the HttpClient's SocketsHttpHandler.
///      It resolves the host, rejects any disallowed (private/loopback/link-local/etc.)
///      address, and connects to a validated IP. Validating at connect time (rather than
///      only on the original URL string) defeats DNS rebinding and redirect-based bypass.
/// Redirect following is disabled on the handler as additional defense in depth.
/// </summary>
public static class SsrfGuard
{
    public static void ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Image source URL is not a valid absolute URL.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Only http and https image source URLs are allowed.");
        }
    }

    public static async ValueTask<Stream> ConnectCallback(
        SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        string host = context.DnsEndPoint.Host;
        int port = context.DnsEndPoint.Port;

        IPAddress[] addresses;
        if (IPAddress.TryParse(host, out var literal))
        {
            addresses = new[] { literal };
        }
        else
        {
            addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        }

        if (addresses.Length == 0)
        {
            throw new IOException($"Could not resolve host '{host}'.");
        }

        // Reject the whole request if ANY resolved address is disallowed. This is strict
        // on purpose: it prevents a host that resolves to both a public and a private
        // address from being used to reach the private one.
        foreach (var address in addresses)
        {
            if (IsBlocked(address))
            {
                throw new IOException($"Refusing to connect to disallowed address {address}.");
            }
        }

        // Create a socket that can reach the validated address(es). Prefer an IPv6
        // dual-mode socket (handles AAAA records and, via IPv4-mapped addresses, A records);
        // fall back to IPv4 when the OS has no IPv6 support. A plain
        // new Socket(SocketType, ProtocolType) would fail for IPv6-only destinations.
        Socket socket;
        IPAddress[] connectAddresses;
        if (Socket.OSSupportsIPv6)
        {
            socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
            {
                DualMode = true,
                NoDelay = true
            };
            connectAddresses = addresses
                .Select(a => a.AddressFamily == AddressFamily.InterNetwork ? a.MapToIPv6() : a)
                .ToArray();
        }
        else
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };
            connectAddresses = addresses
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                .ToArray();

            if (connectAddresses.Length == 0)
            {
                socket.Dispose();
                throw new IOException($"Host '{host}' has no reachable IPv4 address.");
            }
        }

        try
        {
            // Connect to the validated address(es) directly so the destination cannot
            // change between validation and the actual connection (DNS rebinding).
            await socket.ConnectAsync(connectAddresses, port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Returns true for addresses the proxy must never connect to: loopback, private,
    /// link-local (incl. cloud metadata 169.254.0.0/16), CGNAT, multicast/reserved,
    /// unique-local IPv6, etc.
    /// </summary>
    public static bool IsBlocked(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        // IPv6 unspecified address (:: / IPAddress.IPv6Any) — the IPv6 equivalent of
        // 0.0.0.0; must be rejected (the IPv4 0.0.0.0/8 case is handled below).
        if (address.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        byte[] bytes = address.GetAddressBytes();

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            // 0.0.0.0/8 (this network / unspecified)
            if (bytes[0] == 0) return true;
            // 10.0.0.0/8 (private)
            if (bytes[0] == 10) return true;
            // 100.64.0.0/10 (carrier-grade NAT)
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return true;
            // 127.0.0.0/8 (loopback; also covered by IsLoopback)
            if (bytes[0] == 127) return true;
            // 169.254.0.0/16 (link-local, incl. cloud metadata)
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            // 172.16.0.0/12 (private)
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            // 192.168.0.0/16 (private)
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            // 224.0.0.0/4 (multicast) and 240.0.0.0/4 (reserved/broadcast)
            if (bytes[0] >= 224) return true;

            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
            {
                return true;
            }
            // Unique local addresses fc00::/7
            if ((bytes[0] & 0xFE) == 0xFC) return true;

            return false;
        }

        // Unknown address family: block by default.
        return true;
    }
}
