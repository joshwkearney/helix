using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Functions;
using Trophy.Parsing;

namespace Trophy.Features.FlowControl {
    public class ReturnSyntaxA : ISyntaxA {
        private readonly ISyntaxA arg;

        public TokenLocation Location { get; }

        public ReturnSyntaxA(TokenLocation location, ISyntaxA arg) {
            this.Location = location;
            this.arg = arg;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            var result = this.arg.CheckNames(names);
            var region = names.CurrentRegion;

            return new ReturnSyntaxB(this.Location, region, result);
        }
    }

    public class ReturnSyntaxB : ISyntaxB {
        private readonly ISyntaxB result;
        private readonly IdentifierPath region;

        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage => this.result.VariableUsage;

        public ReturnSyntaxB(TokenLocation location, IdentifierPath region, ISyntaxB result) {
            this.Location = location;
            this.region = region;
            this.result = result;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var result = this.result.CheckTypes(types);

            // Make sure we're inside of a function (and not a lambda)
            if (!types.PopContainingFunction().SelectMany(x => x.AsFunctionDeclaration()).TryGetValue(out var sig)) {
                throw TypeCheckingErrors.EarlyReturnInLambda(this.Location);
            }

            // Make sure the result can unify to the return type
            if (types.TryUnifyTo(result, sig.ReturnType).TryGetValue(out var newResult)) {
                result = newResult;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(this.result.Location, sig.ReturnType, result.ReturnType);
            }

            // Make sure the result has the correct lifetime
            FunctionsHelper.CheckForInvalidReturnScope(this.result.Location, RegionsHelper.GetClosestHeap(this.region), result);

            return new ReturnSyntaxC(this.region, result, sig.ReturnType.IsVoidType);
        }
    }

    public class ReturnSyntaxC : ISyntaxC {
        private readonly ISyntaxC result;
        private readonly IdentifierPath region;
        private readonly bool returnVoid;

        public ITrophyType ReturnType => ITrophyType.Void;

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.result.Lifetimes;

        public ReturnSyntaxC(IdentifierPath region, ISyntaxC result, bool returnVoid) {
            this.region = region;
            this.result = result;
            this.returnVoid = returnVoid;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            var result = this.result.GenerateCode(writer, statWriter);

            // Clean up every region between us and the stack
            var reg = this.region;

            while (!RegionsHelper.IsStack(reg)) {
                var stat = CStatement.FromExpression(
                    CExpression.Invoke(
                        CExpression.VariableLiteral("region_delete"),
                        new[] { CExpression.VariableLiteral(reg.ToString()) }));

                statWriter.WriteStatement(stat);

                reg = reg.Pop();
            }

            if (this.returnVoid) {
                statWriter.WriteStatement(CStatement.Return());
            }
            else {
                statWriter.WriteStatement(CStatement.Return(result));
            }

            return CExpression.IntLiteral(0);
        }
    }
}