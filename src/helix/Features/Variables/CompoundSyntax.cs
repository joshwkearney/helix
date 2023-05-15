using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using helix.FlowAnalysis;
using helix.Syntax;
using Helix.Analysis;
using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features.Variables {
    public class CompoundSyntax : ISyntaxTree {
        private readonly IReadOnlyList<ISyntaxTree> args;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => args;

        public bool IsPure { get; }

        public CompoundSyntax(TokenLocation loc, IReadOnlyList<ISyntaxTree> args) {
            this.Location = loc;
            this.args = args;

            this.IsPure = args.All(x => x.IsPure);
        }

        public ISyntaxTree CheckTypes(EvalFrame types) {
            var result = new CompoundSyntax(
                this.Location,
                this.args.Select(x => x.CheckTypes(types)).ToArray());

            this.SetReturnType(PrimitiveType.Void, types);

            return result;
        }

        public ISyntaxTree ToRValue(EvalFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            this.SetLifetimes(new LifetimeBundle(), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            foreach (var arg in this.args) {
                arg.GenerateCode(types, writer);
            }

            return new CIntLiteral(0);
        }
    }
}
