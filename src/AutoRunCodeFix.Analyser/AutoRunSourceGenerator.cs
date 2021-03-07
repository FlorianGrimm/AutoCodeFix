using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace AutoRunCodeFix.Analyser {

    /* https://github.com/dotnet/roslyn/blob/master/docs/features/source-generators.cookbook.md */
    [Generator]
    public class AutoRunSourceGenerator : ISourceGenerator {
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
        public readonly List<ClassDeclarationSyntax> AutoCodeFixAttributeSyntaxes;

        public AutoCodeFixSyntaxReceiver() {
            this.AutoCodeFixAttributeSyntaxes = new List<ClassDeclarationSyntax>();

        }


        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax) {
                var text = classDeclarationSyntax.Identifier.Text;
                if (text is object && String.Equals(text, nameof(AutoRunCodeFix.AutoCodeFixAttribute), StringComparison.Ordinal)) {

                }
            }
        }
    }
}
