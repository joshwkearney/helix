using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.Variables;
using Trophy.Parsing;
using Trophy.Generation.Syntax;
using Trophy.Features.Primitives;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree AssignmentStatement(BlockBuilder block) {
            var start = this.TopExpression(block);

            if (this.TryAdvance(TokenKind.Assignment)) {
                var assign = this.TopExpression(block);
                var loc = start.Location.Span(assign.Location);
                var result = new AssignmentStatement(loc, start, assign);

                block.Statements.Add(result);
                return new VoidLiteral(loc);
            }

            return start;
        }
    }
}

namespace Trophy.Features.Variables {
    public record AssignmentStatement : ISyntaxTree {
        private readonly ISyntaxTree target, assign;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target, this.assign };

        public bool IsPure => false;

        public AssignmentStatement(TokenLocation loc, ISyntaxTree target, 
                                   ISyntaxTree assign, bool isTypeChecked = false) {
            this.Location = loc;
            this.target = target;
            this.assign = assign;
            this.isTypeChecked = isTypeChecked;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var target = this.target.CheckTypes(types).ToLValue(types);
            var assign = this.assign.CheckTypes(types).ToRValue(types);

            var targetType = types.ReturnTypes[target];

            // Make sure the target is a variable type
            if (targetType is not PointerType pointerType || !pointerType.IsWritable) {
                throw new Exception("Compiler inconsistency: lvalues must be writable pointers");
            }

            assign = assign.UnifyTo(pointerType.InnerType, types);

            var result = new AssignmentStatement(this.Location, target, assign, true);
            types.ReturnTypes[result] = PrimitiveType.Void;

            return result;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            var stat = new CAssignment() {
                Left = new CPointerDereference() {
                    Target = this.target.GenerateCode(types, writer)
                },
                Right = this.assign.GenerateCode(types, writer)
            };

            writer.WriteStatement(stat);

            return new CIntLiteral(0);
        }
    }
}