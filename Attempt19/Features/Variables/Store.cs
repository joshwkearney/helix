using System.Collections.Immutable;
using Attempt19.TypeChecking;
using Attempt19.CodeGeneration;
using Attempt19.Parsing;
using Attempt19.Types;
using Attempt19.Features.Variables;
using System.Linq;

namespace Attempt19 {
    public static partial class SyntaxFactory {
        public static Syntax MakeStore(Syntax target, Syntax value, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new StoreData() {
                    Target = target,
                    Value = value,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(StoreTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.Variables {
    public class StoreData : IParsedData, ITypeCheckedData {
        public Syntax Target { get; set; }

        public Syntax Value { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }
    }    

    public static class StoreTransformations {
        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope, NameCache names) {
            var store = (StoreData)data;

            // Delegate name declaration
            store.Target = store.Target.DeclareNames(scope, names);
            store.Value = store.Value.DeclareNames(scope, names);

            return new Syntax() {
                Data = SyntaxData.From(store),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var store = (StoreData)data;

            // Delegate name resolution
            store.Target = store.Target.ResolveNames(names);
            store.Value = store.Value.ResolveNames(names);

            return new Syntax() {
                Data = SyntaxData.From(store),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var store = (StoreData)data;

            // Delegate type declaration
            store.Target = store.Target.DeclareTypes(types);
            store.Value = store.Value.DeclareTypes(types);

            return new Syntax() {
                Data = SyntaxData.From(store),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var store = (StoreData)data;

            // Delegate type resolution
            store.Target = store.Target.ResolveTypes(types, unifier);
            store.Value = store.Value.ResolveTypes(types, unifier);

            var target = store.Target.Data.AsTypeCheckedData().GetValue();
            var value = store.Value.Data.AsTypeCheckedData().GetValue();

            // Make sure the target is a variable type
            if (!(target.ReturnType is VariableType varType)) {
                throw TypeCheckingErrors.ExpectedVariableType(
                    target.Location, target.ReturnType);
            }

            // Make sure the value's type matches the target's inner type
            if (varType.InnerType != value.ReturnType) {
                throw TypeCheckingErrors.UnexpectedType(
                    value.Location, varType.InnerType, value.ReturnType);
            }

            // Set return type
            store.ReturnType = VoidType.Instance;

            // Set no captured variables
            store.Lifetimes = ImmutableHashSet.Create<IdentifierPath>();

            // Make sure all escaping variables in value outlive all of the
            // escaping variables in target
            foreach (var targetCap in target.Lifetimes) {
                foreach (var valueCap in value.Lifetimes) {
                    // Make sure targetCap outlives valueCap
                    if (!targetCap.StartsWith(valueCap)) {
                        throw TypeCheckingErrors.LifetimeExceeded(store.Location, targetCap, valueCap);
                    }
                }
            }

            return new Syntax() {
                Data = SyntaxData.From(store),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(ITypeCheckedData data, ICodeGenerator gen) {
            var store = (StoreData)data;

            var writer = new CWriter();
            var target = store.Target.GenerateCode(gen);
            var value = store.Value.GenerateCode(gen);
            var innerType = store.Value.Data.AsTypeCheckedData().GetValue().ReturnType;
            var cInnerType = gen.Generate(innerType);

            writer.Lines(value.SourceLines);
            writer.Lines(target.SourceLines);
            writer.Line("// Variable store");

            if (target.Value.StartsWith("&")) {
                writer.VariableAssignment(target.Value.Substring(1), value.Value);
            }
            else {
                writer.VariableAssignment("*" + target.Value, value.Value);
            }

            writer.EmptyLine();

            return writer.ToBlock("0");
        }
    }
}