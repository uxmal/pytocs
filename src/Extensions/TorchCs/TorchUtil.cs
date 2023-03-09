#region License
//  Copyright 2023 ToolGood
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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TorchSharp;
using static TorchSharp.torch;

namespace TorchCs
{
    public class TorchUtil
    {
        private const int MAX_LAYER = 5; // Number of times code contains code 

        /// <summary>
        ///  Convert all *.py.cs files in the folder ,Replace grammar rules
        /// </summary>
        /// <param name="folder"></param>
        public static void ReplaceFolder(string folder, bool replaceStringToNetstandard = true)
        {
            var files = Directory.GetFiles(folder, "*.py.cs", SearchOption.AllDirectories);
            HashSet<string> classNames = new HashSet<string>();
            foreach (var file in files) {
                var text = File.ReadAllText(file);
                getClassName(text, classNames);
            }
            classNames.Remove("torch");
            classNames.Remove("nn");
            classNames.Remove("F");
            foreach (var file in files) {
                var text = File.ReadAllText(file);
                File.WriteAllText(file, ReplaceCodes(text, classNames, replaceStringToNetstandard));
            }

            var fileInfos = ClassFile.LoadFiles(folder);
            var classInfos = new List<ClassInfo>();
            foreach (var file in fileInfos) { classInfos.AddRange(file.ClassInfos); }
            bool IsChange;
            do {
                IsChange = false;
                foreach (var fileInfo in fileInfos) {
                    fileInfo.LastChange = fileInfo.HasChange;
                    fileInfo.HasChange = false;
                }
                foreach (var fileInfo in fileInfos) {
                    var dict = fileInfo.MatchClassInfo(fileInfo.Code, classInfos);
                    foreach (var classInfo in fileInfo.ClassInfos) {
                        fileInfo.Code = classInfo.ReplaceMethodParamenterType(fileInfo.Code, dict);
                    }
                    if (fileInfo.HasChange) {
                        File.WriteAllText(fileInfo.FileName, fileInfo.Code);
                        IsChange = true;
                    }
                }
            } while (IsChange);
        }
        /// <summary>
        /// Convert file, Replace grammar rules
        /// </summary>
        /// <param name="file"></param>
        public static void ReplaceFile(string file, bool replaceStringToNetstandard = false)
        {
            var text = File.ReadAllText(file);
            File.WriteAllText(file, ReplaceCodes(text, null, replaceStringToNetstandard));
        }
        /// <summary>
        /// Convert code, Replace grammar rules
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ReplaceCodes(string text, HashSet<string> classNames = null, bool replaceToNetstandard = true)
        {
            // replace 'self' to 'this'
            text = Regex.Replace(text, @"\bself\.", "this.");
            // replace field type
            text = Regex.Replace(text, @"(object|void|bool|int|double|string) (\w+ = ""\S+?""[,;)])", "string $2");
            text = Regex.Replace(text, @"(object|void|bool|int|double|string) (\w+ = \d+[,;)])", "int $2");
            text = Regex.Replace(text, @"(object|void|bool|int|double|string) (\w+ = (\d+\.\d+|\d+(\.\d+)?[Ee]-?\d+)[,;)])", "double $2");
            text = Regex.Replace(text, @"(object|void|bool|int|double|string) (\w+ = (true|false)[,;)])", "bool $2");
            text = Regex.Replace(text, @"\bvoid ([a-zA-Z_][a-zA-Z0-9_]*[ ,);])", "object $1");
            // replace 'd_keys = d_keys or (d_model//n_heads)' to 'd_keys = d_keys ?? d_model / n_heads;'
            text = Regex.Replace(text, @"([a-zA-Z_0-9]+) = (\1 \|\| (.*?;))", "$1 = $1 ?? $3 //$2");
            // replace throw new ValueError
            text = text.Replace("throw new ValueError(", "throw new ArgumentException(");

            text = replaceNamespace(text);
            text = replaceConstructor(text);
            text = replaceListSlice(text);
            text = replaceNewClass(text, classNames);
            text = replaceMethodParameterName(text);
            //  Replace type by class area and static method area 
            var classInfos = ClassInfo.AnalysisCode(text);
            foreach (var classInfo in classInfos) {
                text = classInfo.AddNewField(text); // Add missing fields
                text = classInfo.ReplaceCodes(text);
            }
            //  One file is a static class. There are only static methods in the static class, so I will deal with the static methods in the file. 
            var sss = ClassMethod.AnalysisCodeForStaticMethod(text);
            foreach (var item in sss) {
                text = item.ReplaceCodes(text);
            }

            text = replaceFieldType(text);
            text = replaceMethodParamenterType(text);
            text = replaceMathMethod(text);
            text = replaceStringToEnum(text);
            text = replaceMethodAlias(text);

            text = replaceTensorList(text);
            text = replaceIsType(text);

            if (replaceToNetstandard) {
                text = replaceStringToNetstandard(text);
            }

            text = text.Replace("using (var torch.no_grad())", "using (var _no_grad= torch.no_grad())");
            text = text.Replace("using (var torch.cuda.amp.autocast())", "using (var _autocast= torch.cuda.amp.autocast())");

            text = Regex.Replace(text, @"\bnp\.inf\b", "np.Inf");
            text = text.Replace("time.time()", "DateTime.Now");

            // replace Tenser.requires_grad
            text = text.Replace(".require_grad = true;", ".requires_grad = true;");
            text = text.Replace(".require_grad = false;", ".requires_grad = false;");

            return text;
        }
        /// <summary>
        /// Create netstandard.cs file.
        /// </summary>
        /// <param name="folder"></param>
        public static void CreateNetstandardCode(string folder)
        {
            Assembly myAssem = Assembly.GetExecutingAssembly();
            var manifestResourceStream = myAssem.GetManifestResourceStream("TorchCs.Resources.netstandard.cs");
            if (manifestResourceStream == null) { return; }

            manifestResourceStream.Position = 0;
            using (StreamReader reader = new StreamReader(manifestResourceStream, Encoding.UTF8)) {
                var str = reader.ReadToEnd();
                File.WriteAllText(Path.Combine(folder, "netstandard.cs"), str);
            }
        }

