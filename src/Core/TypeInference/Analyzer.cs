#region License
//  Copyright 2015-2018 John Källén
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pytocs.Core.Syntax;
using Pytocs.Core.Types;
using Name = Pytocs.Core.Syntax.Identifier;
using System.Diagnostics;
using Pytocs.Core;

namespace Pytocs.Core.TypeInference
{
    public interface Analyzer
    {
        DataTypeFactory TypeFactory { get; }
        int CalledFunctions { get; set; }
        State GlobalTable { get; }
        HashSet<Name> Resolved { get; }
        HashSet<Name> Unresolved { get; }
        Dictionary<Node,List<Binding>> References { get; }

        DataType LoadModule(List<Name> name, State state);
        Module GetAstForFile(string file);
        string GetModuleQname(string file);
        IEnumerable<Binding> GetModuleBindings();

        Binding CreateBinding(string id, Node node, DataType type, BindingKind kind);
        void addRef(AttributeAccess attr, DataType targetType, ISet<Binding> bs);
        void putRef(Node node, ICollection<Binding> bs);
        void putRef(Node node, Binding bs);
        void AddExpType(Exp node, DataType type);
        void AddUncalled(FunType f);
        void RemoveUncalled(FunType f);
        void pushStack(Exp v);
        void popStack(Exp v);
        bool InStack(Exp v);

        string ModuleName(string path);
        string ExtendPath(string path, string name);

        void AddProblem(Node loc, string msg);
        void AddProblem(string filename, int start, int end, string msg);

        //void msg(string message);
        void msg_(string message, params object[] args);
        string Percent(long num, long total);
    }

    /// <summary>
    /// Analyzes a directory of Python files, collecting 
    /// and inferring type information as it parses all 
    /// the files.
    /// </summary>
    public class AnalyzerImpl : Analyzer
    {
        //public const string MODEL_LOCATION = "org/yinwang/pysonar/models";
        private List<string> loadedFiles = new List<string>();
        private List<Binding> allBindings = new List<Binding>();
        private Dictionary<Node, DataType> expTypes = new Dictionary<Node, DataType>();
        private Dictionary<string, List<Diagnostic>> semanticErrors = new Dictionary<string, List<Diagnostic>>();
        private Dictionary<string, List<Diagnostic>> parseErrors = new Dictionary<string, List<Diagnostic>>();
        private string cwd = null;
        private List<string> path = new List<string>();
        private HashSet<FunType> uncalled = new HashSet<FunType>();
        private HashSet<object> callStack = new HashSet<object>();
        private HashSet<object> importStack = new HashSet<object>();

        private AstCache astCache;
        private string cacheDir;
        private HashSet<string> failedToParse = new HashSet<string>();
        private IProgress loadingProgress;
        private string projectDir;
        private readonly string suffix;
        private ILogger logger;
        private IFileSystem FileSystem;

        public Dictionary<string, object> options;
        private DateTime startTime;

        public AnalyzerImpl(ILogger logger)
            : this(new FileSystem(), logger, new Dictionary<string, object>(), DateTime.Now)
        {
        }

        public AnalyzerImpl(IFileSystem fs, ILogger logger, Dictionary<string, object> options, DateTime startTime)
        {
            this.FileSystem = fs;
            this.logger = logger;
            this.TypeFactory = new DataTypeFactory(this);
            this.GlobalTable = new State(null, State.StateType.GLOBAL);
            this.Resolved = new HashSet<Name>();
            this.Unresolved = new HashSet<Name>();
            this.References = new Dictionary<Node, List<Binding>>();

            if (options != null)
            {
                this.options = options;
            }
            else
            {
                this.options = new Dictionary<string, object>();
            }
            this.startTime = startTime;
            this.suffix = ".py";
            this.Builtins = new Builtins(this);
            this.Builtins.Initialize();
            AddPythonPath();
            CopyModels();
            CreateCacheDirectory();
            if (astCache == null)
            {
                astCache = new AstCache(this, FileSystem, logger, cacheDir);
            }
        }

        public DataTypeFactory TypeFactory { get; private set; }
        public int CalledFunctions { get; set; }
        public State GlobalTable { get; private set; }
        public HashSet<Name> Resolved { get; private set; }
        public HashSet<Name> Unresolved { get; private set; }
        public Builtins Builtins { get; private set; }
        public Dictionary<Node, List<Binding>> References { get; private set; }

        public State ModuleTable = new State(null, State.StateType.GLOBAL);

