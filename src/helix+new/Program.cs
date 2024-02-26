

using helix.front;

var contents = File.ReadAllText("../../../../../Resources/Program.helix");
var frontend = new HelixFrontEnd();

var result = frontend.Compile(contents);

var x = 4;