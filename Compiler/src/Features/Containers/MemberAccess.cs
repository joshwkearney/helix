using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Containers {
    public class MemberAccessSyntaxA : ISyntaxA  {
        private readonly ISyntaxA target;
        private readonly string memberName;
        public TokenLocation Location { get; }

        public MemberAccessSyntaxA(TokenLocation location, ISyntaxA target, string memberName) {
            this.Location = location;
            this.target = target;
            this.memberName = memberName;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            var target = this.target.CheckNames(names);

            return new MemberAccessSyntaxB(this.Location, target, this.memberName);
        }
    }

    public class MemberAccessSyntaxB : ISyntaxB {
        private readonly ISyntaxB target;
        private readonly string memberName;

        public TokenLocation Location { get; }

        public MemberAccessSyntaxB(TokenLocation location, ISyntaxB target, string memberName) {
            this.Location = location;
            this.target = target;
            this.memberName = memberName;
        }


        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var target = this.target.CheckTypes(types);

            // If this is an array we can get the size
            if (target.ReturnType.AsArrayType().TryGetValue(out _)) {
                if (this.memberName == "size") {
                    return new MemberAccessTypeCheckedSyntax(
                        target, 
                        this.memberName, 
                        TrophyType.Integer, 
                        ImmutableHashSet.Create<IdentifierPath>());
                }
            }

            // If this is a fixed array we can get the size
            if (target.ReturnType.AsFixedArrayType().TryGetValue(out _)) {
                if (this.memberName == "size") {
                    return new MemberAccessTypeCheckedSyntax(
                        target,
                        this.memberName,
                        TrophyType.Integer,
                        ImmutableHashSet.Create<IdentifierPath>());
                }
            }

            // If this is a named type it could be a struct
            if (target.ReturnType.AsNamedType().TryGetValue(out var path)) {
                // If this is a struct we can access the fields
                if (types.TryGetStruct(path).TryGetValue(out var sig)) {
                    // Make sure this field is present
                    var fieldOpt = sig
                        .Members.Where(x => x.MemberName == this.memberName)
                        .FirstOrNone();

                    if (fieldOpt.TryGetValue(out var field)) {
                        var lifetimes = ImmutableHashSet.Create<IdentifierPath>();

                        if (field.MemberType.GetCopiability(types) == TypeCopiability.Conditional) {
                            lifetimes = lifetimes.Union(target.Lifetimes);
                        }

                        return new MemberAccessTypeCheckedSyntax(
                            target,
                            this.memberName,
                            field.MemberType,
                            lifetimes);
                    }
                }
            }

            throw TypeCheckingErrors.MemberUndefined(this.Location, target.ReturnType, this.memberName);
        }
    }

    public class MemberAccessTypeCheckedSyntax : ISyntaxC {
        private readonly ISyntaxC target;
        private readonly string memberName;

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; }

        public MemberAccessTypeCheckedSyntax(
            ISyntaxC target, 
            string memberName, 
            TrophyType returnType, 
            ImmutableHashSet<IdentifierPath> lifetimes) {

            this.target = target;
            this.memberName = memberName;
            this.ReturnType = returnType;
            this.Lifetimes = lifetimes;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            // Optimization: insert a fixed array's size directly instead of a member access
            if (this.target.ReturnType.AsFixedArrayType().TryGetValue(out var fixedArray)) {
                return CExpression.IntLiteral(fixedArray.Size);
            }

            var target = this.target.GenerateCode(declWriter, statWriter);

            return CExpression.MemberAccess(target, this.memberName);
        }
    }
}
