using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Parsing {
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

            return new VarStatement(loc, name, assign, isWritable);
        }
    }
}

namespace Trophy {
    public class VarStatement : ISyntaxTree {
        private readonly string name;
        private readonly ISyntaxTree assign;
        private readonly bool isWritable;

        public TokenLocation Location { get; }

        public VarStatement(TokenLocation loc, string name, ISyntaxTree assign, bool isWritable) {
            this.Location = loc;
            this.name = name;
            this.assign = assign;
            this.isWritable = isWritable;
        }

        public Option<TrophyType> ToType(INamesObserver names) => Option.None;

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
            // Type check the assignment value
            if (!this.assign.CheckTypes(types).ToRValue(types).TryGetValue(out var assign)) {
                throw TypeCheckingErrors.RValueRequired(this.assign.Location);
            }

            var assignType = types.GetReturnType(assign);

            // Declare this variable and make sure we're not shadowing another variable
            if (!types.DeclareVariable(this.name, assignType, this.isWritable)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.name);
            }

            // Set the return type of the new syntax tree
            var result = new VarStatement(this.Location, this.name, assign, this.isWritable);
            types.SetReturnType(result, PrimitiveType.Void);
            //types.SetReturnType(result, new PointerType(assignType, this.isWritable));

            return result;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(CStatementWriter writer) {
            var path = writer.TryFindPath(this.name).GetValue();
            var type = writer.ConvertType(writer.GetReturnType(this.assign));
            var value = this.assign.GenerateCode(writer);
            var name = writer.GetVariableName(path);
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