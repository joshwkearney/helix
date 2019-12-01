using JoshuaKearney.Attempt15.Syntax;
using JoshuaKearney.Attempt15.Types;
using System;
using System.Collections.Generic;

namespace JoshuaKearney.Attempt15.Compiling {
    public class TypeUnifier {
        private readonly Dictionary<(TrophyTypeKind source, TrophyTypeKind target), ITypeUnification> unifiers 
            = new Dictionary<(TrophyTypeKind source, TrophyTypeKind target), ITypeUnification>();

        public TypeUnifier(IEnumerable<ITypeUnification> unifiers) {
            foreach (var unifier in unifiers) {
                this.unifiers[(unifier.SourceTypeKind, unifier.TargetTypeKind)] = unifier;
            }
        }

        public bool TryUnifySyntax(ISyntaxTree target, ITrophyType targetType, out ISyntaxTree result) {
            if (target.ExpressionType.Equals(targetType)) {
                result = target;
                return true;
            }

            var key = (target.ExpressionType.Kind, targetType.Kind);
            if (!this.unifiers.TryGetValue(key, out var unifier)) {
                result = null;
                return false;
            }

            return unifier.TryUnify(target, targetType, out result);
        }
    }
}