        /// <summary>
        /// Replace namespace grammar rules
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string replaceNamespace(string text)
        {
            text = text.Replace("using np = numpy;", "using NumpyDotNet;");
            text = text.Replace("using torch;", "using static TorchSharp.torch;\r\nusing torch = TorchSharp.torch;\r\nusing TorchSharp.Modules;");
            text = text.Replace("using nn = torch.nn;", "using nn = TorchSharp.torch.nn;");
            text = text.Replace("using F = torch.nn.functional;", "using F = TorchSharp.torch.nn.functional;");
            text = text.Replace("using optim = torch.optim;", "using optim = TorchSharp.torch.optim;");
            text = text.Replace("using DataLoader = torch.utils.data.DataLoader;", "using DataLoader = TorchSharp.torch.utils.data.DataLoader;");

            text = text.Replace("using sys;", "");
            text = text.Replace("using math;", "");
            text = text.Replace("using os;", "");
            text = text.Replace("using time;", "");
            text = text.Replace("using warnings;", "");

            return text;
        }

        /// <summary>
        /// Replace constructor grammar rules
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string replaceConstructor(string text)
        {
            var ms = Regex.Matches(text, @"public class (\S+)[\s \t]*: nn.Module");
            if (ms.Count > 0) {
                foreach (Match item in ms) {
                    var name = item.Groups[1].Value.Trim();
                    text = Regex.Replace(text, $@"(public {name}\([^)]*\))", $"$1:base(\"{name}\")");
                    text = text.Replace($":base(\"{name}\"):base(\"{name}\")", $":base(\"{name}\")");
                }
            }
            return text;
        }

