using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{

    /// <summary>
    /// Specifies that the class or method that this attribute is applied to does not require IP address validation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class AllowAnyIpAddressAttribute : Attribute, IFilterMetadata
    {
        
    }

}
