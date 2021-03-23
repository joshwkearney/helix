using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Functions;
using Trophy.Parsing;

namespace Trophy.Features.FlowControl {
    public class AsyncSyntaxA : ISyntaxA {
        private readonly ISyntaxA body;

        public TokenLocation Location { get; }

        public AsyncSyntaxA(TokenLocation location, ISyntaxA body) {
            this.Location = location;
            this.body = body;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var heap = RegionsHelper.GetClosestHeap(names.Context.Region);
            var newRegion = names.Context.Region.Append("$async_region" + names.GetNewVariableId());

            var context = names.Context.WithRegion(_ => newRegion);
            var lambda = names.WithContext(context, names => {
                var syntaxA = new LambdaSyntaxA(this.Location, this.body, new ParseFunctionParameter[0]);
                var syntaxB = (LambdaSyntaxB)syntaxA.CheckNames(names);

                return syntaxB;
            });

            return new AsyncSyntaxB(this.Location, heap, lambda, newRegion);
        }
    }

    public class AsyncSyntaxB : ISyntaxB {
        private readonly LambdaSyntaxB lambdaSyntax;
        private readonly IdentifierPath enclosingHeap;
        private readonly IdentifierPath asyncRegion;

        public TokenLocation Location { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.lambdaSyntax.VariableUsage;
        }

        public AsyncSyntaxB(TokenLocation location, IdentifierPath enclosingHeap, LambdaSyntaxB lambdaSyntax, IdentifierPath asyncRegion) {
            this.enclosingHeap = enclosingHeap;
            this.lambdaSyntax = lambdaSyntax;
            this.asyncRegion = asyncRegion;
            this.Location = location;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var lambda = (LambdaSyntaxC)this.lambdaSyntax.CheckTypes(types);

            lambda = new LambdaSyntaxC(
                lambda.Signature,
                lambda.ReturnType,
                lambda.FunctionPath,
                lambda.EnclosingRegion,
                new AsyncPrefixSyntaxC(lambda.Body, this.asyncRegion.Segments.Last()),
                lambda.FreeVariables,
                lambda.ParameterIds,
                lambda.FreeRegions.Where(x => x != this.asyncRegion).ToArray());

            return new AsyncSyntaxC(lambda, this.enclosingHeap, this.asyncRegion);
        }
    }

    public class AsyncPrefixSyntaxC : ISyntaxC {
        private static int counter = 0;

        private readonly ISyntaxC body;
        private readonly string asyncRegionName;

        public ITrophyType ReturnType => this.body.ReturnType;

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.body.Lifetimes;

        public AsyncPrefixSyntaxC(ISyntaxC body, string asyncRegionName) {
            this.body = body;
            this.asyncRegionName = asyncRegionName;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            var regionType = CType.NamedType("Region*");
            var bufferName = "async_jump_buffer_" + counter++;

            // Write the region
            statWriter.WriteStatement(CStatement.Comment("Create new region"));
            statWriter.WriteStatement(CStatement.VariableDeclaration(
                regionType,
                this.asyncRegionName,
                CExpression.IntLiteral(0)));

            statWriter.WriteStatement(CStatement.VariableDeclaration(CType.NamedType("jmp_buf"), bufferName));

            var cond = CExpression.VariableLiteral(bufferName);
            cond = CExpression.Invoke(CExpression.VariableLiteral("setjmp"), new[] { cond });
            cond = CExpression.BinaryExpression(CExpression.IntLiteral(0), cond, Trophy.Features.Primitives.BinaryOperation.NotEqualTo);
            cond = CExpression.Invoke(CExpression.VariableLiteral("HEDLEY_UNLIKELY"), new[] { cond });

            var cleanup = CStatement.FromExpression(
                CExpression.Invoke(
                    CExpression.VariableLiteral("region_delete"),
                    new[] { CExpression.VariableLiteral(this.asyncRegionName) }));

            var ret = CStatement.Return();

            var ifStatement = CStatement.If(cond, new[] { cleanup, ret });

            statWriter.WriteStatement(ifStatement);
            statWriter.WriteStatement(CStatement.NewLine());

            var newRegion = CExpression.Invoke(CExpression.VariableLiteral("region_create"), new[] {
                CExpression.AddressOf(CExpression.VariableLiteral(bufferName))
            });

            var assign = CStatement.Assignment(CExpression.VariableLiteral(this.asyncRegionName), newRegion);
            statWriter.WriteStatement(assign);
            statWriter.WriteStatement(CStatement.NewLine());

            // Write the body
            var body = this.body.GenerateCode(writer, statWriter);

            // Delete the region
            statWriter.WriteStatement(cleanup);

            return body;
        }
    }

    public class AsyncSyntaxC : ISyntaxC {
        private readonly ISyntaxC lambdaSyntax;
        private readonly IdentifierPath enclosingHeap;
        private readonly IdentifierPath asyncRegion;

        public ITrophyType ReturnType => ITrophyType.Void;

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public AsyncSyntaxC(ISyntaxC lambdaSyntax, IdentifierPath enclosingHeap, IdentifierPath asyncHeap) {
            this.lambdaSyntax = lambdaSyntax;
            this.enclosingHeap = enclosingHeap;
            this.asyncRegion = asyncHeap;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            statWriter.WriteStatement(
                CStatement.VariableDeclaration(
                    CType.NamedType("Region*"), 
                    this.asyncRegion.Segments.Last(), 
                    CExpression.VariableLiteral(this.enclosingHeap.Segments.Last())));

            var lambda = this.lambdaSyntax.GenerateCode(writer, statWriter);
            var call = CExpression.Invoke(CExpression.VariableLiteral("region_async"), new[] {
                CExpression.VariableLiteral(this.enclosingHeap.Segments.Last()),
                CExpression.MemberAccess(lambda, "function"),
                CExpression.MemberAccess(lambda, "environment")
            });

            statWriter.WriteStatement(CStatement.FromExpression(call));
            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.IntLiteral(0);
        }
    }
}