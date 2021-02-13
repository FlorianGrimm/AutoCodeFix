using Microsoft.CodeAnalysis.CodeFixes;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace AutoCodeFixAnalyzer {
    public interface IAutoCodeFixProvider {
    }
    public class AutoCodeFixProvider : CodeFixProvider, IAutoCodeFixProvider {
        public override ImmutableArray<string> FixableDiagnosticIds => throw new NotImplementedException();
        public override Task RegisterCodeFixesAsync(CodeFixContext context) {
            throw new NotImplementedException();
        }
        public override FixAllProvider? GetFixAllProvider() {
            return base.GetFixAllProvider();
        }
    }
}
