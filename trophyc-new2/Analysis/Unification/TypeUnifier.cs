using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis.SyntaxTree;
using Trophy.Features.Primitives;

namespace Trophy.Analysis.Unification {
    public static partial class TypeUnifier {
        public static Option<ISyntaxTree> TryUnify(ISyntaxTree syntax, TrophyType type) {
            if (syntax.ReturnType == type) {
                return Option.Some(syntax);
            }

            var result = (Option<ISyntaxTree>)Option.None; 
            
            result = TryUnifyPrimitives(syntax, type);
            if (result.HasValue) {
                return result;
            }

            return result;
        }
    }
}
