using Helix.Analysis.Predicates;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Syntax;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features.Variables {
    public record AssignmentStatement : ISyntax {
        public required TokenLocation Location { get; init; }
        
        public required ISyntax Operand { get; init; }
        
        public required ISyntax Assignment { get; init; }
        
        public HelixType ReturnType => PrimitiveType.Void;

        public ISyntaxPredicate Predicate => ISyntaxPredicate.Empty;
        
        public bool IsPure => false;

        public ISyntax ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var target = new CPointerDereference {
                Target = this.Operand.GenerateCode(types, writer)
            };

            var assign = this.Assignment.GenerateCode(types, writer);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Assignment statement");

            writer.WriteStatement(new CAssignment() {
                Left = target,
                Right = assign
            });

            writer.WriteEmptyLine();
            return new CIntLiteral(0);
        }
    }
}