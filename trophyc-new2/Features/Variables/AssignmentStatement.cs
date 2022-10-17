using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Analysis.Unification;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Features.Variables;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntax AssignmentStatement() {
            var start = this.TopExpression();

            if (this.TryAdvance(TokenKind.Assignment)) {
                var assign = this.TopExpression();
                var loc = start.Location.Span(assign.Location);

                return new AssignmentStatement(loc, start, assign);
            }

            return start;
        }
    }
}

namespace Trophy.Features.Variables {
    public record AssignmentStatement : ISyntax {
        private readonly ISyntax target, assign;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public AssignmentStatement(TokenLocation loc, ISyntax target, 
                                   ISyntax assign, bool isTypeChecked = false) {
            this.Location = loc;
            this.target = target;
            this.assign = assign;
            this.isTypeChecked = isTypeChecked;
        }

        public Option<TrophyType> TryInterpret(INamesRecorder names) => Option.None;

        public ISyntax CheckTypes(ITypesRecorder types) {
            var target = this.target.CheckTypes(types).ToLValue(types);
            var assign = this.assign.CheckTypes(types).ToRValue(types);

            var targetType = types.GetReturnType(target);
            var assignType = types.GetReturnType(assign);

            // Make sure the target is a variable type
            if (targetType is not PointerType pointerType || !pointerType.IsWritable) {
                throw new Exception("Compiler inconsistency: lvalues must be writable pointers");
            }

            // Make sure the assign expression matches the target's inner type
            if (types.TryUnifyTo(assign, assignType, pointerType.ReferencedType).TryGetValue(out var newAssign)) {
                assign = newAssign;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(
                    this.assign.Location, 
                    pointerType.ReferencedType,
                    assignType);
            }

            var result = new AssignmentStatement(this.Location, target, assign, true);
            types.SetReturnType(result, PrimitiveType.Void);

            return result;
        }

        public ISyntax ToRValue(ITypesRecorder types) {
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