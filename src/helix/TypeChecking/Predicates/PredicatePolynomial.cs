﻿using Helix.Collections;

namespace Helix.TypeChecking.Predicates;

public record PredicatePolynomial : ISyntaxPredicate {
    public ValueSet<ISyntaxPredicate> Operands { get; }

    public PredicatePolynomial(ISyntaxPredicate operand) {
        this.Operands = new[] { operand }.ToValueSet();
    }

    public PredicatePolynomial(IEnumerable<ISyntaxPredicate> operands) {
        this.Operands = operands.ToValueSet();
    }

    public override ISyntaxPredicate And(ISyntaxPredicate other) {
        if (other is PredicateTerm term) {
            return term.And(this);
        }
        else {
            return new PredicateTerm(new[] { this }).And(other);
        }
    }

    public override ISyntaxPredicate Or(ISyntaxPredicate other) {
        if (other is PredicatePolynomial poly) {
            return poly.Operands.Aggregate((ISyntaxPredicate)this, (x, y) => x.Or(y));
        }
        else if (other is PredicateTerm term) {
            return term
                .Operands
                .Select(this.Or)
                .Aggregate((x, y) => x.And(y));
        }
        else if (other is SyntaxPredicateLeaf leaf) {
            foreach (var op in this.Operands) {
                if (leaf.TryOrWith(op, out var result)) {
                    var newOps = this.Operands
                        .Remove(op)
                        .Add(result);

                    return new PredicatePolynomial(newOps);
                }
            }
        }
                
        return new PredicatePolynomial(this.Operands.Add(other));
    }

    public override ISyntaxPredicate Negate() {
        var ops = this.Operands
            .Select(x => x.Negate())
            .Select(x => new PredicatePolynomial(x));

        return new PredicateTerm(ops);
    }

    public override TypeFrame ApplyToTypes(TypeFrame types) {
        if (this.Operands.Count == 1) {
            return this.Operands.First().ApplyToTypes(types);
        }

        return types;
    }
        
    public override string ToString() {
        return "(" + string.Join(" or ", this.Operands) + ")";
    }
}