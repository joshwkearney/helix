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
                Data = SyntaxData.From(new VariableAccessData() {
                    Name = name,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(VariableMoveTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Variables {
    public static class VariableMoveTransformations {
        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope, NameCache names) {
            var access = (VariableAccessData)data;

            // Set containing scope
            access.ContainingScope = scope;

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var access = (VariableAccessData)data;

            // Make sure this name exists
            if (!names.FindName(access.ContainingScope, access.Name, out var varpath, out var target)) {
                throw TypeCheckingErrors.VariableUndefined(access.Location, access.Name);
            }

            // Make sure this name is a variable
            if (target != NameTarget.Variable) {
                throw TypeCheckingErrors.VariableUndefined(access.Location, access.Name);
            }

            // Set the access variable path
            access.VariablePath = varpath;

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var access = (VariableAccessData)data;

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types) {
            var access = (VariableAccessData)data;

            // Set return type
            var info = types.Variables[access.VariablePath];
            access.ReturnType = info.Type;            

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromFlowAnalyzer(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, FlowCache flows) {
            var access = (VariableAccessData)data;

            var dependents = flows.DependentVariables.GetNeighbors(access.VariablePath);
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
            flows.DependentVariables = flows.DependentVariables.AddEdge(access.VariablePath, movedPath);

            // This will always capture the original variable
            access.EscapingVariables = flows.CapturedVariables
                .FindAccessibleNodes(access.VariablePath)
                .ToImmutableHashSet()
                .Remove(access.VariablePath);

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(IFlownData data, ICScope scope, ICodeGenerator gen) {
            var access = (VariableAccessData)data;

            scope.SetVariableDestructed(access.Name);

            return gen.MoveValue(access.Name, access.ReturnType);
        }
    }
}