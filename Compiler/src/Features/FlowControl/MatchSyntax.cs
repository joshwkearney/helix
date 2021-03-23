using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Functions;
using Trophy.Parsing;

namespace Trophy.Features.FlowControl {
    public class MatchSyntaxA : ISyntaxA {
        private readonly ISyntaxA arg;
        private readonly IReadOnlyList<string> patterns;
        private readonly IReadOnlyList<ISyntaxA> patternExprs;
        private readonly IOption<ISyntaxA> elseExpr;

        public TokenLocation Location { get; }

        public MatchSyntaxA(
            TokenLocation location, 
            ISyntaxA arg, 
            IReadOnlyList<string> patterns, 
            IReadOnlyList<ISyntaxA> patternExprs,
            IOption<ISyntaxA> elseExpr) {

            this.Location = location;
            this.arg = arg;
            this.patterns = patterns;
            this.patternExprs = patternExprs;
            this.elseExpr = elseExpr;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var arg = this.arg.CheckNames(names);
            var elseExpr = this.elseExpr.Select(x => x.CheckNames(names));
            var path = names.Context.Scope.Append("$match" + names.GetNewVariableId());

            if (this.patterns.Count != this.patternExprs.Count) {
                throw new Exception("Internal compiler inconsistency");
            }

            var patternIds = new List<int>();
            var patternExprs = new List<ISyntaxB>();

            // Check for duplicate patterns
            FunctionsHelper.CheckForDuplicateParameters(this.Location, this.patterns);

            // Declare the pattern names and check pattern expressions
            for (int i = 0; i < this.patterns.Count; i++) {
                var pattern = this.patterns[i];
                var expr = this.patternExprs[i];
                var id = names.GetNewVariableId();

                var context = names.Context.WithScope(_ => path);
                names.WithContext(context, names => {
                    names.DeclareName(path.Append(pattern), NameTarget.Variable, IdentifierScope.LocalName);

                    patternIds.Add(id);
                    patternExprs.Add(expr.CheckNames(names));

                    return 0;
                });
            }

            return new MatchSyntaxB(
                this.Location, 
                path, 
                arg, 
                this.patterns, 
                patternIds, 
                patternExprs, 
                elseExpr);
        }
    }

    public class MatchSyntaxB : ISyntaxB {
        private readonly ISyntaxB arg;
        private readonly IdentifierPath path;
        private readonly IReadOnlyList<string> patterns;
        private readonly IReadOnlyList<ISyntaxB> patternExprs;
        private readonly IReadOnlyList<int> patternIds;
        private readonly IOption<ISyntaxB> elseExpr;

        public MatchSyntaxB(
            TokenLocation location,
            IdentifierPath path,
            ISyntaxB arg,
            IReadOnlyList<string> patterns,
            IReadOnlyList<int> patternIds,
            IReadOnlyList<ISyntaxB> patternExprs,
            IOption<ISyntaxB> elseExpr) {

            this.Location = location;
            this.path = path;
            this.arg = arg;
            this.patterns = patterns;
            this.patternIds = patternIds;
            this.patternExprs = patternExprs;
            this.elseExpr = elseExpr;
        }

        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get => this.patternExprs
                .Select(x => x.VariableUsage)
                .Aggregate(this.arg.VariableUsage, (x, y) => x.AddRange(y))
                .AddRange(this.elseExpr.Select(x => x.VariableUsage).GetValueOr(() => this.arg.VariableUsage));
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var arg = this.arg.CheckTypes(types);
            var elseExpr = this.elseExpr.Select(x => x.CheckTypes(types));

            // Make sure the target is a named type
            if (!arg.ReturnType.AsNamedType().TryGetValue(out var unionPath)) {
                throw TypeCheckingErrors.ExpectedUnionType(this.arg.Location, arg.ReturnType);
            }

            // Make sure the target is a union
            if (!types.TryGetUnion(unionPath).TryGetValue(out var unionSig)) {
                throw TypeCheckingErrors.ExpectedUnionType(this.arg.Location, arg.ReturnType);
            }

            // Declare pattern variables
            var infos = new List<VariableInfo>();
            var indicies = new List<int>();

            for (int i = 0; i < this.patterns.Count; i++) {
                var pattern = this.patterns[i];
                var expr = this.patternExprs[i];
                var id = this.patternIds[i];
                var memOpt = unionSig.Members.Where(x => x.MemberName == pattern).FirstOrNone();

                // Make sure this is a member on the target union
                if (!memOpt.TryGetValue(out var mem)) {
                    throw TypeCheckingErrors.MemberUndefined(this.Location, arg.ReturnType, pattern);
                }

                var lifetimes = ImmutableHashSet.Create<IdentifierPath>();
                if (mem.MemberType.GetCopiability(types) == TypeCopiability.Conditional) {
                    lifetimes = lifetimes.Union(arg.Lifetimes);
                }

                var info = new VariableInfo(
                    pattern,
                    mem.MemberType,
                    VariableDefinitionKind.LocalRef,
                    id,
                    lifetimes,
                    arg.Lifetimes);

                infos.Add(info);
                indicies.Add(unionSig.Members.ToList().IndexOf(mem));
                types.DeclareVariable(this.path.Append(pattern), info);
            }

