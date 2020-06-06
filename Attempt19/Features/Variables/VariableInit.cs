using System.Collections.Immutable;
using Attempt19.TypeChecking;
using Attempt19.CodeGeneration;
using Attempt19.Parsing;
using Attempt19.Types;
using Attempt19.Features.Variables;
using System.Linq;

namespace Attempt19 {
    public static partial class SyntaxFactory {
        public static Syntax MakeVariableInit(string name, Syntax value, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new VariableInitData() {
                    Name = name,
                    Value = value,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(VariableInitTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Variables {
    public class VariableInitData : IParsedData, ITypeCheckedData {
        public string Name { get; set; }

        public Syntax Value { get; set; }

        public IdentifierPath ContainingScope { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }
    }    

    public static class VariableInitTransformations {
        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope, NameCache names) {
            var init = (VariableInitData)data;

            // Delegate name declaration
            init.Value = init.Value.DeclareNames(scope, names);

            // Set containing scope
            init.ContainingScope = scope;

            return new Syntax() {
                Data = SyntaxData.From(init),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var init = (VariableInitData)data;

            // Delegate name resolution
            init.Value = init.Value.ResolveNames(names);

            // Make sure this variable's name isn't taken
            if (names.FindName(init.ContainingScope, init.Name, out var path, out var target)) {
                throw TypeCheckingErrors.IdentifierDefined(init.Location, init.Name);
            }

            // Add this variable to the local cache
            names.AddLocalName(init.ContainingScope.Append(init.Name), NameTarget.Variable);

            return new Syntax() {
                Data = SyntaxData.From(init),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var init = (VariableInitData)data;

            // Delegate type declaration
            init.Value = init.Value.DeclareTypes(types);

            return new Syntax() {
                Data = SyntaxData.From(init),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var init = (VariableInitData)data;

            // Delegate type resolution
            init.Value = init.Value.ResolveTypes(types, unifier);

            // Set return type
            init.ReturnType = VoidType.Instance;

            var value = init.Value.Data.AsTypeCheckedData().GetValue();
            var varPath = init.ContainingScope.Append(init.Name);
            var info = new VariableInfo(value.ReturnType, VariableDefinitionKind.Local, value.Lifetimes);

            // Add info to the type cache
            types.Variables.Add(varPath, info);

            // Set no captured variables
            init.Lifetimes = ImmutableHashSet.Create<IdentifierPath>();

            return new Syntax() {
                Data = SyntaxData.From(init),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(ITypeCheckedData data, ICodeGenerator gen) {
            var init = (VariableInitData)data;

            var value = init.Value.GenerateCode(gen);
            var type = init.Value.Data.AsTypeCheckedData().GetValue().ReturnType;
            var ctype = gen.Generate(type);

            var writer = new CWriter();
            writer.Lines(value.SourceLines);
            writer.Line("// Variable initalization");
            writer.VariableInit(ctype, init.Name, value.Value);
            writer.EmptyLine();

            return writer.ToBlock("0");
        }
    }
}