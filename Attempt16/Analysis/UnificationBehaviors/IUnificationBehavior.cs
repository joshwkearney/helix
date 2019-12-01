using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Analysis {

    public interface IUnificationBehavior {
        ISyntax Unify(ISyntax input, ILanguageType type);
    }
}