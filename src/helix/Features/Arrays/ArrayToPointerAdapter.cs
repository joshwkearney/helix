using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features.Arrays {
    public record ArrayToPointerAdapter : ISyntaxTree {
        private readonly ArrayType arrayType;
        private readonly ISyntaxTree target;
        private readonly ISyntaxTree? offset = null;

        public TokenLocation Location => this.target.Location;

        public IEnumerable<ISyntaxTree> Children {
            get {
                yield return this.target;

                if (this.offset != null) {
                    yield return this.offset;
                }
            }
        }

        public bool IsPure { get; }

        public ArrayToPointerAdapter(ArrayType arrayType, ISyntaxTree target, ISyntaxTree offset) {
            this.arrayType = arrayType;
            this.target = target;
            this.offset = offset;

            this.IsPure = target.IsPure && offset.IsPure;
        }

        public ArrayToPointerAdapter(ArrayType arrayType, ISyntaxTree target) {
            this.arrayType = arrayType;
            this.target = target;

            this.IsPure = target.IsPure;
        }

        ISyntaxTree ISyntaxTree.ToRValue(SyntaxFrame types) => this;

        ISyntaxTree ISyntaxTree.ToLValue(SyntaxFrame types) => this;

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            types.ReturnTypes[this] = new PointerType(this.arrayType.InnerType, true);
            types.CapturedVariables[this] = types.CapturedVariables[this.target];

            return this;
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            ICSyntax result = new CMemberAccess() {
                Target = this.target.GenerateCode(writer),
                MemberName = "data"
            };

            if (this.offset != null) {
                result = new CBinaryExpression() {
                    Left = result,
                    Right = this.offset.GenerateCode(writer),
                    Operation = BinaryOperationKind.Add
                };
            }

            return result;
        }
    }
}
