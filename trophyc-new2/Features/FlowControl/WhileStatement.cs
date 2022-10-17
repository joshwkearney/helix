using Trophy.Analysis;
using Trophy.Analysis.Unification;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.DoKeyword);
            var body = this.TopExpression();
            var loc = start.Location.Span(body.Location);

            return new WhileStatement(loc, cond, body);
        }
    }
}

namespace Trophy.Features.FlowControl {
    public record WhileStatement : ISyntaxTree {
        private readonly ISyntaxTree cond, body;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public WhileStatement(TokenLocation location, ISyntaxTree cond, 
                              ISyntaxTree body, bool isTypeChecked = false) {
            this.Location = location;
            this.cond = cond;
            this.body = body;
            this.isTypeChecked = isTypeChecked;
        }

        public Option<TrophyType> ToType(INamesRecorder names) => Option.None;

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
            if (!this.cond.CheckTypes(types).ToRValue(types).TryGetValue(out var cond)) {
                throw TypeCheckingErrors.RValueRequired(this.cond.Location);
            }

            if (!this.body.CheckTypes(types).ToRValue(types).TryGetValue(out var body)) {
                throw TypeCheckingErrors.RValueRequired(this.body.Location);
            }

            var condType = types.GetReturnType(cond);
            var bodyType = types.GetReturnType(body);

            // Make sure the condition is a boolean
            if (!types.TryUnifyTo(cond, condType, PrimitiveType.Bool).TryGetValue(out cond)) {
                throw TypeCheckingErrors.UnexpectedType(this.cond.Location, PrimitiveType.Bool, condType);
            }

            var result = new WhileStatement(this.Location, cond, body, true);
            types.SetReturnType(result, PrimitiveType.Void);

            return result;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) {
            return this.isTypeChecked ? this : Option.None;
        }

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(CStatementWriter writer) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            var loopBody = new List<CStatement>();
            var bodyWriter = new CStatementWriter(loopBody);
            var cond = CExpression.Not(this.cond.GenerateCode(bodyWriter));

            bodyWriter.WriteStatement(CStatement.If(cond, new[] { CStatement.Break() }));
            bodyWriter.WriteSpacingLine();

            this.body.GenerateCode(bodyWriter);

            writer.WriteSpacingLine();
            writer.WriteStatement(CStatement.Comment("While loop"));
            writer.WriteStatement(CStatement.While(CExpression.IntLiteral(1), loopBody));
            writer.WriteSpacingLine();

            return CExpression.IntLiteral(0);
        }
    }
}
