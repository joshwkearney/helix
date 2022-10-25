using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.CSyntax;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features.Arrays {
    public record ArrayToPointerAdapter : ISyntaxTree, ILValue {
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

        public bool IsLocal => false;

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

        ILValue ISyntaxTree.ToLValue(SyntaxFrame types) => this;

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            types.ReturnTypes[this] = new PointerType(this.arrayType.InnerType, true);
            types.Lifetimes[this] = types.Lifetimes[this.target];

            return this;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            var target = this.target.GenerateCode(types, writer);

            ICSyntax newData = new CMemberAccess() {
                Target = target,
                MemberName = "data"
            };

            if (this.offset != null) {
                newData = new CBinaryExpression() {
                    Left = newData,
                    Right = this.offset.GenerateCode(types, writer),
                    Operation = BinaryOperationKind.Add
                };
            }

            var ptrType = new PointerType(this.arrayType.InnerType, true);
            var ptrValue = new CCompoundExpression() {
                Arguments = new[] {
                    newData,
                    new CIntLiteral(1),
                    new CMemberAccess() {
                        Target = target,
                        MemberName = "pool"
                    }
                }
            };

            return writer.WriteImpureExpression(writer.ConvertType(ptrType), ptrValue);
        }
    }
}
