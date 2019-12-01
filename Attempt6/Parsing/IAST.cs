using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Parsing {
    public interface IAST {
        Type ReturnType { get; }
        void Accept(IASTVisitor visitor);
    }

    public interface IASTVisitor {
        void Visit(IfExpression expr);
        void Visit(LetExpression expr);
        void Visit(FunctionCallExpression expr);
        void Visit(Int32Literal expr);
        void Visit(IdentifierLiteral expr);
        void Visit(FunctionDeclaration expr);
    }

    public class FunctionDeclaration : IAST {
        public Type ReturnType => IntrinsicFunctions.FunctionType;

        public IReadOnlyList<string> Parameters { get; }

        public IAST Body { get; }

        public FunctionDeclaration(IReadOnlyList<string> pars, IAST body) {
            this.Parameters = pars;
            this.Body = body;
        }

        public void Accept(IASTVisitor visitor) {
            visitor.Visit(this);
        }
    }

    public class IdentifierLiteral : IAST {
        public string Name { get; }

        public Type ReturnType { get; }

        public IdentifierLiteral(string name, Type returnType) {
            this.Name = name;
            this.ReturnType = returnType;
        }

        public void Accept(IASTVisitor visitor) {
            visitor.Visit(this);
        }
    }

    public class IfExpression : IAST {
        public IAST Condition { get; }

        public IAST IfTrue { get; }

        public IAST IfFalse { get; }

        public Type ReturnType => this.IfTrue.ReturnType;

        public IfExpression(IAST cond, IAST iftrue, IAST iffalse) {
            this.Condition = cond;
            this.IfTrue = iftrue;
            this.IfFalse = iffalse;

            if (iffalse.ReturnType != iffalse.ReturnType) {
                throw new Exception();
            }
        }

        public void Accept(IASTVisitor visitor) {
            visitor.Visit(this);
        }
    }

    public class LetExpression : IAST {
        public string Name { get; }

        public IAST AssignExpression { get; }

        public IAST ScopeExpression { get; }

        public Type ReturnType => this.ScopeExpression.ReturnType;

        public LetExpression(string name, IAST assign, IAST scope) {
            this.Name = name;
            this.AssignExpression = assign;
            this.ScopeExpression = scope;
        }

        public void Accept(IASTVisitor visitor) {
            visitor.Visit(this);
        }
    }

    public class FunctionCallExpression : IAST {
        public IAST InvokeTarget { get; }

        public IReadOnlyList<IAST> Arguments { get; }

        public Type ReturnType { get; }

        public FunctionCallExpression(Type returnType, IAST target, IReadOnlyList<IAST> args) {
            this.ReturnType = returnType;
            this.InvokeTarget = target;
            this.Arguments = args;
        }

        public void Accept(IASTVisitor visitor) {
            visitor.Visit(this);
        }
    }
}