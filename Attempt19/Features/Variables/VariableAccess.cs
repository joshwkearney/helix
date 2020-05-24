using System.Collections.Immutable;
using Attempt19.TypeChecking;
using Attempt19.CodeGeneration;
using Attempt19.Parsing;
using Attempt19.Types;
using Attempt19.Features.Variables;
using System.Linq;

namespace Attempt19 {
    public static partial class SyntaxFactory {
        public static Syntax MakeVariableAccess(string name, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new VariableAccessData() {
                    Name = name,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(VariableAccessTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Variables {
    public class VariableAccessData : IParsedData, ITypeCheckedData, IFlownData {
        public string Name { get; set; }

        public IdentifierPath ContainingScope { get; set; }

        public IdentifierPath VariablePath { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> EscapingVariables { get; set; }
    }    

    public static class VariableAccessTransformations {
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

            // Make sure this variable isn't moved
            var neighbors = flows.DependentVariables.GetNeighbors(access.VariablePath);
            var movedPath = new IdentifierPath("$moved");

            if (neighbors.Contains(movedPath)) {
                throw TypeCheckingErrors.AccessedMovedVariable(
                    access.Location, access.VariablePath);
            }

            // If the value type is conditionally copiable, capture the accessed variable
            if (access.ReturnType.GetCopiability() == TypeCopiability.Conditional) {
                access.EscapingVariables = new[] { access.VariablePath }.ToImmutableHashSet();
            }
            else {
                access.EscapingVariables = ImmutableHashSet.Create<IdentifierPath>();
            }

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(IFlownData data, ICScope scope, ICodeGenerator gen) {
            var access = (VariableAccessData)data;

            return gen.CopyValue(access.Name, access.ReturnType, scope);
        }
    }
}