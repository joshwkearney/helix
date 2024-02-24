using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features.Arrays {
    public record ArrayToPointerAdapter : IParseTree {
        private readonly ArrayType arrayType;
        private readonly IParseTree target;
        private readonly IParseTree offset = null;

        public TokenLocation Location => this.target.Location;

        public IEnumerable<IParseTree> Children {
            get {
                yield return this.target;

                if (this.offset != null) {
                    yield return this.offset;
                }
            }
        }

        public bool IsPure { get; }

        public ArrayToPointerAdapter(ArrayType arrayType, IParseTree target, IParseTree offset) {
            this.arrayType = arrayType;
            this.target = target;
            this.offset = offset;

            this.IsPure = target.IsPure && offset.IsPure;
        }

        public ArrayToPointerAdapter(ArrayType arrayType, IParseTree target)
            : this(arrayType, target, new WordLiteral(target.Location, 0)) { }

        IParseTree IParseTree.ToRValue(TypeFrame types) => this;
    }
}
