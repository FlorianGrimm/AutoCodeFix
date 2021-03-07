using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using System;
using System.Collections.Immutable;

namespace AutoRunCodeFix.Analyser {
    public class AutoRunRules {
        private static AutoRunRules? _Instance;
        public static AutoRunRules GetInstance() {
            return _Instance ??= new AutoRunRules();
        }

        public const string Category = "AutoCodeFix";

        public readonly DiagnosticDescriptor ACF001;

        public AutoRunRules() {
            // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization

            this.ACF001 = new DiagnosticDescriptor(
                    id: nameof(ACF001),
                    title: nameof(AutoRunDiagnosticAnalyzer), //Title.Value,
                    messageFormat: (new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources))).ToString(), // Type '{0}' needs to run CodeFix.
                    category: Category,
                    defaultSeverity: DiagnosticSeverity.Info,
                    isEnabledByDefault: true,
                    description: (new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources))).ToString() // Run CodeFixGenerator.
                );
        }
    }
}
