using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using System;
using System.Collections.Immutable;

namespace AutoRunCodeFix.Analyser {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    //[AutoRunDiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AutoRunDiagnosticAnalyzer : DiagnosticAnalyzer {

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
            get {
                var instance = AutoRunRules.GetInstance();
                return ImmutableArray.Create(instance.ACF001);
            }
        }

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            context.RegisterSemanticModelAction(AnalyzeSemanticModel);
            //context.RegisterOperationBlockStartAction((operationBlockStartActionContext) => {
            //    operationBlockStartActionContext.
            //});
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context) {
            // context.ReportDiagnostic();
            var metadataNameAutoCodeFix = context.Compilation.GetTypeByMetadataName(AutoRunWellknownNames.AutoRunCodeFix_AutoCodeFixAttribute);
        }

        private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context) {
            // context.ReportDiagnostic();            
            //context.SemanticModel.getor
        }
    }
}
