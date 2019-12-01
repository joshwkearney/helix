//using System.Collections.Generic;
//using System.Linq;
//using System.Collections.Immutable;

//namespace Attempt8 {
//    public class Scope {
//        public ImmutableDictionary<string, IInterpretedValue> Variables { get; }

//        public ImmutableDictionary<InterpretedType, Scope> TypeExtensions { get; }

//        public Scope() {
//            this.Variables = ImmutableDictionary<string, IInterpretedValue>.Empty;
//            this.TypeExtensions = ImmutableDictionary<InterpretedType, Scope>.Empty;
//        }

//        public Scope(ImmutableDictionary<string, IInterpretedValue> vars, ImmutableDictionary<InterpretedType, Scope> exts) {
//            this.Variables = vars;
//            this.TypeExtensions = exts;
//        }

//        public Scope Append(string name, IInterpretedValue var) {
//            var newVars = this.Variables.Add(name, var);

//            return new Scope(newVars, this.TypeExtensions);             
//        }

//        public Scope Append(InterpretedType type, string name, IInterpretedValue val) {
//            var newExts = this.TypeExtensions;

//            if (newExts.TryGetValue(type, out var scope)) {
//                newExts = newExts.SetItem(type, scope.Append(name, val));
//            }
//            else { 
//                newExts = newExts.Add(type, new Scope().Append(name, val));
//            }

//            return new Scope(this.Variables, newExts);
//        }
//    }
//}