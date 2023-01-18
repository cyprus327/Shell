using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Program {
    public static void Main() {
        Shell shell = new Shell();
        Console.CursorVisible = true;
        while (true) {
            shell.Run();
            Console.WriteLine("Are you sure you'd like to quit? (Y/N)");
            string response = Console.ReadLine();
            if (response == "Y") return;
        }
    }
}

class Shell {
    public Shell() {
        fileSystem.Add("", new List<string>());
    }

    readonly Dictionary<string, List<string>> fileSystem = new Dictionary<string, List<string>>();
    readonly Dictionary<string, string> files = new Dictionary<string, string>();
    readonly Dictionary<string, string> variables = new Dictionary<string, string>();
    string currentPath = "";

    public void Run() {
        while (true) {
            Console.Write(currentPath + "$ ");
            string input = Console.ReadLine();
            string[] tokens = input.Split(' ');
            ProcessCommand(tokens);
        }
    }

    private void ProcessCommand(string[] tokens) {
        switch (tokens[0]) {
            case "mkdir":
                CreateDirectory(tokens);
                break;
            case "ls":
                if (tokens.Length > 1) {
                    ListDirectory(string.Join(" ", tokens, 1, tokens.Length - 1));
                }
                else ListDirectory(currentPath);
                Console.WriteLine();
                break;
            case "cd":
                if (tokens.Length > 1) {
                    ChangeDirectory(string.Join(" ", tokens, 1, tokens.Length - 1));
                }
                else Console.WriteLine("'cd' requires a directory to be specified.");
                break;
            case "touch":
                if (tokens.Length > 1) {
                    CreateFile(tokens[1]);
                }
                else Console.WriteLine("'touch' requires a filename to be specified.");
                break;
            case "rm":
                if (tokens.Length > 1) {
                    RemoveFile(tokens[1]);
                }
                else Console.WriteLine("'rm' requires a directory/filename to be specified.");
                break;
            case "pwd":
                Console.WriteLine(currentPath);
                break;
            case "echo":
                if (tokens.Length > 1) {
                    string str = string.Join(" ", tokens, 1, tokens.Length - 1);
                    EchoCommand(str);
                }
                else Console.WriteLine("'echo' requires a succeeding command.");
                break;
            case "write":
                if (tokens.Length > 2) {
                    WriteToFile(tokens[1], string.Join(" ", tokens, 2, tokens.Length - 2));
                }
                else Console.WriteLine("'write' requires a filename and succeeding text.");
                break;
            case "cat":
                if (tokens.Length > 1 && (tokens[1].EndsWith(".txt") || tokens[1].EndsWith(".sh"))) {
                    ReadFile(tokens[1]);
                }
                else Console.WriteLine("Filetype not supported or none specified.");
                break;
            case "sh":
                if (tokens.Length > 1 && tokens[1].EndsWith(".sh")) {
                    RunScript(tokens[1]);
                }
                else Console.WriteLine("Filetype not supported or none specified.");
                break;
            case "set":
                if (tokens.Length > 2) {
                    string val = string.Join(" ", tokens, 2, tokens.Length - 2);
                    SetVariable(tokens[1], EvaluateExpression(val).ToString());
                }
                else Console.WriteLine("'set' requires a variable name and a value.");
                break;
            case "for":
                if (tokens.Length > 4) {
                    ForCommand(tokens[1], tokens[3], string.Join(" ", tokens, 4, tokens.Length - 4));
                }
                else Console.WriteLine("'for' requires a variable name, a range, and a command.");
                break;
            case "if":
                if (tokens.Length > 4) {
                    IfCommand(tokens);
                }
                else Console.WriteLine("'if' requires at minimum: var1 operator var2 statement.");
                break;
            case "eval":
                if (tokens.Length > 1) {
                    Console.WriteLine(EvaluateExpression(string.Join(" ", tokens, 1, tokens.Length - 1)));
                }
                else Console.WriteLine("Minimum expression length is 3 components, i.e. 9 - 6.");
                break;
            case "edit":
                if (tokens.Length > 1) {
                    EditFile(tokens[1]);
                }
                else Console.WriteLine("'edit' requires a filename to be specified.");
                break;
            case "clear":
                Console.Clear();
                break;
            case "test":
                foreach (var dir in fileSystem.Keys) {
                    Console.WriteLine(dir);
                }
                Console.WriteLine();
                foreach (var file in files.Keys) {
                    Console.WriteLine(file);
                }
                break;
            case "exit":
                return;
            default:
                Console.WriteLine("Command '" + tokens[0] + "' not supported.");
                break;
        }
    }

