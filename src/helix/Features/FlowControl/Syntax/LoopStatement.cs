using Helix.Analysis.Predicates;
using Helix.Generation;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;

namespace Helix.Features.FlowControl {
    public record LoopStatement : ISyntax {
        public required TokenLocation Location { get; init; }

        public required ISyntax Body { get; init; }
        
        public HelixType ReturnType => PrimitiveType.Void;

        public ISyntaxPredicate Predicate => ISyntaxPredicate.Empty;
        
        public bool IsPure => false;

        public ISyntax ToRValue(TypeFrame types) => this;
        
        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var bodyStats = new List<ICStatement>();
            var bodyWriter = new CStatementWriter(writer, bodyStats);

            this.Body.GenerateCode(types, bodyWriter);

            if (bodyStats.Any() && bodyStats.Last().IsEmpty) {
                bodyStats.RemoveAt(bodyStats.Count - 1);
            }

            var stat = new CWhile {
                Condition = new CIntLiteral(1),
                Body = bodyStats
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Loop");
            writer.WriteStatement(stat);
            writer.WriteEmptyLine();

            return new CIntLiteral(0);
        }
    }
}
