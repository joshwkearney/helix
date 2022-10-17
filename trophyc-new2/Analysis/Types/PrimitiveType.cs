using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Analysis.Types {
    public record PrimitiveType : TrophyType {
        private readonly PrimitiveTypeKind kind;

        public static PrimitiveType Int { get; } = new PrimitiveType(PrimitiveTypeKind.Int);

        public static PrimitiveType Bool { get; } = new PrimitiveType(PrimitiveTypeKind.Bool);

        public static PrimitiveType Float { get; } = new PrimitiveType(PrimitiveTypeKind.Float);

        public static PrimitiveType Void { get; } = new PrimitiveType(PrimitiveTypeKind.Void);

        private PrimitiveType(PrimitiveTypeKind kind) {
            this.kind = kind;
        }

        public override bool CanUnifyTo(TrophyType other) {
            if (this == other) {
                return true;
            }

            if (this == Void) {
                return other == Int || other == Bool;
            }

            return false;
        }

        public override ISyntax UnifyTo(TrophyType other, ISyntax syntax) {
            if (this == other) {
                return syntax;
            }

            if (this == Void) {
                if (other == Int) {
                    return new BlockSyntax(syntax.Location, new ISyntax[] { 
                        syntax, new IntLiteral(syntax.Location, 0)
                    });
                }
                else if (other == Bool) {
                    return new BlockSyntax(syntax.Location, new ISyntax[] {
                        syntax, new BoolLiteral(syntax.Location, false)
                    });
                }
            }

            throw new InvalidOperationException();
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