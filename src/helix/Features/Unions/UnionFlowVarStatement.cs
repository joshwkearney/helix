//using Helix.Analysis;
//using Helix.Analysis.TypeChecking;
//using Helix.Parsing;
//using Helix.Syntax;
//using System;
//using System.Collections.Generic;
//using System.Data.Common;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Helix.Features.Unions {
//    public class UnionFlowVarStatement : ISyntaxTree {
//        public VariableSignature ShadowedVariable { get; }

//        public VariableSignature NewVariable { get; }

//        public string UnionMemberName { get; }

//        public TokenLocation Location { get; }

//        public IEnumerable<ISyntaxTree> Children => Array.Empty<ISyntaxTree>();

//        public bool IsPure => true;

//        public UnionFlowVarStatement(TokenLocation loc, string member, VariableSignature oldVar, VariableSignature newVar) {
//            this.Location = loc;
//            this.UnionMemberName = member;
//            this.ShadowedVariable = oldVar;
//            this.NewVariable = newVar;
//        }

//        public ISyntaxTree CheckTypes(TypeFrame types) {
//            throw new NotImplementedException();
//        }
//    }
//}
