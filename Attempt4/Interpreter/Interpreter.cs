//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Attempt4 {
//    public class Interpreter : IAnalyzedSyntaxVisitor {
//        private readonly Stack<IInterpretedValue> values = new Stack<IInterpretedValue>();
//        private readonly IAnalyzedSyntax tree;

//        public Interpreter(IAnalyzedSyntax tree) {
//            this.tree = tree;
//        }

//        public IInterpretedValue Interpret() {
//            this.values.Clear();
//            this.tree.Accept(this);

//            return this.values.Pop();
//        }

//        public void Visit(IInterpretedValue literal) {
//            this.values.Push(literal);
//        }

//        public void Visit(IntrinsicFunction func) {
//            throw new CompileException(CompileExceptionCategory.Semantic, 0, "Cannot evaluate intrinsic functions");
//        }

//        public void Visit(FunctionCall expr) {
//            Func<int, int, int> func;

//            if (expr.Target == IntrinsicFunction.AddInt32) {
//                func = (x, y) => x + y;
//            }
//            else if (expr.Target == IntrinsicFunction.SubtractInt32) {
//                func = (x, y) => x - y;
//            }
//            else if (expr.Target == IntrinsicFunction.MultiplyInt32) {
//                func = (x, y) => x * y;
//            }
//            else if (expr.Target == IntrinsicFunction.DivideInt32) {
//                func = (x, y) => x / y;
//            }
//            else {
//                throw new CompileException(CompileExceptionCategory.Semantic, 0, "Cannot evaluate function call");
//            }

//            expr.Arguments[0].Accept(this);
//            IInterpretedValue<int> first = this.values.Pop() as IInterpretedValue<int>;

//            expr.Arguments[1].Accept(this);
//            IInterpretedValue<int> second = this.values.Pop() as IInterpretedValue<int>;

//            this.values.Push(new IntegerLiteral(func(first.Value, second.Value)));
//        }
//    }
//}