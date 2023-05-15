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

        public override PassingSemantics GetSemantics(ITypedFrame types) {
            return PassingSemantics.ValueType;
        }

        public override UnificationKind TestUnification(HelixType other, EvalFrame types) {
            if (this == other) {
                return UnificationKind.Pun;
            }
            else if (this == Void) {
                if (other == Int || other == Bool || other == Float) {
                    return UnificationKind.Pun;
                }
                else if (other is NamedType named) {
                    if (types.Structs.TryGetValue(named.Path, out var sig)) {
                        var memsConvertable = sig.Members
                            .Select(x => PrimitiveType.Void.TestUnification(x.Type, types))
                            .All(x => x.IsSubsetOf(UnificationKind.Convert));

                        return UnificationKind.Convert;
                    }
                }
            }

            return UnificationKind.None;
        }

        public override ISyntaxTree UnifyTo(HelixType other, ISyntaxTree syntax,
                                            UnificationKind unify, EvalFrame types) {
            var test = this.TestUnification(other, types);

            if (test.IsSubsetOf(unify)) {
                if (test == UnificationKind.Pun) {
                    return syntax;
                }
                else {
                    return new BlockSyntax(syntax.Location, new ISyntaxTree[] {
                        syntax,
                        new PutSyntax(syntax.Location, other.ToSyntax(syntax.Location))
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

        public override ISyntaxTree ToSyntax(TokenLocation loc) {
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