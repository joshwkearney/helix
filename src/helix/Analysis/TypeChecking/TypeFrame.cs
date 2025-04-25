using Helix.Analysis.Types;
using System.Collections.Immutable;
using Helix.Syntax;

namespace Helix.Analysis.TypeChecking {
    public enum DeclarationKind {
        Type, Function, Parameter, Variable
    }
    
    public record struct DeclarationInfo(DeclarationKind Kind, HelixType Type) {
    }

    public record struct TypeCheckResult(ISyntax Syntax, TypeFrame Types) {
    }
    
    public class TypeFrame {
        private int tempCounter = 0;

        public IdentifierPath Scope { get; }

        public ImmutableDictionary<IdentifierPath, DeclarationInfo> Declarations { get; }
        
        public TypeFrame() {
            this.Declarations = ImmutableDictionary<IdentifierPath, DeclarationInfo>.Empty;

            this.Declarations = this.Declarations.Add(
                new IdentifierPath("void"),
                new DeclarationInfo(DeclarationKind.Type, PrimitiveType.Void));

            this.Declarations = this.Declarations.Add(
                new IdentifierPath("word"),
                new DeclarationInfo(DeclarationKind.Type, PrimitiveType.Word));

            this.Declarations = this.Declarations.Add(
                new IdentifierPath("bool"),
                new DeclarationInfo(DeclarationKind.Type, PrimitiveType.Bool));

            this.Scope = new IdentifierPath();
        }

        private TypeFrame(ImmutableDictionary<IdentifierPath, DeclarationInfo> decls, IdentifierPath scope) {
            this.Declarations = decls;
            this.Scope = scope;
        }

        public TypeFrame WithScope(string newSegment) {
            return new TypeFrame(this.Declarations, this.Scope.Append(newSegment));
        }

        public TypeFrame PopScope() {
            var decls = this.Declarations;

            foreach (var (path, _) in this.Declarations) {
                if (path.StartsWith(this.Scope)) {
                    decls = decls.Remove(path);
                }
            }

            return new TypeFrame(decls, this.Scope.Pop());
        }

        public TypeFrame WithDeclaration(IdentifierPath path, DeclarationInfo info) {
            var decls = this.Declarations.SetItem(path, info);

            return new TypeFrame(decls, this.Scope);
        }
        
        public TypeFrame WithDeclaration(IdentifierPath path, DeclarationKind kind, HelixType type) {
            return this.WithDeclaration(path, new DeclarationInfo(kind, type));
        }

        public string GetVariableName() {
            return "$t_" + this.tempCounter++;
        }

        public TypeFrame CombineWith(TypeFrame other) {
            if (this.Scope != other.Scope) {
                throw new InvalidOperationException();
            }

            var decls = this.Declarations;
            var keys = this.Declarations.Keys.Union(other.Declarations.Keys);

            foreach (var key in keys) {
                if (!this.Declarations.ContainsKey(key)) {
                    decls = decls.SetItem(key, other.Declarations[key]);
                    continue;
                }
                else if (!other.Declarations.ContainsKey(key)) {
                    decls = decls.SetItem(key, this.Declarations[key]);
                    continue;
                }
                
                var first = this.Declarations[key];
                var second = other.Declarations[key];

                if (first.Kind != second.Kind) {
                    throw new InvalidOperationException();
                }

                if (!first.Type.CanUnifyFrom(second.Type, this, out var result)) {
                    throw new InvalidCastException();
                }
                
                decls = decls.SetItem(key, new DeclarationInfo(first.Kind, result));
            }

            return new TypeFrame(decls, this.Scope);
        }
    }
}