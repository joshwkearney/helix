using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Features.Variables
{
    public class AssignmentParseTree : IParseTree {
        private readonly IParseTree target, assign;

        public TokenLocation Location { get; }

        public AssignmentParseTree(TokenLocation loc, IParseTree target, IParseTree assign) {
            this.Location = loc;
            this.target = target;
            this.assign = assign;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            var targetOp = this.target.ResolveTypes(scope, names, types).ToLValue();
            var assign = this.assign.ResolveTypes(scope, names, types);

            // Make sure the target is an lvalue
            if (!targetOp.TryGetValue(out var target)) {
                throw TypeCheckingErrors.ExpectedVariableType(this.target.Location, target.ReturnType);
            }

            // Make sure the target is a variable type
            if (target.ReturnType is not PointerType pointerType) {
                throw TypeCheckingErrors.ExpectedVariableType(this.target.Location, target.ReturnType);
            }

            // Make sure the taret is writable
            //if (pointerType.IsReadOnly) {
            //    throw TypeCheckingErrors.ExpectedVariableType(this.Location, varType);
            //}

            // Make sure the assign expression matches the target' inner type
            if (assign.TryUnifyTo(pointerType.ReferencedType).TryGetValue(out var newAssign)) {
                assign = newAssign;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(this.assign.Location, pointerType.ReferencedType, assign.ReturnType);
            }

            return new AssignmentSyntax(target, assign);
        }
    }

    public class AssignmentSyntax : ISyntaxTree {
        private readonly ISyntaxTree target, assign;

        public TrophyType ReturnType => PrimitiveType.Void;

        public AssignmentSyntax(ISyntaxTree target, ISyntaxTree assign) {
            this.target = target;
            this.assign = assign;
        }

        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter) {
            var left = CExpression.Dereference(this.target.GenerateCode(writer, statWriter));
            var right = this.assign.GenerateCode(writer, statWriter);

            statWriter.WriteStatement(CStatement.Assignment(left, right));

            return CExpression.IntLiteral(0);
        }
    }
}