using Helix.Common;
using Helix.Common.Hmm;
using Helix.Common.Tokens;
using Helix.Common.Types;
using System;
using System.Net.NetworkInformation;

namespace Helix.MiddleEnd.TypeChecking {
    internal class TypeUnifier {
        private delegate string TypeConverter(string name, TokenLocation location);

        private readonly AnalysisContext context;

        public TypeUnifier(AnalysisContext context) {
            this.context = context;
        }

        public bool CanConvert(IHelixType fromType, IHelixType toType) => GetConverter(fromType, toType).HasValue;

        public string Convert(string value, IHelixType toType, TokenLocation loc) {
            var fromType = context.Types[value];

            if (!GetConverter(fromType, toType).TryGetValue(out var unifier)) {
                throw TypeCheckException.TypeConversionFailed(loc, fromType, toType);
            }

            var result = unifier.Invoke(value, loc);
            var resultType = context.Types[result];

            Assert.IsTrue(resultType.GetSupertype() == toType);

            return result;
        }

        public Option<IHelixType> TryUnifyTypes(IHelixType type1, IHelixType type2) {
            if (GetConverter(type1, type2).TryGetValue(out _)) {
                return type2;
            }
            else if (GetConverter(type2, type1).TryGetValue(out _)) {
                return type1;
            }

            var sig1 = type1.GetSupertype();
            var sig2 = type2.GetSupertype();

            if (GetConverter(type1, sig2).TryGetValue(out _)) {
                return sig2;
            }
            else if (GetConverter(type2, sig1).TryGetValue(out _)) {
                return sig1;
            }

            return Option.None;
        }

        public IHelixType UnifyTypes(IHelixType type1, IHelixType type2, TokenLocation loc) {
            if (!TryUnifyTypes(type1, type2).TryGetValue(out var type)) {
                throw TypeCheckException.TypeUnificationFailed(loc, type1, type2);
            }

            Assert.IsTrue(type1 == type || type1.GetSupertype() == type);
            Assert.IsTrue(type2 == type || type2.GetSupertype() == type);

            return type;
        }

        private Option<TypeConverter> GetConverter(IHelixType fromType, IHelixType toType) {
            if (fromType == toType) {
                return Option.Some<TypeConverter>((value, _) => value);
            }

            // From void
            if (fromType == VoidType.Instance) {
                if (toType == WordType.Instance) {
                    return Option.Some<TypeConverter>((_, _) => "0");
                }
                else if (toType == BoolType.Instance) {
                    return Option.Some<TypeConverter>((_, _) => "false");
                }
                else if (toType.GetArraySignature(context).TryGetValue(out var arraySig)) {
                    return Option.Some<TypeConverter>((value, loc) => {
                        var name = context.Names.GetConvertName();

                        var line = new HmmNewSyntax() {
                            Location = loc,
                            Result = name,
                            Type = toType
                        };

                        return line.Accept(context.TypeChecker).ResultName;
                    });
                }
            }                        

            // From singular word
            if (fromType is SingularWordType sing) {
                if (toType == WordType.Instance) {
                    return Option.Some<TypeConverter>((_, _) => sing.ToString());
                }
                else if (toType == BoolType.Instance && sing.Value == 1) {
                    return Option.Some<TypeConverter>((_, _) => "true");
                }
                else if (toType == BoolType.Instance && sing.Value == 0) {
                    return Option.Some<TypeConverter>((_, _) => "false");
                }
            }

            // From singular bool
            if (fromType is SingularBoolType sing2) {
                if (toType == BoolType.Instance) {
                    return Option.Some<TypeConverter>((_, _) => sing2.ToString());
                }
                else if (toType == WordType.Instance) {
                    return Option.Some<TypeConverter>((_, _) => sing2.Value ? "1" : "0");
                }
            }

            // From singular union
            if (fromType is SingularUnionType sing3) {
                if (toType == sing3.UnionType) {
                    return Option.Some<TypeConverter>((value, _) => value);
                }
                else if (GetConverter(sing3.Value, toType).TryGetValue(out var innerConverter)) {
                    return Option.Some<TypeConverter>((value, loc) => {
                        var memAccessName = this.context.Names.GetConvertName();

                        var memAccess = new HirIntrinsicUnionMemberAccess() {
                            Location = loc,
                            Result = memAccessName,
                            Operand = value,
                            ResultType = sing3.Value,
                            UnionMember = sing3.Member
                        };

                        this.context.Writer.AddLine(memAccess);
                        this.context.AliasTracker.RegisterLocal(memAccessName, sing3.Value);

                        return innerConverter(memAccessName, loc);
                    });
                }
            }

            // To union
            if (toType.GetUnionSignature(this.context).TryGetValue(out var unionType)) {
                if (FindUnionMember(fromType, unionType, context).TryGetValue(out var member)) {
                    return Option.Some<TypeConverter>((value, loc) => {
                        var name = context.Names.GetConvertName();

                        var syntax = new HmmNewSyntax() {
                            Location = loc,
                            Assignments = [
                                new HmmNewFieldAssignment() {
                                    Field = member.Name,
                                    Value = value
                                }
                            ],
                            Type = toType,
                            Result = name
                        };

                        return syntax.Accept(context.TypeChecker).ResultName;
                    });
                }
            }

            return Option.None;
        }

        private static Option<UnionMember> FindUnionMember(IHelixType fromType, UnionSignature unionType, AnalysisContext context) {
            // If this type exactly matches one union member, convert to that
            var matching = unionType.Members.Where(x => x.Type == fromType).ToArray();

            if (matching.Length == 1) {
                return matching[0];
            }

            // If this type can convert to exactly one member, convert to that
            matching = unionType.Members
                .Where(x => context.Unifier.CanConvert(fromType, x.Type))
                .ToArray();

            if (matching.Length == 1) {
                return matching[0];
            }

            return Option.None;
        }
    }
}
