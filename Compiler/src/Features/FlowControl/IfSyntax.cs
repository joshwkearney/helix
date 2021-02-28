using System.Collections.Generic;
using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Features.FlowControl {
    public class IfSyntaxA : ISyntaxA {
        private readonly ISyntaxA cond, iftrue;
        private readonly IOption<ISyntaxA> iffalse;

        public IfSyntaxA(TokenLocation location, ISyntaxA cond, ISyntaxA iftrue) {
            this.Location = location;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = Option.None<ISyntaxA>();
        }

        public IfSyntaxA(TokenLocation location, ISyntaxA cond, ISyntaxA iftrue, ISyntaxA iffalse)
            : this(location, cond, iftrue) {

            this.iffalse = Option.Some(iffalse);
        }

        public TokenLocation Location { get; }

        public ISyntaxB CheckNames(INameRecorder names) {
            var iftrue = this.iftrue;

            if (!this.iffalse.TryGetValue(out var iffalse)) {
                iffalse = new VoidLiteralAB(this.Location);
                iftrue = new BlockSyntaxA(this.Location, new[] {
                    iftrue, new VoidLiteralAB(this.Location)
                });
            }

            return new IfSyntaxB(
                this.Location, 
                this.cond.CheckNames(names), 
                iftrue.CheckNames(names),
                iffalse.CheckNames(names));
        }
    }

    public class IfSyntaxB : ISyntaxB {
        private readonly ISyntaxB cond, iftrue, iffalse;

        public IfSyntaxB(TokenLocation location, ISyntaxB cond, ISyntaxB iftrue, ISyntaxB iffalse) {
            this.Location = location;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
        }

        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get => this.cond.VariableUsage
                .AddRange(this.iftrue.VariableUsage)
                .AddRange(this.iffalse.VariableUsage);
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var cond = this.cond.CheckTypes(types);
            var iftrue = this.iftrue.CheckTypes(types);
            var iffalse = this.iffalse.CheckTypes(types);

            // Make sure that the condition is a boolean
            if (!cond.ReturnType.IsBoolType) {
                throw TypeCheckingErrors.UnexpectedType(this.cond.Location, TrophyType.Boolean, cond.ReturnType);
            }

            // Make sure that the branches are the same type
            if (types.TryUnifyTo(iffalse, iftrue.ReturnType).TryGetValue(out var newNeg)) {
                iffalse = newNeg;
            }
            else if (types.TryUnifyTo(iftrue, iffalse.ReturnType).TryGetValue(out var newAffirm)) {
                iftrue = newAffirm;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(this.Location, iftrue.ReturnType, iffalse.ReturnType);
            }

            return new IfSyntaxC(cond, iftrue, iffalse);
        }
    }

    public class IfSyntaxC : ISyntaxC {
        private static int ifTemp = 0;

        private readonly ISyntaxC cond, iftrue, iffalse;

        public TrophyType ReturnType => this.iftrue.ReturnType;

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.iftrue.Lifetimes.Union(this.iffalse.Lifetimes);

        public IfSyntaxC(ISyntaxC cond, ISyntaxC iftrue, ISyntaxC iffalse) {
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var affirmList = new List<CStatement>();
            var negList = new List<CStatement>();

            var affirmWriter = new CStatementWriter();
            var negWriter = new CStatementWriter();

            affirmWriter.StatementWritten += (s, e) => affirmList.Add(e);
            negWriter.StatementWritten += (s, e) => negList.Add(e);

            var cond = this.cond.GenerateCode(declWriter, statWriter);
            var affirm = this.iftrue.GenerateCode(declWriter, affirmWriter);
            var neg = this.iffalse.GenerateCode(declWriter, negWriter);

            var tempName = "if_temp_" + ifTemp++;
            var returnType = declWriter.ConvertType(this.ReturnType);

            affirmList.Add(CStatement.Assignment(CExpression.VariableLiteral(tempName), affirm));
            negList.Add(CStatement.Assignment(CExpression.VariableLiteral(tempName), neg));

            statWriter.WriteStatement(CStatement.Comment("If statement"));
            statWriter.WriteStatement(CStatement.VariableDeclaration(returnType, tempName));
            statWriter.WriteStatement(CStatement.If(cond, affirmList, negList));
            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.VariableLiteral(tempName);
        }
    }
}