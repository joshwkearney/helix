//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Attempt10.Compiling {
//    public class VariableTransformer : ISyntaxTreeVisitor {
//        private int currentId = 0;
//        private readonly Stack<ISyntaxTree> values = new Stack<ISyntaxTree>();

//        public void Visit(Int64LiteralSyntax value) {
//            this.values.Push(value);
//        }

//        public void Visit(VariableLiteralSyntax value) {
//            this.values.Push(value);
//        }

//        public void Visit(IfExpressionSyntax value) {
//            string id = "$temp" + this.currentId++;

//            this.values.Push(
//                new VariableDefinitionSyntax(
//                    id,
//                    value,
//                    new VariableLiteralSyntax(id, value.ExpressionType, value.Scope),
//                    value.Scope));
//        }

//        public void Visit(VariableDefinitionSyntax value) {
//            this.values.Push(value);
//        }

//        public void Visit(FunctionLiteralSyntax value) {
//            this.values.Push(value);
//        }

//        public void Visit(FunctionInvokeSyntax value) {
//            string id = "$temp" + this.currentId++;

//            this.values.Push(
//                new VariableDefinitionSyntax(
//                    id,
//                    value,
//                    new VariableLiteralSyntax(id, value.ExpressionType, value.Scope),
//                    value.Scope));
//        }

//        public void Visit(BoolLiteralSyntax value) {
//            this.values.Push(value);
//        }

//        public void Visit(Real64Literal value) {
//            this.values.Push(value);
//        }

//        public void Visit(PrimitiveOperationSyntax value) {
//            this.values.Push(value);
//        }
//    }
//}