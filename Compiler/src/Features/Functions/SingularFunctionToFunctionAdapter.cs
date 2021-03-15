using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;

namespace Trophy.Features.Functions {
    public class SingularFunctionToFunctionAdapter : ISyntaxC {
        private static int counter = 0;

        private readonly ISyntaxC target;
        private readonly IdentifierPath funcPath;

        public ITrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.target.Lifetimes;

        public SingularFunctionToFunctionAdapter(ISyntaxC target, IdentifierPath funcPath, ITrophyType returnType) {
            this.target = target;
            this.funcPath = funcPath;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            var ctype = writer.ConvertType(this.ReturnType);
            var cname = "func_conv_temp" + counter++;
            var cClosure = CExpression.VariableLiteral(cname);

            // Calculate the most restrictive region to allocate on
            var region = this.Lifetimes.Aggregate(IdentifierPath.HeapPath, (x, y) => x.Outlives(y) ? y : x);

            // The stack is not a region
            if (region == IdentifierPath.StackPath) {
                region = IdentifierPath.HeapPath;
            }

            // Write the closure variable
            statWriter.WriteStatement(CStatement.Comment("Singular function to function conversion"));
            statWriter.WriteStatement(CStatement.VariableDeclaration(ctype, cname));

            // Write the environment
            statWriter.WriteStatement(
                CStatement.Assignment(
                    CExpression.MemberAccess(cClosure, "environment"), 
                    CExpression.VariableLiteral(region.Segments.Last())));

            // Write the function pointer
            statWriter.WriteStatement(
                CStatement.Assignment(
                    CExpression.MemberAccess(cClosure, "function"),
                    CExpression.AddressOf(CExpression.VariableLiteral("$" + this.funcPath.ToString()))));

            return cClosure;
        }
    }
}