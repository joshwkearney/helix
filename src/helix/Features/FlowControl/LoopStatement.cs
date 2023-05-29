using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.FlowControl;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Primitives;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();
            var newBlock = new List<ISyntaxTree>();

            var test = new IfSyntax(
                cond.Location,
                new UnaryParseSyntax(cond.Location, UnaryOperatorKind.Not, cond),
                new BreakContinueSyntax(cond.Location, true));

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
            var loop = new LoopStatement(loc, new BlockSyntax(loc, newBlock));

            return loop;
        }
    }
}

namespace Helix.Features.FlowControl {
    public record LoopStatement : ISyntaxTree {
        private static int loopCounter = 0;

        private readonly ISyntaxTree body;
        private readonly string name;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.body };

        public bool IsPure => false;

        public LoopStatement(TokenLocation location, ISyntaxTree body, string name) {
            this.Location = location;
            this.body = body;
            this.name = name;
        }

        public LoopStatement(TokenLocation location, ISyntaxTree body)
            : this(location, body, "$loop" + loopCounter++) { }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (types.ReturnTypes.ContainsKey(this)) {
                return this;
            }

            var bodyTypes = new TypeFrame(types, this.name);
            var body = this.body.CheckTypes(bodyTypes).ToRValue(bodyTypes);
            var result = (ISyntaxTree)new LoopStatement(this.Location, body, this.name);

            result.SetReturnType(PrimitiveType.Void, types);
            result.SetCapturedVariables(body, types);
            result.SetPredicate(types);

            types.MergeFrom(bodyTypes);

            return result;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            var bodyFlow = new FlowFrame(flow);
            this.body.AnalyzeFlow(bodyFlow);

            var modifiedLocalLifetimes = bodyFlow.LocalLifetimes
                .Where(x => !flow.LocalLifetimes.Contains(x))
                .Select(x => x.Key)
                .Where(flow.LocalLifetimes.ContainsKey)
                .ToArray();

            // For every variable that might be modified in the loop, create a new lifetime
            // for it in the loop body so that if it does change, it is only changing the
            // new variable signature and not the old one
            foreach (var path in modifiedLocalLifetimes) {
                var oldBounds = flow.LocalLifetimes[path];
                var newBounds = bodyFlow.LocalLifetimes[path];

                // Add a dependency between the new lifetime and the old lifetime
                // because things outside the loop may depend on things inside
                // the loop, because it's a loop
                flow.DataFlowGraph.AddStored(newBounds.ValueLifetime, oldBounds.ValueLifetime);

                var roots = flow.GetMaximumRoots(newBounds.ValueLifetime);

                // If the new value of this variable depends on a lifetime that was created
                // inside the loop, we need to declare a new root so that nothing after the
                // loop uses code that is no longer in scope
                if (roots.Any(x => !flow.LifetimeRoots.Contains(x))) {
                    var newRoot = new ValueLifetime(
                        oldBounds.ValueLifetime.Path,
                        LifetimeRole.Root,
                        LifetimeOrigin.TempValue,
                        Math.Max(oldBounds.ValueLifetime.Version, newBounds.ValueLifetime.Version));

                    // Add our new root to the list of acceptable roots
                    flow.LifetimeRoots = flow.LifetimeRoots.Add(newRoot);

                    // Replace the current value with our root
                    oldBounds = oldBounds.WithValue(newRoot);
                    flow.LocalLifetimes = flow.LocalLifetimes.SetItem(newRoot.Path, oldBounds);
                }
            }

            this.SetLifetimes(new LifetimeBounds(), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
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
