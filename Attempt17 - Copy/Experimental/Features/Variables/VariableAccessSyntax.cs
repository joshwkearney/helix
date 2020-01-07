namespace Attempt17.Experimental.Features.Variables {
    public class VariableAccessSyntax : ISyntax<TypeCheckInfo> {
        public TypeCheckInfo Tag { get; }

        public VariableAccessKind Kind { get; }

        public VariableInfo VariableInfo { get; }

        public VariableAccessSyntax(TypeCheckInfo tag, VariableAccessKind kind, VariableInfo variableInfo) {
            this.Tag = tag;
            this.Kind = kind;
            this.VariableInfo = variableInfo;
        }
    }
}