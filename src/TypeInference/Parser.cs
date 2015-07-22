using org.yinwang.pysonar.ast;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using AliasedExp = Pytocs.Syntax.AliasedExp;
using Node = Pytocs.Syntax.Node;
using Identifier = Pytocs.Syntax.Identifier;
using SuiteStatement = Pytocs.Syntax.SuiteStatement;
using Module = Pytocs.Syntax.Module;

namespace org.yinwang.pysonar
{
    public partial class Parser
    {
        private const string PYTHON2_EXE = "python";
        private const string PYTHON3_EXE = "python3";
        private const int TIMEOUT = 10000;

        Process python2Process;
        Process python3Process;
        private static Gson gson = new GsonBuilder().setPrettyPrinting().create();
        private const String dumpPythonResource = "org/yinwang/pysonar/python/dump_python.py";
        private string exchangeFile;
        private string endMark;
        private string jsonizer;
        private string parserLog;
        private string file;
        private string content;
        private IFileSystem fs;

        public Parser(IFileSystem fs)
        {
            this.fs = fs;
            exchangeFile = _.locateTmp(fs, "json");
            endMark = _.locateTmp(fs, "end");
            jsonizer = _.locateTmp(fs, "dump_python");
            parserLog = _.locateTmp(fs, "parser_log");

            startPythonProcesses();

            if (python2Process != null)
            {
                _.msg("started: " + PYTHON2_EXE);
            }

            if (python3Process != null)
            {
                _.msg("started: " + PYTHON3_EXE);
            }
        }

        // start or restart python processes
        private void startPythonProcesses()
        {
#if EXTERNAL_PARSER
            if (python2Process != null)
            {
                python2Process.Close();
            }
            if (python3Process != null)
            {
                python3Process.Close();
            }

            // copy dump_python.py to temp dir
            try
            {
                //throw new NotImplementedException();
                //URL url = Thread.CurrentThread.getContextClassLoader().getResource(dumpPythonResource);
                //FileUtils.copyURLToFile(url, new File(jsonizer));
            }
            catch (Exception)
            {
                _.die("Failed to copy resource file:" + dumpPythonResource);
            }

            python2Process = startInterpreter(PYTHON2_EXE);
            python3Process = startInterpreter(PYTHON3_EXE);
            if (python2Process == null && python3Process == null)
            {
                _.die("You don't seem to have either of Python or Python3 on PATH");
            }
#endif
        }


        public void close()
        {
            if (!Analyzer.self.hasOption("debug"))
            {
                fs.DeleteFile(exchangeFile);
                fs.DeleteFile(endMark);
                fs.DeleteFile(jsonizer);
                fs.DeleteFile(parserLog);
            }
        }

        private Node convert(object o) {
            throw new NotImplementedException();
        }

        List<Identifier> segmentQname(string qname, int start, bool hasLoc)
        {
            List<Identifier> result = new List<Identifier>();

            for (int i = 0; i < qname.Length; i++)
            {
                String name = "";
                while (Char.IsWhiteSpace(qname[i]))
                {
                    i++;
                }
                int nameStart = i;

                while (i < qname.Length &&
                        (Char.IsLetterOrDigit(qname[i]) ||
                                qname[i] == '*') &&
                        qname[i] != '.')
                {
                    name += qname[i];
                    i++;
                }

                int nameStop = i;
                int nstart = hasLoc ? start + nameStart : -1;
                int nstop = hasLoc ? start + nameStop : -1;
                result.Add(new Identifier(name, file, nstart, nstop));
            }

            return result;
        }

        public String prettyJson(String json)
        {
            IDictionary<string, object> obj = gson.fromJson<Dictionary<string, object>>(json, typeof(Dictionary<string, object>));
            return gson.toJson(obj);
        }

        public Process startInterpreter(String pythonExe)
        {
            return null;
#if NYI
            Process p;
            try
            {
                ProcessBuilder builder = new ProcessBuilder(pythonExe, "-i", jsonizer);
                builder.redirectErrorStream(true);
                builder.redirectError(parserLog));
                builder.redirectOutput(parserLog));
                builder.environment().remove("PYTHONPATH");
                p = builder.start();
            }
            catch (Exception e)
            {
                _.msg("Failed to start: " + pythonExe);
                return null;
            }
            return p;
#endif
        }


        public Module parseFile(string filename)
        {
            if (filename != null)
            {
                var lexer = new Pytocs.Syntax.Lexer(filename, fs.CreateStreamReader(filename));
                var parser = new Pytocs.Syntax.Parser(filename, lexer);
                var moduleStmts = parser.Parse().ToList();
                var posStart = moduleStmts[0].Start;
                var posEnd = moduleStmts.Last().End;
                return new Module(
                    _.moduleName(fs, filename),
                    new SuiteStatement(file, posStart, posEnd) { stmts = moduleStmts },
                    filename, posStart, posEnd);
            }
            file = filename;
            content = fs.ReadFile(filename);

            Node node2 = parseFileInner(filename, python2Process);
            if (node2 != null)
            {
                return (Module) node2;
            }
            else if (python3Process != null)
            {
                Node node3 = parseFileInner(filename, python3Process);
                if (node3 == null)
                {
                    _.msg("failed to parse: " + filename);
                    Analyzer.self.failedToParse.Add(filename);
                    return null;
                }
                else
                {
                    return (Module) node3;
                }
            }
            else
            {
                _.msg("failed to parse: " + filename);
                Analyzer.self.failedToParse.Add(filename);
                return null;
            }
        }

        public Node parseFileInner(String filename, Process pythonProcess)
        {
            //        _.msg("parsing: " + filename);

            string exchange = exchangeFile;
            string marker = endMark;
            cleanTemp();

            String s1 = _.escapeWindowsPath(filename);
            String s2 = _.escapeWindowsPath(exchangeFile);
            String s3 = _.escapeWindowsPath(endMark);
            String dumpCommand = "parse_dump('" + s1 + "', '" + s2 + "', '" + s3 + "')";

            if (!sendCommand(dumpCommand, pythonProcess))
            {
                cleanTemp();
                return null;
            }

            DateTime waitStart = DateTime.Now;
            while (!File.Exists(marker))
            {
                if (DateTime.Now - waitStart > TimeSpan.FromMilliseconds(TIMEOUT))
                {
                    _.msg("\nTimed out while parsing: " + filename);
                    cleanTemp();
                    startPythonProcesses();
                    return null;
                }
                try
                {
                    Thread.Sleep(1);
                }
                catch (Exception)
                {
                    cleanTemp();
                    return null;
                }
            }

            String json = fs.ReadFile(exchangeFile);
            if (json != null)
            {
                cleanTemp();
                IDictionary<string, object> map = gson.fromJson<IDictionary<string, object>>(json, typeof(IDictionary<string, object>));
                return convert(map);
            }
            else
            {
                cleanTemp();
                return null;
            }
        }

        private void cleanTemp()
        {
            fs.DeleteFile(exchangeFile);
            fs.DeleteFile(endMark);
        }

        private bool sendCommand(String cmd, Process pythonProcess)
        {
            try
            {
                TextWriter writer = pythonProcess.StandardInput;
                writer.Write(cmd);
                writer.Write("\n");
                writer.Flush();
                return true;
            }
            catch (Exception)
            {
                _.msg("\nFailed to send command to interpreter: " + cmd);
                return false;
            }
        }

    }
}