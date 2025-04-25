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
    
    public record struct DeclarationTypeCheckResult(IDeclaration Syntax, TypeFrame Types) {
    }
    
    public class TypeFrame {
        private int tempCounter = 0;

        public IdentifierPath Scope { get; }

        public ImmutableDictionary<IdentifierPath, DeclarationInfo> Declarations { get; }
        
        public ImmutableDictionary<IdentifierPath, HelixType> NominalSignatures { get; }
        
        public TypeFrame() {
            this.Declarations = ImmutableDictionary<IdentifierPath, DeclarationInfo>.Empty;
            this.NominalSignatures = ImmutableDictionary<IdentifierPath, HelixType>.Empty;

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

        private TypeFrame(
            ImmutableDictionary<IdentifierPath, DeclarationInfo> decls, 
            ImmutableDictionary<IdentifierPath, HelixType> sigs, 
            IdentifierPath scope) {
            this.Declarations = decls;
            this.NominalSignatures = sigs;
            this.Scope = scope;
        }

        public TypeFrame WithScope(string newSegment) {
            return new TypeFrame(this.Declarations, this.NominalSignatures, this.Scope.Append(newSegment));
        }

        public TypeFrame PopScope() {
            var decls = this.Declarations;

            foreach (var (path, _) in this.Declarations) {
                if (path.StartsWith(this.Scope)) {
                    //decls = decls.Remove(path);
                }
            }

            return new TypeFrame(decls, this.NominalSignatures, this.Scope.Pop());
        }

        public TypeFrame WithDeclaration(IdentifierPath path, DeclarationInfo info) {
            if (this.Declarations.ContainsKey(path)) {
                throw new InvalidOperationException($"This type frame already contains a declaration at the path '{path}'");
            }
            
            var decls = this.Declarations.Add(path, info);

            return new TypeFrame(decls, this.NominalSignatures, this.Scope);
        }
        
        public TypeFrame WithDeclaration(IdentifierPath path, DeclarationKind kind, HelixType type) {
            return this.WithDeclaration(path, new DeclarationInfo(kind, type));
        }
        
        public TypeFrame WithNominalSignature(IdentifierPath path, HelixType sig) {
            var sigs = this.NominalSignatures.SetItem(path, sig);

            return new TypeFrame(this.Declarations, sigs, this.Scope);
        }

        public string GetVariableName() {
            return "$t_" + this.tempCounter++;
        }

        public TypeFrame CombineSignaturesWith(TypeFrame other) {
            var sigs = this.NominalSignatures;
            var keys = this.NominalSignatures.Keys.Union(other.NominalSignatures.Keys);

            foreach (var key in keys) {
                if (!this.Declarations.ContainsKey(key)) {
                    sigs = sigs.SetItem(key, other.NominalSignatures[key]);
                    continue;
                }
                else if (!other.Declarations.ContainsKey(key)) {
                    sigs = sigs.SetItem(key, this.NominalSignatures[key]);
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
                
                sigs = sigs.SetItem(key, result);
            }

            return new TypeFrame(this.Declarations, sigs, this.Scope);
        }
    }
}