using Trophy;
using Trophy.Analysis;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing {
    public partial class Parser {
        private IParseTree VariableAccess() {
            var tok = this.Advance(TokenKind.Identifier);

            return new VariableAccessParseTree(tok.Location, tok.Value);
        }
    }
}

namespace Trophy {
    public class VariableAccessParseTree : IParseTree {
        private readonly string name;

        public TokenLocation Location { get; }

        public VariableAccessParseTree(TokenLocation location, string name) {
            this.Location = location;
            this.name = name;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            if (!names.TryFindName(scope, this.name).TryGetValue(out var path)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            if (!names.TryGetName(path).TryGetValue(out var target)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            if (target == NameTarget.Variable) {
                return new VariableAccessSyntax(path, types.Variables[path], false);
            }

            throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
        }
    }

    public class VariableAccessSyntax : ISyntaxTree {
        private bool isLValue;

        public IdentifierPath Path { get; }

        public TrophyType ReturnType { get; }

        public VariableAccessSyntax(IdentifierPath path, TrophyType returnType, bool isLValue) {
            this.ReturnType = returnType;
            this.Path = path;
            this.isLValue = isLValue;
        }

        public Option<ISyntaxTree> ToLValue() {
            if (this.isLValue) {
                return this;
            }
            else {
                return new VariableAccessSyntax(this.Path, new PointerType(this.ReturnType), true);
            }
        }

        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter) {
            if (this.isLValue) {
                return CExpression.AddressOf(CExpression.VariableLiteral(this.Path.ToCName()));
            }
            else {
                return CExpression.VariableLiteral(this.Path.ToCName());
            }
        }
    }
}