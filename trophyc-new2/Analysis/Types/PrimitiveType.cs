using Trophy.Features.Aggregates;
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

        public override bool CanUnifyTo(TrophyType other, ITypesRecorder types) {
            if (this == other) {
                return true;
            }
            else if (this == Void) {
                if (other == Int || other == Bool) {
                    return true;
                }
                else if (other is NamedType named && types.TryResolveName(named.Path).TryGetValue(out var target)) {
                    if (target == NameTarget.Aggregate) {
                        var sig = types.GetAggregate(named.Path);

                        return sig.Members.All(x => x.MemberType.HasDefaultValue(types));
                    }
                }
            }
                
            return false;
        }

        public override ISyntax UnifyTo(TrophyType other, ISyntax syntax, ITypesRecorder types) {
            if (this == other) {
                return syntax;
            }

            if (this == Void) {
                return new BlockSyntax(syntax.Location, new ISyntax[] {
                    syntax,
                    new PutSyntax(
                        syntax.Location,
                        other.ToSyntax(syntax.Location))
                });
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

        public override ISyntax ToSyntax(TokenLocation loc) {
            if (this == Void) {
                return new VoidLiteral(loc);
            }

            return base.ToSyntax(loc);
        }

        private enum PrimitiveTypeKind {
            Int = 11, 
            Float = 13, 
            Bool = 17,
            Void = 19,
        }
    }
}