using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Attempt17.NewSyntax.Features.Primitives;
using Attempt18;
using Attempt18.CodeGeneration;
using Attempt18.Parsing;
using Attempt18.Types;

namespace Attempt17.NewSyntax {
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

namespace Attempt17.NewSyntax.Features.Primitives {
    public class BoolLiteralData : IParsedData, ITypeCheckedData, IFlownData {
        public bool Value { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> EscapingVariables { get; set; }
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

        public static Syntax ResolveTypes(IParsedData data, TypeCache types) {
            var literal = (BoolLiteralData)data;

            // Set return type
            literal.ReturnType = BoolType.Instance;

            return new Syntax() {
                Data = SyntaxData.From(literal),
                Operator = SyntaxOp.FromFlowAnalyzer(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, FlowCache flows) {
            var literal = (BoolLiteralData)data;

            // Set no captured variables
            literal.EscapingVariables = ImmutableHashSet.Create<IdentifierPath>();

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