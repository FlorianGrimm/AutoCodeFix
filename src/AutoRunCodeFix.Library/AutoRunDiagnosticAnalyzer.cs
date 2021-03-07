using System;

namespace AutoCodeFixLibrary
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class AutoRunDiagnosticAnalyzer : System.Attribute {
        public AutoRunDiagnosticAnalyzer() {
        }
    }
}
