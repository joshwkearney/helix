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
    public record ArrayToPointerAdapter : ISyntaxTree {
        private readonly ArrayType arrayType;
        private readonly ISyntaxTree target;
        private readonly ISyntaxTree offset = null;

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

        public ArrayToPointerAdapter(ArrayType arrayType, ISyntaxTree target)
            : this(arrayType, target, new IntLiteral(target.Location, 0)) { }

        ISyntaxTree ISyntaxTree.ToRValue(TypeFrame types) => this;

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            this.SetReturnType(new PointerType(this.arrayType.InnerType, true), types);
            this.SetCapturedVariables(this.target, this.offset, types);
            this.SetPredicate(this.target, this.offset, types);
            this.SetLifetimes(this.target.GetLifetimes(types), types);

            return this;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
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

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Array to pointer conversion");

            var ptrType = writer.ConvertType(new PointerType(this.arrayType.InnerType, true), types);
            var ptrValue = new CCompoundExpression() {
                Arguments = new[] {
                    newData,
                    new CMemberAccess() {
                        Target = target,
                        MemberName = "region"
                    }
                },
                Type = ptrType
            };

            var result = writer.WriteImpureExpression(ptrType, ptrValue);
            writer.WriteEmptyLine();

            return result;
        }
    }
}
