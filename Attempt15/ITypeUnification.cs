using JoshuaKearney.Attempt15.Syntax;
using JoshuaKearney.Attempt15.Types;

namespace JoshuaKearney.Attempt15 {
    public interface ITypeUnification {
        TrophyTypeKind SourceTypeKind { get; }

        TrophyTypeKind TargetTypeKind { get; }

        bool TryUnify(ISyntaxTree target, ITrophyType targetType, out ISyntaxTree result);
    }
}