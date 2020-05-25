using System.Collections.Immutable;
using Attempt19.TypeChecking;
using Attempt19.CodeGeneration;
using Attempt19.Parsing;
using Attempt19.Types;
using Attempt19.Features.Variables;
using System.Linq;

namespace Attempt19 {
    public static partial class SyntaxFactory {
        public static Syntax MakeVariableMove(string name, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new VariableMoveData() {
                    VariableName = name,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(VariableMoveTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Variables {
    public class VariableMoveData : VariableAccessBase { }

    public static class VariableMoveTransformations {
        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope, NameCache names) {
            var access = (VariableMoveData)data;

            // Set containing scope
            access.ContainingScope = scope;

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var access = (VariableMoveData)data;

            // Make sure this name exists
            if (!names.FindName(access.ContainingScope, access.VariableName, out var varpath, out var target)) {
                throw TypeCheckingErrors.VariableUndefined(access.Location, access.VariableName);
            }

            // Make sure this name is a variable
            if (target != NameTarget.Variable) {
                throw TypeCheckingErrors.VariableUndefined(access.Location, access.VariableName);
            }

            // Set the access variable path
            access.VariablePath = varpath;

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var access = (VariableMoveData)data;

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var access = (VariableMoveData)data;

            // Set return type
            var info = types.Variables[access.VariablePath];
            access.ReturnType = info.Type;

            // Set escaping variables based on the flow graph
            access.EscapingVariables = types.FlowGraph
                .FindAllCapturedVariables(access.VariablePath)
                .Where(x => x.VariablePath != access.VariablePath)
                .ToImmutableHashSet()
                .Add(new VariableCapture(VariableCaptureKind.MoveCapture, access.VariablePath));

            var dependents = types.FlowGraph.FindAllDependentVariables(access.VariablePath).Select(x => x.VariablePath);
            var movedPath = new IdentifierPath("$moved");

            // Make sure this variable isn't already moved
            if (dependents.Contains(movedPath)) {
                throw TypeCheckingErrors.AccessedMovedVariable(
                    access.Location, access.VariablePath);
            }

            // Make sure this variable isn't captured by anything
            if (dependents.Any()) {
                throw TypeCheckingErrors.MovedCapturedVariable(access.Location,
                    access.VariablePath, dependents.First());
            }

            // Add a moved psuedo-variable to capture the moved variable
            types.FlowGraph = types.FlowGraph.AddEdge(access.VariablePath, 
                new IdentifierPath("$moved"), VariableCaptureKind.ValueCapture);

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromFlowAnalyzer(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, TypeCache types, FlowCache flows) {
            var access = (VariableMoveData)data;

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(IFlownData data, ICScope scope, ICodeGenerator gen) {
            var access = (VariableMoveData)data;

            scope.SetVariableDestructed(access.VariableName);

            return gen.MoveValue(access.VariableName, access.ReturnType);
        }
    }
}