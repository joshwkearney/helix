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
        
        public ImmutableDictionary<IdentifierPath, HelixType> Values { get; }
        
        public ImmutableHashSet<TypeFrame> BreakFrames { get; }
        
        public ImmutableHashSet<TypeFrame> ContinueFrames { get; }
        
        public ImmutableHashSet<IdentifierPath> AllocatedVariables { get; }
        
        public TypeFrame() {
            this.Scope = new IdentifierPath();
            
            this.Declarations = ImmutableDictionary<IdentifierPath, NominalType>.Empty;
            this.Signatures = ImmutableDictionary<IdentifierPath, HelixType>.Empty;
            this.Values = ImmutableDictionary<IdentifierPath, HelixType>.Empty;
            
            this.BreakFrames = ImmutableHashSet<TypeFrame>.Empty;
            this.ContinueFrames = ImmutableHashSet<TypeFrame>.Empty;
            this.AllocatedVariables = ImmutableHashSet<IdentifierPath>.Empty;
        }

        private TypeFrame(
            IdentifierPath scope,
            ImmutableDictionary<IdentifierPath, NominalType> decls, 
            ImmutableDictionary<IdentifierPath, HelixType> sigs, 
            ImmutableDictionary<IdentifierPath, HelixType> values, 
            ImmutableHashSet<TypeFrame> breakFrames,
            ImmutableHashSet<TypeFrame> continueFrames,
            ImmutableHashSet<IdentifierPath> allocatedVars) {
            
            this.Scope = scope;
            this.Declarations = decls;
            this.Signatures = sigs;
            this.Values = values;
            this.BreakFrames = breakFrames;
            this.ContinueFrames = continueFrames;
            this.AllocatedVariables = allocatedVars;
        }

        public TypeFrame WithScope(string newSegment) {
            return new TypeFrame(
                this.Scope.Append(newSegment), 
                this.Declarations, 
                this.Signatures,
                this.Values,
                this.BreakFrames,
                this.ContinueFrames,
                this.AllocatedVariables);
        }

        public TypeFrame PopScope() {
            var decls = this.Declarations;
            
            return new TypeFrame(
                this.Scope.Pop(),
                decls, 
                this.Signatures,
                this.Values,
                this.BreakFrames,
                this.ContinueFrames,
                this.AllocatedVariables);
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
                this.Values,
                this.BreakFrames,
                this.ContinueFrames,
                this.AllocatedVariables);
        }
        
        public TypeFrame WithSignature(IdentifierPath path, HelixType sig) {
            if (sig is NominalType) {
                throw new InvalidOperationException();
            }
            
            if (this.Signatures.ContainsKey(path)) {
                throw new InvalidOperationException($"This type frame already contains a signature at the path '{path}'");
            }
            
            var sigs = this.Signatures.Add(path, sig);

            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                sigs,
                this.Values,
                this.BreakFrames,
                this.ContinueFrames,
                this.AllocatedVariables);
        }
        
        public TypeFrame WithValue(IdentifierPath path, HelixType sig) {
            if (this.AllocatedVariables.Contains(path)) {
                return this;
            }
            
            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.Signatures,
                this.Values.SetItem(path, sig),
                this.BreakFrames,
                this.ContinueFrames,
                this.AllocatedVariables);
        }
        
        public TypeFrame PopValue(IdentifierPath path) {
            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.Signatures,
                this.Values.Remove(path),
                this.BreakFrames,
                this.ContinueFrames,
                this.AllocatedVariables.Add(path));
        }
        
        public TypeFrame WithBreakFrame(TypeFrame types) {
            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.Signatures,
                this.Values,
                this.BreakFrames.Add(types),
                this.ContinueFrames,
                this.AllocatedVariables);
        }
        
        public TypeFrame WithContinueFrame(TypeFrame types) {
            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.Signatures,
                this.Values,
                this.BreakFrames,
                this.ContinueFrames.Add(types),
                this.AllocatedVariables);
        }
        
        public TypeFrame PopLoopFrames() {
            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.Signatures,
                this.Values,
                this.BreakFrames.Clear(),
                this.ContinueFrames.Clear(),
                this.AllocatedVariables);
        }
        
        public string GetVariableName() {
            return "$t_" + this.tempCounter++;
        }

        public TypeFrame CombineValuesWith(TypeFrame other) {
            var values = this.Values;
            var keys = this.Values.Keys.Union(other.Values.Keys);
            var opaque = this.AllocatedVariables.Union(other.AllocatedVariables);

            foreach (var key in keys) {
                if (opaque.Contains(key)) {
                    values = values.Remove(key);
                    continue;
                }
                else if (!this.Declarations.ContainsKey(key)) {
                    values = values.SetItem(key, other.Values[key]);
                    continue;
                }
                else if (!other.Declarations.ContainsKey(key)) {
                    values = values.SetItem(key, this.Values[key]);
                    continue;
                }
                
                var first = this.Values[key];
                var second = other.Values[key];

                if (!first.CanUnifyFrom(second, this, out var result)) {
                    throw new InvalidCastException();
                }
                
                values = values.SetItem(key, result);
            }

            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.Signatures,
                values,
                this.BreakFrames,
                this.ContinueFrames,
                opaque);
        }

        public TypeFrame CombineLoopFramesWith(TypeFrame other) {
            return new TypeFrame(
                this.Scope, 
                this.Declarations, 
                this.Signatures,
                this.Values,
                this.BreakFrames.Union(other.BreakFrames),
                this.ContinueFrames.Union(other.ContinueFrames),
                this.AllocatedVariables);
        }

        public bool DoValuesMatchWith(TypeFrame other) {
            if (this.Values.Count != other.Values.Count) {
                return false;
            }

            foreach (var (key, value) in this.Values) {
                if (!other.Values.TryGetValue(key, out var otherValue)) {
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