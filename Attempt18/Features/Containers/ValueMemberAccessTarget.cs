using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt19.Features.Containers.Arrays;
using Attempt19.Features.Containers.Structs;
using Attempt19.Features.Functions;
using Attempt19.Features.Variables;
using Attempt19.Parsing;
using Attempt19.Types;

namespace Attempt19.Features.Containers {
    public class ValueMemberAccessTarget : IMemberAccessTarget {
        private readonly ISyntax value;
        private readonly TypeChache types;

        public ValueMemberAccessTarget(ISyntax value, TypeChache types) {
            this.value = value;
            this.types = types;
        }

        public IMemberAccessTarget AccessMember(string name) {
            switch (this.value.ReturnType.Kind) {
                case LanguageTypeKind.Array:
                    if (name != "size") {
                        throw new Exception();
                    }

                    var syntax = new ArraySizeAccess() {
                        Scope = this.value.Scope,
                        Target = this.value,
                        ReturnType = IntType.Instance
                    };

                    return new ValueMemberAccessTarget(syntax, this.types);
                case LanguageTypeKind.Struct:
                    var structType = (StructType)this.value.ReturnType;
                    var info = this.types.Structs[structType.Path];
                    var mem = info.Members.FirstOrDefault(x => x.Name == name);

                    var syntax2 = new StructMemberAccess() {
                        MemberName = name,
                        Scope = this.value.Scope,
                        Target = this.value,
                        ReturnType = mem.Type
                    };

                    if (mem == null) {
                        throw new Exception();
                    }

                    return new ValueMemberAccessTarget(syntax2, this. types);
                default:
                    throw new Exception();
            }
        }

        public IMemberAccessTarget InvokeMember(string name, ISyntax[] arguments) {
            arguments = arguments.Select(x => x.ResolveTypes(this.types)).ToArray();

            if (this.value.ReturnType is StructType structType) {
                // Get the method
                var methodPath = this.types.Methods[structType][name];
                var sig = this.types.Functions[methodPath];

                if (arguments.Length + 1 != sig.Parameters.Length) {
                    throw new Exception("Argument and parameter counts must match");
                }

                for (int i = 0; i < arguments.Length; i++) {
                    if (arguments[i].ReturnType != sig.Parameters[i + 1].Type) {
                        throw new Exception("Arguments and parameter types must match");
                    }
                }

                var funcLiteral = new FunctionLiteral() {
                    FunctionPath = methodPath,
                    Scope = this.value.Scope,
                    ReturnType = new FunctionType(methodPath)
                };

                var funcInvoke = new FunctionInvoke() {
                    Target = funcLiteral,
                    Arguments = arguments.Prepend(this.value).ToArray(),
                    Scope = this.value.Scope,
                    ReturnType = sig.ReturnType,
                    TargetPath = methodPath,
                    TargetSignature = sig
                };

                return new ValueMemberAccessTarget(funcInvoke, this.types);
            }
            else {
                throw new Exception();
            }
        }

        public ISyntax ToSyntax() {
            return this.value;
        }
    }
}