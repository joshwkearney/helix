using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.Types;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Features.Memory {
    public class AddressOfSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public AddressOfSyntax(TokenLocation loc, ISyntaxTree target) {
            this.Location = loc;
            this.target = target;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var target = this.target.CheckTypes(types).ToLValue(types);
            var ptrType = (PointerType)target.GetReturnType(types);
            var result = new AddressOfSyntax(this.Location, target);

            result.SetReturnType(ptrType, types);
            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (this.IsFlowAnalyzed(flow)) {
                return;
            }

            this.target.AnalyzeFlow(flow);

            var valueLifetime = this.target.GetLifetimes(flow)[new IdentifierPath()];

            // Make sure we're taking the address of a variable
            if (!valueLifetime.IsLocal) {
                // TODO: Add more specific error message
                throw TypeException.ExpectedVariableType(this.Location, this.target.GetReturnType(flow));
            }

            var locationBounds = flow.LocalLifetimes[valueLifetime.Path];
            var dict = new Dictionary<IdentifierPath, Lifetime>() { { new IdentifierPath(), locationBounds.LValue } };

            this.SetLifetimes(new LifetimeBundle(dict), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            return new CCompoundExpression() {
                Arguments = new ICSyntax[] {
                    this.target.GenerateCode(types, writer),
                    writer.GetLifetime(this.GetLifetimes(types)[new IdentifierPath()], types)
                },
                Type = writer.ConvertType(this.GetReturnType(types))
            };
        }
    }
}