        /// <summary>
        /// Main entry to the analyzer
        /// </summary>
        public void Analyze(string path)
        {
            string upath = FileSystem.GetFullPath(path);
            this.projectDir = FileSystem.DirectoryExists(upath) ? upath : FileSystem.GetDirectoryName(upath);
            LoadFileRecursive(upath);
            msg(Resources.FinishedLoadingFiles, CalledFunctions);
            msg(Resources.AnalyzingUncalledFunctions);
            ApplyUncalled();
        }

        public bool HasOption(string option)
        {
            if (options.TryGetValue(option, out object op) && (bool) op)
                return true;
            else
                return false;
        }

        public void setCWD(string cd)
        {
            if (cd != null)
            {
                cwd = FileSystem.GetFullPath(cd);
            }
        }

#if NOT_USED // This code doesn't seem to be used anywhere
        public void addPaths(List<string> p)
        {
            foreach (string s in p)
            {
                addPath(s);
            }
        }



        public void setPath(List<string> path)
        {
            this.path = new List<string>(path.Count);
            addPaths(path);
        }
#endif
        private void AddPath(string p)
        {
            path.Add(FileSystem.GetFullPath(p));
        }

        private void AddPythonPath()
        {
            string path = Environment.GetEnvironmentVariable("PYTHONPATH");
            if (path != null)
            {
                var pathseparator = ':';
                if (Array.IndexOf(System.IO.Path.GetInvalidPathChars(), ':') >= 0)
                    pathseparator = ';';

                string[] segments = path.Split(pathseparator);
                foreach (string p in segments)
                {
                    AddPath(p);
                }
            }
        }

        private void CopyModels()
        {
#if NOT
        URL resource = Thread.currentThread().getContextClassLoader().getResource(MODEL_LOCATION);
        string dest = _.locateTmp("models");
        this.modelDir = dest;

        try {
            _.copyResourcesRecursively(resource, new File(dest));
            _.msg("copied models to: " + modelDir);
        } catch (Exception e) {
            _.die("Failed to copy models. Please check permissions of writing to: " + dest);
        }
        addPath(dest);
#endif
        }

        public List<string> getLoadPath()
        {
            var loadPath = new List<string>();
            if (cwd != null)
            {
                loadPath.Add(cwd);
            }
            if (projectDir != null && FileSystem.DirectoryExists(projectDir))
            {
                loadPath.Add(projectDir);
            }
            loadPath.AddRange(path);
            return loadPath;
        }

        public bool InStack(Exp f)
        {
            return callStack.Contains(f);
        }

        public void pushStack(Exp f)
        {
            callStack.Add(f);
        }

        public void popStack(Exp f)
        {
            callStack.Remove(f);
        }

        public bool InImportStack(string f)
        {
            return importStack.Contains(f);
        }

        public void PushImportStack(string f)
        {
            importStack.Add(f);
        }

        public void PopImportStack(string f)
        {
            importStack.Remove(f);
        }

        public List<Binding> GetAllBindings()
        {
            return allBindings;
        }

        public IEnumerable<Binding> GetModuleBindings()
        {
            return ModuleTable.table.Values
                .SelectMany(g =>g)
                .Where(g => g.kind == BindingKind.MODULE && 
                            !g.IsBuiltin && !g.IsSynthetic);
        }

        ModuleType GetCachedModule(string file)
        {
            DataType t = ModuleTable.LookupType(GetModuleQname(file));
            if (t == null)
            {
                return null;
            }
            else if (t is UnionType ut)
            {
                foreach (DataType tt in ut.types)
                {
                    if (tt is ModuleType mt)
                    {
                        return mt;
                    }
                }
                return null;
            }
            else if (t is ModuleType mt)
            {
                return mt;
            }
            else
            {
                return null;
            }
        }

        public string GetModuleQname(string file)
        {
            if (file.EndsWith("__init__.py"))
            {
                file = FileSystem.GetDirectoryName(file);
            }
            else if (file.EndsWith(suffix))
            {
                file = file.Substring(0, file.Length - suffix.Length);
            }
            return file.Replace(".", "%20").Replace('/', '.').Replace('\\', '.');
        }

        public List<Diagnostic> GetDiagnosticsForFile(string file)
        {
            if (semanticErrors.TryGetValue(file, out var errs))
            {
                return errs;
            }
            return new List<Diagnostic>();
        }

        public void putRef(Node node, ICollection<Binding> bs)
        {
            if (!(node is Url))
            {
                if (!References.TryGetValue(node, out var bindings))
                {
                    bindings = new List<Binding>(1);
                    References[node] = bindings;
                }
                foreach (Binding b in bs)
                {
                    if (!bindings.Contains(b))
                    {
                        bindings.Add(b);
                    }
                    b.AddReference(node);
                }
            }
        }

