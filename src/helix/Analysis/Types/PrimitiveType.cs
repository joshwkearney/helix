using Helix.Features.Aggregates;
using Helix.Features.FlowControl;
using Helix.Features.Memory;
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

        public override bool CanUnifyTo(HelixType other, EvalFrame types, bool isCast) {
            if (this == other) {
                return true;
            }
            else if (this == Void) {
                if (other == Int || other == Bool) {
                    return true;
                }
                else if (other is NamedType named) {
                    if (types.Aggregates.TryGetValue(named.Path, out var sig)) {
                        return sig.Members.All(x => x.Type.HasDefaultValue(types));
                    }
                }
            }
                
            return false;
        }

        public override ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax, bool isCast, EvalFrame types) {
            if (this == other) {
                return syntax;
            }

            if (this == Void) {
                return new BlockSyntax(syntax.Location, new ISyntaxTree[] {
                    syntax,
                    new NewPutSyntax(
                        syntax.Location,
                        other.ToSyntax(syntax.Location),
                        false)
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

        public override ISyntaxTree ToSyntax(TokenLocation loc) {
            if (this == Void) {
                return new VoidLiteral(loc);
            }

            return base.ToSyntax(loc);
        }

        public override bool IsRemote(EvalFrame types) => false;

        private enum PrimitiveTypeKind {
            Int = 11, 
            Float = 13, 
            Bool = 17,
            Void = 19,
        }
    }
}