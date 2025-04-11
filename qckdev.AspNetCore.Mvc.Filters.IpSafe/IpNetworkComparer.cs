using Microsoft.AspNetCore.HttpOverrides;
using System;
using System.Collections.Generic;
using System.Text;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    sealed class IpNetworkComparer : IEqualityComparer<IPNetwork>
    {
        public bool Equals(IPNetwork x, IPNetwork y)
        {
            if (x == null || y == null)
                return false;

            // Comparar las direcciones de red y las máscaras usando las propiedades correctas de IPNetwork en ASP.NET Core
            return x.Prefix.Equals(y.Prefix) && x.PrefixLength.Equals(y.PrefixLength);
        }

        public int GetHashCode(IPNetwork obj)
        {
            if (obj == null)
                return 0;

            // Combinar los códigos hash de la dirección de red y la máscara de subred
            int hashNetwork = obj.Prefix.GetHashCode();
            int hashNetmask = obj.PrefixLength.GetHashCode();
            return hashNetwork ^ hashNetmask;
        }
    }
}