        public void putRef(Node node, Binding b)
        {
            var bs = new List<Binding> { b };
            putRef(node, bs);
        }

        public void AddExpType(Exp exp, DataType dt)
        {
            //$REVIEW: unify with previous type?
            this.expTypes[exp] = dt;
        }

        public void AddProblem(Node loc, string msg)
        {
            string file = loc.Filename;
            if (file != null)
            {
                AddFileError(file, loc.Start, loc.End, msg);
            }
        }

        // for situations without a Node
        public void AddProblem(string file, int begin, int end, string msg)
        {
            if (file != null)
            {
                AddFileError(file, begin, end, msg);
            }
        }

        private void AddFileError(string file, int begin, int end, string msg)
        {
            var d = new Diagnostic(file, Diagnostic.Category.ERROR, begin, end, msg);
            GetFileErrors(file, semanticErrors).Add(d);
        }

        List<Diagnostic> GetFileErrors(string file, Dictionary<string, List<Diagnostic>> map)
        {
            if (!map.TryGetValue(file, out var msgs))
            {
                msgs = new List<Diagnostic>();
                map[file] = msgs;
            }
            return msgs;
        }

        public DataType LoadFile(string path)
        {
            path = FileSystem.GetFullPath(path);
            if (!FileSystem.FileExists(path))
            {
                return null;
            }

            ModuleType module = GetCachedModule(path);
            if (module != null)
            {
                return module;
            }

            // detect circular import
            if (InImportStack(path))
            {
                return null;
            }

            // set new CWD and save the old one on stack
            string oldcwd = cwd;
            setCWD(FileSystem.GetDirectoryName(path));

            PushImportStack(path);
            loadingProgress.Tick();
            Debug.Print($"*** Path {path}");
            var ast = GetAstForFile(path);
            DataType type = null;
            if (ast == null)
            {
                failedToParse.Add(path);
            }
            else
            {
                loadedFiles.Add(path);
                type = LoadModule(ast);
            }
            PopImportStack(path);

            // restore old CWD
            setCWD(oldcwd);
            return type;
        }

        public DataType LoadModule(Module ast)
        {
            return new TypeTransformer(ModuleTable, this).VisitModule(ast);
        }

        private void CreateCacheDirectory()
        {
            var p = FileSystem.CombinePath(FileSystem.getSystemTempDir(), "pytocs");
            cacheDir =FileSystem.CombinePath(p, "ast_cache");
            string f = cacheDir;
            msg(Resources.AstCacheIsAt, cacheDir);

            if (!FileSystem.FileExists(f))
            {
                try
                {
                    FileSystem.CreateDirectory(f);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(
                        string.Format(Resources.ErrFailedToCreateTmpDirectory, cacheDir),
                        ex);
                }
            }
        }

        /// <summary>
        /// Returns the syntax tree for {@code file}. <p>
        /// </summary>
        public Module GetAstForFile(string file)
        {
            return astCache.getAST(file);
        }

        public ModuleType GetBuiltinModule(string qname)
        {
            return Builtins.get(qname);
        }

        public string MakeQname(List<Name> names)
        {
            if (names.Count == 0)
            {
                return "";
            }
            return string.Join(".", names.Select(n => n.Name));
        }

        /// <summary>
        /// Find the path that contains modname. Used to find the starting point of locating a qname.
        /// </summary>
        /// <param name="headName">first module name segment</param>
        public string LocateModule(string headName)
        {
            List<string> loadPath = getLoadPath();
            foreach (string p in loadPath)
            {
                string startDir = FileSystem.CombinePath(p, headName);
                string initFile = FileSystem.CombinePath(startDir, "__init__.py");

                if (FileSystem.FileExists(initFile))
                {
                    return p;
                }

                string startFile = FileSystem.CombinePath(startDir , suffix);
                if (FileSystem.FileExists(startFile))
                {
                    return p;
                }
            }
            return null;
        }

