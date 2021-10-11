//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.PowerFx.Core.Tests
{
    public class TestRunner
    {
        private readonly BaseRunner[] _runners;
        private List<TestCase> _tests = new List<TestCase>();

        public TestRunner(params BaseRunner[] runners)
        {
            _runners = runners;
        }

        public static string GetDefaultTestDir()
        {
            var curDir = Path.GetDirectoryName(typeof(TestRunner).Assembly.Location);
            var testDir = Path.Combine(curDir, "ExpressionTestCases");
            return testDir;
        }


        public string TestRoot { get; set; } = GetDefaultTestDir();

        public void AddDir(string directory = "")
        {
            directory = Path.GetFullPath(directory, TestRoot);
            IEnumerable<string> allFiles = Directory.EnumerateFiles(directory);

            AddFile(allFiles);
        }

        public void AddFile(params string[] files)
        {
            var x = (IEnumerable<string>)files;
            AddFile(x);
        }

        public void AddFile(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                AddFile(file);
            }
        }

        public void AddFile(string thisFile)
        {
            thisFile = Path.GetFullPath(thisFile, TestRoot);

            string[] lines = File.ReadAllLines(thisFile);

            // Skip blanks or "comments"
            // >> indicates input expression
            // next line is expected result.

            TestCase test = null;

            int i = -1;
            while (true)
            {
                i++;
                if (i == lines.Length)
                {
                    break;
                }
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                {
                    continue;
                }

                if (line.StartsWith(">>"))
                {
                    line = line.Substring(2).Trim();
                    test = new TestCase
                    {
                        Input = line,
                        SourceLine = i + 1, // 1-based
                        SourceFile = thisFile
                    };
                    continue;
                }
                if (test != null)
                {
                    // If it's indented, then part of previous line. 
                    if (line[0] == ' ')
                    {
                        test.Input += "\r\n" + line;
                        continue;
                    }

                    // Line after the input is the response

                    // handle engine-specific results
                    if (line.StartsWith("/*"))
                    {
                        var index = line.IndexOf("*/");
                        if (index > -1)
                        {
                            var engine = line.Substring(2, index - 2).Trim();
                            var result = line.Substring(index + 2).Trim();
                            test.SetExpected(result, engine);
                            continue;
                        }
                    }
                    test.SetExpected(line.Trim());

                    _tests.Add(test);
                    test = null;
                }
            }
        }


        public (int total, int failed, int passed) RunTests()
        {
            int total = 0;
            int fail = 0;
            int pass = 0;

            foreach (var test in _tests)
            {
                foreach (BaseRunner runner in this._runners)
                {
                    total++;

                    var engineName = runner.GetName();
                    // var runner = kv.Value;

                    string actualStr;
                    FormulaValue result = null;
                    try
                    {
                        result = runner.RunAsync(test.Input).Result;
                        actualStr = TestToString(result);
                    }
                    catch (Exception e)
                    {
                        actualStr = e.Message;
                    }

                    var expected = test.GetExpected(engineName);
                    if (result != null && expected == "#Error" && (runner.IsError(result)))
                    {
                        // Pass!
                        pass++;
                        Console.Write(".");
                        continue;
                    }

                    if (actualStr == expected)
                    {
                        pass++;
                        Console.Write(".");
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine($"FAIL: {engineName}, {Path.GetFileName(test.SourceFile)}:{test.SourceLine}");
                        Console.WriteLine($"FAIL: {test.Input}");
                        Console.WriteLine($"expected: {expected}");
                        Console.WriteLine($"actual  : {actualStr}");
                        Console.WriteLine();
                        fail++;
                        continue;
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine($"{total} total. {pass} passed. {fail} failed");
            return (total, fail, pass);
        }

        internal static string TestToString(FormulaValue result)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                TestToString(result, sb);
            }
            catch (Exception e)
            {
                // This will cause a diff and test failure below. 
                sb.Append($"<exception writing result: {e.Message}>");
            }

            string actualStr = sb.ToString();
            return actualStr;
        }

        // $$$ Move onto FormulaValue. 
        // Result here should be a string value that could be parsed. 
        // Normalize so we can use this in test cases. 
        internal static void TestToString(FormulaValue result, StringBuilder sb)
        {
            if (result is NumberValue n)
            {
                sb.Append(n.Value);
            }
            else if (result is BooleanValue b)
            {
                sb.Append(b.Value ? "true" : "false");
            }
            else if (result is StringValue s)
            {
                // $$$ proper escaping?
                sb.Append('"' + s.Value + '"');
            }
            else if (result is TableValue t)
            {
                sb.Append('[');

                string dil = "";
                foreach (var row in t.Rows)
                {
                    sb.Append(dil);

                    if (row.IsValue)
                    {
                        var tableType = (TableType)t.Type;
                        if (t.IsColumn && tableType.GetNames().First().Name == "Value")
                        {
                            var val = row.Value.Fields.First().Value;
                            TestToString(val, sb);
                        }
                        else
                        {
                            TestToString(row.Value, sb);
                        }
                    }
                    else
                    {
                        TestToString(row.ToFormulaValue(), sb);
                    }

                    dil = ",";
                }
                sb.Append(']');
            }
            else if (result is RecordValue r)
            {
                var fields = r.Fields.ToArray();
                Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));

                sb.Append('{');
                string dil = "";

                foreach (var field in fields)
                {
                    sb.Append(dil);
                    sb.Append(field.Name);
                    sb.Append(':');
                    TestToString(field.Value, sb);

                    dil = ",";
                }
                sb.Append('}');
            }
            else if (result is BlankValue)
            {
                sb.Append("Blank()");
            }
            else if (result is ErrorValue)
            {
                sb.Append(result);
            }
            else
            {
                throw new InvalidOperationException($"unsupported value type: {result.GetType().Name}");
            }
        }
    }
}