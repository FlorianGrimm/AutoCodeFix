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

namespace AutoCodeFixAnalyzer {
    public interface IAutoCodeFixProvider {
    }

    //[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AutoCodeFixProvider)), Shared]
    public class AutoCodeFixProvider : CodeFixProvider, IAutoCodeFixProvider {
        public sealed override ImmutableArray<string> FixableDiagnosticIds {
            get { return ImmutableArray.Create("ACF0001"); }
        }
        public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
            await Task.CompletedTask;
        }
        public override FixAllProvider? GetFixAllProvider() {
            return new AutoFixAllProvider();
        }
    }
    public class AutoFixAllProvider : FixAllProvider {
        public override IEnumerable<string> GetSupportedFixAllDiagnosticIds(CodeFixProvider originalCodeFixProvider) {
            return base.GetSupportedFixAllDiagnosticIds(originalCodeFixProvider);
        }
        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() {
            return base.GetSupportedFixAllScopes();
        }
        public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext) {
            await Task.CompletedTask;
            return null;
        }
    }
}
