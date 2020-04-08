using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt18.Features.Containers.Arrays;
using Attempt18.Features.Containers.Structs;
using Attempt18.Features.Variables;
using Attempt18.Parsing;
using Attempt18.Types;

namespace Attempt18.Features.Containers {
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
                        CapturedVariables = new IdentifierPath[0],
                        ReturnType = IntType.Instance,
                        Scope = this.value.Scope,
                        Target = this.value
                    };

                    return new ValueMemberAccessTarget(syntax, this.types);
                case LanguageTypeKind.Struct:
                    var path = ((StructType)this.value.ReturnType).Path;
                    var info = this.types.Structs[path];

                    var mem = info.Members.FirstOrDefault(x => x.Name == name);
                    if (mem == null) {
                        throw new Exception();
                    }

                    var syntax2 = new StructMemberAccess() {
                        MemberName = name,
                        ReturnType = mem.Type,
                        Scope = this.value.Scope,
                        Target = this.value
                    };

                    return new ValueMemberAccessTarget(syntax2, this. types);
                default:
                    throw new Exception();
            }
        }

        public IMemberAccessTarget InvokeMember(string name, ISyntax[] arguments) {
            //if (this.value.ReturnType is StructType structType) {
                // Get the method
            //    var methodPath = this.types.Methods[structType][name];

            //    var funcSyntax = new FunctionLiteral
            //}
            //else {
                throw new Exception();
            //}
        }

        public ISyntax ToSyntax() {
            return this.value;
        }
    }
}