using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Variables;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Primitives;

namespace Helix.Parsing {
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
            else {
                BinaryOperationKind op;

                if (this.TryAdvance(TokenKind.PlusAssignment)) {
                    op = BinaryOperationKind.Add;
                }
                else if (this.TryAdvance(TokenKind.MinusAssignment)) {
                    op = BinaryOperationKind.Subtract;
                }
                else if (this.TryAdvance(TokenKind.StarAssignment)) {
                    op = BinaryOperationKind.Multiply;
                }
                else if (this.TryAdvance(TokenKind.DivideAssignment)) {
                    op = BinaryOperationKind.FloorDivide;
                }
                else if (this.TryAdvance(TokenKind.ModuloAssignment)) {
                    op = BinaryOperationKind.Modulo;
                }
                else {
                    return start;
                }

                var second = this.TopExpression(block);
                var loc = start.Location.Span(second.Location);
                var assign = new BinarySyntax(loc, start, second, op);
                var stat = new AssignmentStatement(loc, start, assign);

                block.Statements.Add(stat);
                return new VoidLiteral(loc);
            }
        }
    }
}

namespace Helix.Features.Variables {
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
            types.CapturedVariables[result] = Array.Empty<IdentifierPath>();

            // If the rvalue captures any variables that the lvalue doesn't,
            // either insert a runtime lifetime check if this function is
            // "pooling", otherwise throw a compile error for an unsafe memory
            // store.

            var uncaptured = types.CapturedVariables[assign]
                .Except(types.CapturedVariables[target])
                .Any();

            // TODO: Insert runtime lifetime check if possible

            if (uncaptured) {
                throw new TypeCheckingException(
                    this.Location,
                    "Unsafe Memory Store",
                    $"Unable to verify that the assigned value outlives its container. " + 
                    "Please declare this function as 'pooling' to check variable " + 
                    "lifetimes at runtime or wrap this assignment in an unsafe block.");
            }

            // Modify the variable declaration to include any new captured variables
            foreach (var cap in types.CapturedVariables[target]) {
                if (cap == new IdentifierPath("$stack")) {
                    continue;
                }

                var sig = types.Variables[cap];
                var newCaptured = sig.CapturedVariables
                    .Concat(types.CapturedVariables[assign])
                    .ToArray();

                var newSig = new VariableSignature(
                    sig.Path, 
                    sig.Type, 
                    sig.IsWritable, 
                    newCaptured);

                types.Variables[cap] = newSig;
            }

            return result;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            var stat = new CAssignment() {
                Left = new CPointerDereference() {
                    Target = this.target.GenerateCode(writer)
                },
                Right = this.assign.GenerateCode(writer)
            };

            writer.WriteStatement(stat);

            return new CIntLiteral(0);
        }
    }
}