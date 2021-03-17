using System;
using System.Collections.Generic;
using System.Text;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Features.Meta;
using Trophy.Parsing;

namespace Trophy.Features.Primitives {
    public enum UnaryOperator {
        Not, Plus, Minus
    }

    public class UnarySyntaxA : ISyntaxA {
        private readonly UnaryOperator op;
        private readonly ISyntaxA arg;

        public TokenLocation Location { get; }

        public UnarySyntaxA(TokenLocation location, UnaryOperator op, ISyntaxA arg) {
            this.Location = location;
            this.op = op;
            this.arg = arg;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            if (this.op == UnaryOperator.Plus || this.op == UnaryOperator.Minus) {
                var syntax = (ISyntaxA)new AsSyntaxA(
                    this.Location, 
                    this.arg, 
                    new TypeAccessSyntaxA(
                        this.Location, 
                        ITrophyType.Integer));

                if (this.op == UnaryOperator.Minus) {
                    syntax = new BinarySyntaxA(
                        this.Location, 
                        new IntLiteralSyntax(this.Location, 0), 
                        syntax, 
                        BinaryOperation.Subtract);
                }

                return syntax.CheckNames(names);
            }
            else {
                var syntax = (ISyntaxA)new AsSyntaxA(
                   this.Location,
                   this.arg,
                   new TypeAccessSyntaxA(
                       this.Location,
                       ITrophyType.Boolean));

                syntax = new BinarySyntaxA(
                    this.Location, 
                    new BoolLiteralSyntax(this.Location, true), 
                    syntax, 
                    BinaryOperation.Xor);

                return syntax.CheckNames(names);
            }
        }
    }
}