using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.Tracing;
using System.Diagnostics.CodeAnalysis;

namespace AutoRunCodeFix.Analyser {
    // [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CodeRefactoringCodeRefactoringProvider)), Shared]
    public class AutoRunRefactoringProvider : CodeRefactoringProvider {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context) {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null) { return; }

            // Find the node at the selection.
            var node = root.FindNode(context.Span);
            if (node is ClassDeclarationSyntax classDeclaration) {
                var id = classDeclaration.Identifier;
                if (context.Document.Project.SupportsCompilation) {
                    var compilation = await context.Document.Project.GetCompilationAsync(context.CancellationToken).ConfigureAwait(false);
                    if (compilation is null) { return; }
                    // Find attribute
                    // compilation.
                }
            }
        }
    }
}
#if false
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace CodeRefactoring
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CodeRefactoringCodeRefactoringProvider)), Shared]
    internal class CodeRefactoringCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Find the node at the selection.
            var node = root.FindNode(context.Span);

            // Only offer a refactoring if the selected node is a method declaration node with non-void return type and at least one 'out' var.
            var methodDecl = node as MethodDeclarationSyntax;
            if (methodDecl == null ||
                methodDecl.ReturnType.Kind() == SyntaxKind.VoidKeyword ||
                !GetOutParameters(methodDecl).Any())
            {
                return;
            }

            // Check if the compilation references System.ValueTuple
            var hasValueTuple = false;
            if (context.Document.Project.SupportsCompilation)
            {
                var compilation = await context.Document.Project.GetCompilationAsync(context.CancellationToken).ConfigureAwait(false);
                var systemValueTuple = compilation?.GetTypeByMetadataName(@"System.ValueTuple");
                if (systemValueTuple != null && systemValueTuple.ContainingAssembly.Name.Equals(@"System.ValueTuple"))
                {
                    hasValueTuple = true;
                }
            }

            if (hasValueTuple)
            {
                // Create a code action to transform the method signature to use tuples.
                var action = CodeAction.Create("Use ValueTuple return type", c => UseValueTupleAsync(context.Document, methodDecl, c));

                // Register this code action.
                context.RegisterRefactoring(action);
            }
        }

        private static IEnumerable<ParameterSyntax> GetOutParameters(MethodDeclarationSyntax methodDecl)
            => methodDecl.ParameterList.Parameters.Where(parameter => parameter.Modifiers.Any(m => m.Kind() == SyntaxKind.OutKeyword));

        private async Task<Document> UseValueTupleAsync(Document document, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var originalMethodDecl = methodDecl;

            // Compute the new parameter list with all the out parameters removed.
            var outParameters = GetOutParameters(methodDecl);
            var newParameters = methodDecl.ParameterList.Parameters.Where(p => !outParameters.Contains(p));
            var newParameterList = methodDecl.ParameterList.Update(
                methodDecl.ParameterList.OpenParenToken,
                new SeparatedSyntaxList<ParameterSyntax>().AddRange(newParameters),
                methodDecl.ParameterList.CloseParenToken);
            methodDecl = methodDecl.WithParameterList(newParameterList);

            // Compute the new return type: Tuple type with the original return type as first element and
            // types for all original out parameters as subsequent elements.
            var newReturnType = GenerateTupleType(methodDecl.ReturnType, outParameters);
            methodDecl = methodDecl.WithReturnType(newReturnType);

            // Add local declaration statements as the start of the method body to declare locals for original out parameters.
            var newStatements = new List<StatementSyntax>(outParameters.Count());
            foreach (var outParam in outParameters)
            {
                var variableDeclarator = SyntaxFactory.VariableDeclarator(outParam.Identifier);
                var variableDeclarationSyntax = SyntaxFactory.VariableDeclaration(outParam.Type, SyntaxFactory.SingletonSeparatedList(variableDeclarator));
                var localDeclarationStatement = SyntaxFactory.LocalDeclarationStatement(variableDeclarationSyntax);
                newStatements.Add(localDeclarationStatement);
            }

            var statements = methodDecl.Body.Statements;
            var newBody = methodDecl.Body.WithStatements(methodDecl.Body.Statements.InsertRange(0, newStatements));
            methodDecl = methodDecl.WithBody(newBody);

            // Replace all return statement expressions with tuple expressions: original return expression
            // as the first argument and identifier name for original out parameters as subsequent arguments.
            var returnStatements = methodDecl.Body.DescendantNodes().OfType<ReturnStatementSyntax>();
            var replacementNodeMap = new Dictionary<ReturnStatementSyntax, ReturnStatementSyntax>(returnStatements.Count());
            foreach (var returnStatement in returnStatements)
            {
                var tupleExpression = GenerateTupleExpression(returnStatement.Expression, outParameters);
                var newReturnStatement = SyntaxFactory.ReturnStatement(tupleExpression);
                replacementNodeMap.Add(returnStatement, newReturnStatement);
            }

            methodDecl = methodDecl.ReplaceNodes(returnStatements, computeReplacementNode: (o, n) => replacementNodeMap[o]);

            // Add formatter annotation to format the edited method declaration and body.
            methodDecl = methodDecl.WithAdditionalAnnotations(Formatter.Annotation);

            // Return new document with replaced method declaration.
            var newRoot = root.ReplaceNode(originalMethodDecl, methodDecl);
            return document.WithSyntaxRoot(newRoot);
        }

        private TupleTypeSyntax GenerateTupleType(TypeSyntax firstArgument, IEnumerable<ParameterSyntax> outParameters)
        {
            var newArgumentList = new SeparatedSyntaxList<TupleElementSyntax>();
            newArgumentList = newArgumentList.Add(SyntaxFactory.TupleElement(firstArgument));
            foreach (var outParam in outParameters)
            {
                var argument = SyntaxFactory.TupleElement(outParam.Type);
                newArgumentList = newArgumentList.Add(argument);
            }

            return SyntaxFactory.TupleType(newArgumentList);
        }

        private TupleExpressionSyntax GenerateTupleExpression(ExpressionSyntax firstArgument, IEnumerable<ParameterSyntax> outParameters)
        {
            var newArgumentList = new SeparatedSyntaxList<ArgumentSyntax>();
            newArgumentList = newArgumentList.Add(SyntaxFactory.Argument(firstArgument));
            foreach (var outParam in outParameters)
            {
                var argument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(outParam.Identifier.ValueText));
                newArgumentList = newArgumentList.Add(argument);
            }

            return SyntaxFactory.TupleExpression(newArgumentList);
        }
    }
}
#endif