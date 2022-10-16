using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree VarExpression() {
            var tok = this.Advance(TokenKind.VarKeyword);
            var name = this.Advance(TokenKind.Identifier).Value;

            this.Advance(TokenKind.Assignment);

            var assign = this.TopExpression();
            var loc = tok.Location.Span(assign.Location);

            return new VarParseStatement(loc, name, assign, true);
        }
    }
}

namespace Trophy {
    public class VarParseStatement : ISyntaxTree {
        private readonly string name;
        private readonly ISyntaxTree assign;
        private readonly bool isWritable;

        public TokenLocation Location { get; }

        public VarParseStatement(TokenLocation loc, string name, ISyntaxTree assign, bool isWritable) {
            this.Location = loc;
            this.name = name;
            this.assign = assign;
            this.isWritable = isWritable;
        }

        public Option<TrophyType> ToType(IdentifierPath scope, TypesRecorder types) => Option.None;

        public ISyntaxTree ResolveTypes(IdentifierPath scope, TypesRecorder types) {
            // Type check the assignment value
            if (!this.assign.ResolveTypes(scope, types).ToRValue(types).TryGetValue(out var assign)) {
                throw TypeCheckingErrors.RValueRequired(this.assign.Location);
            }

            // Declare this variable and make sure we're not shadowing another variable
            if (!types.TrySetNameTarget(scope, this.name, NameTarget.Variable)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.name);
            }

            // Declare this variable
            var assignType = types.GetReturnType(assign);
            types.SetVariable(scope.Append(this.name), assignType, this.isWritable);

            // Set the return type of the new syntax tree
            var result = new VarStatement(this.Location, scope.Append(this.name), assign);
            types.SetReturnType(result, new PointerType(assignType));

            return result;
        }

        public Option<ISyntaxTree> ToRValue(TypesRecorder types) => throw new InvalidOperationException();

        public Option<ISyntaxTree> ToLValue(TypesRecorder types) => throw new InvalidOperationException();

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter statWriter) {
            throw new InvalidOperationException();
        }
    }

    public class VarStatement : ISyntaxTree {
        private readonly IdentifierPath path;
        private readonly ISyntaxTree assign;

        public TokenLocation Location { get; }

        public VarStatement(TokenLocation loc, IdentifierPath path, ISyntaxTree assign) {
            this.Location = loc;
            this.path = path;
            this.assign = assign;
        }

        public Option<TrophyType> ToType(IdentifierPath scope, TypesRecorder types) => Option.None;

        public ISyntaxTree ResolveTypes(IdentifierPath scope, TypesRecorder types) => this;

        public Option<ISyntaxTree> ToRValue(TypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(TypesRecorder types) => Option.None;

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter writer) {
            var type = writer.ConvertType(types.GetReturnType(this.assign));
            var value = this.assign.GenerateCode(types, writer);
            var assign = CStatement.VariableDeclaration(type, this.path.ToCName(), value);

            writer.WriteSpacingLine();
            writer.WriteStatement(CStatement.Comment("Variable declaration statement"));
            writer.WriteStatement(assign);
            writer.WriteSpacingLine();

            return CExpression.AddressOf(CExpression.VariableLiteral(this.path.ToCName()));
        }
    }
}