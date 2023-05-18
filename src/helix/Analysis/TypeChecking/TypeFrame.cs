using Helix.Syntax;
using Helix.Analysis.Flow;
using Helix.Analysis.Types;
using Helix.Features;
using Helix.Features.Aggregates;
using Helix.Features.Functions;
using Helix.Generation;

namespace Helix.Analysis.TypeChecking {
    public delegate void DeclarationCG(ICWriter writer);

    public class TypeFrame : ITypedFrame {
        private int tempCounter = 0;

        // Frame-specific things
        public IDictionary<IdentifierPath, ISyntaxTree> SyntaxValues { get; }

        public ISet<Lifetime> LifetimeRoots { get; }

        // Global things
        public IDictionary<IdentifierPath, VariableSignature> Variables { get; }

        public IDictionary<IdentifierPath, FunctionSignature> Functions { get; }

        public IDictionary<IdentifierPath, StructSignature> Structs { get; }

        public IDictionary<HelixType, DeclarationCG> TypeDeclarations { get; }

        public IDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

        public TypeFrame() {
            Variables = new Dictionary<IdentifierPath, VariableSignature>();
            LifetimeRoots = new HashSet<Lifetime>();

            SyntaxValues = new Dictionary<IdentifierPath, ISyntaxTree>() {
                { new IdentifierPath("void"), new TypeSyntax(default, PrimitiveType.Void) },
                { new IdentifierPath("int"), new TypeSyntax(default, PrimitiveType.Int) },
                { new IdentifierPath("bool"), new TypeSyntax(default, PrimitiveType.Bool) }
            };

            Functions = new Dictionary<IdentifierPath, FunctionSignature>();
            Structs = new Dictionary<IdentifierPath, StructSignature>();

            TypeDeclarations = new Dictionary<HelixType, DeclarationCG>();
            ReturnTypes = new Dictionary<ISyntaxTree, HelixType>();
        }

        public TypeFrame(TypeFrame prev) {
            Variables = prev.Variables; //new StackedDictionary<IdentifierPath, VariableSignature>(prev.Variables);
            SyntaxValues = new StackedDictionary<IdentifierPath, ISyntaxTree>(prev.SyntaxValues);
            LifetimeRoots = new StackedSet<Lifetime>(prev.LifetimeRoots);

            Functions = prev.Functions;
            Structs = prev.Structs;

            TypeDeclarations = prev.TypeDeclarations;
            ReturnTypes = prev.ReturnTypes;
        }

        public string GetVariableName() {
            return "$t_" + tempCounter++;
        }

        public bool TryResolvePath(IdentifierPath scope, string name, out IdentifierPath path) {
            while (true) {
                path = scope.Append(name);
                if (SyntaxValues.ContainsKey(path)) {
                    return true;
                }

                if (scope.Segments.Any()) {
                    scope = scope.Pop();
                }
                else {
                    return false;
                }
            }
        }

        public IdentifierPath ResolvePath(IdentifierPath scope, string path) {
            if (TryResolvePath(scope, path, out var value)) {
                return value;
            }

            throw new InvalidOperationException(
                $"Compiler error: The path '{path}' does not contain a value.");
        }

        public bool TryResolveName(IdentifierPath scope, string name, out ISyntaxTree value) {
            if (!TryResolvePath(scope, name, out var path)) {
                value = null;
                return false;
            }

            return SyntaxValues.TryGetValue(path, out value);
        }

        public ISyntaxTree ResolveName(IdentifierPath scope, string name) {
            return SyntaxValues[ResolvePath(scope, name)];
        }

        public void DeclareLocationLifetimeRoots(IdentifierPath basePath, HelixType baseType, LifetimeRole role) {
            foreach (var (relPath, type) in baseType.GetMembers(this)) {
                if (type.IsValueType(this)) {
                    continue;
                }

                var memPath = basePath.AppendMember(relPath);

                // Even though the lifetime of the variable itself will be inferred, the lifetime
                // of the value stored in that variable is NOT inferred. 
                var locationLifetime = new Lifetime(
                    memPath, 
                    0, 
                    LifetimeTarget.Location, 
                    role);

                // Add this variable's lifetime
                this.LifetimeRoots.Add(locationLifetime);
            }
        }

        public void DeclareValueLifetimeRoots(IdentifierPath basePath, HelixType baseType, LifetimeRole role) {
            foreach (var (relPath, type) in baseType.GetMembers(this)) {
                if (type.IsValueType(this)) {
                    continue;
                }

                var memPath = basePath.AppendMember(relPath);

                // Even though the lifetime of the variable itself will be inferred, the lifetime
                // of the value stored in that variable is NOT inferred. 
                var valueLifetime = new Lifetime(
                    memPath, 
                    0, 
                    LifetimeTarget.StoredValue, 
                    role);

                // Add this variable's lifetime
                this.LifetimeRoots.Add(valueLifetime);
            }
        }
        public void DeclareVariableSignatures(IdentifierPath basePath, HelixType baseType, bool isWritable) {
            foreach (var (compPath, compType) in baseType.GetMembers(this)) {
                var path = basePath.Append(compPath);
                var sig = new VariableSignature(path, compType, isWritable);

                // Add this variable's lifetime
                this.Variables[path] = sig;
            }
        }

    }
}