    private void SetVariable(string name, string value) {
        if (!IsValidVariableName(name)) {
            Console.WriteLine("Invalid variable name.");
            return;
        }

        variables[name] = value;
    }

    private int EvaluateExpression(string expression) {
        if (expression.Length == 1 && int.TryParse(expression, out int num)) {
            return num;
        }

        Stack<int> numbers = new Stack<int>();
        Stack<char> operators = new Stack<char>();

        for (int i = 0; i < expression.Length; i++) {
            char currentChar = expression[i];

            if (char.IsDigit(currentChar)) {
                int currentNumber = currentChar - '0';
                while (i + 1 < expression.Length && char.IsDigit(expression[i + 1])) {
                    currentNumber = currentNumber * 10 + expression[i + 1] - '0';
                    i++;
                }
                numbers.Push(currentNumber);
            }
            else if (currentChar == '(') {
                operators.Push(currentChar);
            }
            else if (currentChar == ')') {
                while (operators.Peek() != '(') {
                    int secondOperand = numbers.Pop();
                    int firstOperand = numbers.Pop();
                    char operatorToApply = operators.Pop();
                    numbers.Push(ApplyOperator(firstOperand, secondOperand, operatorToApply));
                }
                operators.Pop();
            }
            else if (currentChar == '+' || currentChar == '-' || currentChar == '*' || currentChar == '/') {
                while (operators.Count > 0 && HasPrecedence(currentChar, operators.Peek())) {
                    int secondOperand = numbers.Pop();
                    int firstOperand = numbers.Pop();
                    char operatorToApply = operators.Pop();
                    numbers.Push(ApplyOperator(firstOperand, secondOperand, operatorToApply));
                }
                operators.Push(currentChar);
            }
            else if (currentChar == '$') {
                string variableName = "";
                while (i + 1 < expression.Length && (char.IsLetterOrDigit(expression[i + 1]) || expression[i + 1] == '_')) {
                    variableName += expression[i + 1];
                    i++;
                }
                if (variables.ContainsKey(variableName)) {
                    numbers.Push(int.Parse(variables[variableName]));
                }
                else throw new Exception("Variable name '" + variableName + "' does not exist.");
            }
        }

        while (operators.Count > 0) {
            int secondOperand = numbers.Pop();
            int firstOperand = numbers.Pop();
            char operatorToApply = operators.Pop();
            numbers.Push(ApplyOperator(firstOperand, secondOperand, operatorToApply));
        }
        return numbers.Pop();
    }

    private bool HasPrecedence(char op1, char op2) {
        if (op2 == '(' || op2 == ')')
            return false;
        if ((op1 == '*' || op1 == '/') && (op2 == '+' || op2 == '-'))
            return false;
        else
            return true;
    }

    private int ApplyOperator(int a, int b, char op) {
        switch (op) {
            case '+': return a + b;
            case '-': return a - b;
            case '*': return a * b;
            case '/': return a / b;
            default: throw new ArgumentException("Invalid operator");
        }
    }

    private void ForCommand(string variable, string range, string command) {
        if (!IsValidVariableName(variable)) {
            Console.WriteLine("Invalid variable name.");
            return;
        }

        if (!int.TryParse(range.Split('-')[0], out int start) ||
            !int.TryParse(range.Split('-')[1], out int end)) {
            Console.WriteLine("Invalid range.");
            return;
        }

        for (int i = start; i <= end; i++) {
            variables[variable] = i.ToString();
            ProcessCommand(command.Split(' '));
        }
    }

