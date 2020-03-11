using Attempt17.Parsing;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Parsing {
    public class ParseFunctionDeclaration : IParseDeclaration {
        public ParseTag Tag { get; }

        public FunctionInfo FunctionInfo { get; }

        public ISyntax<ParseTag> Body { get; }

        public ParseFunctionDeclaration(ParseTag tag, FunctionInfo info, ISyntax<ParseTag> body) {
            this.Tag = tag;
            this.FunctionInfo = info;
            this.Body = body;
        }

        T IParseDeclaration.Accept<T>(IParseDeclarationVisitor<T> visitor) {
            return visitor.VisitFunctionDeclaration(this);
        }
    }
}