        /// <summary>
        /// Replace field type
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string replaceFieldType(string text)
        {
            var nnType = typeof(TorchSharp.torch.nn);
            var nnMethods = nnType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            foreach (var method in nnMethods) {
                var fieldType = method.ReturnType.Name;
                var methodName = method.Name;
                if (methodName == "ModuleDict" || methodName == "ModuleList") {
                    continue;
                }
                var r = $@"this\.(\S+) = nn\.{methodName}\(";
                var ms = Regex.Matches(text, r);
                foreach (Match m in ms) {
                    var name = m.Groups[1].Value;
                    text = text.Replace($"public object {name};", $"public {fieldType} {name};");
                    text = text.Replace($"public void {name};", $"public {fieldType} {name};");
                    text = Regex.Replace(text, @$"\bthis\.{name}\(", $"this.{name}.forward(");
                }
            }
            var ms2 = Regex.Matches(text, @"this\.(\S+) = new ([a-zA-Z_][a-zA-Z0-9_]+)\(");
            foreach (Match m2 in ms2) {
                var name = m2.Groups[1].Value;
                var typeName = m2.Groups[2].Value;
                text = text.Replace($"public object {name};", $"public {typeName} {name};");
                text = text.Replace($"public void {name};", $"public {typeName} {name};");
            }

            text = replaceFieldType3(text);

            text = Regex.Replace(text, @"public (object|void) (\w+_len;)", "public int $2");
            text = Regex.Replace(text, @"public (object|void) (\w+_in;)", "public int $2");
            text = Regex.Replace(text, @"public (object|void) (\w+_model;)", "public int $2");
            text = Regex.Replace(text, @"public (object|void) (\w+_out;)", "public int $2");
            text = Regex.Replace(text, @"public (object|void) (\w+_channels;)", "public int $2");
            text = Regex.Replace(text, @"public (object|void) (\w+_size;)", "public int $2");
            text = Regex.Replace(text, @"public (object|void) (\w+_dims;)", "public int $2");
            text = Regex.Replace(text, @"public (object|void) (num_\w+;)", "public int $2");
            text = Regex.Replace(text, @"public (object|void) channels;", "public int channels;");


            text = Regex.Replace(text, @"public (object|void) (\w+_path;)", "public string $2");
            text = Regex.Replace(text, @"public (object|void) (\w+_name;)", "public string $2");

            return text;
        }

