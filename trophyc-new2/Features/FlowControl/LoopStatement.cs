using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.FlowControl;
using Trophy.Parsing;
using Trophy.Generation.Syntax;
using Trophy.Features.Primitives;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree WhileStatement(BlockBuilder block) {
            var newBlock = new BlockBuilder();
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression(newBlock);

            var branch = new BlockSyntax(cond.Location, new[] {
                new BreakContinueSyntax(cond.Location, true)
            });

            var test = new IfParseSyntax(
                cond.Location,
                block.GetTempName(),
                new UnaryParseSyntax(cond.Location, UnaryOperatorKind.Not, cond),
                branch);

            newBlock.Statements.Add(test);

            this.Advance(TokenKind.DoKeyword);

            var body = this.TopExpression(newBlock);
            var loc = start.Location.Span(body.Location);
            var loop = new LoopStatement(loc, new BlockSyntax(loc, newBlock.Statements));

            block.Statements.Add(loop);

            return new VoidLiteral(loc);
        }
    }
}

namespace Trophy.Features.FlowControl {
    public record LoopStatement : ISyntaxTree {
        private static int counter = 0;

        private readonly ISyntaxTree body;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.body };

        public LoopStatement(TokenLocation location, 
                             ISyntaxTree body, bool isTypeChecked = false) {

            this.Location = location;
            this.body = body;
            this.isTypeChecked = isTypeChecked;
        }

        public void RewriteNonlocalFlow(SyntaxFrame types, FlowRewriter flow) {
            //int state = flow.NextState++;

            //int nonlocalAffirm = 0;
            //var affirmBlock = new List<ISyntaxTree>();

            //for (; nonlocalAffirm < this.iftrue.Statements.Count; nonlocalAffirm++) {
            //    var stat = this.iftrue.Statements[nonlocalAffirm];

            //    if (stat.HasNonlocalFlow()) {
            //        break;
            //    }
            //    else {
            //        affirmBlock.Add(stat);
            //    }
            //}

            //for (; nonlocalAffirm < this.iftrue.Statements.Count; nonlocalAffirm++) {
            //    var stat = this.iftrue.Statements[nonlocalAffirm];


            //}

        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var wasInLoop = types.InLoop;
            types.InLoop = true;

            var body = this.body.CheckTypes(types).ToRValue(types);
            var result = new LoopStatement(this.Location, body, true);

            types.InLoop = wasInLoop;
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

            this.body.GenerateCode(bodyWriter);

            var loop = new CWhile() {
                Condition = new CIntLiteral(1),
                Body = loopBody
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: While loop");
            writer.WriteStatement(loop);
            writer.WriteEmptyLine();

            return new CIntLiteral(0);
        }
    }
}
