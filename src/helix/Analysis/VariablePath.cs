using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Analysis {
    public record VariablePath(IdentifierPath Variable, IdentifierPath Member) { 
        public VariablePath(IdentifierPath variable) : this(variable, new IdentifierPath()) { }

        public VariablePath AppendMember(IdentifierPath comp) {
            return new VariablePath(this.Variable, this.Member.Append(comp));
        }

        public VariablePath AppendMember(string comp) {
            return new VariablePath(this.Variable, this.Member.Append(comp));
        }
    }
}
