using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt9 {
    public class CodeGenerator : IStatementVisitor, IExpressionVisitor, ITrophyTypeVisitor {
        private readonly IReadOnlyList<IStatementSyntax> block;

        private int currentTab = 1;
        private readonly int tabLength = 4;

        private readonly StringBuilder generatedTypes = new StringBuilder();
        private readonly StringBuilder statements = new StringBuilder();

        private readonly Stack<string> values = new Stack<string>();
        private readonly Stack<string> types = new Stack<string>();

        private int referenceTempId = 0;
        private int structTempId = 0;

        private readonly Dictionary<ITrophyType, string> referenceTypeNames = new Dictionary<ITrophyType, string>();
        private readonly Dictionary<ITrophyType, string> structTypeNames = new Dictionary<ITrophyType, string>();

        public CodeGenerator(IReadOnlyList<IStatementSyntax> block) {
            this.block = block;
        }

        public string Generate() {
            foreach (var stat in this.block) {
                stat.Accept(this);
            }

            return new StringBuilder()
                .AppendLine("#include <inttypes.h>")
                .AppendLine("#include <stdlib.h>")
                .AppendLine()
                .Append(this.generatedTypes)
                .AppendLine("int main(void) {")
                .Append(this.statements.ToString())
                .Append(' ', this.tabLength * this.currentTab)
                .AppendLine($"return 0;")
                .AppendLine("}")
                .ToString();
        }

        public void Visit(IfStatement ifstat) {
            ifstat.Condition.Accept(this);
            string condStr = this.values.Pop();

            this.statements
                .Append(' ', this.tabLength * this.currentTab)
                .AppendLine($"if ({condStr}) {{");

            this.currentTab++;
            foreach (var stat in ifstat.AffirmativeBlock) {
                stat.Accept(this);
            }
            this.currentTab--;

            this.statements
                .Append(' ', this.tabLength * this.currentTab)
                .AppendLine("}")
                .Append(' ', this.tabLength * this.currentTab)
                .AppendLine("else {");

            this.currentTab++;
            foreach (var stat in ifstat.NegativeBlock) {
                stat.Accept(this);
            }
            this.currentTab--;
            
            this.statements
                .Append(' ', this.tabLength * this.currentTab)
                .AppendLine("}");
        }

        public void Visit(VariableDeclarationSyntax stat) {
            stat.VariableType.Accept(this);
            string typeStr = this.types.Pop();

            this.statements
                .Append(' ', this.tabLength * this.currentTab)
                .AppendLine($"{typeStr} {stat.Name};");
        }

        public void Visit(VariableAssignmentSyntax stat) {
            stat.AssignExpression.Accept(this);
            string assignStr = this.values.Pop();

            this.statements
                .Append(' ', this.tabLength * this.currentTab)
                .AppendLine($"{stat.Name} = {assignStr};");
        }

        public void Visit(ReferenceCheckoutStatement stat) {
            stat.Operand.Accept(this);
            string operandStr = this.values.Pop();

            this.statements
                .Append(' ', this.tabLength * this.currentTab).AppendLine($"if (--(*({operandStr}.count)) == 0) {{")
                .Append(' ', this.tabLength * this.currentTab).AppendLine($"    free({operandStr}.count);")
                .Append(' ', this.tabLength * this.currentTab).AppendLine($"    free({operandStr}.value);")
                .Append(' ', this.tabLength * this.currentTab).AppendLine("}");
        }

        public void Visit(ReferenceCheckinStatement stat) {
            stat.Operand.Accept(this);
            string operandStr = this.values.Pop();

            this.statements
                .Append(' ', this.tabLength * this.currentTab)
                .AppendLine($"(*({operandStr}.count))++;");
        }

        public void Visit(ReferenceCreateStatement stat) {
            var referenceType = ReferenceType.GetReferenceType(stat.Operand.ReturnType);

            stat.Operand.ReturnType.Accept(this);
            string innerTypeStr = this.types.Pop();

            referenceType.Accept(this);
            string referenceTypeStr = this.types.Pop();

            stat.Operand.Accept(this);
            string operandStr = this.values.Pop();

            this.statements
                .Append(' ', this.tabLength * this.currentTab).AppendLine($"{referenceTypeStr} {stat.ResultName};")
                .Append(' ', this.tabLength * this.currentTab).AppendLine($"{stat.ResultName}.value = malloc(sizeof({innerTypeStr}));")
                .Append(' ', this.tabLength * this.currentTab).AppendLine($"{stat.ResultName}.count = malloc(sizeof(int));")
                .Append(' ', this.tabLength * this.currentTab).AppendLine($"*{stat.ResultName}.value = {operandStr};")
                .Append(' ', this.tabLength * this.currentTab).AppendLine($"*{stat.ResultName}.count = 0;");
        }

        public void Visit(VariableDeclarationAssignmentSyntax stat) {
            stat.AssignSyntax.ReturnType.Accept(this);
            string typeStr = this.types.Pop();

            stat.AssignSyntax.Accept(this);
            string assignStr = this.values.Pop();

            this.statements
                .Append(' ', this.tabLength * this.currentTab)
                .AppendLine($"{typeStr} {stat.Name} = {assignStr};");
        }

        public void Visit(BinaryExpressionSyntax expr) {
            expr.LeftOperand.Accept(this);
            expr.RightOperand.Accept(this);

            string right = this.values.Pop();
            string left = this.values.Pop();

            switch (expr.Operator) {
                case SyntaxBinaryOperator.Add:
                    this.values.Push($"({left} + {right})");
                    break;
                case SyntaxBinaryOperator.Subtract:
                    this.values.Push($"({left} - {right})");
                    break;
                case SyntaxBinaryOperator.Multiply:
                    this.values.Push($"({left} * {right})");
                    break;
                case SyntaxBinaryOperator.Divide:
                    this.values.Push($"({left} / {right})");
                    break;
                default:
                    throw new Exception();
            }
        }

        public void Visit(UnaryExpressionSyntax expr) {
            expr.Operand.Accept(this);
            string value = this.values.Pop();

            switch (expr.Operator) {
                case SyntaxUnaryOperator.Negation:
                    this.values.Push($"(-{value})");
                    break;
                case SyntaxUnaryOperator.Unbox:
                    this.values.Push($"(*{value}.value)");
                    break;
                default:
                    throw new Exception();
            }
        }

        public void Visit(Int64LiteralSyntax expr) {
            this.values.Push(expr.Value.ToString());
        }

        public void Visit(VariableLiteralSyntax expr) {
            this.values.Push(expr.Name);
        }

        public void Visit(PrimitiveTrophyType value) {
            switch (value.Kind) {
                case PrimitiveTrophyTypeKind.Int64:
                    this.types.Push("int64_t");
                    break;
                default:
                    throw new Exception();
            }
        }

        public void Visit(ReferenceType value) {
            if (referenceTypeNames.TryGetValue(value, out string typeStr)) {
                this.types.Push(typeStr);
            }
            else {
                string temp = "$reftype" + referenceTempId++;
                this.referenceTypeNames.Add(value, temp);

                value.InnerType.Accept(this);
                string innerType = this.types.Pop();

                this.generatedTypes
                    .AppendLine($"typedef struct {{")
                    .AppendLine($"    int* count;")
                    .AppendLine($"    {innerType}* value;")
                    .AppendLine($"}} {temp};")
                    .AppendLine();

                this.types.Push(temp);
            }
        }

        public void Visit(NamedStructType value) {
            if (structTypeNames.TryGetValue(value, out string typeStr)) {
                this.types.Push(typeStr);
            }
            else {
                string temp = "$structtype" + this.structTempId++;
                this.structTypeNames.Add(value, temp);

                this.generatedTypes
                    .AppendLine($"typedef struct {{");

                foreach (var pair in value.Members) {
                    pair.Value.Accept(this);
                    string memberTypeStr = this.types.Pop();

                    this.generatedTypes
                        .Append(' ', this.tabLength)
                        .AppendLine($"{memberTypeStr} {pair.Key};");
                }

                this.generatedTypes.AppendLine($"}} {temp};")
                    .AppendLine();

                this.types.Push(temp);
            }
        }
    }
}