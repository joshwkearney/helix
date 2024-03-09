using Helix.Common;
using Helix.Common.Hmm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.Interpreting {
    internal class LoopEvalJumpRemover : IHmmVisitor<Option<IHmmSyntax>> {
        public static LoopEvalJumpRemover Instance { get; } = new();

        public Option<IHmmSyntax> VisitAddressOf(HmmAddressOf syntax) => syntax;

        public Option<IHmmSyntax> VisitArrayLiteral(HmmArrayLiteral syntax) => syntax;

        public Option<IHmmSyntax> VisitAssignment(HmmAssignment syntax) => syntax;

        public Option<IHmmSyntax> VisitAsSyntax(HmmAsSyntax syntax) => syntax;

        public Option<IHmmSyntax> VisitBinarySyntax(HmmBinarySyntax syntax) => syntax;

        public Option<IHmmSyntax> VisitBreak(HmmBreakSyntax syntax) => Option.None;

        public Option<IHmmSyntax> VisitContinue(HmmContinueSyntax syntax) => Option.None;

        public Option<IHmmSyntax> VisitDereference(HmmDereference syntax) => syntax;

        public Option<IHmmSyntax> VisitFunctionDeclaration(HmmFunctionDeclaration syntax) => syntax;

        public Option<IHmmSyntax> VisitFunctionForwardDeclaration(HmmFunctionForwardDeclaration syntax) => syntax;

        public Option<IHmmSyntax> VisitIfExpression(HmmIfExpression syntax) {
            return new HmmIfExpression() {
                Location = syntax.Location,
                Affirmative = syntax.Affirmative,
                Negative = syntax.Negative,
                Condition = syntax.Condition,
                Result = syntax.Result,
                AffirmativeBody = syntax.AffirmativeBody.SelectMany(x => x.Accept(this)).ToArray(),
                NegativeBody = syntax.NegativeBody.SelectMany(x => x.Accept(this)).ToArray()
            };
        }

        public Option<IHmmSyntax> VisitIndex(HmmIndex syntax) => syntax;

        public Option<IHmmSyntax> VisitInvoke(HmmInvokeSyntax syntax) => syntax;

        public Option<IHmmSyntax> VisitIs(HmmIsSyntax syntax) => syntax;

        public Option<IHmmSyntax> VisitLoop(HmmLoopSyntax syntax) {
            return new HmmLoopSyntax() {
                Location = syntax.Location,
                Body = syntax.Body.SelectMany(x => x.Accept(this)).ToArray()
            };
        }

        public Option<IHmmSyntax> VisitMemberAccess(HmmMemberAccess syntax) => syntax;

        public Option<IHmmSyntax> VisitNew(HmmNewSyntax syntax) => syntax;

        public Option<IHmmSyntax> VisitReturn(HmmReturnSyntax syntax) => syntax;

        public Option<IHmmSyntax> VisitStructDeclaration(HmmStructDeclaration syntax) => syntax;

        public Option<IHmmSyntax> VisitTypeDeclaration(HmmTypeDeclaration syntax) => syntax;

        public Option<IHmmSyntax> VisitUnaryOperator(HmmUnaryOperator syntax) => syntax;

        public Option<IHmmSyntax> VisitUnionDeclaration(HmmUnionDeclaration syntax) => syntax;

        public Option<IHmmSyntax> VisitVariableStatement(HmmVariableStatement syntax) => syntax;
    }
}
