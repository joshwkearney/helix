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
        private readonly bool isLValue;

        public IdentifierPath VariablePath { get; }

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public VariableAccessSyntax(TokenLocation loc, IdentifierPath path, bool isLValue = false) {
            this.Location = loc;
            this.VariablePath = path;
            this.isLValue = isLValue;
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

            this.SetCapturedVariables(this.VariablePath, VariableCaptureKind.ValueCapture, types);

            return this;
        }

        public virtual ISyntaxTree ToLValue(TypeFrame types) {
            // Make sure this variable is writable
            if (!types.Variables.ContainsKey(this.VariablePath) || !types.Variables[this.VariablePath].IsWritable) {
                throw TypeException.WritingToConstVariable(this.Location);
            }

            var result = new VariableAccessSyntax(this.Location, this.VariablePath, true).CheckTypes(types);

            result.SetReturnType(new PointerType(this.GetReturnType(types)), types);
            result.SetCapturedVariables(this.VariablePath, VariableCaptureKind.LocationCapture, types);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            var sig = flow.Variables[this.VariablePath];
            var bundleDict = new Dictionary<IdentifierPath, LifetimeBounds>();

            foreach (var (relPath, type) in sig.Type.GetMembers(flow)) {
                var memPath = this.VariablePath.AppendMember(relPath);
                var locLifetime = flow.LocalLifetimes[memPath].LocationLifetime;
                var valueLifetime = flow.LocalLifetimes[memPath].ValueLifetime;

#if DEBUG
                if (type.IsValueType(flow) && valueLifetime != Lifetime.None) {
                    throw new Exception("Compiler bug");
                }
#endif

                bundleDict[relPath] = new LifetimeBounds(valueLifetime, locLifetime);
            }

            this.SetLifetimes(new LifetimeBundle(bundleDict), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            ICSyntax result = new CVariableLiteral(writer.GetVariableName(this.VariablePath));

            if (writer.VariableKinds[this.VariablePath] == CVariableKind.Allocated) {
                result = new CPointerDereference() {
                    Target = result
                };
            }

            if (this.isLValue) {
                result = new CAddressOf() {
                    Target = result
                };
            }

            return result;
        }
    }
}