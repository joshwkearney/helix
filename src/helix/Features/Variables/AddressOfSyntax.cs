/*using Helix.Analysis;
using Helix.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace helix.Features.Variables {
    public class AddressOfSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public AddressOfSyntax(ISyntaxTree target) {
            this.target = target;
        }

        public ISyntaxTree CheckTypes(EvalFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var target = this.target.CheckTypes(types);

        }
    }
}*/