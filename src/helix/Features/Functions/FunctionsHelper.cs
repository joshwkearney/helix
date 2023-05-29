using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Collections;
using Helix.Features.Types;
using Helix.Features.Variables;
using Helix.Parsing;
using Helix.Syntax;
using System.IO;
using System.Runtime.CompilerServices;

namespace Helix.Features.Functions {
    public static class FunctionsHelper {
        public static void CheckForDuplicateParameters(TokenLocation loc, IEnumerable<string> pars) {
            var dups = pars
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            if (dups.Any()) {
                throw TypeException.IdentifierDefined(loc, dups.First());
            }
        }

        public static void DeclareName(FunctionParseSignature sig, TypeFrame types) {
            // Make sure this name isn't taken
            if (types.TryResolvePath(types.Scope, sig.Name, out _)) {
                throw TypeException.IdentifierDefined(sig.Location, sig.Name);
            }

            // Declare this function
            var path = types.Scope.Append(sig.Name);
            var named = new TypeSyntax(sig.Location, new NominalType(path, NominalTypeKind.Function));

            types.SyntaxValues = types.SyntaxValues.SetItem(path, named);
        }

        public static void DeclareParameterTypes(TokenLocation loc, FunctionType sig, IdentifierPath path, TypeFrame types) {            
            // Declare the parameters
            for (int i = 0; i < sig.Parameters.Count; i++) {
                var parsePar = sig.Parameters[i];
                var type = sig.Parameters[i].Type;
                var parPath = path.Append(parsePar.Name);

                if (parsePar.IsWritable) {
                    type = type.GetMutationSupertype(types);
                }

                // TODO: Fix iswritable here
                types.SyntaxValues = types.SyntaxValues.Add(
                    parPath,
                    new PointerType(type, parsePar.IsWritable).ToSyntax(loc));

                var varSig = new PointerType(type, parsePar.IsWritable);
                types.NominalSignatures = types.NominalSignatures.SetItem(parPath, varSig);

                // Declare this parameter as a root by making an end cycle in the graph
                foreach (var (relPath, memType) in type.GetMembers(types)) {
                    var memPath = parPath.Append(relPath);
                    var locationLifetime = new StackLocationLifetime(memPath, LifetimeOrigin.LocalLocation);
                    var valueLifetime = new ValueLifetime(memPath, LifetimeRole.Root, LifetimeOrigin.LocalValue, 0);
                }
            }
        }

        public static void DeclareParameterFlow(FunctionType sig, IdentifierPath path, TypeFrame flow) {
            // Declare the parameters
            for (int i = 0; i < sig.Parameters.Count; i++) {
                var parsePar = sig.Parameters[i];
                var parPath = path.Append(parsePar.Name);
                var type = sig.Parameters[i].Type;

                if (parsePar.IsWritable) {
                    type = type.GetMutationSupertype(flow);
                }

                var parLifetime = new ValueLifetime(
                    parPath, 
                    type.IsValueType(flow) ? LifetimeRole.Alias : LifetimeRole.Root, 
                    LifetimeOrigin.LocalValue);

                // Declare this parameter as a root by making an end cycle in the graph
                foreach (var (relPath, memType) in type.GetMembers(flow)) {
                    var memPath = parPath.Append(relPath);

                    var valueLifetime = new ValueLifetime(
                        memPath,
                        type.IsValueType(flow) ? LifetimeRole.Alias : LifetimeRole.Root, 
                        LifetimeOrigin.LocalValue);

                    var locationLifetime = new StackLocationLifetime(memPath, LifetimeOrigin.LocalLocation);

                    // Register our members
                    flow.DataFlowGraph.AddMember(parLifetime, valueLifetime);

                    // Make sure the value outlives the location
                    flow.DataFlowGraph.AddStored(valueLifetime, locationLifetime);

                    // Put these lifetimes in the main table
                    flow.LocalLifetimes = flow.LocalLifetimes.SetItem(
                        memPath, 
                        new LifetimeBounds(valueLifetime, locationLifetime));

                    flow.LifetimeRoots = flow.LifetimeRoots.Add(locationLifetime);
                    flow.LifetimeRoots = flow.LifetimeRoots.Add(valueLifetime);
                }
            }
        }

        public static void AnalyzeReturnValueFlow(
            TokenLocation loc,
            FunctionType sig, 
            ISyntaxTree body, 
            TypeFrame flow) {

            // Here we need to make sure that the return value can outlive the heap
            // It's ok if the return value doesn't currently outlive the heap because
            // its lifetime could be inferred. All we need to check for is if it
            // *could* outlive the heap once we instruct the lifetime graph that it
            // must outlive the heap. What we're going to do is check to see every
            // lifetime that has contributed to this lifetime, and confirm that they
            // are all either the heap or an inferred lifetime. This will make sure
            // that no roots other than the heap have allocated a part of the return
            // value.
            var incompatibleRoots = flow.DataFlowGraph.GetPrecursorLifetimes(body.GetLifetimes(flow).ValueLifetime)
                .Where(x => x.Role == LifetimeRole.Root)
                .Where(x => x != Lifetime.Heap)
                .ToValueSet();

            // Make sure all the roots outlive the heap
            if (incompatibleRoots.Any()) {
                throw new LifetimeException(
                   loc,
                   "Lifetime Inference Failed",
                   "This value cannot be returned from the function because the region it is allocated "
                   + "on might not outlive the function's return region. The problematic regions are: "
                   + $"'{string.Join(", ", incompatibleRoots)}'.\n\nTo fix this error, you can try implementing a '.copy()' method "
                   + $"on the type '{sig.ReturnType}' so that it can be moved between regions, "
                   + "or you can try adding explicit region annotations to the function's signature "
                   + "to help the compiler prove that this return value is safe.");
            }

            // Add a dependency between every returned lifetime and the heap
            flow.DataFlowGraph.AddStored(
                flow.SyntaxLifetimes[body].ValueLifetime, 
                Lifetime.Heap);
        }
    }
}