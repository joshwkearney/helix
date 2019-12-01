using Attempt12.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt12.Analyzing {
    public delegate ISymbol TypeResolvable(TypeResolver resolver);

    public class TypeResolver {
        private readonly AnalyticScope scope;
        private readonly TypeResolvable resolvable;

        public TypeResolver(AnalyticScope scope, TypeResolvable resolvable) {
            this.scope = scope;
            this.resolvable = resolvable;
        }

        public ISymbol ResolveType() {
            return this.resolvable(this);
        }

        public ISymbol ResolveTypeIdentifier(string id) {
            if (this.scope.Types.TryGetValue(id, out var symbol)) {
                return symbol;
            }

            throw new Exception($"Use of undeclared type '{id}'");
        }

        public ISymbol ResolveFunctionType(TypeResolvable paramType, TypeResolvable returnType) {
            var param = paramType(this);
            var ret = returnType(this);

            if (param is ProductTypeSymbol product) {
                return new FunctionTypeSymbol(ret, product.FactorTypes);
            }
            else {
                return new FunctionTypeSymbol(ret, param);
            }
        }

        public ISymbol ResolveProductType(IEnumerable<TypeResolvable> types) {
            return new ProductTypeSymbol(types.Select(x => x(this)));
        }
    }
}