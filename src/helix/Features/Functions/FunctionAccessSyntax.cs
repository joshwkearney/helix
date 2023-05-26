using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Analysis;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Features.Functions {
    public record FunctionAccessSyntax : ISyntaxTree {
        public IdentifierPath FunctionPath { get; }

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public FunctionAccessSyntax(TokenLocation loc, IdentifierPath path) {
            this.Location = loc;
            this.FunctionPath = path;
        }

        public virtual ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var funcType = types.SyntaxValues[this.FunctionPath].AsType(types).GetValue();

            this.SetReturnType(funcType, types);
            this.SetCapturedVariables(types);
            this.SetPredicate(types);

            return this;
        }

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            this.SetLifetimes(new LifetimeBundle(), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            return new CVariableLiteral(writer.GetVariableName(this.FunctionPath));
        }
    }
}