using helix.common.Hmm;
using Helix;
using Helix.Frontend;
using Helix.HelixMinusMinus;
using Helix.MiddleEnd;

var contents = File.ReadAllText("../../../../../Resources/Program.helix");
var frontend = new HelixFrontend();
var middleend = new HelixMiddleEnd();

contents = contents
    .Replace("\r\n", "\n")
    .Replace('\r', '\n')
    .Replace("\t", "    ");

try {
    var step1 = frontend.Compile(contents);
    var step2 = middleend.TypeCheck(step1);
    var result = HmmToString(step2);

    Console.WriteLine(result);
}
catch (HelixException ex) {
    Console.WriteLine(ex.CreateConsoleMessage(contents));

    //throw;
}

Console.Read();

static string HmmToString(IReadOnlyList<IHmmSyntax> hmm) {
    var stringifier = new HmmStringifier();
    var result = "";

    foreach (var line in hmm) {
        result += line.Accept(stringifier);
    }

    return result;
}