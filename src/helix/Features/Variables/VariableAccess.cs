using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
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

namespace Helix {
    public record VariableAccessParseSyntax : ISyntaxTree {
        public string Name { get; }

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public VariableAccessParseSyntax(TokenLocation location, string name) {
            this.Location = location;
            this.Name = name;
        }

        public Option<HelixType> AsType(SyntaxFrame types) {
            // If we're pointing at a type then return it
            if (types.TryResolveName(this.Location.Scope, this.Name, out var syntax)) {
                if (syntax.AsType(types).TryGetValue(out var type)) {
                    return type;
                }
            }

            return Option.None;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
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

                // The lifetime of the value of a variable is dependent on
                // what was stored in that variable, and the variable itself.
                // It is important to capture this variable's current lifetime
                // plus this variable's path because this variable's value could
                // change in the future, so we need to get an accurate snapshot 
                // of the current value
                types.ReturnTypes[result] = varSig.Type;
                types.Lifetimes[result] = new[] { new Lifetime(path, varSig.MutationCount, varSig.IsLifetimeRoot) };

                return result;
            }

            if (types.Functions.ContainsKey(path)) {
                var result = new VariableAccessSyntax(this.Location, path);

                types.ReturnTypes[result] = new NamedType(path);
                types.Lifetimes[result] = Array.Empty<Lifetime>();

                return result;
            }

            throw TypeCheckingErrors.VariableUndefined(this.Location, this.Name);
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record VariableAccessSyntax : ISyntaxTree, ILValue {
        private readonly IdentifierPath variablePath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public bool IsLocal => true;

        public VariableAccessSyntax(TokenLocation loc, IdentifierPath path) {
            this.Location = loc;
            this.variablePath = path;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ILValue ToLValue(SyntaxFrame types) {
            // Make sure this variable is writable
            if (!types.Variables[this.variablePath].IsWritable) {
                throw TypeCheckingErrors.WritingToConstVariable(this.Location);
            }

            return this;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.variablePath);

            return new CVariableLiteral(name);
        }
    }
}