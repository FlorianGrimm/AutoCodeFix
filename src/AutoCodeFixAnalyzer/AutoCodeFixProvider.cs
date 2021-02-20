using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Editing;
using System.Threading;

namespace AutoCodeFixAnalyzer {
    public interface IAutoCodeFixProvider {
    }

    //[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AutoCodeFixProvider)), Shared]
    public class AutoCodeFixProvider : CodeFixProvider, IAutoCodeFixProvider {
        private const string title = "Remove unused local";

        public sealed override ImmutableArray<string> FixableDiagnosticIds {
            get { return ImmutableArray.Create("ACF0001"); }
        }

        public override FixAllProvider? GetFixAllProvider() {
            return new AutoFixAllProvider();
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
            var diagnostic = context.Diagnostics.First();

            // Get syntax node to remove for the unused local.
            var nodeToRemove = await GetNodeToRemoveAsync(context.Document, diagnostic, context.CancellationToken).ConfigureAwait(false);
            if (nodeToRemove == null) { return; }

            // Register a code action that will invoke the fix.
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) { return; }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => RemoveDeclarationAsync(context.Document, root, nodeToRemove, c),
                    equivalenceKey: title),
                diagnostic);
        }

        internal static async Task<SyntaxNode?> GetNodeToRemoveAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken) {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the variable declarator identified by the diagnostic.
            var variableDeclarator = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().First();
            if (variableDeclarator == null) { return null; }

            // Bail out if the initializer is non-constant (could have side effects if removed).
            if (variableDeclarator.Initializer != null) {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (semanticModel is null) { return null; }
                if (!semanticModel.GetConstantValue(variableDeclarator.Initializer.Value).HasValue) {
                    return null;
                }
            }

            // Bail out for code with syntax errors - parent of a declaration is not a local declaration statement.
            var variableDeclaration = variableDeclarator.Parent as VariableDeclarationSyntax;
            if (variableDeclaration == null) { return null; }
            var localDeclaration = variableDeclaration.Parent as LocalDeclarationStatementSyntax;
            if (localDeclaration == null) { return null; }

            // If the statement declares a single variable, the code fix should remove the whole statement.
            // Otherwise, the code fix should remove only this variable declaration.
            SyntaxNode nodeToRemove;
            if (variableDeclaration.Variables.Count == 1) {
                if (!(localDeclaration.Parent is BlockSyntax)) {
                    // Bail out for error case where local declaration is not embedded in a block.
                    // Compiler generates errors CS1023 (Embedded statement cannot be a declaration or labeled statement)
                    return null;
                }

                nodeToRemove = localDeclaration;
            } else {
                nodeToRemove = variableDeclarator;
            }

            return nodeToRemove;
        }

        private Task<Document> RemoveDeclarationAsync(Document document, SyntaxNode root, SyntaxNode declaration, CancellationToken cancellationToken) {
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            var newRoot = syntaxGenerator.RemoveNode(root, declaration);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }



    public class AutoFixAllProvider : FixAllProvider {
        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() {
            return ImmutableArray.Create(FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution);
        }
        public override IEnumerable<string> GetSupportedFixAllDiagnosticIds(CodeFixProvider originalCodeFixProvider) {
            return originalCodeFixProvider.FixableDiagnosticIds;
        }
        public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext) {
#if false
            await Task.CompletedTask;
            return null;
#endif
#if true
            var diagnosticsToFix = new List<KeyValuePair<Document, ImmutableArray<Diagnostic>>>();
            string titleFormat = "Remove all unused locals in {0} {1}";
            string? title = null;
            var documentsToFix = ImmutableArray<Document>.Empty;

            switch (fixAllContext.Scope) {
                case FixAllScope.Document: {
                        if (fixAllContext.Document is null) { return null; }
                        documentsToFix = ImmutableArray.Create(fixAllContext.Document);
                        title = string.Format(titleFormat, "document", fixAllContext.Document.Name);
                        break;
                    }

                case FixAllScope.Project: {
                        documentsToFix = fixAllContext.Project.Documents.ToImmutableArray();
                        title = string.Format(titleFormat, "project", fixAllContext.Project.Name);
                        break;
                    }

                case FixAllScope.Solution: {
                        foreach (Project project in fixAllContext.Solution.Projects) {
                            documentsToFix = documentsToFix.AddRange(project.Documents);
                        }

                        title = "Add all items in the solution to the public API";
                        break;
                    }

                case FixAllScope.Custom:
                    return null;
                default:
                    break;
            }

            foreach (Document document in documentsToFix) {
                ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                diagnosticsToFix.Add(new KeyValuePair<Document, ImmutableArray<Diagnostic>>(document, diagnostics));
            }
            if (title is null || diagnosticsToFix.Count == 0) { return null; }
            return new AutoFixAllCodeAction(title, fixAllContext.Solution, diagnosticsToFix);
#endif
        }
    }
    public class AutoFixAllCodeAction : CodeAction {
        private readonly List<KeyValuePair<Document, ImmutableArray<Diagnostic>>> _DiagnosticsToFix;
        private readonly Solution _Solution;

        public AutoFixAllCodeAction(string title, Solution solution, List<KeyValuePair<Document, ImmutableArray<Diagnostic>>> diagnosticsToFix) {
            this.Title = title;
            _Solution = solution;
            _DiagnosticsToFix = diagnosticsToFix;
        }

        public override string Title { get; }

        protected override async Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken) {
            var nodesToRemoveMap = new Dictionary<Document, HashSet<SyntaxNode>>();
            foreach (KeyValuePair<Document, ImmutableArray<Diagnostic>> pair in _DiagnosticsToFix) {
                Document document = pair.Key;
                ImmutableArray<Diagnostic> diagnostics = pair.Value;
                var nodesToRemove = new HashSet<SyntaxNode>();
                foreach (var diagnostic in diagnostics) {
                    var nodeToRemove = await AutoCodeFixProvider.GetNodeToRemoveAsync(document, diagnostic, cancellationToken).ConfigureAwait(false);
                    if (nodeToRemove != null) {
                        nodesToRemove.Add(nodeToRemove);
                    }
                }

                var candidateLocalDeclarationsToRemove = new HashSet<LocalDeclarationStatementSyntax>();
                foreach (var variableDeclarator in nodesToRemove.OfType<VariableDeclaratorSyntax>()) {
                    var localDeclaration = (LocalDeclarationStatementSyntax?)variableDeclarator?.Parent?.Parent;
                    if (localDeclaration is object) {
                        candidateLocalDeclarationsToRemove.Add(localDeclaration);
                    }
                }

                foreach (var candidate in candidateLocalDeclarationsToRemove) {
                    var hasUsedLocal = false;
                    foreach (var variable in candidate.Declaration.Variables) {
                        if (!nodesToRemove.Contains(variable)) {
                            hasUsedLocal = true;
                            break;
                        }
                    }

                    if (!hasUsedLocal) {
                        nodesToRemove.Add(candidate);
                        foreach (var variable in candidate.Declaration.Variables) {
                            nodesToRemove.Remove(variable);
                        }
                    }
                }

                nodesToRemoveMap.Add(document, nodesToRemove);
            }

            Solution newSolution = _Solution;

            foreach (KeyValuePair<Document, HashSet<SyntaxNode>> pair in nodesToRemoveMap) {
                var document = pair.Key;
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
                var newRoot = syntaxGenerator.RemoveNodes(root, pair.Value);
                newSolution = newSolution.WithDocumentSyntaxRoot(document.Id, newRoot);
            }

            return newSolution;
        }
    }
}
