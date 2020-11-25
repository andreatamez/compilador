// Andrea Tamez A01176494
using System;
using System.IO;

namespace MeMySelf
{
    public class MeMySelf
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0) {
                Scanner scanner = new Scanner(args[0]);
                bool debugMode = false;
                if (args.Length > 1)
                {
                    debugMode = bool.Parse(args[1]);
                }

                /*Inicializo el parser */
                Parser parser = new Parser(scanner);
                parser.codegen = new CodeGenerator(new Logger(parser), debugMode);
                parser.cuboSemantico = new CuboSemantico();
                parser.DebugMode = debugMode;
                //Llamamos el parser y aqui empiez a leer el codigo.
                parser.Parse();

                if (parser.errors.count == 0) {
                    parser.codegen.SaveCodeToObjectFile("a.obj");
                    CodeGenerator cgTest = CodeGenerator.ReadCodeFromObjectFile(new Logger(parser), "a.obj");
                    cgTest.SaveCodeToObjectFile("a2.obj");

                    VM vm = new VM(cgTest);
                    vm.DebugMode = debugMode;
                    vm.Interpret(Console.In);
                    // parser.codeGen.Decode();
                    /*
                    Console.Out.WriteLine("=======================");
                    int count = 0;
                    foreach (Quadruple q in parser.codegen.quads)
                    {
                        Console.Out.WriteLine($"[{count}] - {q}");
                        ++count;
                    }

                    /*Stream input = null;
                    if (args.Length > 1)
                    {
                        input = new FileStream(args[1], FileMode.Open);
                    }
                    */

                    // codigo para ejecutar el programa
                    // parser.codeGen.Interpret(input);

                }
            } else {
                Console.WriteLine("-- No source file specified");
            }
        }
    }

}