using System.Collections.Generic;
using JoshuaKearney.Attempt15.Types;

namespace JoshuaKearney.Attempt15.Syntax.Logic {
    public static class BooleanUnifiers {
        public static IEnumerable<ITypeUnification> Unifiers { get; } = new ITypeUnification[] {
            new BoolToIntUnification(),
            new BoolToRealUnification()
        };

        private class BoolToIntUnification : ITypeUnification {
            public TrophyTypeKind SourceTypeKind => SimpleType.Boolean.Kind;

            public TrophyTypeKind TargetTypeKind => SimpleType.Int.Kind;

            public bool TryUnify(ISyntaxTree target, ITrophyType type, out ISyntaxTree result) {
                result = new BooleanUnarySyntaxTree(
                    operand:    target,
                    kind:       BooleanUnaryOperationKind.ConvertBoolToInt,
                    returnType: SimpleType.Int
                );

                return true;
            }
        }

        private class BoolToRealUnification : ITypeUnification {
            public TrophyTypeKind SourceTypeKind => SimpleType.Boolean.Kind;

            public TrophyTypeKind TargetTypeKind => SimpleType.Float.Kind;

            public bool TryUnify(ISyntaxTree target, ITrophyType targetType, out ISyntaxTree result) {
                result = new BooleanUnarySyntaxTree(
                    operand: target,
                    kind: BooleanUnaryOperationKind.ConvertBoolToReal,
                    returnType: SimpleType.Float
                );

                return true;
            }
        }
    }
}