        public DataType LoadModule(List<Name> name, State state)
        {
            if (name.Count == 0)
            {
                return null;
            }

            string qname = MakeQname(name);
            DataType mt = GetBuiltinModule(qname);
            if (mt != null)
            {
                state.Insert(
                    this,
                    name[0].Name,
                    new Url(Builtins.LIBRARY_URL + mt.Table.Path + ".html"),
                    mt, BindingKind.SCOPE);
                return mt;
            }

            // If there are more than one segment load the packages first
            string startPath = LocateModule(name[0].Name);

            if (startPath == null)
            {
                return null;
            }

            DataType prev = null;
            string path = startPath;
            for (int i = 0; i < name.Count; i++)
            {
                path = FileSystem.CombinePath(path, name[i].Name);
                string initFile = FileSystem.CombinePath(path, "__init__.py");
                if (FileSystem.FileExists(initFile))
                {
                    DataType mod = LoadFile(initFile);
                    if (mod == null)
                    {
                        return null;
                    }

                    if (prev != null)
                    {
                        prev.Table.Insert(this, name[i].Name, name[i], mod, BindingKind.VARIABLE);
                    }
                    else
                    {
                        state.Insert(this, name[i].Name, name[i], mod, BindingKind.VARIABLE);
                    }

                    prev = mod;

                }
                else if (i == name.Count - 1)
                {
                    string startFile = path + suffix;
                    if (FileSystem.FileExists(startFile))
                    {
                        DataType mod = LoadFile(startFile);
                        if (mod == null)
                        {
                            return null;
                        }
                        if (prev != null)
                        {
                            prev.Table.Insert(this, name[i].Name, name[i], mod, BindingKind.VARIABLE);
                        }
                        else
                        {
                            state.Insert(this, name[i].Name, name[i], mod, BindingKind.VARIABLE);
                        }
                        prev = mod;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            return prev;
        }

        /// <summary>
        /// Load all Python source files recursively if the given fullname is a
        /// directory; otherwise just load a file.  Looks at file extension to
        /// determine whether to load a given file.
        /// </summary>
        public void LoadFileRecursive(string fullname)
        {
            int count = CountFileRecursive(fullname);
            if (loadingProgress == null)
            {
                loadingProgress = new Progress(this, count, 50, this.HasOption("quiet"));
            }

            string file_or_dir = fullname;

            if (FileSystem.DirectoryExists(file_or_dir))
            {
                foreach (string file in FileSystem.GetFileSystemEntries(file_or_dir))
                {
                    LoadFileRecursive(file);
                }
            }
            else if (file_or_dir.EndsWith(suffix))
            {
                LoadFile(file_or_dir);
            }
        }

        /// <summary>
        /// Count number of .py files
        /// </summary>
        /// <param name="fullname"></param>
        /// <returns></returns>
        public int CountFileRecursive(string fullname)
        {
            string file_or_dir = fullname;
            int sum = 0;

            if (FileSystem.DirectoryExists(file_or_dir))
            {
                foreach (string file in FileSystem.GetFileSystemEntries(file_or_dir))
                {
                    sum += CountFileRecursive(file);
                }
            }
            else
            {
                if (file_or_dir.EndsWith(suffix))
                {
                    ++sum;
                }
            }
            return sum;
        }

        public Dictionary<Node, DataType> BuildTypeDictionary()
        {
            var types = new Dictionary<Node, DataType>();
            foreach (var b in GetAllBindings())
            {
                var dt = b.type;
                if (types.TryGetValue(b.node, out var dtOld))
                {
                    dt = UnionType.CreateUnion(new[] { dt, dtOld });
                }
                types[b.node] = dt;
            }

            foreach (var b in GetAllBindings())
            {
                var dt = types[b.node];
                foreach (var r in b.References)
                {
                    if (types.TryGetValue(r, out var dtOld))
                    {
                        dt = UnionType.CreateUnion(new[] { dt, dtOld });
                    }
                    types[r] = dt;
                }
            }

            foreach (var de in this.expTypes)
            {
                types[de.Key] = de.Value;
            }

            return types;
        }

        public void Finish()
        {
            // mark unused variables
            foreach (Binding b in allBindings)
            {
                if (!(b.type is ClassType) &&
                        !(b.type is FunType) &&
                        !(b.type is ModuleType)
                        && b.References.Count == 0)
                {
                    AddProblem(b.node, string.Format(Resources.UnusedVariable, b.name));
                }
            }
            msg(GetAnalysisSummary());
        }

        public void Close()
        {
            astCache.Close();
        }

        public void msg(string m, params object[] args)
        {
            if (!HasOption("quiet"))
            {
                Debug.Print(m, args);
                Console.WriteLine(m, args);
            }
        }

        public void msg_(string m, params object[] args)
        {
            if (!HasOption("quiet"))
            {
                Console.Write(string.Format(m, args));
            }
        }

        public void AddUncalled(FunType cl)
        {
            if (cl.Definition != null && !cl.Definition.called)
            {
                uncalled.Add(cl);
            }
        }

        public void RemoveUncalled(FunType f)
        {
            uncalled.Remove(f);
        }

        public void ApplyUncalled()
        {
            IProgress progress = new Progress(this, uncalled.Count, 50, this.HasOption("quiet"));
            while (uncalled.Count != 0)
            {
                var uncalledDup = uncalled.ToList();
                foreach (FunType cl in uncalledDup)
                {
                    progress.Tick();
                    TypeTransformer.Apply(this, cl, null, null, null, null, null);
                }
            }
        }

        public string FormatTime(TimeSpan span)
        {
            return string.Format("{0:hh\\:mm\\:ss}", span);
        }

        public string GetAnalysisSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(Banner(Resources.AnalysisSummary));

            string duration = FormatTime(DateTime.Now  - this.startTime);
            sb.AppendFormat(Resources.AnalysisTotalTime, duration);
            sb.AppendLine();
            sb.AppendFormat(Resources.AnalysisModulesLoaded, loadedFiles.Count);
            sb.AppendLine();
            sb.AppendFormat(Resources.AnalysisSemanticProblems, semanticErrors.Count);
            sb.AppendLine();
            sb.AppendFormat(Resources.AnalysisParseFailures, failedToParse.Count);
            sb.AppendLine();

            // calculate number of defs, refs, xrefs
            int nDef = 0, nXRef = 0;
            foreach (Binding b in GetAllBindings())
            {
                nDef += 1;
                nXRef += b.References.Count;
            }

            sb.AppendFormat(Resources.AnalysisNumberDefinitions, nDef);
            sb.AppendLine();
            sb.AppendFormat(Resources.AnalysisNumberXrefs, nXRef);
            sb.AppendLine();
            sb.AppendFormat(Resources.AnalysisNumberReferences, References.Count);
            sb.AppendLine();

            long resolved = this.Resolved.Count;
            long unresolved = this.Unresolved.Count;
            sb.AppendFormat(Resources.AnalysisNumberResolvedNames, resolved);
            sb.AppendLine();
            sb.AppendFormat(Resources.AnalysisNumberUnresolvedNames, unresolved);
            sb.AppendLine();
            sb.AppendFormat(Resources.AnalysisNumberResolutionPercentage, Percent(resolved, resolved + unresolved));
            sb.AppendLine();

            return sb.ToString();
        }

        public string Banner(string msg)
        {
            return "---------------- " + msg + " ----------------";
        }

        public List<string> GetLoadedFiles()
        {
            List<string> files = new List<string>();
            foreach (string file in loadedFiles)
            {
                if (file.EndsWith(suffix))
                {
                    files.Add(file);
                }
            }
            return files;
        }

        public void RegisterBinding(Binding b)
        {
            allBindings.Add(b);
        }

        public override string ToString()
        {
            return "(analyzer:" +
                    "[" + allBindings.Count + " bindings] " +
                    "[" + References.Count + " refs] " +
                    "[" + loadedFiles.Count + " files] " +
                    ")";
        }

        /// <summary>
        /// Given an absolute <paramref name="path"/> to a file (not a directory), 
        /// returns the module name for the file.  If the file is an __init__.py, 
        /// returns the last component of the file's parent directory, else 
        /// returns the filename without path or extension. 
        /// </summary>
        public string ModuleName(string path)
        {
            string f = path;
            string name = FileSystem.GetFileName(f);
            if (name == "__init__.py")
            {
                return FileSystem.GetDirectoryName(f);
            }
            else if (name.EndsWith(suffix))
            {
                return name.Substring(0, name.Length - suffix.Length);
            }
            else
            {
                return name;
            }
        }

        public Binding CreateBinding(string id, Node node, DataType type, BindingKind kind)
        {
            var b = new Binding(id, node, type, kind);
            RegisterBinding(b);
            return b;
        }

        public string Percent(long num, long total)
        {
            if (total == 0)
            {
                return "100%";
            }
            else
            {
                int pct = (int) (num * 100 / total);
                return string.Format("{0,3}%", pct);
            }
        }

        public void addRef(AttributeAccess attr, DataType targetType, ISet<Binding> bs)
        {
            foreach (Binding b in bs)
            {
                putRef(attr, b);
                if (attr.Parent != null && attr.Parent is Application &&
                        b.type is FunType && targetType is InstanceType)
                {  // method call 
                    ((FunType) b.type).SelfType = targetType;
                }
            }
        }

        public string ExtendPath(string path, string name)
        {
            name = ModuleName(name);
            if (path == "")
            {
                return name;
            }
            return path + "." + name;
        }
    }
}
