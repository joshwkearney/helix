using Helix.MiddleEnd.Predicates;
using System.Text;

var A = (CnfTerm)new BooleanPredicate("A");
var B = (CnfTerm)new BooleanPredicate("B");
var C = (CnfTerm)new BooleanPredicate("C");

var step1 = A & B;
var step2 = !A & !B;
var result = step1 | step2;

Console.WriteLine(result);
Console.WriteLine(!result);
Console.Read();