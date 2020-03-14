using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.FlowControl {
    public class FlowControlCodeGenerator
        : IFlowControlVisitor<CBlock, TypeCheckTag, CodeGenerationContext> {

        private int tempCounter = 0;
        private int blockCounter = 0;

        public CBlock VisitBlock(BlockSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            // Get a new scope
            var scope = new BlockCScope(context.Scope);
            context = context.WithScope(scope);

            // Generate statements
            var stats = syntax.Statements.Select(x => x.Accept(visitor, context)).ToArray();
            var writer = new CWriter();

            if (stats.Any()) {
                var returnType = context
                    .Generator
                    .Generate(syntax.Statements.Last().Tag.ReturnType);

                var returnVal = stats.Last().Value;
                var tempName = $"$block_return_" + this.blockCounter++;

                var varsToCleanUp = scope
                    .GetUndestructedVariables()
                    .ToImmutableDictionary(x => x.Key, x => x.Value)
                    .AddRange(
                        syntax
                            .Statements
                            .Zip(stats, (x, y) => (v: y.Value, t: x.Tag.ReturnType))
                            .Select(x => new KeyValuePair<string, LanguageType>(x.v, x.t))
                            .SkipLast(1)
                    );

                var lines = stats
                    .Select(x => x.SourceLines)
                    .Aggregate((x, y) => x.AddRange(y));

                writer.Lines(lines);
                writer.Line("// Block cleanup");
                writer.VariableInit(returnType, tempName, returnVal);
                writer.Lines(ScopeHelper.CleanupScope(varsToCleanUp, context.Generator));
                writer.EmptyLine();

                return writer.ToBlock(tempName);
            }
            else {
                return new CBlock("0");
            }
        }

        public CBlock VisitIf(IfSyntax<TypeCheckTag> syntax, ISyntaxVisitor<CBlock, TypeCheckTag,
            CodeGenerationContext> visitor, CodeGenerationContext context) {

            var writer = new CWriter();
            var cond = syntax.Condition.Accept(visitor, context);
            var affirmScope = new IfBranchCScope(context.Scope);
            var negScope = new IfBranchCScope(context.Scope);
            var affirm = syntax.Affirmative.Accept(visitor, context.WithScope(affirmScope));
            var neg = syntax.Negative.Select(x => x.Accept(visitor, context.WithScope(negScope)));

            writer.Lines(cond.SourceLines);

            // Set any variables that were moved in both branches to moved
            // If there is no negative branch, the intersection will be empty
            foreach (var varName in affirmScope.MovedVariables.Intersect(negScope.MovedVariables)) {
                context.Scope.SetVariableMoved(varName);
            }

            if (syntax.Kind == IfKind.Expression) {
                var tempType = context.Generator.Generate(syntax.Affirmative.Tag.ReturnType);
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

                context.Scope.SetVariableUndestructed(tempName, syntax.Affirmative.Tag.ReturnType);

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

        public CBlock VisitWhile(WhileSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var cond = syntax.Condition.Accept(visitor, context);
            var body = syntax.Body.Accept(visitor, context);
            var writer = new CWriter();

            writer.Lines(cond.SourceLines);
            writer.Line("// While loop");
            writer.Line($"while ({cond.Value}) {{");
            writer.Lines(CWriter.Trim(CWriter.Indent(body.SourceLines)));
            writer.Line("}");
            writer.EmptyLine();

            return writer.ToBlock("0");
        }
    }
}
