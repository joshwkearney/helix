using helix.common.Hmm;
using Helix.MiddleEnd.TypeChecking;
using Helix.MiddleEnd.Unification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd {
    internal class TypeCheckingContext {
        public required HmmWriter Writer { get; init; }

        public required TypeStore Types { get; init; }

        public required TypeCheckingNamesStore Names { get; init; }

        public TypeChecker TypeChecker { get; }

        public TypeUnifier Unifier { get; }

        public TypeCheckingContext() {
            this.TypeChecker = new TypeChecker(this);
            this.Unifier = new TypeUnifier(this);
        }
    }
}
