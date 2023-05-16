using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;

namespace Helix.Analysis {
    public static partial class TypeCheckingExtensions {
        public static PointerType AssertIsPointer(this ISyntaxTree syntax, ITypedFrame types) {
            var type = types.ReturnTypes[syntax];

            if (type is not PointerType pointer) {
                throw TypeException.ExpectedVariableType(syntax.Location, type);
            }

            return pointer;
        }

        public static ISyntaxTree WithMutableType(this ISyntaxTree syntax, TypeFrame types) {
            var betterType = types.ReturnTypes[syntax].ToMutableType();

            return syntax.ConvertTypeTo(betterType, types);
        }

        public static ISyntaxTree ConvertTypeTo(this ISyntaxTree fromSyntax, HelixType toType, TypeFrame types) {
            var type = types.ReturnTypes[fromSyntax];

            if (!type.CanConvertTo(toType, types)) {
                throw TypeException.UnexpectedType(fromSyntax.Location, toType, type);
            }

            var result = type.ConvertTo(toType, fromSyntax, types).CheckTypes(types);

            types.ReturnTypes[result] = toType;
            return result;
        }

        public static ISyntaxTree ConvertTypeFrom(this ISyntaxTree fromSyntax, ISyntaxTree otherSyntax, TypeFrame types) {
            var type1 = types.ReturnTypes[fromSyntax];
            var type2 = types.ReturnTypes[otherSyntax];

            if (type1.CanConvertFrom(type2, types)) {
                return fromSyntax.ConvertTypeTo(type1.ConvertFrom(type2, types), types);
            }
            else if (type2.CanConvertFrom(type1, types)) {
                return fromSyntax.ConvertTypeTo(type2.ConvertFrom(type1, types), types);
            }
            else {
                throw TypeException.UnexpectedType(fromSyntax.Location, type1, type2);
            }
        }
    }
}