        private static string replaceFieldType3(string text)
        {
            var ms = Regex.Matches(text, @"public (object|void) (\S+);");
            if (ms.Count > 0) {
                foreach (Match m in ms) {
                    var name = m.Groups[2].Value;
                    if (text.Contains($"if (this.{name})") || text.Contains($"if (!this.{name})") || text.Contains($"if (this.{name} == true)") || text.Contains($"if (this.{name} == false)")) {
                        text = text.Replace($"public object {name};", $"public bool {name};");
                        text = text.Replace($"public void {name};", $"public bool {name};");
                    } else if (text.Contains($"this.{name} = false") || text.Contains($"this.{name} = true")) {
                        text = text.Replace($"public object {name};", $"public bool {name};");
                        text = text.Replace($"public void {name};", $"public bool {name};");
                    } else if (Regex.IsMatch(text, $@"this\.{name} (=|==|!=|\+=) """) || Regex.IsMatch(text, $@"this\.{name}\.(startswith|endswith|upper|lower|replace|strip|lstrip|rstrip)\(")) {
                        text = text.Replace($"public object {name};", $"public string {name};");
                        text = text.Replace($"public void {name};", $"public string {name};");
                    } else if (Regex.IsMatch(text, $@"this\.{name} (=|==|!=|>|<|>=|<=|\+=|\-=|\*=|/=|%=) \d+\.\d+")) {
                        text = text.Replace($"public object {name};", $"public doulbe {name};");
                        text = text.Replace($"public void {name};", $"public doulbe {name};");
                    } else if (Regex.IsMatch(text, $@"this\.{name} (=|==|!=|>|<|>=|<=|\+=|\-=|\*=|/=|%=) \d+")) {
                        text = text.Replace($"public object {name};", $"public int {name};");
                        text = text.Replace($"public void {name};", $"public int {name};");
                    } else if (text.Contains($"this.{name} = {name};")) {
                        if (Regex.IsMatch(text, @$"int {name}\b")) {
                            text = text.Replace($"public object {name};", $"public int {name};");
                            text = text.Replace($"public void {name};", $"public int {name};");
                        } else if (Regex.IsMatch(text, @$"long {name}\b")) {
                            text = text.Replace($"public object {name};", $"public long {name};");
                            text = text.Replace($"public void {name};", $"public long {name};");
                        } else if (Regex.IsMatch(text, @$"doulbe {name}\b")) {
                            text = text.Replace($"public object {name};", $"public doulbe {name};");
                            text = text.Replace($"public void {name};", $"public doulbe {name};");
                        } else if (Regex.IsMatch(text, @$"string {name}\b")) {
                            text = text.Replace($"public object {name};", $"public string {name};");
                            text = text.Replace($"public void {name};", $"public string {name};");
                        } else if (Regex.IsMatch(text, @$"bool {name}\b")) {
                            text = text.Replace($"public object {name};", $"public bool {name};");
                            text = text.Replace($"public void {name};", $"public bool {name};");
                        }
                    }

                }
            }
            return text;
        }
        /// <summary>
        /// Replace Method Parameter Name
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string replaceMethodParameterName(string text)
        {
            var nnType = typeof(TorchSharp.torch.nn);
            var nnMethods = nnType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var torchType = typeof(TorchSharp.torch);
            var torchMethods = torchType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            Dictionary<string, string> parameters = new Dictionary<string, string>() {
                {"inputChannel","in_channels" },
                {"outputChannel" ,"out_channels"},
                {"dimensions" ,"dim"},
                {"hasBias" ,"bias"},
            };

            foreach (var methodInfo in nnMethods) {
                var ps = methodInfo.GetParameters();
                foreach (var p in ps) {
                    var paramName = p.Name!;
                    text = replaceMethodParameterName(text, "nn." + methodInfo.Name, getPythonParameterName(paramName), paramName);
                    if (parameters.ContainsKey(paramName)) {
                        text = replaceMethodParameterName(text, "nn." + methodInfo.Name, parameters[paramName], paramName);
                    }
                }
            }
            foreach (var methodInfo in torchMethods) {
                var ps = methodInfo.GetParameters();
                foreach (var p in ps) {
                    var paramName = p.Name!;
                    for (int i = 0; i < MAX_LAYER; i++) {
                        text = replaceMethodParameterName(text, "torch." + methodInfo.Name, getPythonParameterName(paramName), paramName);
                        if (parameters.ContainsKey(paramName)) {
                            text = replaceMethodParameterName(text, "torch." + methodInfo.Name, parameters[paramName], paramName);
                        }
                    }
                }
            }
            return text;
        }

