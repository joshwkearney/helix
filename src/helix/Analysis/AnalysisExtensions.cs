using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Features.Aggregates;
using Helix.Parsing;

namespace Helix.Analysis {
    public static partial class AnalysisExtensions {
        public static PointerType AssertIsPointer(this ISyntaxTree syntax, ITypedFrame types) {
            var type = types.ReturnTypes[syntax];

            if (type is not PointerType pointer) {
                throw TypeCheckingErrors.ExpectedVariableType(syntax.Location, type);
            }

            return pointer;
        }

        public static ISyntaxTree WithMutableType(this ISyntaxTree syntax, EvalFrame types) {
            var betterType = types.ReturnTypes[syntax].ToMutableType();

            return syntax.ConvertTo(betterType, types);
        }

        public static ISyntaxTree ConvertTo(this ISyntaxTree fromSyntax, HelixType toType, EvalFrame types) {
            var type = types.ReturnTypes[fromSyntax];

            if (!type.CanConvertTo(toType, types)) {
                throw TypeCheckingErrors.UnexpectedType(fromSyntax.Location, toType, type);
            }

            var result = type.ConvertTo(toType, fromSyntax, types).CheckTypes(types);

            types.ReturnTypes[result] = toType;
            return result;
        }

        public static ISyntaxTree ConvertFrom(this ISyntaxTree fromSyntax, ISyntaxTree otherSyntax, EvalFrame types) {
            var type1 = types.ReturnTypes[fromSyntax];
            var type2 = types.ReturnTypes[otherSyntax];

            if (type1.CanConvertFrom(type2, types)) {
                return fromSyntax.ConvertTo(type1.ConvertFrom(type2, types), types);
            }
            else if (type2.CanConvertFrom(type1, types)) {
                return fromSyntax.ConvertTo(type2.ConvertFrom(type1, types), types);
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(fromSyntax.Location, type1, type2);
            }
        }

        public static bool IsTypeChecked(this ISyntaxTree syntax, ITypedFrame types) {
            return types.ReturnTypes.ContainsKey(syntax);
        }

        public static bool IsFlowAnalyzed(this ISyntaxTree syntax, FlowFrame flow) {
            return flow.Lifetimes.ContainsKey(syntax);
        }

        public static HelixType GetReturnType(this ISyntaxTree syntax, ITypedFrame types) {
            return types.ReturnTypes[syntax];
        }

        public static void SetReturnType(this ISyntaxTree syntax, HelixType type, ITypedFrame types) {
            types.ReturnTypes[syntax] = type;
        }
    }
}
