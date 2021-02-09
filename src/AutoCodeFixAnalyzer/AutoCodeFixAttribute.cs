namespace Microsoft.CodeAnalysis {
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
    internal sealed class AutoCodeFixAttribute : System.Attribute {
        public AutoCodeFixAttribute(string codeFixId) {
            this.CodeFixId = codeFixId;
        }

        public string CodeFixId { get; set; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Enabled() => true;
    }
}