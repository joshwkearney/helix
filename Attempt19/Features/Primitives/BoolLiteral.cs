using System.Collections.Immutable;
using Attempt19.CodeGeneration;
using Attempt19.Features.Primitives;
using Attempt19.Parsing;
using Attempt19.TypeChecking;
using Attempt19.Types;

namespace Attempt19 {
    public static partial class SyntaxFactory {
        public static Syntax MakeBoolLiteral(bool value, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new BoolLiteralData() {
                    Value = value,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(BoolLiteralTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Primitives {
    public class BoolLiteralData : IParsedData, ITypeCheckedData, IFlownData {
        public bool Value { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<VariableCapture> EscapingVariables { get; set; }
    }    

    public static class BoolLiteralTransformations {
        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope,
                NameCache names) {

            var literal = (BoolLiteralData)data;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var literal = (BoolLiteralData)data;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var literal = (BoolLiteralData)data;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var literal = (BoolLiteralData)data;

            // Set return type
            literal.ReturnType = BoolType.Instance;

            // Set no captured variables
            literal.EscapingVariables = ImmutableHashSet.Create<VariableCapture>();

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromFlowAnalyzer(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, TypeCache cache, FlowCache flows) {
            var literal = (BoolLiteralData)data;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(IFlownData data, ICScope scope, ICodeGenerator gen) {
            var literal = (BoolLiteralData)data;

            return new CBlock(literal.Value ? "1" : "0");
        }
    }
}