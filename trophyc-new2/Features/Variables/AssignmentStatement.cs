using Trophy.Analysis;
using Trophy.Analysis.Unification;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Variables;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree AssignmentStatement() {
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
    public class AssignmentStatement : ISyntaxTree {
        private readonly ISyntaxTree target, assign;

        public TokenLocation Location { get; }

        public AssignmentStatement(TokenLocation loc, ISyntaxTree target, ISyntaxTree assign) {
            this.Location = loc;
            this.target = target;
            this.assign = assign;
        }

        public Option<TrophyType> ToType(INamesObserver names) => Option.None;

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
            var targetOp = this.target.CheckTypes(types).ToLValue(types);
            var assignOp = this.assign.CheckTypes(types).ToRValue(types);

            // Make sure the target is an lvalue
            if (!targetOp.TryGetValue(out var target)) {
                throw TypeCheckingErrors.LValueRequired(this.target.Location);
            }

            // Make sure the assignment value is an rvalue
            if (!assignOp.TryGetValue(out var assign)) {
                throw TypeCheckingErrors.RValueRequired(this.assign.Location);
            }

            var targetType = types.GetReturnType(target);
            var assignType = types.GetReturnType(assign);

            // Make sure the target is a variable type
            if (targetType is not PointerType pointerType || !pointerType.IsWritable) {
                throw new Exception("Compiler inconsistency: lvalues must be writable pointers");
            }

            // Make sure the assign expression matches the target's inner type
            if (TypeUnifier.TryUnifyTo(assign, assignType, pointerType.ReferencedType).TryGetValue(out var newAssign)) {
                assign = newAssign;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(
                    this.assign.Location, 
                    pointerType.ReferencedType,
                    assignType);
            }

            var result = new AssignmentStatement(this.Location, target, assign);
            types.SetReturnType(result, PrimitiveType.Void);

            return result;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(CStatementWriter writer) {
            var left = CExpression.Dereference(this.target.GenerateCode(writer));
            var right = this.assign.GenerateCode(writer);

            writer.WriteStatement(CStatement.Assignment(left, right));

            return CExpression.IntLiteral(0);
        }
    }
}