using Helix.Frontend;

var contents = File.ReadAllText("../../../../../Resources/Program.helix");
var frontend = new HelixFrontend();

var result = frontend.CompileToString(contents);

Console.WriteLine(result);
Console.Read();