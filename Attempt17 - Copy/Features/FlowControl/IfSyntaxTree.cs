using Attempt17.CodeGeneration;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.FlowControl {
    public class IfSyntaxTree : ISyntaxTree {
        public LanguageType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> CapturedVariables { get; }

        public IfKind Kind { get; }

        public ISyntaxTree Condition { get; }

        public ISyntaxTree Affirmative { get; }

        public IOption<ISyntaxTree> Negative { get; }

        public IfSyntaxTree(IfKind kind, ISyntaxTree cond, ISyntaxTree affirm, IOption<ISyntaxTree> neg) {
            this.Kind = kind;
            this.Condition = cond;
            this.Affirmative = affirm;
            this.Negative = neg;

            if (this.Kind == IfKind.Expression) {
                this.ReturnType = affirm.ReturnType;
                this.CapturedVariables = cond.CapturedVariables
                    .Union(affirm.CapturedVariables)
                    .Union(neg.GetValue().CapturedVariables);
            }
            else {
                this.ReturnType = VoidType.Instance;
                this.CapturedVariables = ImmutableHashSet<IdentifierPath>.Empty;
            }
        }

        public Scope ModifyLateralScope(Scope scope) => scope;

        public CBlock GenerateCode(CodeGenerator gen) {
            var writer = new CWriter();
            var cond = this.Condition.GenerateCode(gen);
            var affirm = this.Affirmative.GenerateCode(gen);
            var neg = this.Negative.Select(x => x.GenerateCode(gen));

            writer.Lines(cond.SourceLines);

            if (this.Kind == IfKind.Expression) {
                var tempType = this.Affirmative.ReturnType.GenerateCType();
                var tempName = gen.GetTempVariableName();

                writer.VariableInit(tempType, tempName);
                writer.Line($"if ({cond.Value}) {{");
                writer.Lines(CWriter.Indent(affirm.SourceLines));
                writer.Line($"    {tempName} = {affirm.Value};");
                writer.Line("}");
                writer.Line("else {");
                writer.Lines(CWriter.Indent(neg.GetValue().SourceLines));
                writer.Line($"    {tempName} = {neg.GetValue().Value};");
                writer.Line("}");
                writer.EmptyLine();

                return writer.ToBlock(tempName);
            }
            else {
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
    }
}