using Helix;

var header = File.ReadAllText("../../../../../Resources/Helix.h");
var contents = File.ReadAllText("../../../../../Resources/Program.helix");

try {    
    var parse = new HelixCompiler(header, contents).Compile();

    File.WriteAllText("../../../../../Resources/Program.c", parse);
    Console.WriteLine(parse);
}
catch (HelixException ex) {
    Console.WriteLine(ex.CreateConsoleMessage(contents));
}

Console.Read();
