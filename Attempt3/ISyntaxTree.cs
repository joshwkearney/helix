using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt3 {
    public interface ISyntaxTree {
        void Accept(ISyntaxVisitor visitor);
    }

    public class IntegerLiteral : ISyntaxTree, IValue {
        public int Value { get; }

        public IntegerLiteral(int value) {
            this.Value = value;
        }

        public override string ToString() {
            return this.Value.ToString();
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }

        public void Accept(IValueVisitor visitor) {
            visitor.Visit(this);
        }

        public bool IsInvokable(IReadOnlyList<ILanguageType> types) {
            return false;
        }
    }

    public class IdentifierLiteral : ISyntaxTree {
        public string VariableName { get; }

        public IdentifierLiteral(string name) {
            this.VariableName = name;
        }

        public override string ToString() {
            return this.VariableName;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }

    public class FunctionCallExpression : ISyntaxTree {
        public ISyntaxTree Target { get; }

        public IReadOnlyList<ISyntaxTree> Arguments { get; }

        public FunctionCallExpression(ISyntaxTree target, params ISyntaxTree[] args) {
            this.Target = target;
            this.Arguments = args;
        }

        public override string ToString() {
            return $"{this.Target}({string.Join(", ", this.Arguments)})";
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }

    public struct FunctionParameter {
        public string Name { get; }

        public string Type { get; }

        public FunctionParameter(string type, string name) {
            this.Type = type;
            this.Name = name;
        }
    }

    public class FunctionLiteral : ISyntaxTree {
        public string ReturnTypeName { get; }

        public IReadOnlyList<FunctionParameter> Parameters { get; }

        public FunctionLiteral(string type, params FunctionParameter[] param) {
            this.ReturnTypeName = type;
            this.Parameters = param;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}