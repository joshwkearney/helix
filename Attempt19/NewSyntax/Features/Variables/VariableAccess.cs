using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Attempt17.NewSyntax.Features.Variables;
using Attempt17.TypeChecking;
using Attempt18;
using Attempt18.CodeGeneration;
using Attempt18.Parsing;
using Attempt18.Types;

namespace Attempt17.NewSyntax {
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

namespace Attempt17.NewSyntax.Features.Variables {
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
            var init = (VariableInitData)data;

            var value = init.Value.GenerateCode(scope, gen);
            var type = init.Value.Data.AsTypeCheckedData().GetValue().ReturnType;
            var ctype = gen.Generate(type);

            var writer = new CWriter();
            writer.Lines(value.SourceLines);
            writer.Line("// Variable initalization");
            writer.VariableInit(ctype, init.Name, value.Value);
            writer.EmptyLine();

            scope.SetVariableUndestructed(init.Name, type);

            if (value.Value.StartsWith("$")) {
                scope.SetVariableDestructed(value.Value);
            }

            return writer.ToBlock("0");
        }
    }
}