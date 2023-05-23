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
        private readonly ISyntaxTree body;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.body };

        public bool IsPure => false;

        public LoopStatement(TokenLocation location, ISyntaxTree body) {

            this.Location = location;
            this.body = body;
        }

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

            var bodyTypes = new TypeFrame(types);
            var body = this.body.CheckTypes(bodyTypes).ToRValue(bodyTypes);
            var result = (ISyntaxTree)new LoopStatement(this.Location, body);

            result.SetReturnType(PrimitiveType.Void, types);
            result.SetCapturedVariables(body, types);

            return result;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            var modifiedVars = this.body.GetCapturedVariables(flow)
                .Where(x => x.Kind == VariableCaptureKind.LocationCapture)
                .Select(x => x.VariablePath)
                .Where(x => flow.LocalLifetimes.ContainsKey(x.ToVariablePath()))
                .ToArray();

            var modifiedVarMems = modifiedVars
                .SelectMany(path => flow.Variables[path].Type
                    .GetMembers(flow)
                    .Where(x => !x.Value.IsValueType(flow))
                    .Select(x => path.AppendMember(x.Key)))
                .ToArray();

            // For every variable that might be modified in the loop, create a new lifetime
            // for it in the loop body so that if it does change, it is only changing the
            // new variable signature and not the old one
            foreach (var memPath in modifiedVarMems) {
                var bounds = flow.LocalLifetimes[memPath];
                var newValueLifetime = new ValueLifetime(memPath, LifetimeRole.Root, LifetimeOrigin.TempValue);

                // Make sure the old value depends on the new root we just created
                // This is to ensure inference works correctly for things above the loop
                flow.LifetimeGraph.AddStored(bounds.ValueLifetime, newValueLifetime, null);

                // Make sure our new value outlives its location
                flow.LifetimeGraph.AddStored(newValueLifetime, bounds.LocationLifetime, null);

                // Replace the variable's value with our new root. This is because
                // this variable might be modified in the loop, so anything accessing
                // it from this point forward must assume we don't know where the value
                // came from.
                bounds = bounds.WithValue(newValueLifetime);
                flow.LocalLifetimes = flow.LocalLifetimes.SetItem(memPath, bounds);

                // Add this root to the root set
                flow.LifetimeRoots = flow.LifetimeRoots.Add(newValueLifetime);
            }

            var bodyFlow = new FlowFrame(flow);
            this.body.AnalyzeFlow(bodyFlow);

            MutateLocals(bodyFlow, flow);
            this.SetLifetimes(new LifetimeBundle(), flow);
        }

        private static void MutateLocals(
            FlowFrame bodyFrame,
            FlowFrame flow) {

            var modifiedLocals = bodyFrame.LocalLifetimes
                .Where(x => !flow.LocalLifetimes.Contains(x))
                .Distinct()
                .Select(x => x.Key)
                .Where(flow.LocalLifetimes.ContainsKey)
                .ToArray();

            foreach (var varPath in modifiedLocals) {
                var trueLifetime = bodyFrame.LocalLifetimes[varPath].ValueLifetime;

                var postLifetime = trueLifetime.IncrementVersion();
                flow.LifetimeGraph.AddAssignment(trueLifetime, postLifetime, null);

                var newValue = flow.LocalLifetimes[varPath].WithValue(postLifetime);
                flow.LocalLifetimes = flow.LocalLifetimes.SetItem(varPath, newValue);
            }
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
