using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
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

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var cond = this.cond.CheckTypes(types).ToRValue(types).UnifyTo(PrimitiveType.Bool, types);
            var body = this.body.CheckTypes(types).ToRValue(types);

            var result = new WhileStatement(this.Location, cond, body, true);
            types.ReturnTypes[result] = PrimitiveType.Void;

            return result;
        }

        public Option<ISyntaxTree> ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

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

            this.body.GenerateCode(bodyWriter);

            if (loopBody.Any()) {
                loopBody.Insert(0, new CEmptyLine());
                loopBody.Insert(0, terminator);
            }
            else {
                bodyWriter.WriteStatement(terminator);
            }

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
