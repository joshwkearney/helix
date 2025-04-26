using Helix.Analysis.Types;
using Helix.Syntax;
using System.Collections.Immutable;

namespace Helix.Analysis.TypeChecking {
    public record struct TypeCheckResult(ISyntax Syntax, TypeFrame Types) {
    }
    
    public record struct DeclarationTypeCheckResult(IDeclaration Syntax, TypeFrame Types) {
    }
    
    public class TypeFrame {
        private int tempCounter = 0;

        public IdentifierPath Scope { get; }

        public ImmutableDictionary<IdentifierPath, NominalType> Declarations { get; }
        
        public ImmutableDictionary<IdentifierPath, HelixType> Signatures { get; }
        
        public ImmutableHashSet<TypeFrame> BreakFrames { get; }
        
        public ImmutableHashSet<TypeFrame> ContinueFrames { get; }
        
        public TypeFrame() {
            this.Scope = new IdentifierPath();
            
            this.Declarations = ImmutableDictionary<IdentifierPath, NominalType>.Empty;
            this.Signatures = ImmutableDictionary<IdentifierPath, HelixType>.Empty;
            
            this.BreakFrames = ImmutableHashSet<TypeFrame>.Empty;
            this.ContinueFrames = ImmutableHashSet<TypeFrame>.Empty;
        }

        private TypeFrame(
            IdentifierPath scope,
            ImmutableDictionary<IdentifierPath, NominalType> decls, 
            ImmutableDictionary<IdentifierPath, HelixType> sigs, 
            ImmutableHashSet<TypeFrame> breakFrames,
            ImmutableHashSet<TypeFrame> continueFrames) {
            
            this.Scope = scope;
            this.Declarations = decls;
            this.Signatures = sigs;
            this.BreakFrames = breakFrames;
            this.ContinueFrames = continueFrames;
        }

        public TypeFrame WithScope(string newSegment) {
            return new TypeFrame(
                this.Scope.Append(newSegment), 
                this.Declarations, 
                this.Signatures,
                this.BreakFrames,
                this.ContinueFrames);
        }

        public TypeFrame PopScope() {
            var decls = this.Declarations;
            
            return new TypeFrame(
                this.Scope.Pop(),
                decls, 
                this.Signatures,
                this.BreakFrames,
                this.ContinueFrames);
        }

        public TypeFrame WithDeclaration(IdentifierPath path, NominalType type) {
            if (this.Declarations.ContainsKey(path)) {
                throw new InvalidOperationException($"This type frame already contains a declaration at the path '{path}'");
            }
            
            var decls = this.Declarations.Add(path, type);

            return new TypeFrame(
                this.Scope,
                decls, 
                this.Signatures,
                this.BreakFrames,
                this.ContinueFrames);
        }
        
        public TypeFrame WithSignature(IdentifierPath path, HelixType sig) {
            if (sig is NominalType) {
                throw new InvalidOperationException();
            }
            
            var sigs = this.Signatures.SetItem(path, sig);

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
                this.Signatures,
                this.BreakFrames.Add(types),
                this.ContinueFrames);
        }
        
        public TypeFrame WithContinueFrame(TypeFrame types) {
            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.Signatures,
                this.BreakFrames,
                this.ContinueFrames.Add(types));
        }
        
        public TypeFrame PopLoopFrames() {
            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.Signatures,
                this.BreakFrames.Clear(),
                this.ContinueFrames.Clear());
        }
        
        public string GetVariableName() {
            return "$t_" + this.tempCounter++;
        }

        public TypeFrame CombineSignaturesWith(TypeFrame other) {
            var sigs = this.Signatures;
            var keys = this.Signatures.Keys.Union(other.Signatures.Keys);

            foreach (var key in keys) {
                if (!this.Declarations.ContainsKey(key)) {
                    sigs = sigs.SetItem(key, other.Signatures[key]);
                    continue;
                }
                else if (!other.Declarations.ContainsKey(key)) {
                    sigs = sigs.SetItem(key, this.Signatures[key]);
                    continue;
                }
                
                var first = this.Signatures[key];
                var second = other.Signatures[key];

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

        public TypeFrame CombineBreakFramesWith(TypeFrame other) {
            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.Signatures,
                this.BreakFrames.Union(other.BreakFrames),
                this.ContinueFrames);
        }
        
        public TypeFrame CombineContinueFramesWith(TypeFrame other) {
            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.Signatures,
                this.BreakFrames,
                this.ContinueFrames.Union(other.ContinueFrames));
        }

        public bool DoSignaturesMatchWith(TypeFrame other) {
            if (this.Signatures.Count != other.Signatures.Count) {
                return false;
            }

            foreach (var (key, value) in this.Signatures) {
                if (!other.Signatures.TryGetValue(key, out var otherValue)) {
                    return false;
                }

                if (value != otherValue) {
                    return false;
                }
            }

            return true;
        }
    }
}