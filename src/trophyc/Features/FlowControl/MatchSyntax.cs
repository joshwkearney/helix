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
    public class MatchPatternA {
        public string Member { get; }

        public IOption<string> Name { get; }

        public ISyntaxA Expression { get; }

        public MatchPatternA(string member, IOption<string> name, ISyntaxA expression) {
            this.Member = member;
            this.Name = name;
            this.Expression = expression;
        }
    }

    public class MatchPatternB {
        public string Member { get; }

        public IOption<string> Name { get; }

        public ISyntaxB Expression { get; }

        public int Id { get; }

        public MatchPatternB(string member, IOption<string> name, ISyntaxB expression, int id) {
            this.Member = member;
            this.Name = name;
            this.Expression = expression;
            this.Id = id;
        }
    }

    public class MatchPatternC {
        public string Member { get; }

        public ISyntaxC Expression { get; }

        public IOption<VariableInfo> Info { get; }

        public int Index { get; }

        public MatchPatternC(string member, ISyntaxC expression, IOption<VariableInfo> info, int index) {
            this.Member = member;
            this.Expression = expression;
            this.Info = info;
            this.Index = index;
        }
    }

    public class MatchSyntaxA : ISyntaxA {
        private readonly ISyntaxA arg;
        private readonly IReadOnlyList<MatchPatternA> patterns;
        private readonly IOption<ISyntaxA> elseExpr;

        public TokenLocation Location { get; }

        public MatchSyntaxA(
            TokenLocation location, 
            ISyntaxA arg, 
            IReadOnlyList<MatchPatternA> patterns, 
            IOption<ISyntaxA> elseExpr) {

            this.Location = location;
            this.arg = arg;
            this.patterns = patterns;
            this.elseExpr = elseExpr;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var arg = this.arg.CheckNames(names);
            var elseExpr = this.elseExpr.Select(x => x.CheckNames(names));
            var newPatterns = new List<MatchPatternB>();

            // Check for duplicate pattern members
            FunctionsHelper.CheckForDuplicateParameters(this.Location, this.patterns.Select(x => x.Member));

            // Declare the pattern names and check pattern expressions
            foreach (var pattern in this.patterns) {
                var path = names.Context.Scope.Append("$match_" + pattern.Member);
                var context = names.Context.WithScope(_ => path);

                var expr = names.WithContext(context, names => {
                    if (pattern.Name.TryGetValue(out var name)) {
                        names.DeclareName(path.Append(name), NameTarget.Variable, IdentifierScope.LocalName);
                    }

                    return pattern.Expression.CheckNames(names);
                });

                var id = names.GetNewVariableId();
                var newPattern = new MatchPatternB(pattern.Member, pattern.Name, expr, id);

                newPatterns.Add(newPattern);
            }

            return new MatchSyntaxB(
                this.Location, 
                names.Context.Scope, 
                arg, 
                newPatterns,
                elseExpr);
        }
    }

    public class MatchSyntaxB : ISyntaxB {
        private readonly ISyntaxB arg;
        private readonly IdentifierPath path;
        private readonly IReadOnlyList<MatchPatternB> patterns;
        private readonly IOption<ISyntaxB> elseExpr;

        public MatchSyntaxB(
            TokenLocation location,
            IdentifierPath path,
            ISyntaxB arg,
            IReadOnlyList<MatchPatternB> patterns,
            IOption<ISyntaxB> elseExpr) {

            this.Location = location;
            this.path = path;
            this.arg = arg;
            this.patterns = patterns;
            this.elseExpr = elseExpr;
        }

        public TokenLocation Location { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.patterns
                .Select(x => x.Expression)
                .SelectMany(x => x.VariableUsage)
                .Concat(this.arg.VariableUsage)
                .ToImmutableHashSet()
                .Union(this.elseExpr.Select(x => x.VariableUsage).GetValueOr(() => this.arg.VariableUsage));
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
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

            var newPatterns = new List<MatchPatternC>();

            foreach (var pattern in this.patterns) {
                var memOpt = unionSig.Members.Where(x => x.MemberName == pattern.Member).FirstOrNone();

                // Make sure this is a member on the target union
                if (!memOpt.TryGetValue(out var mem)) {
                    throw TypeCheckingErrors.MemberUndefined(this.Location, arg.ReturnType, pattern.Member);
                }

                var infoOpt = Option.None<VariableInfo>();

                if (pattern.Name.TryGetValue(out var name)) {
                    var info = new VariableInfo(
                        name: name,
                        innerType: new VarRefType(mem.MemberType, false),
                        kind: mem.Kind,
                        source: mem.Kind == VariableKind.Value ? VariableSource.Local : VariableSource.Parameter,
                        pattern.Id
                    );

                    infoOpt = Option.Some(info);

                    types.DeclareName(this.path.Append("$match_" + pattern.Member).Append(name), NamePayload.FromVariable(info));
                }

                var expr = pattern.Expression.CheckTypes(types);
                var index = unionSig.Members.ToList().IndexOf(mem);
                var newPattern = new MatchPatternC(pattern.Member, expr, infoOpt, index);

                newPatterns.Add(newPattern);
            }

            // Make sure all cases are covered
            if (!elseExpr.Any() && this.patterns.Count < unionSig.Members.Count) {
                throw TypeCheckingErrors.IncompleteMatch(this.Location);
            }

            // Type check pattern expressions
            var returnType = newPatterns[0].Expression.ReturnType;

            var newExprs = new ISyntaxC[this.patterns.Count];
            newExprs[0] = newPatterns[0].Expression;

            // Try to unify pattern types
            for (int i = 1; i < this.patterns.Count; i++) {
                var pattern = newPatterns[i];

                if (!types.TryUnifyTo(pattern.Expression, returnType).TryGetValue(out var unified)) {
                    throw TypeCheckingErrors.UnexpectedType(this.patterns[i].Expression.Location, returnType, pattern.Expression.ReturnType);
                }
                else {
                    newExprs[i] = unified;
                }
            }

            // Unify else expression too
            if (elseExpr.TryGetValue(out var elseSyntax)) {
                if (!types.TryUnifyTo(elseSyntax, returnType).Select(Option.Some).TryGetValue(out elseExpr)) {
                    throw TypeCheckingErrors.UnexpectedType(this.elseExpr.GetValue().Location, returnType, elseSyntax.ReturnType);
                }
            }

            // Reinclude the unified expressions
            newPatterns = newPatterns.Select((x, i) => new MatchPatternC(x.Member, newExprs[i], x.Info, x.Index)).ToList();

            return new MatchSyntaxC(returnType, arg, newPatterns, elseExpr);
        }
    }

    public class MatchSyntaxC : ISyntaxC {
        private static int counter = 0;

        private readonly ISyntaxC arg;
        private readonly IReadOnlyList<MatchPatternC> patterns;
        private readonly IOption<ISyntaxC> elseExpr;

        public MatchSyntaxC(
            ITrophyType returnType, 
            ISyntaxC arg, 
            IReadOnlyList<MatchPatternC> patterns, 
            IOption<ISyntaxC> elseExpr) {

            this.ReturnType = returnType;
            this.arg = arg;
            this.patterns = patterns;
            this.elseExpr = elseExpr;
        }

        public ITrophyType ReturnType { get; }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            var arg = this.arg.GenerateCode(writer, statWriter);
            var tag = CExpression.MemberAccess(arg, "tag");
            var cases = new List<CStatement>();

            var varName = "$switch_temp_" + counter++;
            var varType = writer.ConvertType(this.ReturnType);

            statWriter.WriteStatement(CStatement.VariableDeclaration(varType, varName));

            foreach (var pattern in this.patterns) {
                var member = CExpression.MemberAccess(CExpression.MemberAccess(arg, "data"), pattern.Member);
                var body = new List<CStatement>();

                if (pattern.Info.TryGetValue(out var info)) {
                    var decl = CStatement.VariableDeclaration(writer.ConvertType(info.Type), "$" + info.Name + info.UniqueId, member);

                    body.Add(decl);
                    body.Add(CStatement.NewLine());
                }

                var bodyWriter = new CStatementWriter();
                bodyWriter.StatementWritten += (s, e) => body.Add(e);

                var result = pattern.Expression.GenerateCode(writer, bodyWriter);

                body.Add(CStatement.Assignment(CExpression.VariableLiteral(varName), result));
                body.Add(CStatement.Break());
                cases.Add(CStatement.CaseLabel(CExpression.IntLiteral(pattern.Index), body));
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