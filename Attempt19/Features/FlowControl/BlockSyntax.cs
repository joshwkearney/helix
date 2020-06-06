using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt19.TypeChecking;
using Attempt19;
using Attempt19.CodeGeneration;
using Attempt19.Features.FlowControl;
using Attempt19.Parsing;
using Attempt19.Types;

namespace Attempt19 {
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

namespace Attempt19.Features.FlowControl {
    public class BlockData : IParsedData, ITypeCheckedData {
        public IReadOnlyList<Syntax> Statements { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public IdentifierPath BlockPath { get; set; }
    }

    public static class BlockTransformations {
        private static int blockId = 0;

        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope, NameCache names) {
            var block = (BlockData)data;

            // Set the block path
            block.BlockPath = scope.Append("$block" + blockId++);

            // Delegate name declarations
            block.Statements = block.Statements
                .Select(x => x.DeclareNames(block.BlockPath, names))
                .ToArray();

            return new Syntax() {
                Data = SyntaxData.From(block),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var block = (BlockData)data;

            names.PushLocalFrame();

            // Delegate name resolutions
            block.Statements = block.Statements
                .Select(x => x.ResolveNames(names))
                .ToArray();

            names.PopLocalFrame();

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

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var block = (BlockData)data;

            // Delegate type resolutions
            block.Statements = block.Statements
                .Select(x => x.ResolveTypes(types, unifier))
                .ToArray();

            // Set the return type
            block.ReturnType = block.Statements
                .LastOrNone()
                .Select(x => x.Data.AsTypeCheckedData().GetValue().ReturnType)
                .GetValueOr(() => VoidType.Instance);

            // Set escaping variables
            if (block.Statements.Any()) {
                block.Lifetimes = block.Statements.Last().Data.AsTypeCheckedData().GetValue().Lifetimes;
            }
            else {
                block.Lifetimes = ImmutableHashSet.Create<IdentifierPath>();
            }

            return new Syntax() {
                Data = SyntaxData.From(block),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(ITypeCheckedData data, ICodeGenerator gen) {
            var block = (BlockData)data;
            var stats = block.Statements
                .Select(x => x.Data.AsTypeCheckedData().GetValue())
                .ToArray();

            // Generate statements
            var genStats = block.Statements
                .Select(x => x.GenerateCode(gen))
                .ToArray();

            var writer = new CWriter();

            if (!genStats.Any()) {
                return new CBlock("0");
            }

            var returnType = gen.Generate(block.ReturnType);
            var returnVal = genStats.Last().Value;
            var lines = genStats
                .Select(x => x.SourceLines)
                .Aggregate((x, y) => x.AddRange(y));

            writer.Lines(lines);

            return writer.ToBlock(returnVal);
        }
    }
}