using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace dcpu_bf
{
    public class Brainfuck
    {
        public bool EndWithLoop = true;

        public static void Main(string[] args)
        {
            Brainfuck bf = new Brainfuck();
            string inputFile = null, outputFile = null;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith("-"))
                {
                    switch (arg)
                    {
                        case "-i":
                        case "--input-file":
                            if (args.Length == i + 1)
                            {
                                Console.WriteLine("Error: Invalid parameter: " + arg);
                                return;
                            }
                            inputFile = args[++i];
                            break;
                        case "-o":
                        case "--output-file":
                            if (args.Length == i + 1)
                            {
                                Console.WriteLine("Error: Invalid parameter: " + arg);
                                return;
                            }
                            outputFile = args[++i];
                            break;
                        case "-l":
                        case "--no-closing-loop":
                            bf.EndWithLoop = false;
                            break;
                        default:
                            Console.WriteLine("Error: Invalid parameter: " + arg);
                            return;
                    }
                }
                else
                {
                    if (inputFile == null)
                        inputFile = arg;
                    else if (outputFile == null)
                        outputFile = arg;
                    else
                    {
                        Console.WriteLine("Error: Invalid parameter: " + arg);
                        return;
                    }
                }
            }
            if (inputFile == null)
            {
                Console.WriteLine("No input file specified.");
                return;
            }
            if (outputFile == null)
                outputFile = Path.GetFileNameWithoutExtension(inputFile) + ".dasm";
            string code = "";
            using (StreamReader sr = new StreamReader(inputFile))
                code = sr.ReadToEnd().Replace("\r", "");
            string output = bf.Compile(code);
            string bootstrap = "";
            using (StreamReader sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("dcpu_bf.bootstrap.dasm")))
                bootstrap = sr.ReadToEnd().Replace("\r", "");
            output = bootstrap + output;
            using (StreamWriter writer = new StreamWriter(outputFile))
                writer.Write(output);
        }

        public string Compile(string brainfuck)
        {
            string output = "";
            int dataOffset = 0;
            int valueOffset = 0;
            int bracketStack = 0, totalLabelPairs = 0;
            int codeLength = 100;
            foreach (char c in brainfuck)
            {
                switch (c)
                {
                    case '>':
                        if (valueOffset > 0)
                        {
                            output += "ADD [A], " + valueOffset.ToString() + "\n";
                            valueOffset = 0;
                            codeLength += 2; // Worst case (no short literals)
                        }
                        if (valueOffset < 0)
                        {
                            output += "SUB [A], " + (-valueOffset).ToString() + "\n";
                            valueOffset = 0;
                            codeLength += 2; // Worst case (no short literals)
                        }
                        dataOffset++;
                        break;
                    case '<':
                        if (valueOffset > 0)
                        {
                            output += "ADD [A], " + valueOffset.ToString() + "\n";
                            valueOffset = 0;
                            codeLength += 2; // Worst case (no short literals)
                        }
                        if (valueOffset < 0)
                        {
                            output += "SUB [A], " + (-valueOffset).ToString() + "\n";
                            valueOffset = 0;
                            codeLength += 2; // Worst case (no short literals)
                        }
                        dataOffset--;
                        break;
                    case '+':
                        if (dataOffset > 0)
                        {
                            output += "ADD A, " + dataOffset.ToString() + "\n";
                            dataOffset = 0;
                            codeLength += 2; // Worst case (no short literals)
                        }
                        if (dataOffset < 0)
                        {
                            output += "SUB A, " + (-dataOffset).ToString() + "\n";
                            dataOffset = 0;
                            codeLength += 2; // Worst case (no short literals)
                        }
                        valueOffset++;
                        break;
                    case '-':
                        if (dataOffset > 0)
                        {
                            output += "ADD A, " + dataOffset.ToString() + "\n";
                            dataOffset = 0;
                            codeLength += 2; // Worst case (no short literals)
                        }
                        if (dataOffset < 0)
                        {
                            output += "SUB A, " + (-dataOffset).ToString() + "\n";
                            dataOffset = 0;
                            codeLength += 2; // Worst case (no short literals)
                        }
                        valueOffset--;
                        break;
                    default:
                        if (dataOffset > 0)
                        {
                            output += "ADD A, " + dataOffset.ToString() + "\n";
                            dataOffset = 0;
                            codeLength += 2; // Worst case (no short literals)
                        }
                        if (dataOffset < 0)
                        {
                            output += "SUB A, " + (-dataOffset).ToString() + "\n";
                            dataOffset = 0;
                            codeLength += 2; // Worst case (no short literals)
                        }
                        if (valueOffset > 0)
                        {
                            output += "ADD [A], " + valueOffset.ToString() + "\n";
                            valueOffset = 0;
                            codeLength += 2; // Worst case (no short literals)
                        }
                        if (valueOffset < 0)
                        {
                            output += "SUB [A], " + (-valueOffset).ToString() + "\n";
                            valueOffset = 0;
                            codeLength += 2; // Worst case (no short literals)
                        }
                        switch (c)
                        {
                            case '.':
                                output += "JSR output_char\n";
                                codeLength += 2;
                                break;
                            case ',':
                                output += "JSR read_key_in\n";
                                codeLength += 2;
                                break;
                            case '[':
                                bracketStack++;
                                output += ":bracket_start_" + totalLabelPairs.ToString() + "_" + bracketStack.ToString() + 
                                    "\nIFE [A], 0\nSET PC, bracket_end_" +
                                    totalLabelPairs.ToString() + "_" + bracketStack.ToString() + "\n";
                                codeLength += 4;
                                break;
                            case ']':
                                output += "IFN [A], 0\nSET PC, bracket_start_" +
                                    totalLabelPairs.ToString() + "_" + bracketStack.ToString() +
                                    "\n:bracket_end_" + totalLabelPairs.ToString() + "_" + bracketStack.ToString() + "\n";
                                bracketStack--;
                                totalLabelPairs++;
                                codeLength += 4;
                                break;
                        }
                        break;
                }
            }
            output = "SET A, " + codeLength.ToString() + "\n" + output;
            if (EndWithLoop)
                output += "SUB PC, 1";
            return output;
        }
    }
}
