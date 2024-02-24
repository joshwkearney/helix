using Helix.Analysis.Flow;
using Helix.Analysis.Predicates;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.HelixMinusMinus;
using Helix.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Analysis.TypeChecking {
    public record SyntaxTag {
        public HelixType ReturnType { get; }

        public SyntaxTag(HelixType returnType) {
            this.ReturnType = returnType;
        }
    }

    public class SyntaxTagBuilder {
        private readonly TypeFrame types;
        private HelixType ReturnType = PrimitiveType.Void;

        public static SyntaxTagBuilder AtFrame(TypeFrame types) {
            return new SyntaxTagBuilder(types);
        }

        private SyntaxTagBuilder(TypeFrame types) {
            this.types = types;
        }

        public SyntaxTagBuilder WithReturnType(HelixType type) {
            this.ReturnType = type;

            return this;
        }

        public void BuildFor(IdentifierPath path) {
            var tag = new SyntaxTag(this.ReturnType);

            this.types.Locals = this.types.Locals.SetItem(path, new LocalInfo(this.ReturnType));
        }
    }
}
