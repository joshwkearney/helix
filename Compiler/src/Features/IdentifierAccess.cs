using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Features.Functions;
using Trophy.Parsing;
using Trophy.Features.Meta;

namespace Trophy.Features.Variables {
    public class IdentifierAccessSyntaxA : ISyntaxA {
        private readonly string name;
        private readonly VariableAccessKind kind;

        public TokenLocation Location { get; }

        public IdentifierAccessSyntaxA(TokenLocation location, string name, VariableAccessKind kind) {
            this.Location = location;
            this.name = name;
            this.kind = kind;
        }

        public IOption<ITrophyType> ResolveToType(INamesRecorder names) {
            return this.Rewrite(names).SelectMany(x => x.ResolveToType(names));
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            // See if we need to rewrite this to another syntax tree
            if (this.Rewrite(names).TryGetValue(out var rewrite)) {
                return rewrite.CheckNames(names);
            }

            // Make sure this name exists
            if (!names.TryFindName(this.name, out var target, out var path)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            // Make sure this is a variable
            if (target != NameTarget.Variable) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            return new VariableAccessSyntaxB(this.Location, path, this.kind);
        }

        private IOption<ISyntaxA> Rewrite(INamesRecorder names) {
            // Make sure this name exists
            if (!names.TryFindName(this.name, out var target, out var path)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            if (target == NameTarget.Function) {
                return Option.Some(new FunctionAccessSyntaxA(this.Location, path));
            }
            else if (target == NameTarget.Struct || target == NameTarget.Union) {
                return Option.Some(new TypeAccessSyntaxA(this.Location, new NamedType(path)));
            }
            else {
                return Option.None<ISyntaxA>();
            }
        }
    }
}