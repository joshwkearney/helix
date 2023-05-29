using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features.Variables {
    public class CompoundSyntax : ISyntaxTree {
        private readonly IReadOnlyList<ISyntaxTree> args;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.args;

        public bool IsPure { get; }

        public CompoundSyntax(TokenLocation loc, IReadOnlyList<ISyntaxTree> args) {
            this.Location = loc;
            this.args = args;

            this.IsPure = args.All(x => x.IsPure);
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            var args = this.args.Select(x => x.CheckTypes(types)).ToArray();
            var result = new CompoundSyntax(this.Location, args);

            this.SetReturnType(PrimitiveType.Void, types);
            this.SetCapturedVariables(args, types);
            this.SetPredicate(args, types);
            this.SetLifetimes(new LifetimeBounds(), types);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            foreach (var arg in this.args) {
                arg.GenerateCode(types, writer);
            }

            return new CIntLiteral(0);
        }
    }
}
