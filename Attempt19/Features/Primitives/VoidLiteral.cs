using System.Collections.Immutable;
using Attempt19.CodeGeneration;
using Attempt19.Parsing;
using Attempt19.Types;
using Attempt19.Features.Primitives;
using Attempt19.TypeChecking;

namespace Attempt19 {
    public static partial class SyntaxFactory {
        public static Syntax MakeVoidLiteral(TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new VoidLiteralData() { Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(VoidLiteralTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Primitives {
    public class VoidLiteralData : IParsedData, ITypeCheckedData, IFlownData {
        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<VariableCapture> EscapingVariables { get; set; }
    }    

    public static class VoidLiteralTransformations {
        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope,
                NameCache names) {

            var literal = (VoidLiteralData)data;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var literal = (VoidLiteralData)data;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var literal = (VoidLiteralData)data;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var literal = (VoidLiteralData)data;

            // Set return type
            literal.ReturnType = VoidType.Instance;

            // Set no captured variables
            literal.EscapingVariables = ImmutableHashSet.Create<VariableCapture>();

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromFlowAnalyzer(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, TypeCache types, FlowCache flows) {
            var literal = (VoidLiteralData)data;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(IFlownData data, ICScope scope, ICodeGenerator gen) {
            return new CBlock("0");
        }
    }
}