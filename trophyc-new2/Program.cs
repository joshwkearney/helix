using Trophy;

var header = File.ReadAllText("Resources/trophy.h");
var contents = File.ReadAllText("Resources/Program.trophy");
var parse = new TrophyCompiler(header, contents).Compile();

Console.WriteLine(parse);
Console.Read();