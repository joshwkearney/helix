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
    public class WhileStatement : ISyntaxTree {
        private readonly ISyntaxTree cond, body;

        public TokenLocation Location { get; }

        public WhileStatement(TokenLocation location, ISyntaxTree cond, ISyntaxTree body) {
            this.Location = location;
            this.cond = cond;
            this.body = body;
        }

        public Option<TrophyType> ToType(IdentifierPath scope, TypesRecorder types) {
            return Option.None;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, TypesRecorder types) {
            if (!this.cond.ResolveTypes(scope, types).ToRValue(types).TryGetValue(out var cond)) {
                throw TypeCheckingErrors.RValueRequired(this.cond.Location);
            }

            if (!this.body.ResolveTypes(scope, types).ToRValue(types).TryGetValue(out var body)) {
                throw TypeCheckingErrors.RValueRequired(this.body.Location);
            }

            var condType = types.GetReturnType(cond);
            var bodyType = types.GetReturnType(body);

            // Make sure the condition is a boolean
            if (!TypeUnifier.TryUnifyTo(cond, condType, PrimitiveType.Bool).TryGetValue(out cond)) {
                throw TypeCheckingErrors.UnexpectedType(this.cond.Location, PrimitiveType.Bool, condType);
            }

            var result = new WhileStatement(this.Location, cond, body);
            types.SetReturnType(result, PrimitiveType.Void);

            return result;
        }

        public Option<ISyntaxTree> ToRValue(TypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(TypesRecorder types) => Option.None;

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter writer) {
            var loopBody = new List<CStatement>();
            var bodyWriter = new CStatementWriter(writer, loopBody);
            var cond = CExpression.Not(this.cond.GenerateCode(types, bodyWriter));

            loopBody.Add(CStatement.If(cond, new[] { CStatement.Break() }));
            loopBody.Add(CStatement.NewLine());

            this.body.GenerateCode(types, bodyWriter);

            writer.WriteSpacingLine();
            writer.WriteStatement(CStatement.Comment("While loop"));
            writer.WriteStatement(CStatement.While(CExpression.IntLiteral(1), loopBody));
            writer.WriteSpacingLine();

            return CExpression.IntLiteral(0);
        }
    }
}
