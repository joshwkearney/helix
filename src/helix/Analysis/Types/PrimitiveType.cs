using Helix.Syntax;
using Helix.Features.Primitives;
using Helix.Parsing;

namespace Helix.Analysis.Types {
    public record PrimitiveType : HelixType {
        private readonly PrimitiveTypeKind kind;

        public static PrimitiveType Int { get; } = new PrimitiveType(PrimitiveTypeKind.Int);

        public static PrimitiveType Bool { get; } = new PrimitiveType(PrimitiveTypeKind.Bool);

        public static PrimitiveType Float { get; } = new PrimitiveType(PrimitiveTypeKind.Float);

        public static PrimitiveType Void { get; } = new PrimitiveType(PrimitiveTypeKind.Void);

        private PrimitiveType(PrimitiveTypeKind kind) {
            this.kind = kind;
        }

        public override PassingSemantics GetSemantics(ITypedFrame types) {
            return PassingSemantics.ValueType;
        }

        public override HelixType GetMutationSupertype(ITypedFrame types) => this;

        public override HelixType GetSignatureSupertype(ITypedFrame types) => this;

        public override ISyntaxTree ToSyntax(TokenLocation loc) {
            if (this == Void) {
                return new VoidLiteral(loc);
            }

            return base.ToSyntax(loc);
        }

        public override string ToString() {
            return this.kind switch {
                PrimitiveTypeKind.Int   => "int",
                PrimitiveTypeKind.Float => "float",
                PrimitiveTypeKind.Bool  => "bool",
                PrimitiveTypeKind.Void  => "void",
                _                       => throw new Exception("Unexpected primitive type kind"),
            };
        }

        private enum PrimitiveTypeKind {
            Int = 11, 
            Float = 13, 
            Bool = 17,
            Void = 19,
        }
    }
}