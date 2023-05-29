using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.Types;
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
            var varSig = target.GetReturnType(types).AsVariable(types).GetValue();
            var result = new AddressOfSyntax(Location, target);

            target = target.UnifyTo(varSig, types);

            var capturedVars = target.GetCapturedVariables(types)
                .Select(x => new VariableCapture(x.VariablePath, VariableCaptureKind.LocationCapture, varSig))
                .ToArray();

            result.SetReturnType(varSig, types);
            result.SetCapturedVariables(capturedVars, types);
            result.SetPredicate(target, types);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (this.IsFlowAnalyzed(flow)) {
                return;
            }

            target.AnalyzeFlow(flow);
            var locationLifetime = target.GetLifetimes(flow).LocationLifetime;

            // Make sure we're taking the address of a variable location
            if (locationLifetime == Lifetime.None) {
                // TODO: Add more specific error message
                throw TypeException.ExpectedVariableType(Location, target.GetReturnType(flow));
            }

            this.SetLifetimes(new LifetimeBounds(locationLifetime), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            return new CCompoundExpression() {
                Arguments = new ICSyntax[] {
                    target.GenerateCode(types, writer),
                    writer.GetLifetime(this.GetLifetimes(types).ValueLifetime, types)
                },
                Type = writer.ConvertType(this.GetReturnType(types))
            };
        }
    }
}