    private void IfCommand(string[] tokens) {
        string condition = string.Join(" ", tokens, 1, tokens.Length - 1);
        char[] numbers = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        int lastNumberIndex = condition.LastIndexOfAny(numbers);
        condition = condition.Substring(0, lastNumberIndex + 1);

        string op;
        string[] sides;
        if (condition.Contains("==")) {
            sides = condition.Split(new[] { "==" }, StringSplitOptions.None);
            op = "==";
        }
        else if (condition.Contains("!=")) {
            sides = condition.Split(new[] { "!=" }, StringSplitOptions.None);
            op = "!=";
        }
        else if (condition.Contains(">")) {
            sides = condition.Split(new[] { ">" }, StringSplitOptions.None);
            op = ">";
        }
        else if (condition.Contains("<")) {
            sides = condition.Split(new[] { "<" }, StringSplitOptions.None);
            op = "<";
        }
        else if (condition.Contains(">=")) {
            sides = condition.Split(new[] { ">=" }, StringSplitOptions.None);
            op = ">=";
        }
        else if (condition.Contains("<=")) {
            sides = condition.Split(new[] { "<=" }, StringSplitOptions.None);
            op = "<=";
        }
        else {
            Console.WriteLine("Invalid operator in equation.");
            return;
        }

        int leftSide = EvaluateExpression(sides[0]);
        int rightSide = EvaluateExpression(sides[1]);

        if (EvaluateCondition(leftSide + " " + op + " " + rightSide)) {
            for (int i = 3; i < tokens.Length; i++) {
                if (tokens[i].Any(c => char.IsDigit(c))) {
                    lastNumberIndex = i;
                }
            }
            string[] command = tokens.Skip(lastNumberIndex + 1).Take(tokens.Length).ToArray();
            ProcessCommand(command);
        }
    }

    private bool EvaluateCondition(string condition) {
        string[] tokens = condition.Split(' ');

        if (!int.TryParse(tokens[0], out int a) && tokens[0][0] == '$') {
            string variable = tokens[0].Substring(1, tokens[0].Length - 1);
            if (!variables.ContainsKey(variable)) {
                Console.WriteLine("Variable '" + variable + "' does not exist.");
                return false;
            }
            a = int.Parse(variables[variable]);
        }
        if (!int.TryParse(tokens[2], out int b) && tokens[2][0] == '$') {
            string variable = tokens[2].Substring(1, tokens[2].Length - 1);
            if (!variables.ContainsKey(variable)) {
                Console.WriteLine("Variable '" + variable + "' does not exist.");
                return false;
            }
            b = int.Parse(variables[tokens[2].Substring(1, tokens[2].Length - 1)]);
        }

        switch (tokens[1]) {
            case "==":
                return a == b;
            case "!=":
                return a != b;
            case ">":
                return a > b;
            case "<":
                return a < b;
            case ">=":
                return a >= b;
            case "<=":
                return a <= b;
            default:
                return false;
        }
    }

    private bool IsValidVariableName(string name) {
        return !name.Contains('$');
    }

    private void RunScript(string name) {
        if (!fileSystem[currentPath].Contains(name)) {
            Console.WriteLine("Script specified does not exist.");
            return;
        }

        string script = files[name];
        string[] commands = script.Split('\n');

        for (int i = 0; i < commands.Length - 1; i++) {
            string[] command = commands[i].Split();
            ProcessCommand(command);
        }
    }

    private void WriteToFile(string filename, string text) {
        if (!fileSystem[currentPath].Contains(filename) || !files.ContainsKey(filename)) {
            Console.WriteLine("File not found.");
            return;
        }

        files[filename] += text + "\n";
        Console.WriteLine("Text written to '" + filename + "'successfully.");
    }

    private void ReadFile(string filename) {
        if (!fileSystem[currentPath].Contains(filename) || !files.ContainsKey(filename)) {
            Console.WriteLine("File not found.");
            return;
        }

        Console.WriteLine(files[filename]);
    }

