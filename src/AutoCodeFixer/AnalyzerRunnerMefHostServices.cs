using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;

namespace AutoCodeFixer {
    internal static class AnalyzerRunnerMefHostServices {
        private static MefHostServices s_defaultServices;

        public static MefHostServices DefaultServices {
            get {
                if (s_defaultServices is null) {
                    Interlocked.CompareExchange(ref s_defaultServices, MefHostServices.Create(DefaultAssemblies), null);
                }

                return s_defaultServices;
            }
        }

        private static ImmutableArray<Assembly> DefaultAssemblies
            => MSBuildMefHostServices.DefaultAssemblies.Add(typeof(AnalyzerRunnerMefHostServices).Assembly);
    }
}
