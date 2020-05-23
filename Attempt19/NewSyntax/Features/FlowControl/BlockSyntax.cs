using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Attempt17.NewSyntax.Features.FlowControl;
using Attempt17.TypeChecking;
using Attempt18;
using Attempt18.CodeGeneration;
using Attempt18.Features.FlowControl;
using Attempt18.Parsing;
using Attempt18.TypeChecking;
using Attempt18.Types;

namespace Attempt17.NewSyntax {
    public static partial class SyntaxFactory {
        public static Syntax MakeBlock(IReadOnlyList<Syntax> stats, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new BlockData() {
                    Statements = stats,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(BlockTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt17.NewSyntax.Features.FlowControl {
    public class BlockData : IParsedData, ITypeCheckedData, IFlownData {
        public IReadOnlyList<Syntax> Statements { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> EscapingVariables { get; set; }

        public IdentifierPath BlockPath { get; set; }
    }

    public static class BlockTransformations {
        private static int blockId = 0;
        private static int blockReturnCounter = 0;

        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope, NameCache names) {
            var block = (BlockData)data;

            // Delegate name declarations
            block.Statements = block.Statements
                .Select(x => x.DeclareNames(scope, names))
                .ToArray();

            // Set the block path
            block.BlockPath = scope.Append("block" + blockId++);

            return new Syntax() {
                Data = SyntaxData.From(block),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var block = (BlockData)data;

            // Delegate name resolutions
            block.Statements = block.Statements
                .Select(x => x.ResolveNames(names))
                .ToArray();

            return new Syntax() {
                Data = SyntaxData.From(block),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var block = (BlockData)data;

            // Delegate type declarations
            block.Statements = block.Statements
                .Select(x => x.DeclareTypes(types))
                .ToArray();

            return new Syntax() {
                Data = SyntaxData.From(block),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types) {
            var block = (BlockData)data;

            // Delegate type resolutions
            block.Statements = block.Statements
                .Select(x => x.ResolveTypes(types))
                .ToArray();

            // Set the return type
            block.ReturnType = block.Statements
                .LastOrNone()
                .Select(x => x.Data.AsTypeCheckedData().GetValue().ReturnType)
                .GetValueOr(() => VoidType.Instance);

            return new Syntax() {
                Data = SyntaxData.From(block),
                Operator = SyntaxOp.FromCodeGenerator(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, FlowCache flows) {
            var block = (BlockData)data;

            // Delegate flow analysis
            block.Statements = block.Statements
                .Select(x => x.AnalyzeFlow(flows))
                .ToArray();

            if (block.Statements.Any()) {
                // Get the last statement
                var retValue = block.Statements
                    .Last()
                    .Data
                    .AsFlownData()
                    .GetValue();

                // Make sure that the return value doesn't capture any variable inside the scope
                foreach (var cap in retValue.EscapingVariables) {
                    if (cap.StartsWith(block.BlockPath)) {
                        throw TypeCheckingErrors.VariableScopeExceeded(block.Location, cap);
                    }
                }

                // Set the captured variables to the those of the return value
                block.EscapingVariables = retValue.EscapingVariables;
            }
            else {
                // If there are no statements, there are no captured variables
                block.EscapingVariables = ImmutableHashSet.Create<IdentifierPath>();
            }

            return new Syntax() {
                Data = SyntaxData.From(block),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(IFlownData data, ICScope scope, ICodeGenerator gen) {
            // Get a new scope
            scope = new BlockCScope();

            var block = (BlockData)data;
            var stats = block.Statements
                .Select(x => x.Data.AsFlownData().GetValue())
                .ToArray();

            // Generate statements
            var genStats = block.Statements
                .Select(x => x.GenerateCode(scope, gen))
                .ToArray();

            var writer = new CWriter();

            if (!genStats.Any()) {
                return new CBlock("0");
            }

            var returnType = gen.Generate(block.ReturnType);
            var returnVal = genStats.Last().Value;
            var tempName = $"$block_return_" + blockReturnCounter++;

            var varsToCleanUp = scope
                .GetUndestructedVariables()
                .ToImmutableDictionary(x => x.Key, x => x.Value)
                .AddRange(
                    stats
                        .Zip(genStats, (x, y) => (v: y.Value, t: x.ReturnType))
                        .Select(x => new KeyValuePair<string, LanguageType>(x.v, x.t))
                        .SkipLast(1)
                );

            var lines = genStats
                .Select(x => x.SourceLines)
                .Aggregate((x, y) => x.AddRange(y));

            writer.Lines(lines);
            writer.Line("// Block cleanup");
            writer.VariableInit(returnType, tempName, returnVal);
            writer.Lines(ScopeHelper.CleanupScope(varsToCleanUp, gen));
            writer.EmptyLine();

            return writer.ToBlock(tempName);
        }
    }
}