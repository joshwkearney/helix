using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.IRGeneration;
using Helix.Parsing;
using Helix.Parsing.IR;
using Helix.Syntax;

namespace Helix.Features.Variables.Syntax {
    public class AddressOfSyntax : ISyntax {
        public required TokenLocation Location { get; init; }

        public required HelixType ReturnType { get; init; }
        
        public required IdentifierPath VariablePath { get; init; }
        
        public required bool AlwaysJumps { get; init; }
        
        public Immediate GenerateIR(IRWriter writer, IRFrame context) {
            // If we're taking the address of a local, it's already been promoted to heap allocated and
            // the variable is actually storing a reference. We can just return that
            return context.GetVariable(this.VariablePath);
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CCompoundExpression {
                Arguments = [
                    new CVariableLiteral(writer.GetVariableName(this.VariablePath))
                ],
                Type = writer.ConvertType(this.ReturnType, types)
            };
        }
    }
}