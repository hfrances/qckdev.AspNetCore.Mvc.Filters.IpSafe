using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Test
{
    [TestClass]
    public sealed class TestAssemblySetup
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext _)
        {
            LocalTestServiceManager.StartIfNeeded();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            LocalTestServiceManager.Stop();
        }
    }
}
