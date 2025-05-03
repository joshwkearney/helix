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

    public ImmutableDictionary<IdentifierPath, FunctionSignature> FunctionSignatures { get; }

    public ImmutableDictionary<IdentifierPath, StructSignature> StructSignatures { get; }

    public ImmutableDictionary<IdentifierPath, UnionSignature> UnionSignatures { get; }

    /// <summary>
    /// Refinements are more specific types that have been determined from context
    /// for a local variable. Refinements must always be bit-compatible with their
    /// signature type, and can be destroyed by assignments and the address of
    /// operator. Refinements are created by flow typing in if statements.
    /// </summary>
    public ImmutableDictionary<IdentifierPath, HelixType> VariableRefinements { get; }
        
    public ImmutableHashSet<TypeFrame> BreakFrames { get; }
        
    public ImmutableHashSet<TypeFrame> ContinueFrames { get; }
        
    /// <summary>
    /// These variables might have their value changed through a pointer
    /// </summary>
    public ImmutableHashSet<IdentifierPath> PromotedVariables { get; }
        
    public TypeFrame() {
        this.Scope = new IdentifierPath();            
        this.Declarations = ImmutableDictionary<IdentifierPath, NominalType>.Empty;

        this.FunctionSignatures = ImmutableDictionary<IdentifierPath, FunctionSignature>.Empty;
        this.StructSignatures = ImmutableDictionary<IdentifierPath, StructSignature>.Empty;
        this.UnionSignatures = ImmutableDictionary<IdentifierPath, UnionSignature>.Empty;
        this.VariableRefinements = ImmutableDictionary<IdentifierPath, HelixType>.Empty;
            
        this.BreakFrames = ImmutableHashSet<TypeFrame>.Empty;
        this.ContinueFrames = ImmutableHashSet<TypeFrame>.Empty;
        this.PromotedVariables = ImmutableHashSet<IdentifierPath>.Empty;
    }

    private TypeFrame(
        IdentifierPath scope,
        ImmutableDictionary<IdentifierPath, NominalType> decls, 
        ImmutableDictionary<IdentifierPath, FunctionSignature> functionSigs,
        ImmutableDictionary<IdentifierPath, StructSignature> structSigs,
        ImmutableDictionary<IdentifierPath, UnionSignature> unionSigs,
        ImmutableDictionary<IdentifierPath, HelixType> refinements, 
        ImmutableHashSet<TypeFrame> breakFrames,
        ImmutableHashSet<TypeFrame> continueFrames,
        ImmutableHashSet<IdentifierPath> opaqueVars) {
            
        this.Scope = scope;
        this.Declarations = decls;
        this.FunctionSignatures = functionSigs;
        this.StructSignatures = structSigs;
        this.UnionSignatures = unionSigs;
        this.VariableRefinements = refinements;
        this.BreakFrames = breakFrames;
        this.ContinueFrames = continueFrames;
        this.PromotedVariables = opaqueVars;
    }

    public TypeFrame WithScope(string newSegment) {
        return new TypeFrame(
            this.Scope.Append(newSegment), 
            this.Declarations, 
            this.FunctionSignatures,
            this.StructSignatures,
            this.UnionSignatures,
            this.VariableRefinements,
            this.BreakFrames,
            this.ContinueFrames,
            this.PromotedVariables);
    }

    public TypeFrame PopScope() {
        var decls = this.Declarations;
            
        return new TypeFrame(
            this.Scope.Pop(),
            decls,
            this.FunctionSignatures,
            this.StructSignatures,
            this.UnionSignatures,
            this.VariableRefinements,
            this.BreakFrames,
            this.ContinueFrames,
            this.PromotedVariables);
    }

    public TypeFrame WithDeclaration(IdentifierPath path, NominalType type) {
        if (this.Declarations.ContainsKey(path)) {
            throw new InvalidOperationException($"This type frame already contains a declaration at the path '{path}'");
        }
            
        var decls = this.Declarations.Add(path, type);

        return new TypeFrame(
            this.Scope,
            decls,
            this.FunctionSignatures,
            this.StructSignatures,
            this.UnionSignatures,
            this.VariableRefinements,
            this.BreakFrames,
            this.ContinueFrames,
            this.PromotedVariables);
    }
        
    public TypeFrame WithSignature(IdentifierPath path, FunctionSignature sig) {
        return new TypeFrame(
            this.Scope, 
            this.Declarations, 
            this.FunctionSignatures.Add(path, sig),
            this.StructSignatures,
            this.UnionSignatures,
            this.VariableRefinements,
            this.BreakFrames,
            this.ContinueFrames,
            this.PromotedVariables);
    }

    public TypeFrame WithSignature(IdentifierPath path, StructSignature sig) {
        return new TypeFrame(
            this.Scope,
            this.Declarations,
            this.FunctionSignatures,
            this.StructSignatures.Add(path, sig),
            this.UnionSignatures,
            this.VariableRefinements,
            this.BreakFrames,
            this.ContinueFrames,
            this.PromotedVariables);
    }

    public TypeFrame WithSignature(IdentifierPath path, UnionSignature sig) {
        return new TypeFrame(
            this.Scope,
            this.Declarations,
            this.FunctionSignatures,
            this.StructSignatures,
            this.UnionSignatures.Add(path, sig),
            this.VariableRefinements,
            this.BreakFrames,
            this.ContinueFrames,
            this.PromotedVariables);
    }

    public TypeFrame WithVariableRefinement(IdentifierPath path, HelixType sig) {
        if (this.PromotedVariables.Contains(path)) {
            return this;
        }
            
        return new TypeFrame(
            this.Scope, 
            this.Declarations,
            this.FunctionSignatures,
            this.StructSignatures,
            this.UnionSignatures,
            this.VariableRefinements.SetItem(path, sig),
            this.BreakFrames,
            this.ContinueFrames,
            this.PromotedVariables);
    }
        
    public TypeFrame WithVariablePromotion(IdentifierPath path) {
        var type = this.VariableRefinements[path];
        var superType = type.GetSupertype(this);

        // We need to make the refinement as general as possible
        while (type != superType) {
            type = superType;
            superType = type.GetSupertype(this);
        }

        return new TypeFrame(
            this.Scope, 
            this.Declarations,
            this.FunctionSignatures,
            this.StructSignatures,
            this.UnionSignatures,
            this.VariableRefinements.SetItem(path, type),
            this.BreakFrames,
            this.ContinueFrames,
            this.PromotedVariables.Add(path));
    }
        
    public TypeFrame WithBreakFrame(TypeFrame types) {
        return new TypeFrame(
            this.Scope, 
            this.Declarations,
            this.FunctionSignatures,
            this.StructSignatures,
            this.UnionSignatures,
            this.VariableRefinements,
            this.BreakFrames.Add(types),
            this.ContinueFrames,
            this.PromotedVariables);
    }
        
    public TypeFrame WithContinueFrame(TypeFrame types) {
        return new TypeFrame(
            this.Scope, 
            this.Declarations,
            this.FunctionSignatures,
            this.StructSignatures,
            this.UnionSignatures,
            this.VariableRefinements,
            this.BreakFrames,
            this.ContinueFrames.Add(types),
            this.PromotedVariables);
    }
        
    public TypeFrame PopLoopFrames() {
        return new TypeFrame(
            this.Scope, 
            this.Declarations,
            this.FunctionSignatures,
            this.StructSignatures,
            this.UnionSignatures,
            this.VariableRefinements,
            this.BreakFrames.Clear(),
            this.ContinueFrames.Clear(),
            this.PromotedVariables);
    }
        
    public string GetVariableName() {
        return "$t_" + this.tempCounter++;
    }

    public TypeFrame CombineRefinementsWith(TypeFrame other) {
        var values = this.VariableRefinements;
        var keys = this.VariableRefinements.Keys.Union(other.VariableRefinements.Keys);
        var opaque = this.PromotedVariables.Union(other.PromotedVariables);

        foreach (var key in keys) {
            if (opaque.Contains(key)) {
                values = values.Remove(key);
                continue;
            }
            else if (!this.Declarations.ContainsKey(key)) {
                values = values.SetItem(key, other.VariableRefinements[key]);
                continue;
            }
            else if (!other.Declarations.ContainsKey(key)) {
                values = values.SetItem(key, this.VariableRefinements[key]);
                continue;
            }
                
            var first = this.VariableRefinements[key];
            var second = other.VariableRefinements[key];

            if (!first.CanUnifyFrom(second, this, out var result)) {
                throw new InvalidCastException();
            }
                
            values = values.SetItem(key, result);
        }

        return new TypeFrame(
            this.Scope, 
            this.Declarations,
            this.FunctionSignatures,
            this.StructSignatures,
            this.UnionSignatures,
            values,
            this.BreakFrames,
            this.ContinueFrames,
            opaque);
    }

    public TypeFrame CombineLoopFramesWith(TypeFrame other) {
        return new TypeFrame(
            this.Scope, 
            this.Declarations,
            this.FunctionSignatures,
            this.StructSignatures,
            this.UnionSignatures,
            this.VariableRefinements,
            this.BreakFrames.Union(other.BreakFrames),
            this.ContinueFrames.Union(other.ContinueFrames),
            this.PromotedVariables);
    }

    public bool DoValuesMatchWith(TypeFrame other) {
        if (this.VariableRefinements.Count != other.VariableRefinements.Count) {
            return false;
        }

        foreach (var (key, value) in this.VariableRefinements) {
            if (!other.VariableRefinements.TryGetValue(key, out var otherValue)) {
                return false;
            }

            if (value != otherValue) {
                return false;
            }
        }

        return true;
    }
}