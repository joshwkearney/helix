using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Features.Primitives {
    public class IsSyntaxA : ISyntaxA {
        public TokenLocation Location { get; }

        public ISyntaxA Argument { get; }

        public string Pattern { get; }

        public IOption<string> PatternName { get; }

        public IsSyntaxA(TokenLocation loc, ISyntaxA arg, string pattern, IOption<string> patternName) {
            this.Location = loc;
            this.Argument = arg;
            this.Pattern = pattern;
            this.PatternName = patternName;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            if (this.PatternName.TryGetValue(out var name)) {
                var path = names.Context.Scope.Append(name);

                names.DeclareName(path, NameTarget.Variable, IdentifierScope.LocalName);
            }

            var arg = this.Argument.CheckNames(names);

            return new IsSyntaxB(this.Location, arg, this.Pattern, this.PatternName, names.GetNewVariableId(), names.Context.Scope);
        }
    }

    public class IsSyntaxB : ISyntaxB {
        public TokenLocation Location { get; }

        public ISyntaxB Argument { get; }

        public string Pattern { get; }

        public IOption<string> PatternName { get; }

        public int Id { get; }

        public IdentifierPath Scope { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.Argument.VariableUsage;
        }

        public IsSyntaxB(TokenLocation loc, ISyntaxB arg, string pattern, IOption<string> patternName, int id, IdentifierPath scope) {
            this.Location = loc;
            this.Argument = arg;
            this.Pattern = pattern;
            this.PatternName = patternName;
            this.Id = id;
            this.Scope = scope;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var arg = this.Argument.CheckTypes(types);

            // Make sure the type is a named type
            if (!arg.ReturnType.AsNamedType().TryGetValue(out var path)) {
                throw TypeCheckingErrors.ExpectedUnionType(this.Argument.Location, arg.ReturnType);
            }

            // Make sure the named type is a union type
            if (!types.TryGetUnion(path).TryGetValue(out var unionSig)) {
                throw TypeCheckingErrors.ExpectedUnionType(this.Argument.Location, arg.ReturnType);
            }

            // Make sure this pattern is a member on the union
            var memOpt = unionSig.Members.Where(x => x.MemberName == this.Pattern).FirstOrNone();
            if (!memOpt.TryGetValue(out var mem)) {
                throw TypeCheckingErrors.MemberUndefined(this.Location, arg.ReturnType, this.Pattern);
            }

            var index = unionSig.Members.ToList().IndexOf(mem);
            var infoOpt = Option.None<VariableInfo>();
            var defValueOpt = Option.None<ISyntaxC>();

            if (this.PatternName.TryGetValue(out var name)) {
                // If we have a name, make sure the union member's type has a default value
                if (types.TryUnifyTo(new VoidLiteralC(), mem.MemberType).TryGetValue(out var defValue)) {
                    defValueOpt = Option.Some(defValue);
                }
                else {
                    throw TypeCheckingErrors.TypeWithoutDefaultValue(this.Location, mem.MemberType);
                }

                var info = new VariableInfo(name, new VarRefType(mem.MemberType, false), VariableKind.VarVariable, VariableSource.Local, this.Id);
                infoOpt = Option.Some(info);

                types.DeclareName(this.Scope.Append(name), NamePayload.FromVariable(info));
            }

            return new IsSyntaxC(arg, index, this.Pattern, infoOpt, defValueOpt);
        }
    }

    public class IsSyntaxC : ISyntaxC {
        private static int counter = 0;

        public ISyntaxC Argument { get; }

        public int MemberIndex { get; }

        public string MemberName { get; }

        public ITrophyType ReturnType => ITrophyType.Boolean;

        public IOption<VariableInfo> PatternInfo { get; }

        public IOption<ISyntaxC> PatternDefaultValue { get; }

        public IsSyntaxC(ISyntaxC arg, int mem, string memName, IOption<VariableInfo> patternInfo, IOption<ISyntaxC> defValue) {
            this.Argument = arg;
            this.MemberIndex = mem;
            this.MemberName = memName;
            this.PatternInfo = patternInfo;
            this.PatternDefaultValue = defValue;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            var arg = this.Argument.GenerateCode(writer, statWriter);
            var argName = "$is_temp_" + counter++;          
            var argType = writer.ConvertType(this.Argument.ReturnType);
            var argDecl = CStatement.VariableDeclaration(argType, argName, arg);
            var exprOpt = this.PatternDefaultValue.Select(x => x.GenerateCode(writer, statWriter));

            statWriter.WriteStatement(CStatement.Comment("Is Expression"));
            statWriter.WriteStatement(argDecl);

            var tagTest = CExpression.BinaryExpression(
                CExpression.MemberAccess(CExpression.VariableLiteral(argName), "tag"),
                CExpression.IntLiteral(this.MemberIndex),
                BinaryOperation.EqualTo);

            if (this.PatternInfo.TryGetValue(out var info)) {
                var type = writer.ConvertType((info.Type as VarRefType).InnerType);

                var assign = CStatement.VariableDeclaration(type, "$" + info.Name + info.UniqueId, exprOpt.GetValue());
                var iff = CStatement.If(tagTest, new[] { 
                    CStatement.Assignment(
                        CExpression.VariableLiteral("$" + info.Name + info.UniqueId), 
                        CExpression.MemberAccess(CExpression.MemberAccess(CExpression.VariableLiteral(argName), "data"), this.MemberName))
                });

                statWriter.WriteStatement(assign);
                statWriter.WriteStatement(iff);
            }

            statWriter.WriteStatement(CStatement.NewLine());

            return tagTest;
        }
    }
}