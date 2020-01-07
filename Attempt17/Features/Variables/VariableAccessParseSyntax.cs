using Attempt17.Parsing;

namespace Attempt17.Features.Variables {
    public class VariableAccessParseSyntax : ISyntax<ParseTag> {
        public ParseTag Tag { get; }

        public string VariableName { get; }

        public VariableAccessKind Kind { get; }

        public VariableAccessParseSyntax(ParseTag tag, VariableAccessKind kind, string variableName) {
            this.Tag = tag;
            this.Kind = kind;
            this.VariableName = variableName;
        }
    }
}