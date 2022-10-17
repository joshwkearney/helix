using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Analysis.Unification;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

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

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            var loopBody = new List<ICStatement>();
            var bodyWriter = new CStatementWriter(writer, loopBody);

            var terminator = new CIf() {
                Condition = new CNot() {
                    Target = this.cond.GenerateCode(writer)
                },
                IfTrue = new[] { new CBreak() }
            };

            bodyWriter.WriteStatement(terminator);
            bodyWriter.WriteEmptyLine();

            this.body.GenerateCode(writer);

            var loop = new CWhile() {
                Condition = new CIntLiteral(1),
                Body = loopBody
            };

            writer.WriteEmptyLine();
            writer.WriteStatement(new CComment("While loop"));
            writer.WriteStatement(loop);
            writer.WriteEmptyLine();

            return new CIntLiteral(0);
        }
    }
}
