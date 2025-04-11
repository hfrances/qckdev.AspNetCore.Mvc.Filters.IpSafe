using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    sealed class IpSafeProperties
    {

        public IEnumerable<IPAddress> IpAddresses { get; }
        public IEnumerable<IPNetwork2> IpNetworks { get; }
        public IEnumerable<IPAddress> KnownProxies { get; }


        public IpSafeProperties(IEnumerable<IPAddress> ipAddresses, IEnumerable<IPNetwork2> ipNetworks, IEnumerable<IPAddress> knownProxies)
        {
            this.IpAddresses = ipAddresses;
            this.IpNetworks = ipNetworks;
            this.KnownProxies = knownProxies;
        }

    }
}
