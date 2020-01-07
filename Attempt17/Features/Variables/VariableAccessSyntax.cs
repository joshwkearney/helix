using Attempt17.TypeChecking;

namespace Attempt17.Features.Variables {
    public class VariableAccessSyntax : ISyntax<TypeCheckTag> {
        public TypeCheckTag Tag { get; }

        public VariableAccessKind Kind { get; }

        public VariableInfo VariableInfo { get; }

        public VariableAccessSyntax(TypeCheckTag tag, VariableAccessKind kind, VariableInfo variableInfo) {
            this.Tag = tag;
            this.Kind = kind;
            this.VariableInfo = variableInfo;
        }
    }
}