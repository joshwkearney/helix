using System.Collections.Immutable;
using Attempt19.CodeGeneration;
using Attempt19.Parsing;
using Attempt19.Types;
using Attempt19.Features.Primitives;
using Attempt19.TypeChecking;

namespace Attempt19 {
    public static partial class SyntaxFactory {
        public static Syntax MakeIntLiteral(int value, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new IntLiteralData() {
                    Value = value,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(IntLiteralTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Primitives {
    public class IntLiteralData : IParsedData, ITypeCheckedData, IFlownData {
        public int Value { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<VariableCapture> EscapingVariables { get; set; }
    }    

    public static class IntLiteralTransformations {
        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope,
                NameCache names) {

            var literal = (IntLiteralData)data;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var literal = (IntLiteralData)data;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var literal = (IntLiteralData)data;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var literal = (IntLiteralData)data;

            // Set return type
            literal.ReturnType = IntType.Instance;

            // Set no captured variables
            literal.EscapingVariables = ImmutableHashSet.Create<VariableCapture>();

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromFlowAnalyzer(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, TypeCache types, FlowCache flows) {
            var literal = (IntLiteralData)data;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(IFlownData data, ICScope scope, ICodeGenerator gen) {
            var literal = (IntLiteralData)data;

            return new CBlock(literal.Value.ToString());
        }
    }
}