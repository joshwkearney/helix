using Helix.Generation;
using Helix.Features.FlowControl;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Primitives;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseSyntax WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();
            var newBlock = new List<IParseSyntax>();

            var test = new IfParse(
                cond.Location,
                new UnaryParseSyntax(cond.Location, UnaryOperatorKind.Not, cond),
                new BreakContinueParse(cond.Location, true));

            // False loops will never run and true loops don't need a break test
            if (cond is not Features.Primitives.BoolLiteral) {
                newBlock.Add(test);
            }

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            this.isInLoop.Push(true);
            var body = this.TopExpression();
            this.isInLoop.Pop();

            newBlock.Add(body);

            var loc = start.Location.Span(body.Location);
            var loop = new LoopStatement(loc, BlockParse.FromMany(loc, newBlock));

            return loop;
        }
    }
}

namespace Helix.Features.FlowControl {
    public record LoopStatement : IParseSyntax {
        private static int loopCounter = 0;

        private readonly IParseSyntax body;
        private readonly string name;

        public TokenLocation Location { get; }

        public IEnumerable<IParseSyntax> Children => new[] { this.body };

        public bool IsPure => false;

        public LoopStatement(TokenLocation location, IParseSyntax body, string name) {
            this.Location = location;
            this.body = body;
            this.name = name;
        }

        public LoopStatement(TokenLocation location, IParseSyntax body)
            : this(location, body, "$loop" + loopCounter++) { }

        public IParseSyntax ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public IParseSyntax CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var bodyTypes = new TypeFrame(types, this.name);
            var body = this.body.CheckTypes(bodyTypes).ToRValue(bodyTypes);
            var result = (IParseSyntax)new LoopStatement(this.Location, body, this.name);
            
            types.SyntaxTags[result] = new SyntaxTagBuilder(types)
                .WithChildren(body)
                .Build();

            return result;
        }
        
        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var bodyStats = new List<ICStatement>();
            var bodyWriter = new CStatementWriter(writer, bodyStats);

            this.body.GenerateCode(types, bodyWriter);

            if (bodyStats.Any() && bodyStats.Last().IsEmpty) {
                bodyStats.RemoveAt(bodyStats.Count - 1);
            }

            var stat = new CWhile() {
                Condition = new CIntLiteral(1),
                Body = bodyStats
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: While or for loop");
            writer.WriteStatement(stat);
            writer.WriteEmptyLine();

            return new CIntLiteral(0);
        }
    }
}