            // Make sure all cases are covered
            if (!elseExpr.Any() && this.patterns.Count < unionSig.Members.Count) {
                throw TypeCheckingErrors.IncompleteMatch(this.Location);
            }

            // Type check pattern expressions
            var exprs = this.patternExprs.Select(x => x.CheckTypes(types)).ToArray();
            var returnType = exprs[0].ReturnType;

            // Try to unify pattern types
            for (int i = 1; i < exprs.Length; i++) {
                if (!types.TryUnifyTo(exprs[i], returnType).TryGetValue(out exprs[i])) {
                    throw TypeCheckingErrors.UnexpectedType(this.patternExprs[i].Location, returnType, exprs[i].ReturnType);
                }
            }

            // Unify else expression too
            if (elseExpr.TryGetValue(out var elseSyntax)) {
                if (!types.TryUnifyTo(elseSyntax, returnType).Select(Option.Some).TryGetValue(out elseExpr)) {
                    throw TypeCheckingErrors.UnexpectedType(this.elseExpr.GetValue().Location, returnType, elseSyntax.ReturnType);
                }
            }

            return new MatchSyntaxC(returnType, arg, infos, exprs, indicies, elseExpr);
        }
    }

    public class MatchSyntaxC : ISyntaxC {
        private static int counter = 0;

        private readonly ISyntaxC arg;
        private readonly IReadOnlyList<VariableInfo> patterns;
        private readonly IReadOnlyList<ISyntaxC> patternExprs;
        private readonly IReadOnlyList<int> patternIndicies;
        private readonly IOption<ISyntaxC> elseExpr;

        public MatchSyntaxC(
            ITrophyType returnType, 
            ISyntaxC arg, 
            IReadOnlyList<VariableInfo> patterns, 
            IReadOnlyList<ISyntaxC> patternExprs, 
            IReadOnlyList<int> patternIndicies,
            IOption<ISyntaxC> elseExpr) {

            this.ReturnType = returnType;
            this.arg = arg;
            this.patterns = patterns;
            this.patternExprs = patternExprs;
            this.patternIndicies = patternIndicies;
            this.elseExpr = elseExpr;
        }

        public ITrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes {
            get => this.patternExprs
                .Select(x => x.Lifetimes)
                .Aggregate(this.arg.Lifetimes, (x, y) => x.Union(y));
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            var arg = this.arg.GenerateCode(writer, statWriter);
            var tag = CExpression.MemberAccess(arg, "tag");
            var cases = new List<CStatement>();

            var varName = "$switch_temp_" + counter++;
            var varType = writer.ConvertType(this.ReturnType);

            statWriter.WriteStatement(CStatement.VariableDeclaration(varType, varName));

            for (int i = 0; i < this.patternExprs.Count; i++) {
                var info = this.patterns[i];
                var index = this.patternIndicies[i];

                var member = CExpression.MemberAccess(CExpression.MemberAccess(arg, "data"), info.Name);
                var decl = CStatement.VariableDeclaration(writer.ConvertType(info.Type), "$" + info.Name + info.UniqueId, member);

                var body = new List<CStatement>(new[] { decl, CStatement.NewLine() });
                var bodyWriter = new CStatementWriter();
                bodyWriter.StatementWritten += (s, e) => body.Add(e);

                var result = this.patternExprs[i].GenerateCode(writer, bodyWriter);

                body.Add(CStatement.Assignment(CExpression.VariableLiteral(varName), result));
                body.Add(CStatement.Break());
                cases.Add(CStatement.CaseLabel(CExpression.IntLiteral(index), body));
            }

            if (this.elseExpr.TryGetValue(out var elseExpr)) {
                var body = new List<CStatement>();
                var bodyWriter = new CStatementWriter();
                bodyWriter.StatementWritten += (s, e) => body.Add(e);

                var result = elseExpr.GenerateCode(writer, bodyWriter);

                body.Add(CStatement.Assignment(CExpression.VariableLiteral(varName), result));
                body.Add(CStatement.Break());
                cases.Add(CStatement.DefaultLabel(body));
            }

            statWriter.WriteStatement(CStatement.SwitchStatement(tag, cases));

            return CExpression.VariableLiteral(varName);
        }
    }
}