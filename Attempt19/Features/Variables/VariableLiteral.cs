using System.Collections.Immutable;
using Attempt19.TypeChecking;
using Attempt19.CodeGeneration;
using Attempt19.Parsing;
using Attempt19.Types;
using Attempt19.Features.Variables;
using System.Linq;

namespace Attempt19 {
    public static partial class SyntaxFactory {
        public static Syntax MakeVariableLiteral(string name, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new VariableLiteralData() {
                    VariableName = name,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(VariableLiteralTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Variables {
    public class VariableLiteralData : VariableAccessBase { 
        public VariableInfo VariableInfo { get; set; }
    }

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
            var access = (VariableLiteralData)data;

            // Set variable info
            access.VariableInfo = types.Variables[access.VariablePath];

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var access = (VariableLiteralData)data;
            var innerType = (LanguageType)VoidType.Instance;

            if (access.VariableInfo.DefinitionKind == VariableDefinitionKind.Parameter) {
                // Make sure this isn't a non-variable parameter
                if (access.VariableInfo.Type is VariableType varType) {
                    innerType = varType.InnerType;
                }
                else {
                    throw TypeCheckingErrors.AccessedFunctionParameterLikeVariable(access.Location, access.VariableName);
                }
            }
            else {
                innerType = access.VariableInfo.Type;
            }

            // Set return type
            access.ReturnType = new VariableType(innerType);

            // Set variable lifetimes
            access.Lifetimes = access.VariableInfo.Lifetimes;

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(ITypeCheckedData data, ICodeGenerator gen) {
            var access = (VariableLiteralData)data;

            if (access.VariableInfo.DefinitionKind == VariableDefinitionKind.Parameter && access.VariableInfo.Type is VariableType varType) {
                return new CBlock(access.VariableName);
            }
            else {
                return new CBlock("&" + access.VariableName);
            }
        }
    }
}