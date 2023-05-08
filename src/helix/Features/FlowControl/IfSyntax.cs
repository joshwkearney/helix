using Helix.Analysis.Types;
using Helix.Analysis;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using System.Reflection;
using Helix.Features.Variables;
using Helix.Analysis.Lifetimes;
using Helix.Features.Memory;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree IfExpression() {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.ThenKeyword);
            var affirm = this.TopExpression();

            if (this.TryAdvance(TokenKind.ElseKeyword)) {
                var neg = this.TopExpression();
                var loc = start.Location.Span(neg.Location);

                return new IfSyntax(loc, cond, affirm, neg);
            }
            else {
                var loc = start.Location.Span(affirm.Location);

                return new IfSyntax(loc, cond, affirm);
            }
        }
    }
}

namespace Helix.Features.FlowControl {
    public record IfSyntax : ISyntaxTree {
        private static int ifTempCounter = 0;

        private readonly ISyntaxTree cond, iftrue, iffalse;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.cond, this.iftrue, this.iffalse };

        public bool IsPure { get; }

        public IfSyntax(
            TokenLocation location, 
            ISyntaxTree cond, 
            ISyntaxTree iftrue) {

            this.Location = location;
            this.cond = cond;

            this.iftrue = new BlockSyntax(iftrue.Location, new ISyntaxTree[] {
                iftrue, new VoidLiteral(iftrue.Location)
            });

            this.iffalse = new VoidLiteral(location);
            this.IsPure = cond.IsPure && iftrue.IsPure;
            this.tempPath = location.Scope.Append("$if_temp_" + ifTempCounter++);
        }

        public IfSyntax(
            TokenLocation location, 
            ISyntaxTree cond, 
            ISyntaxTree iftrue, 
            ISyntaxTree iffalse) : this(location, cond, iftrue) {

            this.Location = location;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
            this.IsPure = cond.IsPure && iftrue.IsPure && iffalse.IsPure;
        }

        public ISyntaxTree CheckTypes(EvalFrame types) {
            if (types.ReturnTypes.ContainsKey(this)) {
                return this;
            }

            var iftrueTypes = new EvalFrame(types);
            var iffalseTypes = new EvalFrame(types);

            var cond = this.cond.CheckTypes(types).ToRValue(types).UnifyTo(PrimitiveType.Bool, types);
            var iftrue = this.iftrue.CheckTypes(iftrueTypes).ToRValue(iftrueTypes);
            var iffalse = this.iffalse.CheckTypes(iffalseTypes).ToRValue(iffalseTypes);

            iftrue = iftrue.UnifyFrom(iffalse, types);
            iffalse = iffalse.UnifyFrom(iftrue, types);

            //var newLifetimes = this.CalculateModifiedVariables(iftrueTypes, iffalseTypes, types);            

            // Make sure to bind all the new lifetimes we have discovered
            //var bindings = newLifetimes
            //    .Select(x => new BindLifetimeSyntax(this.Location, x.Lifetime, x.Path))
            //    .Select(x => (ISyntaxTree)x)
             //   .ToList();

            //var lifetimeBundle = this.CalculateLifetimes(iftrue, iffalse, bindings, types);
            var resultType = types.ReturnTypes[iftrue];

            var result = new IfSyntax(
                this.Location,
                cond,
                iftrue,
                iffalse);

            types.ReturnTypes[result] = resultType;

            return result;
        }