        private static string replaceMethodParameterName(string text, string methodName, string oldName, string newName)
        {
            if (oldName == newName) { return text; }
            var r = $"({methodName}\\([^;]*?)\\b{oldName}:";
            return Regex.Replace(text, r, new MatchEvaluator((m) => {
                return m.Groups[1].Value + newName + ":";
            }));
        }
        private static string getPythonParameterName(string text)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < text.Length; i++) {
                var c = text[i];
                if (i == 0) {
                    stringBuilder.Append(char.ToLower(c));
                } else if (c >= 'A' && c <= 'Z') {
                    stringBuilder.Append('_');
                    stringBuilder.Append(char.ToLower(c));
                } else {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Replace Method Parameter Type
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string replaceMethodParamenterType(string text)
        {
            var tensorType = typeof(TorchSharp.torch.Tensor);
            var fields = tensorType.GetFields();
            HashSet<string> names = new HashSet<string>();

            foreach (var field in fields) {
                var ms2 = Regex.Matches(text, @"\b(\w+)\." + field.Name + "\\b");
                foreach (Match m in ms2) {
                    names.Add(m.Groups[1].Value);
                }
            }
            var properties = tensorType.GetProperties();
            foreach (var property in properties) {
                var ms2 = Regex.Matches(text, @"\b(\w+)\." + property.Name + "\\b");
                foreach (Match m in ms2) {
                    names.Add(m.Groups[1].Value);
                }
            }
            var methodInfos = tensorType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methodInfos) {
                var ms2 = Regex.Matches(text, @"\b(\w+)\." + method.Name + "\\(");
                foreach (Match m in ms2) {
                    names.Add(m.Groups[1].Value);
                }
            }
            var ms = Regex.Matches(text, @"\b(\w+) = torch\.");
            foreach (Match m in ms) {
                names.Add(m.Groups[1].Value);
            }
            foreach (var name in names) {
                text = text.Replace("object " + name + ",", "Tensor " + name + ",");
                text = text.Replace("void " + name + ",", "Tensor " + name + ",");
                text = text.Replace("object " + name + ";", "Tensor " + name + ";");
                text = text.Replace("void " + name + ";", "Tensor " + name + ";");
                text = text.Replace("object " + name + ")", "Tensor " + name + ")");
                text = text.Replace("void " + name + ")", "Tensor " + name + ")");
            }

            text = Regex.Replace(text, @"(object|void) (\w+_len[,;)])", "int $2");
            text = Regex.Replace(text, @"(object|void) (\w+_in[,;)])", "int $2");
            text = Regex.Replace(text, @"(object|void) (\w+_model[,;)])", "int $2");
            text = Regex.Replace(text, @"(object|void) (\w+_out[,;)])", "int $2");
            text = Regex.Replace(text, @"(object|void) (\w+_channels[,;)])", "int $2");
            text = Regex.Replace(text, @"(object|void) (\w+_size[,;)])", "int $2");
            text = Regex.Replace(text, @"(object|void) (\w+_dims[,;)])", "int $2");
            text = Regex.Replace(text, @"(object|void) (num_\w+[,;)])", "int $2");
            text = Regex.Replace(text, @"(object|void) (channels[,;)])", "int $2");

            text = Regex.Replace(text, @"(object|void) (\w+_path[,;)])", "string $2");
            text = Regex.Replace(text, @"(object|void) (\w+_name[,;)])", "string $2");

            return text;
        }

        private static string replaceMethodAlias(string text)
        {
            text = text.Replace("torch.concat(", "torch.cat("); // alias
            Dictionary<string, string> convertDict = new Dictionary<string, string>() {
                {"F.alpha_dropout","nn.AlphaDropout()" },
                {"F.celu","nn.CELU()" },
                {"F.dropout","nn.Dropout()" },
                {"F.elu","nn.ELU()" },
                {"F.feature_alpha_dropout","nn.FeatureAlphaDropout()" },
                {"F.gelu","nn.GELU()" },
                {"F.glu","nn.GLU()" },
                {"F.Hardshrink","nn.Hardshrink()" },
                {"F.hardsigmoid","nn.Hardsigmoid()" },
                {"F.hardswish","nn.Hardswish()" },
                {"F.Hardtanh","nn.Hardtanh()" },
                {"F.leaky_relu","nn.LeakyReLU()" },
                {"F.Mish","nn.Mish()" },
                {"F.relu","nn.ReLU()" },
                {"F.relu6","nn.ReLU6()" },
                {"F.rrelu","nn.RReLU()" },
                {"F.selu","nn.SELU()" },
                {"F.Sigmoid","nn.Sigmoid()" },
                {"F.SiLU","nn.SiLU()" },
                {"F.softplus","nn.Softplus()" },
                {"F.Softshrink","nn.Softshrink()" },
                {"F.Softsign","nn.Softsign()" },
                {"F.softmax2d","nn.Softmax2d()" },
                {"F.tanh","nn.Tanh()" },
                {"F.Tanhshrink","nn.Tanhshrink()" },
            };
            text = Regex.Replace(text, @"== (.*?) \? ((F\.\w+?) : (F\.\w+?);)", new MatchEvaluator(m => {
                var t = m.Groups[1].Value;
                var name1 = m.Groups[3].Value;
                var name2 = m.Groups[4].Value;
                if (convertDict.ContainsKey(name1)) { name1 = convertDict[name1]; }
                if (convertDict.ContainsKey(name2)) { name2 = convertDict[name2]; }
                return $"== {t} ? {name1} : {name2};//{m.Groups[2].Value}";
            }));

            return text;
        }


        /// <summary>
        /// Replace Math Method
        /// Convert 'math.log'(python) to 'Math.Log'(C#)
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string replaceMathMethod(string text)
        {
            var mathType = typeof(Math);
            var mathMethods = mathType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            foreach (var methodInfo in mathMethods) {
                var name = methodInfo.Name;
                var nameL = name.ToLower();
                text = Regex.Replace(text, @$"\bmath\.{nameL}\(", $"Math.{name}(");
            }
            text = Regex.Replace(text, @"\bmath\.pi\b", "Math.PI");
            text = Regex.Replace(text, @"\bmath\.e\b", "Math.E");
            text = Regex.Replace(text, @"\bmath\.tau\b", "Math.Tau");
            text = Regex.Replace(text, @"\bmath\.inf\b", "double.PositiveInfinity");
            return text;
        }

        /// <summary>
        /// Replace common Tensor list
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string replaceTensorList(string text)
        {
            text = text.Replace("torch.cat(new List<object>", "torch.cat(new List<Tensor>");

            text = text.Replace("torch.cat(new List<object>", "torch.cat(new List<Tensor>");
            text = text.Replace("torch.ones(new List<object>", "torch.ones(new long[]");
            text = text.Replace("torch.ones(new List<int>", "torch.ones(new long[]");
            text = text.Replace("torch.zeros(new List<object>", "torch.zeros(new long[]");
            text = text.Replace("torch.zeros(new List<int>", "torch.zeros(new long[]");

            text = text.Replace("new List<object>();", "new List<Tensor>();");
            return text;
        }
        /// <summary>
        /// Convert python's [:,:,:] syntax
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string replaceListSlice(string text)
        {
            text = Regex.Replace(text, @"\[(((?<BR>\[)|(?<-BR>\])|[^\[\]])+)\]", new MatchEvaluator(m => {
                if (m.Groups[1].Value.Contains(":") == false) {
                    return m.Value;
                }
                var ts = replaceListSlice(m.Groups[1].Value); // recurrence , exclude nesting
                var strs = ts.Split(',');
                List<string> list = new List<string>();
                foreach (var str in strs) {
                    if (str.Trim() == "\":\"") {
                        list.Add("TensorIndex.Colon");
                    } else if (str.Trim() == "") {
                        list.Add("TensorIndex.Null");
                    } else if (str.Contains(":")) {
                        var ss = str.Trim().Split(':');
                        string r = "TensorIndex.Slice(";
                        for (int i = 0; i < ss.Length; i++) {
                            var s = ss[i];
                            if (i > 0) { r += ","; }
                            if (s.Trim() == "") {
                                r += "null";
                            } else {
                                r += s;
                            }
                        }
                        r += ")";
                        list.Add(r);
                    } else {
                        list.Add(str);
                    }
                }
                return "[" + string.Join(",", list) + "]";
            }));
            return text;
        }

        /// <summary>
        /// Convert  'xx is nn.Conv1d' to 'xx is Conv1d'
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string replaceIsType(string text)
        {
            var nnType = typeof(TorchSharp.torch.nn);
            var nnMethods = nnType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            foreach (var method in nnMethods) {
                var fieldType = method.ReturnType.Name;
                var methodName = method.Name;
                if (methodName == "ModuleDict" || methodName == "ModuleList") {
                    continue;
                }
                text = text.Replace($" is nn.{methodName}", $" is {methodName}");
            }
            return text;
        }

        /// <summary>
        /// Replace String To Enum
        /// example: Convert 'paddingMode: "zeros"' to 'paddingMode: TorchSharp.PaddingModes.Zeros'
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string replaceStringToEnum(string text)
        {
            text = Regex.Replace(text, @"\bpaddingMode: ""zeros""", "paddingMode: TorchSharp.PaddingModes.Zeros");
            text = Regex.Replace(text, @"\bpaddingMode: ""reflect""", "paddingMode: TorchSharp.PaddingModes.Reflect");
            text = Regex.Replace(text, @"\bpaddingMode: ""replicate""", "paddingMode: TorchSharp.PaddingModes.Replicate");
            text = Regex.Replace(text, @"\bpaddingMode: ""circular""", "paddingMode: TorchSharp.PaddingModes.Circular");
            text = Regex.Replace(text, @"\bpaddingMode: ""constant""", "paddingMode: TorchSharp.PaddingModes.Constant");

            text = Regex.Replace(text, @"\breduction: ""none""", "reduction: Reduction.None");
            text = Regex.Replace(text, @"\breduction: ""mean""", "reduction: Reduction.Mean");
            text = Regex.Replace(text, @"\breduction: ""sum""", "reduction: Reduction.Sum");

            text = Regex.Replace(text, @"\bnonLinearity: ""relu""", "nonLinearity: NonLinearities.ReLU");
            text = Regex.Replace(text, @"\bnonLinearity: ""tanh""", "nonLinearity: NonLinearities.Tanh");

            text = Regex.Replace(text, @"\bactivation: ""relu""", "activation: Activations.ReLU");
            text = Regex.Replace(text, @"\bactivation: ""gelu""", "activation: Activations.GELU");

            text = Regex.Replace(text, @"\bmode: ""nearest""", "mode: UpsampleMode.Nearest");
            text = Regex.Replace(text, @"\bmode: ""linear""", "mode: UpsampleMode.Linear");
            text = Regex.Replace(text, @"\bmode: ""bilinear""", "mode: UpsampleMode.Bilinear");
            text = Regex.Replace(text, @"\bmode: ""bicubic""", "mode: UpsampleMode.Bicubic");
            text = Regex.Replace(text, @"\bmode: ""trilinear""", "mode: UpsampleMode.Trilinear");

            return text;
        }

        /// <summary>
        ///  Convert to the syntax style of netstandard.cs 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string replaceStringToNetstandard(string text)
        {
            text = Regex.Replace(text, @" zip\(", " TorchEnumerable.zip(");

            text = Regex.Replace(text, @"(\([A-Za-z_0-9]+,[A-Za-z_0-9 ]+\) = \w+\.shape);", "var $1.ToLong2();");
            text = Regex.Replace(text, @"(\([A-Za-z_0-9]+,[A-Za-z_0-9 ]+,[A-Za-z_0-9 ]+\) = \w+\.shape);", "var $1.ToLong3();");
            text = Regex.Replace(text, @"(\([A-Za-z_0-9]+,[A-Za-z_0-9 ]+,[A-Za-z_0-9 ]+,[A-Za-z_0-9 ]+\) = \w+\.shape);", "var $1.ToLong4();");

            text = Regex.Replace(text, @"(\([A-Za-z_0-9]+,[A-Za-z_0-9 ]+\) = \w+\.size\(\));", "var $1.ToLong2();");
            text = Regex.Replace(text, @"(\([A-Za-z_0-9]+,[A-Za-z_0-9 ]+,[A-Za-z_0-9 ]+\) = \w+\.size\(\));", "var $1.ToLong3();");
            text = Regex.Replace(text, @"(\([A-Za-z_0-9]+,[A-Za-z_0-9 ]+,[A-Za-z_0-9 ]+,[A-Za-z_0-9 ]+\) = \w+\.size\(\));", "var $1.ToLong4();");

            return text;
        }

        /// <summary>
        ///  Add 'new' word to class initialization 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="classNames"></param>
        /// <returns></returns>
        private static string replaceNewClass(string text, HashSet<string> classNames)
        {
            if (classNames == null) { return text; }
            const string classRegex = @"using ([a-zA-Z_@][a-zA-Z0-9_]*) = ([a-zA-Z_@][a-zA-Z0-9_.@]*);";

            List<string> names = new List<string>();
            var ms = Regex.Matches(text, classRegex);
            foreach (Match m in ms) {
                if (classNames.Contains(m.Groups[1].Value)) {
                    names.Add(m.Groups[1].Value);
                }
            }
            if (names.Count == 0) { return text; }

            var namereg = string.Join("|", names);
            text = Regex.Replace(text, $@"\b({namereg})\(", "new $1(");
            text = Regex.Replace(text, @"\bnew new ", "new ");
            return text;
        }
        /// <summary>
        ///  Get all type names, excluding static classes 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="classNames"></param>
        private static void getClassName(string text, HashSet<string> classNames)
        {
            const string classRegex = @"public class ([a-zA-Z_][a-zA-Z0-9_]*)";
            var ms = Regex.Matches(text, classRegex);
            foreach (Match m in ms) {
                classNames.Add(m.Groups[1].Value);
            }
        }
        /// <summary>
        /// Split parameter, applicable to method definition and method call
        /// </summary>
        /// <param name="paramenters"></param>
        /// <returns></returns>
        internal static List<string> splitParamenters(string paramenters)
        {
            bool inText = false;
            int bracketLayer = 0; // 

            List<string> result = new List<string>();
            var index = 0;
            string temp = "";
            while (index < paramenters.Length) {
                var c = paramenters[index];
                if (inText) {
                    temp += c;
                    if (c == '\\') {
                        index++;
                        temp += paramenters[index];
                    } else if (c == '"') {
                        inText = false;
                    }
                } else if (c == '(' || c == '{' || c == '[' || c == '<') {
                    bracketLayer++;
                    temp += c;
                } else if (c == ')' || c == '}' || c == ']' || c == '>') {
                    bracketLayer--;
                    temp += c;
                } else if (c == ',' && bracketLayer == 0) {
                    result.Add(temp.Trim());
                    temp = "";
                } else {
                    temp += c;
                }
                index++;
            }
            result.Add(temp.Trim());
            return result;
        }

        /// <summary>
        ///  Judge whether it is a Double type according to the parameter name 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static bool isDoubleTypeByName(string name)
        {
            if (Regex.IsMatch(name, "^(dropout|lr|lr_step|factor|lr_max|num)$", RegexOptions.IgnoreCase)) {
                return true;
            }
            if (Regex.IsMatch(name, "^.*(_dropout|_factor|_momentum|_lr|_min|_max)$", RegexOptions.IgnoreCase)) {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Judge whether it is a Int type according to the parameter name 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static bool isIntTypeByName(string name)
        {
            if (Regex.IsMatch(name, "^(channels|index|length|step|epoch|stride|total_steps|d_k|d_v|d_q)$", RegexOptions.IgnoreCase)) {
                return true;
            }
            if (Regex.IsMatch(name, "^.*(_len|_length|_in|_model|_out|_channels|_size|_dims|_count|_index|_epoch|_num|_side)$", RegexOptions.IgnoreCase)) {
                return true;
            }
            if (Regex.IsMatch(name, "^(num_|n_).*$", RegexOptions.IgnoreCase)) {
                return true;
            }
            if (Regex.IsMatch(name, "^.*(_num_|_len_).*$", RegexOptions.IgnoreCase)) {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Judge whether it is a String type according to the parameter name 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static bool isStringTypeByName(string name)
        {
            if (Regex.IsMatch(name, "^(name|path|dir|file|device)$", RegexOptions.IgnoreCase)) {
                return true;
            }
            if (Regex.IsMatch(name, "^.*(_path|_name|_dir|file|_str|_txt)$", RegexOptions.IgnoreCase)) {
                return true;
            }
            return false;
        }


    }
}
