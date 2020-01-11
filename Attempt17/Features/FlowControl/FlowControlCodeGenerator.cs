using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt17.Features.FlowControl {
    public class FlowControlCodeGenerator {
        private int tempCounter = 0;
        private int blockCounter = 0;

        public CBlock GenerateWhileSyntax(WhileSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
            var cond = gen.Generate(syntax.Condition, scope);
            var body = gen.Generate(syntax.Body, scope);
            var writer = new CWriter();

            writer.Lines(cond.SourceLines);
            writer.Line("// While loop");
            writer.Line($"while ({cond.Value}) {{");
            writer.Lines(CWriter.Trim(CWriter.Indent(body.SourceLines)));
            writer.Line("}");
            writer.EmptyLine();

            return writer.ToBlock("0");
        }

        public CBlock GenerateIfSyntax(IfSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
            var writer = new CWriter();
            var cond = gen.Generate(syntax.Condition, scope);
            var affirmScope = new IfBranchCScope(scope);
            var negScope = new IfBranchCScope(scope);
            var affirm = gen.Generate(syntax.Affirmative, affirmScope);
            var neg = syntax.Negative.Select(x => gen.Generate(x, negScope));

            writer.Lines(cond.SourceLines);
            
            // Set any variables that were moved in both branches to moved
            // If there is no negative branch, the intersection will be empty
            foreach (var varName in affirmScope.MovedVariables.Intersect(negScope.MovedVariables)) {
                scope.SetVariableMoved(varName);
            }

            if (syntax.Kind == IfKind.Expression) {
                var tempType = gen.Generate(syntax.Affirmative.Tag.ReturnType);
                var tempName = "$if_result_" + this.tempCounter++;

                writer.Line("// If expression");
                writer.VariableInit(tempType, tempName);
                writer.Line($"if ({cond.Value}) {{");
                writer.Lines(CWriter.Indent(affirm.SourceLines));
                writer.Lines(CWriter.Indent("// If result assignment"));
                writer.Line($"    {tempName} = {affirm.Value};");
                writer.Line("}");
                writer.Line("else {");
                writer.Lines(CWriter.Indent(neg.GetValue().SourceLines));
                writer.Lines(CWriter.Indent("// If result assignment"));
                writer.Line($"    {tempName} = {neg.GetValue().Value};");
                writer.Line("}");
                writer.EmptyLine();

                scope.SetVariableUndestructed(tempName, syntax.Affirmative.Tag.ReturnType);

                return writer.ToBlock(tempName);
            }
            else {
                writer.Line("// If statement");
                writer.Line($"if ({cond.Value}) {{");
                writer.Lines(CWriter.Trim(CWriter.Indent(affirm.SourceLines)));
                writer.Line("}");

                if (neg.Any()) {
                    writer.Line("else {");
                    writer.Lines(CWriter.Trim(CWriter.Indent(neg.GetValue().SourceLines)));
                    writer.Line("}");
                }

                writer.EmptyLine();

                return writer.ToBlock("0");
            }
        }

        public CBlock GenerateBlockSyntax(BlockSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
            // Get a new scope
            scope = new BlockCScope(scope);

            // Generate statements
            var stats = syntax.Statements.Select(x => gen.Generate(x, scope)).ToArray();
            var writer = new CWriter();

            if (stats.Any()) {
                var returnType = gen.Generate(syntax.Statements.Last().Tag.ReturnType);
                var returnVal = stats.Last().Value;
                var tempName = $"$block_return_" + this.blockCounter++;
                var varsToCleanUp = scope
                    .GetUndestructedVariables()
                    .ToImmutableDictionary(x => x.Key, x => x.Value)
                    .AddRange(
                        syntax
                            .Statements
                            .Zip(stats, (x, y) => new KeyValuePair<string, LanguageType>(y.Value, x.Tag.ReturnType))
                            .SkipLast(1)
                    );
                var lines = stats
                    .Select(x => x.SourceLines)
                    .Aggregate((x, y) => x.AddRange(y));

                writer.Lines(lines);
                writer.Line("// Block cleanup");
                writer.VariableInit(returnType, tempName, returnVal);
                writer.Lines(ScopeHelper.CleanupScope(varsToCleanUp, gen));
                writer.EmptyLine();

                return writer.ToBlock(tempName);
            }
            else {
                return new CBlock("0");
            }
        }
    }
}