    private void EditFile(string filename) {
        if (!files.ContainsKey(filename)) {
            Console.WriteLine("The specified file does not exist.");
            return;
        }

        StringBuilder sb = new StringBuilder(files[filename]);
        int cursorInd = 0;
        string sbstr;
        Console.CursorVisible = false;
        Console.Clear();
        while (true) {
            sbstr = sb.ToString();
            string longestLine = new string('-', sbstr.Split('\n').Max(s => s.Length));
            Console.WriteLine($"Original contents of {filename}:\n{files[filename]}\n\n{longestLine}\n");

            Console.Write(sbstr.Substring(0, cursorInd) + "|" + sbstr.Substring(cursorInd, sbstr.Length - cursorInd));
            var key = Console.ReadKey(true);
            switch (key.Key) {
                case ConsoleKey.LeftArrow:
                    if (cursorInd > 0) cursorInd--;
                    break;
                case ConsoleKey.RightArrow:
                    if (cursorInd < sbstr.Length) cursorInd++;
                    break;
                case ConsoleKey.Enter:
                    sb.Insert(cursorInd, "\n");
                    cursorInd++;
                    break;
                case ConsoleKey.Backspace:
                    if (cursorInd > 0) {
                        sb.Remove(cursorInd - 1, 1);
                        cursorInd--;
                    }
                    break;
                case ConsoleKey.Escape:
                    //sb.Append("\n");
                    files[filename] = sb.ToString();
                    Console.CursorVisible = true;
                    Console.Clear();
                    Console.WriteLine("File saved.");
                    return;
                default:
                    sb.Insert(cursorInd, key.KeyChar);
                    cursorInd++;
                    break;
            }

            Console.Clear();
        }
    }

    private void CreateDirectory(string[] tokens) {
        if (tokens.Length < 1) {
            Console.WriteLine("'mkdir' requires a directory to be specified.");
            return;
        }

        string directory = string.Join(" ", tokens, 1, tokens.Length - 1);
        string newPath = currentPath + "/" + directory;
        if (fileSystem.ContainsKey(newPath)) {
            Console.WriteLine("Directory already exists.");
            return;
        }

        fileSystem[currentPath].Add(directory);
        fileSystem.Add(newPath, new List<string>());
        Console.WriteLine("Created directory '" + directory + "' in '" + currentPath + "'");
    }

    private void ListDirectory(string path, int depth = 0) {
        if (!fileSystem.ContainsKey(path)) {
            Console.WriteLine("Invalid path.\n");
            return;
        }

        foreach (string item in fileSystem[path]) {
            string newPath = path + "/" + item;
            Console.WriteLine(new string(' ', depth * 3) + item);
            if (fileSystem.ContainsKey(newPath)) {
                ListDirectory(newPath, depth + 1);
            }
        }
    }

    private void ChangeDirectory(string directory) {
        if (directory == "..") {
            if (currentPath == "") {
                Console.WriteLine("Cannot go up from root directory.");
                return;
            }
            int lastSlash = currentPath.LastIndexOf("/");
            currentPath = currentPath.Substring(0, lastSlash);
            return;
        }

        string newPath = currentPath + "/" + directory;

        if (!fileSystem.ContainsKey(newPath)) {
            Console.WriteLine("Path does not exist.");
            return;
        }

        currentPath = newPath;
    }

    private void CreateFile(string filename) {
        if (fileSystem[currentPath].Contains(filename)) {
            Console.WriteLine("File already exists.");
            return;
        }

        fileSystem[currentPath].Add(filename);
        files.Add(filename, "");
        Console.WriteLine("File '" + filename + "' created.");
    }

    private void RemoveFile(string name) {
        if (!fileSystem[currentPath].Contains(name)) {
            Console.WriteLine("File/directory does not exist.");
            return;
        }

        string newPath = currentPath + "/" + name;
        if (!fileSystem.ContainsKey(newPath)) {
            fileSystem[currentPath].Remove(name);
            files.Remove(name);
            Console.WriteLine("File removed.");
            return;
        }

        if (fileSystem[newPath].Count != 0) {
            Console.WriteLine("Directory has contents in it, you must remove them first.");
            return;
        }

        fileSystem.Remove(newPath);
        fileSystem[currentPath].Remove(name);
        Console.WriteLine("Directory removed.");
    }

    private void EchoCommand(string text) {
        foreach (var variable in variables) {
            text = text.Replace("$" + variable.Key, variable.Value);
        }
        Console.WriteLine(text);
    }
}