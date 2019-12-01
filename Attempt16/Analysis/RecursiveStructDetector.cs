using Attempt16.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt16.Analysis {
    public class RecursiveStructDetector : ITypeVisitor<bool> {
        private readonly Scope scope;
        private readonly HashSet<SingularStructType> foundStructTypes = new HashSet<SingularStructType>();

        public RecursiveStructDetector(Scope scope) {
            this.scope = scope;
        }

        public bool VisitFunctionType(SingularFunctionType type) {
            return false;
        }

        public bool VisitIntType(IntType type) {
            return false;
        }

        public bool VisitStructType(SingularStructType type) {
            if (this.foundStructTypes.Contains(type)) {
                return true;
            }

            this.foundStructTypes.Add(type);

            foreach (var mem in type.Members) {
                if (this.scope.FindType(mem.TypePath).GetValue().Accept(this)) {
                    return true;
                }
            }

            return false;
        }

        public bool VisitVariableType(VariableType type) {
            return false;
        }

        public bool VisitVoidType(VoidType type) {
            return false;
        }
    }
}