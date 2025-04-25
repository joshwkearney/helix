using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Syntax;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features.Variables {
    public record AssignmentStatement : ISyntax {
        public required TokenLocation Location { get; init; }
        
        public required ISyntax Left { get; init; }
        
        public required ISyntax Right { get; init; }
        
        public HelixType ReturnType => PrimitiveType.Void;

        public bool IsPure => false;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var target = new CPointerDereference {
                Target = this.Left.GenerateCode(types, writer)
            };

            var assign = this.Right.GenerateCode(types, writer);

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