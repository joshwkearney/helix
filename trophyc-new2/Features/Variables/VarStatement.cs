using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Variables;
using Trophy.Parsing;

namespace Trophy.Parsing
{
    public partial class Parser {
        private ISyntaxTree VarExpression() {
            TokenLocation startLok;
            bool isWritable;

            if (this.Peek(TokenKind.VarKeyword)) {
                startLok = this.Advance(TokenKind.VarKeyword).Location;
                isWritable = true;
            }
            else {
                startLok = this.Advance(TokenKind.LetKeyword).Location;
                isWritable = false;
            }

            var name = this.Advance(TokenKind.Identifier).Value;

            this.Advance(TokenKind.Assignment);

            var assign = this.TopExpression();
            var loc = startLok.Span(assign.Location);

            return new VarParseStatement(loc, name, assign, isWritable);
        }
    }
}

namespace Trophy
{
    public record VarParseStatement : ISyntaxTree {
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

        public Option<TrophyType> ToType(INamesRecorder names) => Option.None;

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
            // Type check the assignment value
            if (!this.assign.CheckTypes(types).ToRValue(types).TryGetValue(out var assign)) {
                throw TypeCheckingErrors.RValueRequired(this.assign.Location);
            }

            var assignType = types.GetReturnType(assign);
            var path = types.CurrentScope.Append(this.name);
            var sig = new VariableSignature(path, assignType, this.isWritable);

            // Declare this variable and make sure we're not shadowing another variable
            if (!types.DeclareVariable(sig)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.name);
            }

            // Set the return type of the new syntax tree
            var result = new VarStatement(this.Location, sig, assign);
            types.SetReturnType(result, PrimitiveType.Void);
            //types.SetReturnType(result, new PointerType(assignType, this.isWritable));

            return result;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => Option.None;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(CStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record VarStatement : ISyntaxTree {
        private readonly ISyntaxTree assign;
        private readonly VariableSignature signature;

        public TokenLocation Location { get; }

        public VarStatement(TokenLocation loc, VariableSignature sig, ISyntaxTree assign) {
            this.Location = loc;
            this.signature = sig;
            this.assign = assign;
        }

        public Option<TrophyType> ToType(INamesRecorder names) => Option.None;

        public ISyntaxTree CheckTypes(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(CStatementWriter writer) {
            var type = writer.ConvertType(this.signature.Type);
            var value = this.assign.GenerateCode(writer);
            var name = writer.GetVariableName(this.signature.Path);
            var assign = CStatement.VariableDeclaration(type, name, value);

            writer.WriteSpacingLine();
            writer.WriteStatement(CStatement.Comment("Variable declaration statement"));
            writer.WriteStatement(assign);
            writer.WriteSpacingLine();

            return CExpression.IntLiteral(0);
            //return CExpression.AddressOf(CExpression.VariableLiteral(name));
        }
    }
}