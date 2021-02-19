using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Containers {
    public class MemberAccessParsedSyntax : IParsedSyntax  {
        public TokenLocation Location { get; set; }

        public IParsedSyntax Target { get; set; }

        public string MemberName { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.Target = this.Target.CheckNames(names);

            return this;
        }

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            var target = this.Target.CheckTypes(names, types);

            // If this is an array we can get the size
            if (target.ReturnType.AsArrayType().TryGetValue(out var arrayType)) {
                if (this.MemberName == "size") {
                    return new MemberAccessTypeCheckedSyntax() {
                        Location = this.Location,
                        ReturnType = TrophyType.Integer,
                        Lifetimes = ImmutableHashSet.Create<IdentifierPath>(),
                        MemberName = this.MemberName,
                        Target = target
                    };
                }
            }
            
            // If this is a named type it could be a struct
            if(target.ReturnType.AsNamedType().TryGetValue(out var path)) {
                // If this is a struct we can access the fields
                if (types.TryGetStruct(path).TryGetValue(out var sig)) {
                    // Make sure this field is present
                    var fieldOpt = sig
                        .Members.Where(x => x.MemberName == this.MemberName)
                        .FirstOrNone();

                    if (fieldOpt.TryGetValue(out var field)) {
                        return new MemberAccessTypeCheckedSyntax() {
                            Location = this.Location,
                            ReturnType = field.MemberType,
                            Lifetimes = target.Lifetimes,
                            MemberName = this.MemberName,
                            Target = target
                        };
                    }
                }
            }

            throw TypeCheckingErrors.MemberUndefined(this.Location, target.ReturnType, this.MemberName);
        }
    }

    public class MemberAccessTypeCheckedSyntax : ISyntax {
        public TokenLocation Location { get; set; }

        public TrophyType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public ISyntax Target { get; set; }

        public string MemberName { get; set; }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var target = this.Target.GenerateCode(declWriter, statWriter);

            return CExpression.MemberAccess(target, this.MemberName);
        }
    }
}
