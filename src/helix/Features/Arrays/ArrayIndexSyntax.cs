using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.Arrays;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Parsing {
    public partial class Parser {
        public ISyntaxTree ArrayExpression(ISyntaxTree start, BlockBuilder block) {
            this.Advance(TokenKind.OpenBracket);

            if (this.Peek(TokenKind.CloseBracket)) {
                var end = this.Advance(TokenKind.CloseBracket);
                var loc = start.Location.Span(end.Location);

                return new ArrayTypeSyntax(loc, start);
            }
            else {
                var index = this.TopExpression(block);
                var end = this.Advance(TokenKind.CloseBracket);
                var loc = start.Location.Span(end.Location);

                return new ArrayIndexSyntax(loc, start, index);
            }            
        }
    }
}

namespace Helix.Features.Arrays {
    public record ArrayIndexSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly ISyntaxTree index;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target, this.index };

        public bool IsPure { get; }

        public ArrayIndexSyntax(TokenLocation loc, ISyntaxTree target, 
            ISyntaxTree index, bool isTypeChecked = false) {

            this.Location = loc;
            this.target = target;
            this.index = index;
            this.isTypeChecked = isTypeChecked;

            this.IsPure = this.target.IsPure && this.index.IsPure;
        }

        ISyntaxTree ISyntaxTree.ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            return this;
        }

        ISyntaxTree ISyntaxTree.ToLValue(SyntaxFrame types) {
            var arrayType = (ArrayType)types.ReturnTypes[this.target];
            var result = new ArrayToPointerAdapter(arrayType, this.target, this.index);

            return result.CheckTypes(types);
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var target = this.target
                .CheckTypes(types)
                .ToRValue(types);

            var index = this.index
                .CheckTypes(types)
                .ToRValue(types)
                .UnifyTo(PrimitiveType.Int, types);

            // Make sure we have an array
            if (types.ReturnTypes[target] is not ArrayType arrayType) {
                throw TypeCheckingErrors.ExpectedArrayType(
                    this.target.Location, 
                    types.ReturnTypes[target]);
            }

            var result = new ArrayIndexSyntax(this.Location, target, index, true);
            types.ReturnTypes[result] = arrayType.InnerType;

            if (arrayType.InnerType.IsValueType(types)) {
                types.CapturedVariables[result] = Array.Empty<IdentifierPath>();
            }
            else {
                types.CapturedVariables[result] = types.CapturedVariables[target];
            }

            return result;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            return new CPointerDereference() {
                Target = new CBinaryExpression() {
                    Operation = BinaryOperationKind.Add,
                    Left = new CMemberAccess() {
                        MemberName = "data",
                        Target = this.target.GenerateCode(types, writer)
                    },
                    Right = this.index.GenerateCode(types, writer)
                }
            };
        }
    }
}