using System.Collections.Generic;
using JoshuaKearney.Attempt15.Types;

namespace JoshuaKearney.Attempt15.Syntax.Arithmetic {
    public static class ArithmeticUnifiers {
        public static IEnumerable<ITypeUnification> Unifiers { get; } = new[] {
            new IntToRealUnication()
        };

        private class IntToRealUnication : ITypeUnification {
            public TrophyTypeKind SourceTypeKind => TrophyTypeKind.Int;

            public TrophyTypeKind TargetTypeKind => TrophyTypeKind.Float;

            public bool TryUnify(ISyntaxTree target, ITrophyType targetType, out ISyntaxTree result) {
                result = new ArithmeticUnarySyntaxTree(
                    operand:    target,
                    kind:       ArithmeticUnaryOperationKind.ConvertIntToReal,
                    returnType: new SimpleType(TrophyTypeKind.Float)
                );

                return true;
            }
        }
    }
}