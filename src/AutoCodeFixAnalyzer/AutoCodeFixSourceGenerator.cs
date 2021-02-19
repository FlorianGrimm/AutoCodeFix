using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace AutoCodeFixAnalyzer {

    /* https://github.com/dotnet/roslyn/blob/master/docs/features/source-generators.cookbook.md */
    [Generator]
    public class AutoCodeFixSourceGenerator : ISourceGenerator {
        public void Initialize(GeneratorInitializationContext context) {
            context.RegisterForSyntaxNotifications(() => new AutoCodeFixSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context) {
            if (context.SyntaxReceiver is AutoCodeFixSyntaxReceiver autoCodeFixSyntaxReceiver) {
                // autoCodeFixSyntaxReceiver
                // context.AddSource
            }
        }
    }
    public class AutoCodeFixSyntaxReceiver : ISyntaxReceiver {
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
        }
    }
}
