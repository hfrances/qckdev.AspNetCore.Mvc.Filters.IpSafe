using System;
using System.Collections.Generic;
#if NET10a_0_OR_GREATER
using PlatformIPNetwork = System.Net.IPNetwork;
#else
using PlatformIPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;
#endif

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    sealed class IpNetworkComparer : IEqualityComparer<PlatformIPNetwork>
    {
#if NET10a_0_OR_GREATER
        public bool Equals(PlatformIPNetwork x, PlatformIPNetwork y)
        {
            return x.BaseAddress.Equals(y.BaseAddress) && x.PrefixLength.Equals(y.PrefixLength);
        }

        public int GetHashCode(PlatformIPNetwork obj)
        {
            int hashNetwork = obj.BaseAddress.GetHashCode();
            int hashNetmask = obj.PrefixLength.GetHashCode();
            return hashNetwork ^ hashNetmask;
        }
#else
        public bool Equals(PlatformIPNetwork? x, PlatformIPNetwork? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Prefix.Equals(y.Prefix) && x.PrefixLength.Equals(y.PrefixLength);
        }

        public int GetHashCode(PlatformIPNetwork obj)
        {
            if (obj == null)
            {
                return 0;
            }

            int hashNetwork = obj.Prefix.GetHashCode();
            int hashNetmask = obj.PrefixLength.GetHashCode();
            return hashNetwork ^ hashNetmask;
        }
#endif
    }
}
