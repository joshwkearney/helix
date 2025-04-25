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
        
        public ImmutableHashSet<TypeFrame> BreakFrames { get; }
        
        public ImmutableHashSet<TypeFrame> ContinueFrames { get; }
        
        public TypeFrame() {
            this.Scope = new IdentifierPath();
            
            this.Declarations = ImmutableDictionary<IdentifierPath, DeclarationInfo>.Empty;
            this.NominalSignatures = ImmutableDictionary<IdentifierPath, HelixType>.Empty;
            
            this.BreakFrames = ImmutableHashSet<TypeFrame>.Empty;
            this.ContinueFrames = ImmutableHashSet<TypeFrame>.Empty;

            this.Declarations = this.Declarations.Add(
                new IdentifierPath("void"),
                new DeclarationInfo(DeclarationKind.Type, PrimitiveType.Void));

            this.Declarations = this.Declarations.Add(
                new IdentifierPath("word"),
                new DeclarationInfo(DeclarationKind.Type, PrimitiveType.Word));

            this.Declarations = this.Declarations.Add(
                new IdentifierPath("bool"),
                new DeclarationInfo(DeclarationKind.Type, PrimitiveType.Bool));
        }

        private TypeFrame(
            IdentifierPath scope,
            ImmutableDictionary<IdentifierPath, DeclarationInfo> decls, 
            ImmutableDictionary<IdentifierPath, HelixType> sigs, 
            ImmutableHashSet<TypeFrame> breakFrames,
            ImmutableHashSet<TypeFrame> continueFrames) {
            
            this.Scope = scope;
            this.Declarations = decls;
            this.NominalSignatures = sigs;
            this.BreakFrames = breakFrames;
            this.ContinueFrames = continueFrames;
        }

        public TypeFrame WithScope(string newSegment) {
            return new TypeFrame(
                this.Scope.Append(newSegment), 
                this.Declarations, 
                this.NominalSignatures,
                this.BreakFrames,
                this.ContinueFrames);
        }

        public TypeFrame PopScope() {
            var decls = this.Declarations;
            
            return new TypeFrame(
                this.Scope.Pop(),
                decls, 
                this.NominalSignatures,
                this.BreakFrames,
                this.ContinueFrames);
        }

        public TypeFrame WithDeclaration(IdentifierPath path, DeclarationInfo info) {
            if (this.Declarations.ContainsKey(path)) {
                throw new InvalidOperationException($"This type frame already contains a declaration at the path '{path}'");
            }
            
            var decls = this.Declarations.Add(path, info);

            return new TypeFrame(
                this.Scope,
                decls, 
                this.NominalSignatures,
                this.BreakFrames,
                this.ContinueFrames);
        }
        
        public TypeFrame WithDeclaration(IdentifierPath path, DeclarationKind kind, HelixType type) {
            return this.WithDeclaration(path, new DeclarationInfo(kind, type));
        }
        
        public TypeFrame WithNominalSignature(IdentifierPath path, HelixType sig) {
            if (sig is NominalType) {
                throw new InvalidOperationException();
            }
            
            var sigs = this.NominalSignatures.SetItem(path, sig);

            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                sigs,
                this.BreakFrames,
                this.ContinueFrames);
        }
        
        public TypeFrame WithBreakFrame(TypeFrame types) {
            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.NominalSignatures,
                this.BreakFrames.Add(types),
                this.ContinueFrames);
        }
        
        public TypeFrame WithContinueFrame(TypeFrame types) {
            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.NominalSignatures,
                this.BreakFrames,
                this.ContinueFrames.Add(types));
        }
        
        public TypeFrame ClearLoopFrames() {
            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.NominalSignatures,
                this.BreakFrames.Clear(),
                this.ContinueFrames.Clear());
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
                
                var first = this.NominalSignatures[key];
                var second = other.NominalSignatures[key];

                if (!first.CanUnifyFrom(second, this, out var result)) {
                    throw new InvalidCastException();
                }
                
                sigs = sigs.SetItem(key, result);
            }

            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                sigs,
                this.BreakFrames,
                this.ContinueFrames);
        }
    }
}