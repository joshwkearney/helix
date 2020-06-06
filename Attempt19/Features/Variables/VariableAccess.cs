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
                    VariableName = name,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(VariableAccessTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Variables {
    public class VariableAccessData : VariableAccessBase { 
        public VariableInfo VariableInfo { get; set; }
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
            var access = (VariableAccessData)data;

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var access = (VariableAccessData)data;
            var type = (LanguageType)VoidType.Instance;

            // Set variable info
            access.VariableInfo = types.Variables[access.VariablePath];

            if (access.VariableInfo.DefinitionKind == VariableDefinitionKind.Parameter && access.VariableInfo.Type is VariableType varType) {
                type = varType.InnerType;
            }
            else {
                type = access.VariableInfo.Type;
            }

            // Set return type
            access.ReturnType = type;

            // Set variable lifetimes
            if (access.VariableInfo.Type.GetCopiability() == TypeCopiability.Unconditional) {
                access.Lifetimes = ImmutableHashSet.Create<IdentifierPath>();
            }
            else {
                access.Lifetimes = access.VariableInfo.Lifetimes;
            }

            return new Syntax() {
                Data = SyntaxData.From(access),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(ITypeCheckedData data, ICodeGenerator gen) {
            var access = (VariableAccessData)data;
            
            if (access.VariableInfo.DefinitionKind == VariableDefinitionKind.Parameter && access.VariableInfo.Type is VariableType varType) {
                return new CBlock("(*" + access.VariableName + ")");
            }
            else {
                return new CBlock(access.VariableName);
            }
        }
    }
}