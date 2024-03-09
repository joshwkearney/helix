using Helix.Common;
using Helix.Common.Hmm;
using Helix.Frontend;
using Helix.MiddleEnd;
using System.Diagnostics;

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

    var watch = new Stopwatch();
    watch.Start();

    step1 = frontend.Compile(contents);
    step2 = middleend.TypeCheck(step1);
    result = HmmToString(step2);

    watch.Stop();
    var ms = watch.ElapsedMilliseconds;

    Console.WriteLine(result);
    Console.WriteLine();
    Console.WriteLine($"Done in {ms} ms");
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