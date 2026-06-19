using System.Net;
using TownSuite.Web.ImageGen;

namespace TownSuite.Web.ImageGen.Tests;

[TestFixture]
public class SsrfGuardTest
{
    [TestCase("http://example.com/a.png")]
    [TestCase("https://example.com/a.png")]
    [TestCase("HTTP://Example.com")]
    [TestCase("HTTPS://example.com:8443/x")]
    public void ValidateUrl_AllowsHttpAndHttps(string url)
    {
        Assert.DoesNotThrow(() => SsrfGuard.ValidateUrl(url));
    }

    [TestCase("ftp://example.com/a.png")]
    [TestCase("file:///etc/passwd")]
    [TestCase("gopher://example.com")]
    [TestCase("data:text/plain,hello")]
    [TestCase("/relative/path")]
    [TestCase("not a url")]
    [TestCase("")]
    [TestCase("   ")]
    public void ValidateUrl_RejectsNonHttp(string url)
    {
        Assert.Throws<ArgumentException>(() => SsrfGuard.ValidateUrl(url));
    }

    [Test]
    public void ValidateUrl_RejectsNull()
    {
        Assert.Throws<ArgumentException>(() => SsrfGuard.ValidateUrl(null!));
    }

    // IPv4
    [TestCase("127.0.0.1", true)]            // loopback
    [TestCase("127.5.5.5", true)]            // loopback /8
    [TestCase("10.0.0.1", true)]             // private
    [TestCase("10.255.255.255", true)]       // private
    [TestCase("172.16.0.1", true)]           // private /12 lower bound
    [TestCase("172.31.255.255", true)]       // private /12 upper bound
    [TestCase("172.15.0.1", false)]          // just below /12
    [TestCase("172.32.0.1", false)]          // just above /12
    [TestCase("192.168.1.1", true)]          // private
    [TestCase("169.254.169.254", true)]      // link-local / cloud metadata
    [TestCase("100.64.0.1", true)]           // CGNAT lower bound
    [TestCase("100.127.255.255", true)]      // CGNAT upper bound
    [TestCase("100.63.255.255", false)]      // just below CGNAT
    [TestCase("100.128.0.1", false)]         // just above CGNAT
    [TestCase("0.0.0.0", true)]              // "this network"
    [TestCase("224.0.0.1", true)]            // multicast
    [TestCase("240.0.0.1", true)]            // reserved
    [TestCase("8.8.8.8", false)]             // public
    [TestCase("1.1.1.1", false)]             // public
    [TestCase("93.184.216.34", false)]       // public (example.com)
    // IPv6
    [TestCase("::1", true)]                  // loopback
    [TestCase("fe80::1", true)]              // link-local
    [TestCase("fc00::1", true)]              // unique local (fc00::/7)
    [TestCase("fd00::1", true)]              // unique local
    [TestCase("ff02::1", true)]              // multicast
    [TestCase("::ffff:10.0.0.1", true)]      // IPv4-mapped private
    [TestCase("::ffff:169.254.169.254", true)] // IPv4-mapped metadata
    [TestCase("::ffff:8.8.8.8", false)]      // IPv4-mapped public
    [TestCase("2606:4700:4700::1111", false)] // public (Cloudflare)
    public void IsBlocked_ClassifiesAddresses(string ip, bool expected)
    {
        var address = IPAddress.Parse(ip);
        Assert.That(SsrfGuard.IsBlocked(address), Is.EqualTo(expected));
    }
}
