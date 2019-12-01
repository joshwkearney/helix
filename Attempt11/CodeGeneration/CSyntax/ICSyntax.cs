//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Attempt10.CodeGeneration.CSyntax {
//    public interface ICSyntaxVisitor {
//        void Visit(CBlock syntax);
//        void Visit(CFunctionPointerTypedef syntax);
//        void Visit(CStructTypedef syntax);
//        void Visit(CFunctionDeclaration syntax);
//        void Visit(CAssignment syntax);
//    }

//    public interface ICSyntax {
//        void Accept(ICSyntaxVisitor visitor);
//    }

//   // public class 

//    public class CAssignment : ICSyntax {
//        public ICSyntax AssignTarget { get; }

//        public ICSyntax AssignValue { get; }

//        public CAssignment(ICSyntax target, ICSyntax value) {
//            this.AssignTarget = target;
//            this.AssignValue = value;
//        }

//        public void Accept(ICSyntaxVisitor visitor) => visitor.Visit(this);
//    }

//    public class CFunctionDeclaration : ICSyntax {
//        public string ReturnType { get; }

//        public string Name { get; }

//        public IReadOnlyList<CVariable> Parameters { get; }

//        public CBlock Body { get; }

//        public CFunctionDeclaration(string name, string returnType, IReadOnlyList<CVariable> pars, CBlock body) {
//            this.Name = name;
//            this.ReturnType = returnType;
//            this.Parameters = pars;
//            this.Body = body;
//        }

//        public void Accept(ICSyntaxVisitor visitor) => visitor.Visit(this);
//    }

//    public class CVariable {
//        public string Type { get; }

//        public string Name { get; }

//        public CVariable(string name, string type) {
//            this.Name = name;
//            this.Type = type;
//        }
//    }

//    public class CStructTypedef : ICSyntax {
//        public string Name { get; }

//        public IReadOnlyList<CVariable> Members { get; }

//        public CStructTypedef(string name, IReadOnlyList<CVariable> members) {
//            this.Name = name;
//            this.Members = members;
//        }

//        public void Accept(ICSyntaxVisitor visitor) => visitor.Visit(this);
//    }

//    public class CFunctionPointerTypedef : ICSyntax {
//        public string Name { get; }

//        public string ReturnType { get; }

//        public IReadOnlyList<string> ParameterTypes { get; }

//        public CFunctionPointerTypedef(string name, string returnType, IReadOnlyList<string> paramTypes) {
//            this.Name = name;
//            this.ReturnType = returnType;
//            this.ParameterTypes = paramTypes;
//        }

//        public void Accept(ICSyntaxVisitor visitor) => visitor.Visit(this);
//    }

//    public class CBlock : ICSyntax {
//        public IReadOnlyList<ICSyntax> Statements { get; }

//        public CBlock(IReadOnlyList<ICSyntax> stats) {
//            this.Statements = stats;
//        }

//        public void Accept(ICSyntaxVisitor visitor) => visitor.Visit(this);
//    }
//}
