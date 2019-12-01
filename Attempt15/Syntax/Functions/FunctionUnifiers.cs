using JoshuaKearney.Attempt15.Types;
using System.Collections.Generic;

namespace JoshuaKearney.Attempt15.Syntax.Functions {
    public static class FunctionUnifiers {
        public static IEnumerable<ITypeUnification> Unifiers { get; } = new[] {
            new FunctionInterfaceUnifier()
        };

        private class FunctionInterfaceUnifier : ITypeUnification {
            public TrophyTypeKind SourceTypeKind => TrophyTypeKind.Function;

            public TrophyTypeKind TargetTypeKind => TrophyTypeKind.FunctionInterface;

            public bool TryUnify(ISyntaxTree target, ITrophyType targetType, out ISyntaxTree result) {
                var funcType = (FunctionType)target.ExpressionType;
                var faceType = (FunctionInterfaceType)targetType;

                if (funcType.GetCompatibleInterface().Equals(faceType)) {
                    result = new FunctionBoxSyntaxTree(
                        operand: target,
                        target: faceType
                    );

                    return true;
                }

                result = null;
                return false;
            }
        }
    }
}
