using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt10 {
    public enum ReferenceCountMode {
        Decrement,
        DecrementCleanup,
        Cleanup,
        None
    }

    public partial class CodeGenerator : ISyntaxTreeVisitor, ITrophyTypeVisitor {
        private readonly ISyntaxTree tree;

        // Function pointer generation
        //private readonly Dictionary<TrophyFunctionType, string> functionTypes 
        //    = new Dictionary<TrophyFunctionType, string>();

        private int currentTempId = 0;
        private int currentClosureId = 0;

        private readonly List<string> globalScope = new List<string>();
        private readonly Stack<List<string>> localScope = new Stack<List<string>>();
        private readonly Stack<string> values = new Stack<string>();
        private readonly Stack<string> types = new Stack<string>();

        public CodeGenerator(ISyntaxTree tree) {
            this.tree = tree;
        }

        public string Generate() {
            this.globalScope.Clear();
            this.localScope.Clear();
            this.localScope.Push(new List<string>());

            this.globalScope.Add("#include <inttypes.h>");
            this.globalScope.Add("#include <stdlib.h>");
            this.globalScope.Add("");

            this.globalScope.Add("typedef char Unit;");
            this.globalScope.Add("");

            this.tree.Accept(this);

            this.globalScope.Add("int main(void) {");
            this.globalScope.AddRange(this.localScope.Pop().Select(x => "    " + x));

            this.globalScope.Add("    return (int)(" + this.values.Pop() + ");");
            this.globalScope.Add("}");

            StringBuilder sb = new StringBuilder();
            foreach (var line in this.globalScope) {
                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        public void Visit(BinaryExpressionSyntax value) {
            value.Right.Accept(this);
            value.Left.Accept(this);

            string op;
            switch (value.Operator) {
                case BinaryOperator.Add:
                    op = "+";
                    break;
                case BinaryOperator.Subtract:
                    op = "-";
                    break;
                case BinaryOperator.Multiply:
                    op = "*";
                    break;
                case BinaryOperator.StrictDivide:
                    op = "/";
                    break;
                case BinaryOperator.RealDivide:
                    op = "* 1.0 /";
                    break;
                case BinaryOperator.LogicalAnd:
                    op = "&&";
                    break;
                case BinaryOperator.LogicalOr:
                    op = "||";
                    break;
                case BinaryOperator.BitwiseAnd:
                    op = "&";
                    break;
                case BinaryOperator.BitwiseOr:
                    op = "|";
                    break;
                case BinaryOperator.Xor:
                    op = "^";
                    break;
                default:
                    throw new Exception();
            }

            var left = this.values.Pop();
            var right = this.values.Pop();

            value.ExpressionType.Accept(this);
            string typeName = this.types.Pop();

            string tempName = "$temp" + this.currentTempId++;
            this.localScope.Peek().Add(CSyntax.Declaration(typeName, tempName, $"{left} {op} {right}"));

            this.values.Push(tempName);
        }

        public void Visit(UnaryExpressionSyntax value) {
            value.Operand.Accept(this);

            string op;
            switch (value.Operator) {
                case UnaryOperator.Negation:
                    op = "-";
                    break;
                case UnaryOperator.Not:
                    op = "~";
                    break;
                default:
                    throw new Exception();
            }

            this.values.Push($"({op}{this.values.Pop()})");
        }

        public void Visit(Int64LiteralSyntax value) {
            this.values.Push(value.Value.ToString());
        }

        public void Visit(VariableLiteralSyntax value) {
            this.values.Push(value.Name);
        }

        public void Visit(IfExpressionSyntax value) {
            // Get condition
            value.Condition.Accept(this);
            var cond = this.values.Pop();

            // Get affirmative block
            this.localScope.Push(new List<string>());
            value.AffirmativeExpression.Accept(this);

            var affirm = this.values.Pop();

            var affirmBlock = this.localScope.Pop();

            // Get negative block
            this.localScope.Push(new List<string>());
            value.NegativeExpression.Accept(this);

            var negative = this.values.Pop();

            var negativeBlock = this.localScope.Pop();

            // Declare the temp variable
            string tempName = this.CreateTempVariable(value.ExpressionType);
            this.values.Push(tempName);

            // Assign temp variable at the end of each block
            affirmBlock.Add(CSyntax.Assignment(tempName, affirm));
            negativeBlock.Add(CSyntax.Assignment(tempName, negative));

            // Write the if statement
            this.localScope.Peek().AddRange(CSyntax.IfStatement(cond, affirmBlock, negativeBlock));
        }

        public void Visit(VariableDefinitionSyntax value) {
            // Get assign value
            value.AssignExpression.Accept(this);
            var assign = this.values.Pop();

            // Get assign type
            value.AssignExpression.ExpressionType.Accept(this);
            string typeName = this.types.Pop();

            // Add the statement
            this.localScope.Peek().Add(CSyntax.Declaration(typeName, value.Name, assign));

            // The result is the appendix
            value.ScopeExpression.Accept(this);
        }

        public void Visit(PrimitiveTrophyType value) {
            switch (value.Kind) {
                case PrimitiveTrophyTypes.Int64:
                    this.types.Push("int64_t");
                    break;
                case PrimitiveTrophyTypes.Boolean:
                    this.types.Push("uint8_t");
                    break;
                case PrimitiveTrophyTypes.Real64:
                    this.types.Push("double");
                    break;
                default:
                    throw new Exception();
            }
        }

        public void Visit(BoolLiteralSyntax value) {
            this.values.Push(value.Value ? "1" : "0");
        }

        public void Visit(Real64Literal value) {
            this.values.Push(value.Value.ToString());
        }

        private string CreateTempVariable(ITrophyType type, string value, bool constant =  true) {
            string name = "$temp" + this.currentTempId++;

            type.Accept(this);
            string typeName = this.types.Pop();

            this.localScope.Peek().Add(CSyntax.Declaration(typeName, name, value, constant));

            return name;
        }

        private string CreateTempVariable(ITrophyType type, bool constant = true) {
            string name = "$temp" + this.currentTempId++;

            type.Accept(this);
            string typeName = this.types.Pop();

            this.localScope.Peek().Add(CSyntax.Declaration(typeName, name, constant));

            return name;
        }

        public void Visit(FunctionTrophyType value) {
            throw new NotImplementedException();
        }
    }
}