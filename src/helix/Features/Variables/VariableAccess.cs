using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

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

        public Option<HelixType> AsType(TypeFrame types) {
            // If we're pointing at a type then return it
            if (types.TryResolveName(this.Location.Scope, this.Name, out var syntax)) {
                if (syntax.AsType(types).TryGetValue(out var type)) {
                    return type;
                }
            }

            return Option.None;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            // Make sure this name exists
            if (!types.TryResolvePath(this.Location.Scope, this.Name, out var path)) {
                throw TypeException.VariableUndefined(this.Location, this.Name);
            }

            if (path == new IdentifierPath("void")) {
                return new VoidLiteral(this.Location).CheckTypes(types);
            }

            // See if we are accessing a variable
            if (types.Variables.ContainsKey(path)) {
                return new VariableAccessSyntax(this.Location, path).CheckTypes(types);
            }

            // See if we are accessing a function
            if (types.Functions.ContainsKey(path)) {
                return new VariableAccessSyntax(this.Location, path).CheckTypes(types);
            }

            throw TypeException.VariableUndefined(this.Location, this.Name);
        }
    }

    public record VariableAccessSyntax : ISyntaxTree {
        private static int tempCounter = 0;

        public IdentifierPath VariablePath { get; }

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public VariableAccessSyntax(TokenLocation loc, IdentifierPath path) {
            this.Location = loc;
            this.VariablePath = path;
        }

        public virtual ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            if (types.Variables.ContainsKey(this.VariablePath)) {
                this.SetReturnType(types.Variables[this.VariablePath].Type, types);
            }
            else if (types.Functions.ContainsKey(this.VariablePath)) {
                this.SetReturnType(new NamedType(this.VariablePath), types);
            }
            else {
                throw new InvalidOperationException("Compiler bug");
            }

            return this;
        }

        public virtual ISyntaxTree ToLValue(TypeFrame types) {
            // Make sure this variable is writable
            if (!types.Variables.ContainsKey(this.VariablePath) || !types.Variables[this.VariablePath].IsWritable) {
                throw TypeException.WritingToConstVariable(this.Location);
            }

            return new VariableAccessLValue(this.Location, this.VariablePath).CheckTypes(types);
        }

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public virtual void AnalyzeFlow(FlowFrame flow) {
            var sig = flow.Variables[this.VariablePath];
            var bundleDict = new Dictionary<IdentifierPath, Lifetime>();

            foreach (var (relPath, type) in sig.Type.GetMembers(flow)) {
                var memPath = this.VariablePath.AppendMember(relPath);

                if (type.IsValueType(flow)) {
                    bundleDict[relPath] = Lifetime.None;
                }
                else if (flow.StoredValueLifetimes.TryGetValue(memPath, out var bundle)) {
                    // We know what's in this variable, so return it as a more specific answer
                    bundleDict[relPath] = bundle;
                }
                else {
                    // Not sure what's in the variable, so just return the variable's lifetime to
                    // be safe
                    bundleDict[relPath] = flow.LocationLifetimes[memPath];
                }
            }

            this.SetLifetimes(new LifetimeBundle(bundleDict), flow);
        }

        public virtual ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            ICSyntax result = new CVariableLiteral(writer.GetVariableName(this.VariablePath));

            if (writer.VariableKinds[this.VariablePath] == CVariableKind.Allocated) {
                result = new CPointerDereference() {
                    Target = result
                };
            }

            return result;
        }
    }

    public record VariableAccessLValue : VariableAccessSyntax {
        public VariableAccessLValue(TokenLocation loc, IdentifierPath path) : base(loc, path) { }

        public override ISyntaxTree ToLValue(TypeFrame types) => this;

        public override ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var returnType = new PointerType(types.Variables[this.VariablePath].Type, true);

            this.SetReturnType(returnType, types);
            return this;
        }

        public override void AnalyzeFlow(FlowFrame flow) {
            var sig = flow.Variables[this.VariablePath];
            var bundleDict = new Dictionary<IdentifierPath, Lifetime>();

            foreach (var (relPath, _) in sig.Type.GetMembers(flow)) {
                var memPath = this.VariablePath.AppendMember(relPath);

                // TODO: This will break when variable invalidating is implemented
                bundleDict[relPath] = flow.StoredValueLifetimes[memPath];
            }

            this.SetLifetimes(new LifetimeBundle(bundleDict), flow);
        }

        public override ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            return new CAddressOf() {
                Target = base.GenerateCode(types, writer)
            };
        }
    }
}