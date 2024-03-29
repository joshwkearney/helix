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
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var cond = this.cond.CheckTypes(types).ToRValue(types);
            var condPredicate = ISyntaxPredicate.Empty;

            if (cond.GetReturnType(types) is PredicateBool predBool) {
                condPredicate = predBool.Predicate;
            }

            cond = cond.UnifyTo(PrimitiveType.Bool, types);

            var name = this.path.Segments.Last();
            var iftrueTypes = new TypeFrame(types, name + "T");
            var iffalseTypes = new TypeFrame(types, name + "F");

            var ifTruePrepend = condPredicate.ApplyToTypes(this.cond.Location, iftrueTypes);
            var ifFalsePrepend = condPredicate.Negate().ApplyToTypes(this.cond.Location, iffalseTypes);

            ISyntaxTree iftrue = new BlockSyntax(this.iftrue.Location, ifTruePrepend.Append(this.iftrue).ToArray());
            ISyntaxTree iffalse = new BlockSyntax(this.iffalse.Location, ifFalsePrepend.Append(this.iffalse).ToArray());

            iftrue = iftrue.CheckTypes(iftrueTypes).ToRValue(iftrueTypes);
            iffalse = iffalse.CheckTypes(iffalseTypes).ToRValue(iffalseTypes);

            iftrue = iftrue.UnifyFrom(iffalse, types);
            iffalse = iffalse.UnifyFrom(iftrue, types);

            // Update any variables modified in either branch
            MutateLocals(iftrueTypes, iffalseTypes, types);

            var resultType = iftrue.GetReturnType(types);

            var result = new IfSyntax(
                this.Location,
                cond,
                iftrue,
                iffalse,
                types.Scope.Append(name));

            SyntaxTagBuilder.AtFrame(types)
                .WithChildren(cond, iftrue, iffalse)
                .WithReturnType(resultType)
                .WithLifetimes(AnalyzeFlow(this.path, iftrue, iffalse, types))
                .BuildFor(result);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            return this;
        }

        private static LifetimeBounds AnalyzeFlow(IdentifierPath path, ISyntaxTree iftrue, 
                                                  ISyntaxTree iffalse, TypeFrame flow) {
            var ifTrueBounds = iftrue.GetLifetimes(flow);
            var ifFalseBounds = iffalse.GetLifetimes(flow);

            if (iftrue.GetReturnType(flow).IsValueType(flow) || iffalse.GetReturnType(flow).IsValueType(flow)) {
                return new LifetimeBounds();
            }

            var role = LifetimeRole.Alias;
            var newRoots = false
                || HasNewRoots(ifTrueBounds.ValueLifetime, flow) 
                || HasNewRoots(ifFalseBounds.ValueLifetime, flow);

            if (newRoots) {
                role = LifetimeRole.Root;
            }

            var valueLifetime = new ValueLifetime(
                path,  
                role, 
                LifetimeOrigin.TempValue);

            flow.DataFlowGraph.AddAssignment(
                valueLifetime, 
                ifTrueBounds.ValueLifetime, 
                iftrue.GetReturnType(flow));

            flow.DataFlowGraph.AddAssignment(
                valueLifetime, 
                ifFalseBounds.ValueLifetime,
                iffalse.GetReturnType(flow));

            if (newRoots) {
                flow.ValidRoots = flow.ValidRoots.Add(valueLifetime);
            }

            return new LifetimeBounds(valueLifetime);
        }

        private static bool HasNewRoots(Lifetime lifetime, TypeFrame flow) {
            var roots = flow.GetMaximumRoots(lifetime);

            return roots.Any(x => !flow.ValidRoots.Contains(x));
        }

        private static void MutateLocals(
            TypeFrame trueFrame,
            TypeFrame falseFrame,
            TypeFrame flow) {

            var modifiedLocals = trueFrame.Locals
                .Concat(falseFrame.Locals)
                .Where(x => !flow.Locals.Contains(x))
                .Select(x => x.Key)
                .Where(flow.Locals.ContainsKey)
                .ToArray();

            foreach (var varPath in modifiedLocals) {
                var parentType = flow.Locals[varPath].Type;

                var trueLocal = trueFrame
                    .Locals
                    .GetValueOrNone(varPath)
                    .OrElse(() => new LocalInfo(parentType));

                var falseLocal = falseFrame
                    .Locals
                    .GetValueOrNone(varPath)
                    .OrElse(() => new LocalInfo(parentType));

                var trueLifetime = trueLocal.Bounds.ValueLifetime;
                var falseLifetime = falseLocal.Bounds.ValueLifetime;

                Lifetime postLifetime;
                if (trueLifetime.Version >= falseLifetime.Version || falseLifetime == Lifetime.None) {
                    postLifetime = trueLifetime.IncrementVersion();
                }
                else {
                    postLifetime = falseLifetime.IncrementVersion();
                }

                flow.DataFlowGraph.AddAssignment(trueLifetime, postLifetime, trueLocal.Type);
                flow.DataFlowGraph.AddAssignment(falseLifetime, postLifetime, falseLocal.Type);

                var roots = flow.GetMaximumRoots(postLifetime);

                // If the new value of this variable depends on a lifetime that was created
                // inside the loop, we need to declare a new root so that nothing after the
                // loop uses code that is no longer in scope
                if (roots.Any(x => !flow.ValidRoots.Contains(x))) {
                    postLifetime = new ValueLifetime(
                        postLifetime.Path,
                        LifetimeRole.Root,
                        LifetimeOrigin.TempValue,
                        postLifetime.Version + 1);
                }

                var newLocal = flow.Locals[varPath]
                    .WithBounds(new LifetimeBounds(postLifetime))
                    .WithType(parentType.GetMutationSupertype(flow));                

                flow.Locals = flow.Locals.SetItem(varPath, newLocal);
            }
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
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
                Type = writer.ConvertType(returnType, types),
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