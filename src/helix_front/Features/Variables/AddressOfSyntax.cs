using Helix.Analysis;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Features.Variables {
    public class AddressOfSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { target };

        public bool IsPure => target.IsPure;

        public AddressOfSyntax(TokenLocation loc, ISyntaxTree target) {
            Location = loc;
            this.target = target;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var target = this.target.CheckTypes(types).ToLValue(types);
            var varType = target.GetReturnType(types);
            var result = new AddressOfSyntax(Location, target);

            var capturedVars = target.GetCapturedVariables(types)
                .Select(x => new VariableCapture(x.VariablePath, VariableCaptureKind.LocationCapture, x.Signature))
                .ToArray();

            SyntaxTagBuilder.AtFrame(types)
                .WithChildren(target)
                .WithReturnType(varType)
                .WithCapturedVariables(capturedVars)
                .BuildFor(result);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            return this;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CCompoundExpression() {
                Arguments = new ICSyntax[] {
                    target.GenerateCode(types, writer),
                    new CVariableLiteral("TEMPLIFETIME")
                },
                Type = writer.ConvertType(this.GetReturnType(types), types)
            };
        }
    }
}