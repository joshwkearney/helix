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
    public class StoreData : IParsedData, ITypeCheckedData, IFlownData {
        public Syntax Target { get; set; }

        public Syntax Value { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> EscapingVariables { get; set; }
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

        public static Syntax ResolveTypes(IParsedData data, TypeCache types) {
            var store = (StoreData)data;

            // Delegate type resolution
            store.Target = store.Target.ResolveTypes(types);
            store.Value = store.Value.ResolveTypes(types);

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

            return new Syntax() {
                Data = SyntaxData.From(store),
                Operator = SyntaxOp.FromFlowAnalyzer(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, FlowCache flows) {
            var store = (StoreData)data;

            // Delegate flow analysis
            store.Target = store.Target.AnalyzeFlow(flows);
            store.Value = store.Value.AnalyzeFlow(flows);

            var target = store.Target.Data.AsFlownData().GetValue();
            var value = store.Value.Data.AsFlownData().GetValue();

            var targetDependents = target.EscapingVariables
                .SelectMany(x => flows.DependentVariables.FindAccessibleNodes(x))
                .ToImmutableHashSet()
                .Except(target.EscapingVariables);

            // TODO: make this more permissive
            // Make sure the target's escaping variables are not captured
            if (targetDependents.Any()) {
                var capturing = targetDependents.First();
                var captured = flows.CapturedVariables
                    .FindAccessibleNodes(capturing)
                    .Intersect(target.EscapingVariables)
                    .First();

                throw TypeCheckingErrors.StoredToCapturedVariable(
                    store.Location, captured, capturing);
            }

            // Make sure all escaping variables in value outlive all of the
            // escaping variables in target
            foreach (var targetVar in target.EscapingVariables) {
                foreach (var valueVar in value.EscapingVariables) {
                    var targetScope = targetVar.Pop();
                    var valueScope = valueVar.Pop();

                    // There is a problem if valueScope is more specific
                    // than targetScope
                    if (valueScope != targetScope && valueScope.StartsWith(targetScope)) {
                        throw TypeCheckingErrors.StoreScopeExceeded(store.Location, 
                            targetVar, valueVar);
                    }
                }
            }

            // Set no captured variables
            store.EscapingVariables = ImmutableHashSet.Create<IdentifierPath>();

            return new Syntax() {
                Data = SyntaxData.From(store),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(IFlownData data, ICScope scope, ICodeGenerator gen) {
            var store = (StoreData)data;

            var writer = new CWriter();
            var target = store.Target.GenerateCode(scope, gen);
            var value = store.Value.GenerateCode(scope, gen);
            var innerType = store.Value.Data.AsTypeCheckedData().GetValue().ReturnType;
            var cInnerType = gen.Generate(innerType);

            // Optimization: If the target is a literal variable access don't force a copy
            if (store.Target.Data.AsFlownData().GetValue() is VariableLiteralData literal) {
                // This would have produced another variable to clean up, so remove
                // that from the scope
                scope.SetVariableDestructed(target.Value);

                writer.Lines(value.SourceLines);
                writer.Line("// Variable store");

                var destructorOp = gen.GetDestructor(innerType);
                if (destructorOp.TryGetValue(out var destructor)) {
                    writer.Line($"{destructor}({literal.VariableName});");
                }

                writer.VariableAssignment(literal.VariableName, value.Value);
                writer.EmptyLine();
            }
            else {
                writer.Lines(value.SourceLines);
                writer.Lines(target.SourceLines);
                writer.Line("// Variable store");

                var destructorOp = gen.GetDestructor(innerType);
                if (destructorOp.TryGetValue(out var destructor)) {
                    writer.Line($"{destructor}({CWriter.Dereference(target.Value, cInnerType)});");
                }

                writer.VariableAssignment(CWriter.Dereference(target.Value, cInnerType), value.Value);
                writer.EmptyLine();
            }

            return writer.ToBlock("0");
        }
    }
}