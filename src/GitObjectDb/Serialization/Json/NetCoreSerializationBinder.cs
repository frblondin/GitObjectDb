using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace GitObjectDb.Serialization.Json
{
    internal class NetCoreSerializationBinder : DefaultSerializationBinder
    {
        private const string NetCoreLib = "System.Private.CoreLib";
        private const string MsCoreLib = "mscorlib";

        private NetCoreSerializationBinder()
        {
        }

        public static NetCoreSerializationBinder Instance { get; } = new NetCoreSerializationBinder();

        private static bool IsFullFramework { get; } =
            RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);

        public override Type BindToType(string assemblyName, string typeName) =>
            base.BindToType(ReplaceCoreLib(assemblyName), ReplaceCoreLib(typeName));

        private static string ReplaceCoreLib(string name) =>
            IsFullFramework ?
            name?.Replace(NetCoreLib, MsCoreLib) :
            name?.Replace(MsCoreLib, NetCoreLib);
    }
}
