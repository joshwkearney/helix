﻿using Helix.Analysis.Types;
using Helix.Analysis;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using System.Reflection;
using Helix.Features.Variables;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Collections;
using Helix.Analysis.Predicates;

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
        private readonly IdentifierPath path;

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
            this.path = new IdentifierPath("$if" + ifTempCounter++);
        }

        public IfSyntax(TokenLocation location, ISyntaxTree cond, ISyntaxTree iftrue,
                        ISyntaxTree iffalse) 
            : this(location, cond, iftrue, iffalse, new IdentifierPath("$if" + ifTempCounter++)) {}

        public IfSyntax(TokenLocation location, ISyntaxTree cond, ISyntaxTree iftrue,
                        ISyntaxTree iffalse, IdentifierPath path) 
            : this(location, cond, iftrue) {

            this.Location = location;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
            this.path = path;
            this.IsPure = cond.IsPure && iftrue.IsPure && iffalse.IsPure;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (types.ReturnTypes.ContainsKey(this)) {
                return this;
            }

            var name = this.path.Segments.Last();
            var iftrueTypes = new TypeFrame(types, name + "T");
            var iffalseTypes = new TypeFrame(types, name + "F");

            var cond = this.cond.CheckTypes(types).ToRValue(types);
            var condPredicate = ISyntaxPredicate.Empty;

            if (cond.GetReturnType(types) is PredicateBool predBool) {
                condPredicate = predBool.Predicate;
            }

            cond = cond.UnifyTo(PrimitiveType.Bool, types);

            var ifTruePrepend = condPredicate.ApplyToTypes(this.cond.Location, iftrueTypes);
            var ifFalsePrepend = condPredicate.Negate().ApplyToTypes(this.cond.Location, iffalseTypes);

            ISyntaxTree iftrue = new BlockSyntax(this.iftrue.Location, ifTruePrepend.Append(this.iftrue).ToArray());
            ISyntaxTree iffalse = new BlockSyntax(this.iffalse.Location, ifFalsePrepend.Append(this.iffalse).ToArray());

            iftrue = iftrue.CheckTypes(iftrueTypes).ToRValue(iftrueTypes);
            iffalse = iffalse.CheckTypes(iffalseTypes).ToRValue(iffalseTypes);

            iftrue = iftrue.UnifyFrom(iffalse, types);
            iffalse = iffalse.UnifyFrom(iftrue, types);

            var resultType = types.ReturnTypes[iftrue];

            var result = new IfSyntax(
                this.Location,
                cond,
                iftrue,
                iffalse,
                types.Scope.Append(name));

            result.SetReturnType(resultType, types);
            result.SetCapturedVariables(cond, iftrue, iffalse, types);
            result.SetPredicate(iftrue, iffalse, types);

            types.MergeFrom(iftrueTypes, iffalseTypes);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (types.ReturnTypes.ContainsKey(this)) {
                return this;
            }
            else {
                throw new InvalidOperationException();
            }
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (this.IsFlowAnalyzed(flow)) {
                return;
            }

            var iftrueFlow = new FlowFrame(flow);
            var iffalseFlow = new FlowFrame(flow);

            this.cond.AnalyzeFlow(flow);
            this.iftrue.AnalyzeFlow(iftrueFlow);
            this.iffalse.AnalyzeFlow(iffalseFlow);

            var ifTrueBounds = this.iftrue.GetLifetimes(flow);
            var ifFalseBounds = this.iffalse.GetLifetimes(flow);

            MutateLocals(iftrueFlow, iffalseFlow, flow);

            if (this.GetReturnType(flow).IsValueType(flow)) {
                this.SetLifetimes(new LifetimeBounds(), flow);
                return;
            }

            var role = LifetimeRole.Alias;
            var newRoots = false
                || HasNewRoots(ifTrueBounds.ValueLifetime, flow) 
                || HasNewRoots(ifFalseBounds.ValueLifetime, flow);

            if (newRoots) {
                role = LifetimeRole.Root;
            }

            var valueLifetime = new ValueLifetime(
                this.path,  
                role, 
                LifetimeOrigin.TempValue);

            flow.DataFlowGraph.AddAssignment(valueLifetime, ifTrueBounds.ValueLifetime);
            flow.DataFlowGraph.AddAssignment(valueLifetime, ifFalseBounds.ValueLifetime);

            if (newRoots) {
                flow.LifetimeRoots = flow.LifetimeRoots.Add(valueLifetime);
            }

            this.SetLifetimes(new LifetimeBounds(valueLifetime), flow);
        }

        private static bool HasNewRoots(Lifetime lifetime, FlowFrame flow) {
            var roots = flow.GetMaximumRoots(lifetime);

            return roots.Any(x => !flow.LifetimeRoots.Contains(x));
        }

        private static void MutateLocals(
            FlowFrame trueFrame,
            FlowFrame falseFrame,
            FlowFrame flow) {

            var modifiedLocals = trueFrame.LocalLifetimes
                .Concat(falseFrame.LocalLifetimes)
                .Where(x => !flow.LocalLifetimes.Contains(x))
                .Distinct()
                .Select(x => x.Key)
                .Where(flow.LocalLifetimes.ContainsKey)
                .ToArray();

            foreach (var varPath in modifiedLocals) {
                var trueLifetime = trueFrame
                    .LocalLifetimes
                    .GetValueOrNone(varPath)
                    .OrElse(() => new LifetimeBounds())
                    .ValueLifetime;

                var falseLifetime = falseFrame
                    .LocalLifetimes
                    .GetValueOrNone(varPath)
                    .OrElse(() => new LifetimeBounds())
                    .ValueLifetime;

                Lifetime postLifetime;
                if (trueLifetime.Version >= falseLifetime.Version || falseLifetime == Lifetime.None) {
                    postLifetime = trueLifetime.IncrementVersion();
                }
                else {
                    postLifetime = falseLifetime.IncrementVersion();
                }

                flow.DataFlowGraph.AddAssignment(trueLifetime, postLifetime);
                flow.DataFlowGraph.AddAssignment(falseLifetime, postLifetime);

                var roots = flow.GetMaximumRoots(postLifetime);

                // If the new value of this if expression depends on a root that was created
                // inside one of the branches, we need to emit a new root because any code
                // after the if statement can't access our branch's roots
                if (roots.Any(x => !flow.LifetimeRoots.Contains(x))) {
                    var newRoot = new ValueLifetime(
                        postLifetime.Path,
                        LifetimeRole.Root,
                        LifetimeOrigin.TempValue,
                        postLifetime.Version + 1);

                    flow.LifetimeRoots = flow.LifetimeRoots.Add(newRoot);
                    postLifetime = newRoot;
                }

                var newValue = flow.LocalLifetimes[varPath].WithValue(postLifetime);
                flow.LocalLifetimes = flow.LocalLifetimes.SetItem(varPath, newValue);
            }
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            var affirmList = new List<ICStatement>();
            var negList = new List<ICStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            var affirm = this.iftrue.GenerateCode(types, affirmWriter);
            var neg = this.iffalse.GenerateCode(types, negWriter);

            var tempName = writer.GetVariableName();
            var returnType = this.GetReturnType(types);

            if (returnType != PrimitiveType.Void) {
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
                Type = writer.ConvertType(returnType),
                Name = tempName
            };

            if (affirmList.Any() && affirmList.Last().IsEmpty) {
                affirmList.RemoveAt(affirmList.Count - 1);
            }

            if (negList.Any() && negList.Last().IsEmpty) {
                negList.RemoveAt(negList.Count - 1);
            }

            var expr = new CIf() {
                Condition = this.cond.GenerateCode(types, writer),
                IfTrue = affirmList,
                IfFalse = negList
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.cond.Location.Line}: If statement");

            // Don't bother writing the temp variable if we are returning void
            if (returnType != PrimitiveType.Void) {
                writer.WriteStatement(tempStat);
            }

            writer.WriteStatement(expr);
            writer.WriteEmptyLine();

            if (returnType != PrimitiveType.Void) {
                return new CVariableLiteral(tempName);
            }
            else {
                return new CIntLiteral(0);
            }
        }
    }
}