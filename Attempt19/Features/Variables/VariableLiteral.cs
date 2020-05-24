using System.Collections.Immutable;
using Attempt19.TypeChecking;
using Attempt19.CodeGeneration;
using Attempt19.Parsing;
using Attempt19.Types;
using Attempt19.Features.Variables;

namespace Attempt19 {
    public static partial class SyntaxFactory {
        public static Syntax MakeVariableLiteral(string name, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new VariableLiteralData() {
                    Name = name,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(VariableLiteralTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Variables {
    public class VariableLiteralData : VariableAccessData { }

    public static class VariableLiteralTransformations {
        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope, NameCache names) {
            var access = (VariableLiteralData)data;

            // Set containing scope
            access.ContainingScope = scope;

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var access = (VariableLiteralData)data;

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
            var access = (VariableLiteralData)data;

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types) {
            var access = (VariableLiteralData)data;

            // Set return type
            var info = types.Variables[access.VariablePath];
            access.ReturnType = new VariableType(info.Type);            

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromFlowAnalyzer(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, FlowCache flows) {
            var access = (VariableLiteralData)data;

            // Make sure this variable isn't moved
            var neighbors = flows.DependentVariables.GetNeighbors(access.VariablePath);
            var movedPath = new IdentifierPath("$moved");

            if (neighbors.Contains(movedPath)) {
                throw TypeCheckingErrors.AccessedMovedVariable(
                    access.Location, access.VariablePath);
            }

            // This will always capture the original variable
            access.EscapingVariables = new[] { access.VariablePath }.ToImmutableHashSet();

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(IFlownData data, ICScope scope, ICodeGenerator gen) {
            var access = (VariableLiteralData)data;

            return gen.CopyValue(CWriter.AddressOf(access.Name), access.ReturnType, scope);
        }
    }
}