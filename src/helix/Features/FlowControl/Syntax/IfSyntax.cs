using Helix.Analysis.Types;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Predicates;

namespace Helix.Features.FlowControl {
    public record IfSyntax : ISyntax {
        public required TokenLocation Location { get; init; }

        public required ISyntax Condition { get; init; }
        
        public required ISyntax Affirmative { get; init; }
        
        public required ISyntax  Negative { get; init; }
        
        public required HelixType ReturnType { get; init; }

        public ISyntaxPredicate Predicate => ISyntaxPredicate.Empty;

        public ISyntax ToRValue(TypeFrame types) => this;
        
        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var affirmList = new List<ICStatement>();
            var negList = new List<ICStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            var affirm = this.Affirmative.GenerateCode(types, affirmWriter);
            var neg = this.Negative.GenerateCode(types, negWriter);

            var tempName = writer.GetVariableName();

            if (this.ReturnType != PrimitiveType.Void) {
                affirmWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = affirm
                });

                negWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = neg
                });
            }

            var tempStat = new CVariableDeclaration() {
                Type = writer.ConvertType(this.ReturnType, types),
                Name = tempName
            };

            if (affirmList.Any() && affirmList.Last().IsEmpty) {
                affirmList.RemoveAt(affirmList.Count - 1);
            }

            if (negList.Any() && negList.Last().IsEmpty) {
                negList.RemoveAt(negList.Count - 1);
            }

            var expr = new CIf() {
                Condition = this.Condition.GenerateCode(types, writer),
                IfTrue = affirmList,
                IfFalse = negList
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Condition.Location.Line}: If statement");

            // Don't bother writing the temp variable if we are returning void
            if (this.ReturnType != PrimitiveType.Void) {
                writer.WriteStatement(tempStat);
            }

            writer.WriteStatement(expr);
            writer.WriteEmptyLine();

            if (this.ReturnType != PrimitiveType.Void) {
                return new CVariableLiteral(tempName);
            }
            else {
                return new CIntLiteral(0);
            }
        }
    }
}