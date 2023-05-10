using Helix.Analysis;
using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree VariableAccess() {
            var tok = this.Advance(TokenKind.Identifier);

            return new VariableAccessParseSyntax(tok.Location, tok.Value);
        }
    }
}

namespace Helix.Features.Variables {                                                                                
    public record VariableAccessParseSyntax : ISyntaxTree {
        public string Name { get; }

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public VariableAccessParseSyntax(TokenLocation location, string name) {
            this.Location = location;
            this.Name = name;
        }

        public Option<HelixType> AsType(EvalFrame types) {
            // If we're pointing at a type then return it
            if (types.TryResolveName(this.Location.Scope, this.Name, out var syntax)) {
                if (syntax.AsType(types).TryGetValue(out var type)) {
                    return type;
                }
            }

            return Option.None;
        }

        public ISyntaxTree CheckTypes(EvalFrame types) {
            // Make sure this name exists
            if (!types.TryResolvePath(this.Location.Scope, this.Name, out var path)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.Name);
            }

            if (path == new IdentifierPath("void")) {
                return new VoidLiteral(this.Location).CheckTypes(types);
            }

            // Make sure we are accessing a variable
            if (types.Variables.TryGetValue(path, out var varSig)) {
                var result = new VariableAccessSyntax(this.Location, path);
                types.ReturnTypes[result] = varSig.Type;

                return result;
            }

            if (types.Functions.ContainsKey(path)) {
                var result = new VariableAccessSyntax(this.Location, path);
                types.ReturnTypes[result] = new NamedType(path);

                return result;
            }

            throw TypeCheckingErrors.VariableUndefined(this.Location, this.Name);
        }
    }

    public record VariableAccessSyntax : ISyntaxTree, ILValue {
        private readonly IdentifierPath variablePath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public bool IsLocalVariable => true;

        public VariableAccessSyntax(TokenLocation loc, IdentifierPath path) {
            this.Location = loc;
            this.variablePath = path;
        }

        public ISyntaxTree CheckTypes(EvalFrame types) => this;

        public ILValue ToLValue(EvalFrame types) {
            // Make sure this variable is writable
            if (!types.Variables[this.variablePath].IsWritable) {
                throw TypeCheckingErrors.WritingToConstVariable(this.Location);
            }

            return this;
        }

        public ISyntaxTree ToRValue(EvalFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            var lifetimes = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>();
            var sig = flow.Variables[this.variablePath];

            // Go through all this variable's members and set the lifetime bundle correctly
            foreach (var (compPath, compType) in VariablesHelper.GetMemberPaths(sig.Type, flow)) {
                var memPath = sig.Path.Append(compPath);

                //if (compType.IsValueType(flow)) {
                //    lifetimes[compPath] = new Lifetime[0];
                //}
                //else {
                    lifetimes[compPath] = new[] { flow.VariableLifetimes[memPath] };
                //}
            }

            flow.Lifetimes[this] = new LifetimeBundle(lifetimes);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.variablePath);

            return new CVariableLiteral(name);
        }
    }
}