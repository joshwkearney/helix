using helix.common.Hmm;
using Helix.HelixMinusMinus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd {
    internal class TypeChecker : IHmmVisitor<string> {
        private readonly HmmWriter writer = new();

        public string VisitArrayLiteral(HmmArrayLiteral syntax) {
            throw new NotImplementedException();
        }

        public string VisitAssignment(HmmAssignment syntax) {
            throw new NotImplementedException();
        }

        public string VisitAsSyntax(HmmAsSyntax syntax) {
            throw new NotImplementedException();
        }

        public string VisitBinaryOperator(HmmBinaryOperator syntax) {
            throw new NotImplementedException();
        }

        public string VisitBreak(HmmBreakSyntax syntax) {
            throw new NotImplementedException();
        }

        public string VisitContinue(HmmContinueSyntax syntax) {
            throw new NotImplementedException();
        }

        public string VisitFunctionDeclaration(HmmFunctionDeclaration syntax) {
            throw new NotImplementedException();
        }

        public string VisitFunctionForwardDeclaration(HmmFunctionForwardDeclaration syntax) {
            throw new NotImplementedException();
        }

        public string VisitIfExpression(HmmIfExpression syntax) {
            throw new NotImplementedException();
        }

        public string VisitInvoke(HmmInvokeSyntax syntax) {
            throw new NotImplementedException();
        }

        public string VisitIs(HmmIsSyntax syntax) {
            throw new NotImplementedException();
        }

        public string VisitLoop(HmmLoopSyntax syntax) {
            throw new NotImplementedException();
        }

        public string VisitMemberAccess(HmmMemberAccess syntax) {
            throw new NotImplementedException();
        }

        public string VisitNew(HmmNewSyntax syntax) {
            throw new NotImplementedException();
        }

        public string VisitReturn(HmmReturnSyntax syntax) {
            throw new NotImplementedException();
        }

        public string VisitStructDeclaration(HmmStructDeclaration syntax) {
            throw new NotImplementedException();
        }

        public string VisitTypeDeclaration(HmmTypeDeclaration syntax) {
            throw new NotImplementedException();
        }

        public string VisitUnaryOperator(HmmUnaryOperator syntax) {
            throw new NotImplementedException();
        }

        public string VisitUnionDeclaration(HmmUnionDeclaration syntax) {
            throw new NotImplementedException();
        }

        public string VisitVariableStatement(HmmVariableStatement syntax) {
            throw new NotImplementedException();
        }
    }
}
