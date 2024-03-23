using Helix.Common;
using Helix.Common.Hmm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.Interpreting {
    internal class LoopEvalJumpRemover : IHirVisitor<Option<IHirSyntax>> {
        public static LoopEvalJumpRemover Instance { get; } = new();

        public Option<IHirSyntax> VisitAddressOf(HirAddressOf syntax) => syntax;

        public Option<IHirSyntax> VisitArrayLiteral(HirArrayLiteral syntax) => syntax;

        public Option<IHirSyntax> VisitAssignment(HmmAssignment syntax) => syntax;

        public Option<IHirSyntax> VisitBinarySyntax(HirBinarySyntax syntax) => syntax;

        public Option<IHirSyntax> VisitBreak(HmmBreakSyntax syntax) => Option.None;

        public Option<IHirSyntax> VisitContinue(HmmContinueSyntax syntax) => Option.None;

        public Option<IHirSyntax> VisitDereference(HirDereference syntax) => syntax;

        public Option<IHirSyntax> VisitFunctionDeclaration(HirFunctionDeclaration syntax) => syntax;

        public Option<IHirSyntax> VisitFunctionForwardDeclaration(HmmFunctionForwardDeclaration syntax) => syntax;

        public Option<IHirSyntax> VisitIfExpression(HirIfExpression syntax) {
            return new HirIfExpression() {
                Location = syntax.Location,
                Condition = syntax.Condition,
                AffirmativeBody = syntax.AffirmativeBody.SelectMany(x => x.Accept(this)).ToArray(),
                NegativeBody = syntax.NegativeBody.SelectMany(x => x.Accept(this)).ToArray()
            };
        }

        public Option<IHirSyntax> VisitIndex(HirIndex syntax) => syntax;

        public Option<IHirSyntax> VisitInvoke(HirInvokeSyntax syntax) => syntax;

        public Option<IHirSyntax> VisitIs(HirIsSyntax syntax) => syntax;

        public Option<IHirSyntax> VisitLoop(HirLoopSyntax syntax) {
            return new HirLoopSyntax() {
                Location = syntax.Location,
                Body = syntax.Body.SelectMany(x => x.Accept(this)).ToArray()
            };
        }

        public Option<IHirSyntax> VisitMemberAccess(HirMemberAccess syntax) => syntax;

        public Option<IHirSyntax> VisitNew(HirNewSyntax syntax) => syntax;

        public Option<IHirSyntax> VisitReturn(HmmReturnSyntax syntax) => syntax;

        public Option<IHirSyntax> VisitStructDeclaration(HmmStructDeclaration syntax) => syntax;

        public Option<IHirSyntax> VisitTypeDeclaration(HmmTypeDeclaration syntax) => syntax;

        public Option<IHirSyntax> VisitUnaryOperator(HirUnaryOperator syntax) => syntax;

        public Option<IHirSyntax> VisitUnionDeclaration(HmmUnionDeclaration syntax) => syntax;

        public Option<IHirSyntax> VisitVariableStatement(HirVariableStatement syntax) => syntax;
    }
}
