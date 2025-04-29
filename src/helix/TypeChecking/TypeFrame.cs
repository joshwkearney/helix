using System.Collections.Immutable;
using Helix.Types;

namespace Helix.TypeChecking;

public class TypeFrame {
    private int tempCounter = 0;

    public IdentifierPath Scope { get; }

    /// <summary>
    /// A list of nominal types that represent all the named values in a program.
    /// Declarations include structs, unions, functions, and variables, and they
    /// all have their own nominal type for referring to them.
    /// </summary>
    public ImmutableDictionary<IdentifierPath, NominalType> Declarations { get; }
        
    // TODO: Why are signatures the same as types? Shouldn't this be a separate 
    // concept?
    /// <summary>
    /// Each nominal type requires a signature to provide its semantics
    /// </summary>
    public ImmutableDictionary<IdentifierPath, HelixType> Signatures { get; }
        
    /// <summary>
    /// Refinements are more specific types that have been determined from context
    /// for a local variable. Refinements must always be bit-compatible with their
    /// signature type, and can be destroyed by assignments and the address of
    /// operator. Refinements are created by flow typing in if statements.
    /// </summary>
    public ImmutableDictionary<IdentifierPath, HelixType> Refinements { get; }
        
    public ImmutableHashSet<TypeFrame> BreakFrames { get; }
        
    public ImmutableHashSet<TypeFrame> ContinueFrames { get; }
        
    /// <summary>
    /// These variables might have their value changed through a pointer, and are
    /// therefore ineligible for type refinements.
    /// </summary>
    public ImmutableHashSet<IdentifierPath> OpaqueVariables { get; }
        
    public TypeFrame() {
        this.Scope = new IdentifierPath();
            
        this.Declarations = ImmutableDictionary<IdentifierPath, NominalType>.Empty;
        this.Signatures = ImmutableDictionary<IdentifierPath, HelixType>.Empty;
        this.Refinements = ImmutableDictionary<IdentifierPath, HelixType>.Empty;
            
        this.BreakFrames = ImmutableHashSet<TypeFrame>.Empty;
        this.ContinueFrames = ImmutableHashSet<TypeFrame>.Empty;
        this.OpaqueVariables = ImmutableHashSet<IdentifierPath>.Empty;
    }

    private TypeFrame(
        IdentifierPath scope,
        ImmutableDictionary<IdentifierPath, NominalType> decls, 
        ImmutableDictionary<IdentifierPath, HelixType> sigs, 
        ImmutableDictionary<IdentifierPath, HelixType> refinements, 
        ImmutableHashSet<TypeFrame> breakFrames,
        ImmutableHashSet<TypeFrame> continueFrames,
        ImmutableHashSet<IdentifierPath> opaqueVars) {
            
        this.Scope = scope;
        this.Declarations = decls;
        this.Signatures = sigs;
        this.Refinements = refinements;
        this.BreakFrames = breakFrames;
        this.ContinueFrames = continueFrames;
        this.OpaqueVariables = opaqueVars;
    }

    public TypeFrame WithScope(string newSegment) {
        return new TypeFrame(
            this.Scope.Append(newSegment), 
            this.Declarations, 
            this.Signatures,
            this.Refinements,
            this.BreakFrames,
            this.ContinueFrames,
            this.OpaqueVariables);
    }

    public TypeFrame PopScope() {
        var decls = this.Declarations;
            
        return new TypeFrame(
            this.Scope.Pop(),
            decls, 
            this.Signatures,
            this.Refinements,
            this.BreakFrames,
            this.ContinueFrames,
            this.OpaqueVariables);
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
            this.Refinements,
            this.BreakFrames,
            this.ContinueFrames,
            this.OpaqueVariables);
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
            this.Refinements,
            this.BreakFrames,
            this.ContinueFrames,
            this.OpaqueVariables);
    }
        
    public TypeFrame WithRefinement(IdentifierPath path, HelixType sig) {
        if (this.OpaqueVariables.Contains(path)) {
            return this;
        }
            
        return new TypeFrame(
            this.Scope, 
            this.Declarations, 
            this.Signatures,
            this.Refinements.SetItem(path, sig),
            this.BreakFrames,
            this.ContinueFrames,
            this.OpaqueVariables);
    }
        
    public TypeFrame PopValue(IdentifierPath path) {
        return new TypeFrame(
            this.Scope, 
            this.Declarations, 
            this.Signatures,
            this.Refinements.Remove(path),
            this.BreakFrames,
            this.ContinueFrames,
            this.OpaqueVariables.Add(path));
    }
        
    public TypeFrame WithBreakFrame(TypeFrame types) {
        return new TypeFrame(
            this.Scope, 
            this.Declarations, 
            this.Signatures,
            this.Refinements,
            this.BreakFrames.Add(types),
            this.ContinueFrames,
            this.OpaqueVariables);
    }
        
    public TypeFrame WithContinueFrame(TypeFrame types) {
        return new TypeFrame(
            this.Scope, 
            this.Declarations, 
            this.Signatures,
            this.Refinements,
            this.BreakFrames,
            this.ContinueFrames.Add(types),
            this.OpaqueVariables);
    }
        
    public TypeFrame PopLoopFrames() {
        return new TypeFrame(
            this.Scope, 
            this.Declarations, 
            this.Signatures,
            this.Refinements,
            this.BreakFrames.Clear(),
            this.ContinueFrames.Clear(),
            this.OpaqueVariables);
    }
        
    public string GetVariableName() {
        return "$t_" + this.tempCounter++;
    }

    public TypeFrame CombineRefinementsWith(TypeFrame other) {
        var values = this.Refinements;
        var keys = this.Refinements.Keys.Union(other.Refinements.Keys);
        var opaque = this.OpaqueVariables.Union(other.OpaqueVariables);

        foreach (var key in keys) {
            if (opaque.Contains(key)) {
                values = values.Remove(key);
                continue;
            }
            else if (!this.Declarations.ContainsKey(key)) {
                values = values.SetItem(key, other.Refinements[key]);
                continue;
            }
            else if (!other.Declarations.ContainsKey(key)) {
                values = values.SetItem(key, this.Refinements[key]);
                continue;
            }
                
            var first = this.Refinements[key];
            var second = other.Refinements[key];

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
            this.Refinements,
            this.BreakFrames.Union(other.BreakFrames),
            this.ContinueFrames.Union(other.ContinueFrames),
            this.OpaqueVariables);
    }

    public bool DoValuesMatchWith(TypeFrame other) {
        if (this.Refinements.Count != other.Refinements.Count) {
            return false;
        }

        foreach (var (key, value) in this.Refinements) {
            if (!other.Refinements.TryGetValue(key, out var otherValue)) {
                return false;
            }

            if (value != otherValue) {
                return false;
            }
        }

        return true;
    }
}