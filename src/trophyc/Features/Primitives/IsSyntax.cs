using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Features.Primitives {
    public class IsSyntaxA : ISyntaxA {
        public TokenLocation Location { get; }

        public ISyntaxA Argument { get; }

        public string Pattern { get; }

        public IsSyntaxA(TokenLocation loc, ISyntaxA arg, string pattern) {
            this.Location = loc;
            this.Argument = arg;
            this.Pattern = pattern;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var arg = this.Argument.CheckNames(names);

            return new IsSyntaxB(this.Location, arg, this.Pattern);
        }
    }

    public class IsSyntaxB : ISyntaxB {
        public TokenLocation Location { get; }

        public ISyntaxB Argument { get; }

        public string Pattern { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.Argument.VariableUsage;
        }

        public IsSyntaxB(TokenLocation loc, ISyntaxB arg, string pattern) {
            this.Location = loc;
            this.Argument = arg;
            this.Pattern = pattern;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var arg = this.Argument.CheckTypes(types);

            if (!arg.ReturnType.AsNamedType().TryGetValue(out var path)) {
                throw TypeCheckingErrors.ExpectedUnionType(this.Argument.Location, arg.ReturnType);
            }

            if (!types.TryGetUnion(path).TryGetValue(out var unionSig)) {
                throw TypeCheckingErrors.ExpectedUnionType(this.Argument.Location, arg.ReturnType);
            }

            var memOpt = unionSig.Members.Where(x => x.MemberName == this.Pattern).FirstOrNone();
            if (!memOpt.TryGetValue(out var mem)) {
                throw TypeCheckingErrors.MemberUndefined(this.Location, arg.ReturnType, this.Pattern);
            }

            int index = unionSig.Members.ToList().IndexOf(mem);

            return new IsSyntaxC(arg, index, ITrophyType.Boolean);
        }
    }

    public class IsSyntaxC : ISyntaxC {
        public ISyntaxC Argument { get; }

        public int MemberIndex { get; }

        public ITrophyType ReturnType { get; }

        public IsSyntaxC(ISyntaxC arg, int mem, ITrophyType retType) {
            this.Argument = arg;
            this.MemberIndex = mem;
            this.ReturnType = retType;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            var arg = this.Argument.GenerateCode(writer, statWriter);

            return CExpression.BinaryExpression(
                CExpression.MemberAccess(arg, "tag"),
                CExpression.IntLiteral(this.MemberIndex),
                BinaryOperation.EqualTo);
        }
    }
}