namespace IpSafeExample.Services
{
    public sealed class IpSafeServiceSettings
    {
        public string? IpAddresses { get; set; }
        public string? IpNetworks { get; set; }
        public string? KnownProxies { get; set; }
        public string? TrustedKnownNetworks { get; set; }
    }
}
