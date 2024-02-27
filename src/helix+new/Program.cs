using Helix;
using Helix.Frontend;

var contents = File.ReadAllText("../../../../../Resources/Program.helix");
var frontend = new HelixFrontend();

contents = contents
    .Replace("\r\n", "\n")
    .Replace('\r', '\n')
    .Replace("\t", "    ");

try {
    var result = frontend.CompileToString(contents);

    Console.WriteLine(result);
}
catch (HelixException ex) {
    Console.WriteLine(ex.CreateConsoleMessage(contents));

    //throw;
}

Console.Read();