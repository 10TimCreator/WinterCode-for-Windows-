using System;
using System.Collections.Generic;
using System.IO;

namespace WinterCode
{
    class Program
    {
        static Dictionary<string, double> vars = new Dictionary<string, double>();
        static List<string> errors = new List<string>();
        static string[] scriptLines;
        static int lineNumber;
        static Dictionary<string, Action<string[]>> commands = new Dictionary<string, Action<string[]>>();

        static void Main(string[] args)
        {
            Console.WriteLine("WinterCode (verCD:2025, Dec 14 2025) [64-bit] for Windows.");
            Console.WriteLine("WinterCode • by Amina. All rights reserved (c).");
            Console.WriteLine("Powered by TimCreator and dHLab.");
            Console.WriteLine("Type \"help\" to see available commands.");
            Console.WriteLine("Type 'exit' to quit.\n");

            InitCommands();

            while (true)
            {
                List<string> inputScript = new List<string>();
                Console.WriteLine("Enter your script. Finish with 'end'.");

                string lineInput;
                while ((lineInput = Console.ReadLine()) != null && lineInput.ToLower() != "end")
                {
                    if (lineInput.ToLower() == "exit") return;
                    inputScript.Add(lineInput);
                }

                if (inputScript.Count == 0)
                {
                    Console.WriteLine("No script entered.\n");
                    continue;
                }

                scriptLines = inputScript.ToArray();

                Console.WriteLine("\nPress Shift+R to run the script, or Escape to exit.");
                while (true)
                {
                    var keyInfo = Console.ReadKey(true);
                    if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift) && keyInfo.Key == ConsoleKey.R)
                    {
                        Console.WriteLine("\nRunning script...");
                        RunScript();

                        if (errors.Count > 0)
                        {
                            Console.WriteLine($"\nWinterCode finished with {errors.Count} error(s):");
                            foreach (var e in errors)
                                Console.WriteLine(e);
                            errors.Clear();
                        }
                        else
                        {
                            Console.WriteLine("\nWinterCode finished successfully. No errors.\n");
                        }

                        break;
                    }
                    else if (keyInfo.Key == ConsoleKey.Escape)
                    {
                        Console.WriteLine("Exit...");
                        return;
                    }
                }
            }
        }

        static void InitCommands()
        {
            // Базовые команды
            commands["message"] = args => Console.WriteLine(string.Join(" ", args));
            commands["snow"] = args => Console.WriteLine("❄");
            commands["ice"] = args => Console.WriteLine("🧊");
            commands["frost"] = args => Console.WriteLine("FROST MODE");
            commands["blizzard"] = args => Console.WriteLine("BLIZZARD ❄❄❄");
            commands["winter"] = args => Console.WriteLine("WINTER IS HERE");

            // Переменные
            commands["set"] = args =>
            {
                if (args.Length != 3 || args[1] != "=")
                {
                    errors.Add($"Line {lineNumber + 1}: invalid set syntax");
                    return;
                }
                if (!double.TryParse(args[2], out double val))
                {
                    errors.Add($"Line {lineNumber + 1}: invalid number {args[2]}");
                    return;
                }
                vars[args[0]] = val;
            };

            commands["add"] = args => OperateVar(args, (a, b) => a + b);
            commands["sub"] = args => OperateVar(args, (a, b) => a - b);
            commands["mul"] = args => OperateVar(args, (a, b) => a * b);
            commands["div"] = args => OperateVar(args, (a, b) => a / b);
            commands["inc"] = args => OperateVar(args, (a, b) => a + 1, single: true);
            commands["dec"] = args => OperateVar(args, (a, b) => a - 1, single: true);
            commands["show"] = args =>
            {
                if (vars.ContainsKey(args[0]))
                    Console.WriteLine($"{args[0]} = {vars[args[0]]}");
                else
                    errors.Add($"Line {lineNumber + 1}: variable {args[0]} not found");
            };

            // Логика
            commands["equal"] = args => CompareVars(args, (a, b) => a == b);
            commands["greater"] = args => CompareVars(args, (a, b) => a > b);
            commands["less"] = args => CompareVars(args, (a, b) => a < b);

            // Циклы
            commands["repeat"] = args =>
            {
                if (args.Length < 2) return;
                if (!int.TryParse(args[0], out int times))
                {
                    errors.Add($"Line {lineNumber + 1}: invalid repeat count {args[0]}");
                    return;
                }
                string text = string.Join(" ", args, 1, args.Length - 1);
                for (int i = 0; i < times; i++)
                    Console.WriteLine(text);
            };

            commands["loop3"] = args => { for (int i = 0; i < 3; i++) Console.WriteLine("LOOP"); };
            commands["loop5"] = args => { for (int i = 0; i < 5; i++) Console.WriteLine("LOOP"); };

            // Система
            commands["version"] = args => Console.WriteLine("WinterCode v1.0");
            commands["author"] = args => Console.WriteLine("by Amina & TimCreator");
            commands["help"] = args => Console.WriteLine("Manual command language. Available: message, snow, ice, frost, blizzard, winter, set, add, sub, mul, div, inc, dec, show, equal, greater, less, repeat, loop3, loop5, version, author, help, cmd001–cmd1000, craft window, time, date, ping, echo, line, star, snowline, open, int, double, string, bool, if, for, while, import audio");

            // CMD001–CMD1000
            for (int i = 1; i <= 1000; i++)
            {
                int idx = i;
                commands[$"cmd{idx:D3}"] = args => Console.WriteLine($"CMD{idx:D3} OK");
            }

            // Расширенные команды
            commands["time"] = args => Console.WriteLine(DateTime.Now.ToLongTimeString());
            commands["date"] = args => Console.WriteLine(DateTime.Now.ToShortDateString());
            commands["ping"] = args => Console.WriteLine("PONG");
            commands["echo"] = args => Console.WriteLine(string.Join(" ", args));
            commands["noop"] = args => { };
            commands["line"] = args => Console.WriteLine("----------------");
            commands["star"] = args => Console.WriteLine("★");
            commands["snowline"] = args => Console.WriteLine("❄❄❄❄❄");
            commands["craft"] = args => Console.WriteLine($"Crafting window {string.Join(" ", args)}");

            // Команда для открытия файлов .wc
            commands["open"] = args =>
            {
                if (args.Length != 1)
                {
                    errors.Add($"Line {lineNumber + 1}: open requires exactly 1 argument (filename)");
                    return;
                }

                string filename = args[0];
                if (!filename.EndsWith(".wc"))
                    filename += ".wc";

                if (!File.Exists(filename))
                {
                    errors.Add($"Line {lineNumber + 1}: file '{filename}' not found. " +
                               "To open files on your desktop, move the .wc file to where WinterCode is located, " +
                               "or move WinterCode to your desktop.");
                    return;
                }

                try
                {
                    string[] fileLines = File.ReadAllLines(filename);
                    Console.WriteLine($"--- Contents of {filename} ---");
                    foreach (var l in fileLines)
                        Console.WriteLine(l);
                    Console.WriteLine($"--- End of {filename} ---");
                    Console.WriteLine("File displayed. You can continue coding your script...\n");
                }
                catch (Exception ex)
                {
                    errors.Add($"Line {lineNumber + 1}: error reading file '{filename}': {ex.Message}");
                }
            };

            // Новые команды Java-подобного стиля
            commands["int"] = args =>
            {
                if (args.Length != 3 || args[1] != "=") { errors.Add($"Line {lineNumber + 1}: invalid int syntax"); return; }
                if (!int.TryParse(args[2], out int val)) { errors.Add($"Line {lineNumber + 1}: invalid int {args[2]}"); return; }
                vars[args[0]] = val;
            };

            commands["double"] = args =>
            {
                if (args.Length != 3 || args[1] != "=") { errors.Add($"Line {lineNumber + 1}: invalid double syntax"); return; }
                if (!double.TryParse(args[2], out double val)) { errors.Add($"Line {lineNumber + 1}: invalid double {args[2]}"); return; }
                vars[args[0]] = val;
            };

            commands["string"] = args =>
            {
                if (args.Length < 3 || args[1] != "=") { errors.Add($"Line {lineNumber + 1}: invalid string syntax"); return; }
                string val = string.Join(" ", args, 2, args.Length - 2);
                vars[args[0]] = 0; // или можно не добавлять вовсе, если не требуется
                // Для хранения строк создайте отдельный словарь:
                // Например: static Dictionary<string, string> stringVars = new Dictionary<string, string>();
                // stringVars[args[0]] = val;
            };

            commands["bool"] = args =>
            {
                if (args.Length != 3 || args[1] != "=") { errors.Add($"Line {lineNumber + 1}: invalid bool syntax"); return; }
                if (args[2].ToLower() != "true" && args[2].ToLower() != "false") { errors.Add($"Line {lineNumber + 1}: invalid bool {args[2]}"); return; }
                vars[args[0]] = args[2].ToLower() == "true" ? 1.0 : 0.0;
            };

            // if условие
            commands["if"] = args =>
            {
                if (args.Length < 3) { errors.Add($"Line {lineNumber + 1}: if requires 3 arguments"); return; }
                string varName = args[0];
                string op = args[1];
                string value = args[2];
                if (!vars.ContainsKey(varName)) { errors.Add($"Line {lineNumber + 1}: variable {varName} not found"); return; }

                bool condition = op switch
                {
                    "==" => vars[varName].ToString() == value,
                    "!=" => vars[varName].ToString() != value,
                    ">" => Convert.ToDouble(vars[varName]) > Convert.ToDouble(value),
                    "<" => Convert.ToDouble(vars[varName]) < Convert.ToDouble(value),
                    ">=" => Convert.ToDouble(vars[varName]) >= Convert.ToDouble(value),
                    "<=" => Convert.ToDouble(vars[varName]) <= Convert.ToDouble(value),
                    _ => false
                };

                if (!condition) lineNumber++;
            };

            // for цикл
            commands["for"] = args =>
            {
                if (args.Length < 3) { errors.Add($"Line {lineNumber + 1}: for requires 3 arguments"); return; }
                string varName = args[0];
                if (!int.TryParse(args[1], out int from) || !int.TryParse(args[2], out int to)) { errors.Add($"Line {lineNumber + 1}: invalid for range"); return; }
                for (int i = from; i <= to; i++)
                {
                    vars[varName] = i;
                    lineNumber++;
                    if (lineNumber < scriptLines.Length) RunScriptLine(scriptLines[lineNumber].Split());
                }
            };

            // while цикл
            commands["while"] = args =>
            {
                int startLine = lineNumber;
                while (true)
                {
                    RunScriptLine(args);
                    break; // для безопасности
                }
            };

            // import audio
            commands["import"] = args =>
            {
                if (args.Length != 2 || args[0].ToLower() != "audio")
                {
                    errors.Add($"Line {lineNumber + 1}: invalid import syntax. Use 'import audio filename'");
                    return;
                }

                string audioFile = args[1];
                if (!File.Exists(audioFile)) { errors.Add($"Line {lineNumber + 1}: audio file '{audioFile}' not found"); return; }

                Console.WriteLine($"Audio file '{audioFile}' imported successfully (placeholder, playback not implemented)");
            };
        }

        static void OperateVar(string[] args, Func<double, double, double> op, bool single = false)
        {
            if (args.Length < 1) return;
            string name = args[0];
            if (!vars.ContainsKey(name))
            {
                errors.Add($"Line {lineNumber + 1}: variable {name} not found");
                return;
            }
            double val2 = single ? 0 : (args.Length < 2 || !double.TryParse(args[1], out val2) ? 0 : val2);
            vars[name] = op(vars[name], val2);
        }

        static void CompareVars(string[] args, Func<double, double, bool> cmp)
        {
            if (args.Length < 2)
            {
                errors.Add($"Line {lineNumber + 1}: not enough arguments");
                return;
            }
            if (!vars.ContainsKey(args[0]) || !vars.ContainsKey(args[1]))
            {
                errors.Add($"Line {lineNumber + 1}: variable not found");
                return;
            }
            Console.WriteLine(cmp(vars[args[0]], vars[args[1]]));
        }

        static void RunScriptLine(string[] parts)
        {
            string cmd = parts[0];
            string[] args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, parts.Length - 1);

            if (commands.ContainsKey(cmd))
                commands[cmd](args);
            else
                errors.Add($"Line {lineNumber + 1}: Unknown command '{cmd}'");
        }

        static void RunScript()
        {
            for (lineNumber = 0; lineNumber < scriptLines.Length; lineNumber++)
            {
                string line = scriptLines[lineNumber].Trim();
                if (line == "" || line.StartsWith("#")) continue;
                string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                RunScriptLine(parts);
            }
        }
    }
}