        public ISyntaxTree ToRValue(EvalFrame types) {
            if (types.ReturnTypes.ContainsKey(this)) {
                return this;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (flow.Lifetimes.ContainsKey(this)) {
                return;
            }

            var iftrueFlow = new FlowFrame(flow);
            var iffalseFlow = new FlowFrame(flow);

            this.cond.AnalyzeFlow(flow);
            this.iftrue.AnalyzeFlow(iftrueFlow);
            this.iffalse.AnalyzeFlow(iffalseFlow);

            var lifetimeBundle = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>();
            var resultType = flow.ReturnTypes[iftrue];

            // If we are returning a reference type then we need to calculate a new lifetime
            // This is required because the lifetimes that were used inside of the if body
            // may not be availible outside of it, so we need to reuinify around a new lifetime
            foreach (var (relPath, type) in VariablesHelper.GetMemberPaths(resultType, flow)) {
                var bodyLifetimes = flow.Lifetimes[iftrue].ComponentLifetimes[relPath]
                    .Concat(flow.Lifetimes[iffalse].ComponentLifetimes[relPath])
                    .ToValueList();

                var path = this.tempPath.Append(relPath);
                var resultLifetime = new Lifetime(path, 0);

                lifetimeBundle.Add(relPath, new[] { resultLifetime });

                // Make sure that our new lifetime is derived from the body lifetimes, and that
                // the body lifetimes are precursors to our lifetime for the purposes of lifetime
                // analysis
                foreach (var bodyLifetime in bodyLifetimes) {
                    flow.LifetimeGraph.AddAlias(resultLifetime, bodyLifetime);
                }

                // TODO: Add bindings
                // Add this new lifetime to the list of bindings we calculated earlier
                //if (type.IsRemote(flow)) {
                //bindings.Add(new BindLifetimeSyntax(this.Location, resultLifetime, path));
                //}
            }

            // TODO: Add bindings
            // CalculateModifiedVariables();

            flow.Lifetimes[this] = new LifetimeBundle(lifetimeBundle);
        }

        public ICSyntax GenerateCode(EvalFrame types, ICStatementWriter writer) {
            var affirmList = new List<ICStatement>();
            var negList = new List<ICStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            var affirm = this.iftrue.GenerateCode(types, affirmWriter);
            var neg = this.iffalse.GenerateCode(types, negWriter);

            var tempName = writer.GetVariableName(this.tempPath);

            // Register our member paths with the code generator
            foreach (var relPath in VariablesHelper.GetMemberPaths(this.returnType, types)) {
                writer.SetMemberPath(this.tempPath, relPath);
            }

            if (this.returnType != PrimitiveType.Void) {
                affirmWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = affirm
                });

                negWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = neg
                });
            }

            var tempStat = new CVariableDeclaration() {
                Type = writer.ConvertType(this.returnType),
                Name = tempName
            };

            var expr = new CIf() {
                Condition = this.cond.GenerateCode(types, writer),
                IfTrue = affirmList,
                IfFalse = negList
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.cond.Location.Line}: If statement");

            // Don't bother writing the temp variable if we are returning void
            if (this.returnType != PrimitiveType.Void) {
                writer.WriteStatement(tempStat);
            }

            writer.WriteStatement(expr);

            // Register all the lifetimes that changed within this if statement, including
            // the binding that register our return lifetime
            foreach (var binding in this.bindings) {
                binding.GenerateCode(types, writer);
            }

            writer.WriteEmptyLine();

            if (this.returnType != PrimitiveType.Void) {
                return new CVariableLiteral(tempName);
            }
            else {
                return new CIntLiteral(0);
            }
        }

        private IReadOnlyList<Lifetime> CalculateModifiedVariables(
            FlowFrame ifTrueFlow,
            FlowFrame ifFalseFlow,
            FlowFrame flow) {

            var modifiedVars = ifTrueFlow.Variables
                .Concat(ifFalseFlow.Variables)
                .Select(x => x.Key)
                .Intersect(flow.Variables.Select(x => x.Key));

            var newLifetimes = new List<Lifetime>();

            // For every variable mutated within this if statement, we need to create a new
            // lifetime for that variable after the if statement so that code that runs after
            // doesn't use outdated lifetime information. Also, variables can be changed
            // in different ways so we need to re-unify the branches anyway
            foreach (var path in modifiedVars) {
                var oldSig = flow.Variables[path];

                // If this variable is changed in both paths, take the max mutation count and add one
                //if (ifTrueFlow.Variables.Keys.Contains(path) && ifFalseFlow.Variables.Keys.Contains(path)) {
                    var trueLifetime = ifTrueFlow.VariableLifetimes[path];
                    var falseLifetime = ifFalseFlow.VariableLifetimes[path];

                    var mutationCount = 1 + Math.Max(
                        trueLifetime.MutationCount,
                        falseLifetime.MutationCount);

                    var newLifetime = new Lifetime(path, mutationCount);

                    newLifetimes.Add(newLifetime);
                    flow.VariableLifetimes[path] = newLifetime;

                    // Make sure that the new lifetime is dependent on both if branches
                    flow.LifetimeGraph.AddAlias(newLifetime, trueLifetime);
                    flow.LifetimeGraph.AddAlias(newLifetime, falseLifetime);
               // }
              /*  else {
                    // TODO: Why not just always use the first branch?

                    // If this variable is changed in only one path
                    Lifetime oldLifetime;

                    if (ifTrueFlow.Variables.ContainsKey(path)) {
                        oldLifetime = ifTrueFlow.VariableLifetimes[path];
                    }
                    else {
                        oldLifetime = ifFalseFlow.VariableLifetimes[path];
                    }

                    var newSig = new VariableSignature(
                        path,
                        oldSig.Type,
                        oldSig.IsWritable,
                        oldLifetime.MutationCount + 1);

                    newLifetimes.Add(newSig);

                    // Make sure the new lifetime is dependent on the if branch
                    flow.LifetimeGraph.AddAlias(newSig.Lifetime, oldLifetime);
                    flow.LifetimeGraph.AddAlias(newSig.Lifetime, oldSig.Lifetime);

                    //types.LifetimeGraph.AddPrecursor(newSig.Lifetime, oldLifetime);
                    //types.LifetimeGraph.AddDerived(oldLifetime, newSig.Lifetime);

                    //// Make sure the new lifetime is dependent on the old lifetime
                    //types.LifetimeGraph.AddPrecursor(newSig.Lifetime, oldSig.Lifetime);
                    //types.LifetimeGraph.AddDerived(oldSig.Lifetime, newSig.Lifetime);

                    flow.Variables[path] = newSig;
                }*/
            }

            return newLifetimes;
        }
    }
}