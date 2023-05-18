//using Helix.Analysis.TypeChecking;

//namespace Helix.Syntax.Decorators {
//    public class ShadowingPreventer : ISyntaxDecorator {
//        public IEnumerable<string> Names { get; }

//        public ShadowingPreventer(IEnumerable<string> names) {
//            Names = names;
//        }

//        public void PreCheckTypes(ISyntaxTree syntax, TypeFrame types) {
//            foreach (var name in Names) {
//                if (types.TryResolveName(syntax.Location.Scope, name, out _)) {
//                    throw TypeException.IdentifierDefined(syntax.Location, name);
//                }
//            }
//        }
//    }
//}