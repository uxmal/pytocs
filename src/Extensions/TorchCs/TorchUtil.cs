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

namespace TorchCs
{
    public class TorchUtil
    {
        private const int MAX_LAYER = 5; // Number of times code contains code 

        /// <summary>
        ///  Convert all *.py.cs files in the folder ,Replace grammar rules
        /// </summary>
        /// <param name="folder"></param>
        public static void ReplaceFolder(string folder)
        {
            var files = Directory.GetFiles(folder, "*.py.cs", SearchOption.AllDirectories);
            foreach (var file in files) {
                var text = File.ReadAllText(file);
                File.WriteAllText(file, ReplaceCodes(text));
            }
        }
        /// <summary>
        /// Convert file, Replace grammar rules
        /// </summary>
        /// <param name="file"></param>
        public static void ReplaceFile(string file)
        {
            var text = File.ReadAllText(file);
            File.WriteAllText(file, ReplaceCodes(text));
        }
        /// <summary>
        /// Convert code, Replace grammar rules
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ReplaceCodes(string text)
        {
            // replace 'self' to 'this'
            text = Regex.Replace(text, @"\bself\.", "this.");
            // replace field type
            text = Regex.Replace(text, @"(object|void) (\w+ = ""\S+?""[,;)])", "string $2");
            text = Regex.Replace(text, @"(object|void) (\w+ = \d+[,;)])", "int $2");
            text = Regex.Replace(text, @"(object|void) (\w+ = \d+\.\d+[,;)])", "double $2");
            text = Regex.Replace(text, @"(object|void) (\w+ = (true|false)[,;)])", "bool $2");
            // replace 'd_keys = d_keys or (d_model//n_heads)' to 'd_keys = d_keys ?? d_model / n_heads;'
            text = Regex.Replace(text, @"([a-zA-Z_0-9]+) = (\1 \|\| (.*?;))", "$1 = $1 ?? $3 //$2");


            text = replaceNamespace(text);
            text = replaceConstructor(text);
            text = replaceFieldType(text);
            text = replaceMethodParameterName(text);
            text = replaceMethodParamenterType(text);
            text = replaceMathMethod(text);
            text = replaceStringToEnum(text);
            text = replaceMethodAlias(text);

            text = replaceForwardMethod(text);
            text = replaceCallForwardMethod(text);

            text = replaceListSlice(text);

            text = replaceTensorList(text);
            text = replaceIsType(text);

            text = replaceStringToNetstandard(text);

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
                if (ms.Count > 0) {
                    foreach (Match m in ms) {
                        var name = m.Groups[1].Value;
                        text = text.Replace($"public object {name};", $"public {fieldType} {name};");
                        text = text.Replace($"public void {name};", $"public {fieldType} {name};");
                        text = Regex.Replace(text, @$"\bthis\.{name}\(", $"this.{name}.forward(");
                    }
                }
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
                    if (text.Contains($"this.{name} = {name};")) {
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
                    } else if (text.Contains($"if (this.{name})") || text.Contains($"if (!this.{name})") || text.Contains($"if (this.{name} == true)") || text.Contains($"if (this.{name} == false)")) {
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
        /// Replace forward method's return type and forward method's parameter type
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string replaceForwardMethod(string text)
        {
            text = text.Replace(" Tuple<object, object>", " (Tensor, Tensor)");
            text = text.Replace(" Tuple<object, void> forward(", " (Tensor, Tensor) forward(");
            text = text.Replace(" object[] forward(", " (Tensor, Tensor) forward(");
            text = text.Replace(" Tuple<object, List<object>> forward(", " (Tensor, List<Tensor>) forward(");
            text = text.Replace(" object forward(", " Tensor forward(");
            text = text.Replace(" void forward(", " Tensor forward(");
            text = text.Replace(" forward(object x", " forward(Tensor x");
            text = text.Replace(" forward(object t", " forward(Tensor t");
            text = text.Replace(" forward(object queries, object keys, object values", " forward(Tensor queries, Tensor keys, Tensor values");
            return text;
        }
        /// <summary>
        /// Replace common forward method calls
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string replaceCallForwardMethod(string text)
        {
            text = Regex.Replace(text, @"\bthis\.inner_attention\(", "this.inner_attention.forward(");
            text = Regex.Replace(text, @"\bthis\.dropout\(", "this.dropout.forward(");
            text = Regex.Replace(text, @"\bthis\.attention\(", "this.attention.forward(");
            text = Regex.Replace(text, @"\bthis\.self_attention\(", "this.self_attention.forward(");
            text = Regex.Replace(text, @"\bthis\.cross_attention\(", "this.cross_attention.forward(");
            text = Regex.Replace(text, @"\bthis\.projection\(", "this.projection.forward(");
            text = Regex.Replace(text, @"\bthis\.activation\(", "this.activation.forward(");
            text = Regex.Replace(text, @"\bthis\.norm\(", "this.norm.forward(");
            text = Regex.Replace(text, @"\bthis\.conv\(", "this.conv.forward(");
            text = Regex.Replace(text, @"\bthis\.decomp\(", "this.decomp.forward(");
            text = Regex.Replace(text, @"\bthis\.decomp1\(", "this.decomp1.forward(");
            text = Regex.Replace(text, @"\bthis\.decomp2\(", "this.decomp2.forward(");
            text = Regex.Replace(text, @"\bthis\.decomp3\(", "this.decomp3.forward(");
            text = Regex.Replace(text, @"\bthis\.decomp4\(", "this.decomp4.forward(");
            text = Regex.Replace(text, @"\bthis\.decomp5\(", "this.decomp5.forward(");
            text = Regex.Replace(text, @"\bthis\.conv1\(", "this.conv1.forward(");
            text = Regex.Replace(text, @"\bthis\.conv2\(", "this.conv2.forward(");
            text = Regex.Replace(text, @"\bthis\.conv3\(", "this.conv3.forward(");
            text = Regex.Replace(text, @"\bthis\.conv4\(", "this.conv4.forward(");
            text = Regex.Replace(text, @"\bthis\.conv5\(", "this.conv5.forward(");
            text = Regex.Replace(text, @"\bthis\.norm1\(", "this.norm1.forward(");
            text = Regex.Replace(text, @"\bthis\.norm2\(", "this.norm2.forward(");
            text = Regex.Replace(text, @"\bthis\.norm3\(", "this.norm3.forward(");
            text = Regex.Replace(text, @"\bthis\.norm4\(", "this.norm4.forward(");
            text = Regex.Replace(text, @"\bthis\.norm5\(", "this.norm5.forward(");

            text = Regex.Replace(text, @"\bthis\.downConv\(", "this.downConv.forward(");
            text = Regex.Replace(text, @"\bthis\.maxPool\(", "this.maxPool.forward(");
            text = Regex.Replace(text, @"\bthis\.avg\(", "this.avg.forward(");
            text = Regex.Replace(text, @"\bthis\.layernorm\(", "this.layernorm.forward(");
            text = Regex.Replace(text, @"\bthis\.tokenConv\(", "this.tokenConv.forward(");

            text = Regex.Replace(text, @"\bthis\.embedding\(", "this.embedding.forward(");
            text = Regex.Replace(text, @"\bthis\.emb\(", "this.emb.forward(");
            text = Regex.Replace(text, @"\bthis\.embed\(", "this.embed.forward(");
            text = Regex.Replace(text, @"\bthis\.position_embedding\(", "this.position_embedding.forward(");
            text = Regex.Replace(text, @"\bthis\.temporal_embedding\(", "this.temporal_embedding.forward(");
            text = Regex.Replace(text, @"\bthis\.value_embedding\(", "this.value_embedding.forward(");

            text = Regex.Replace(text, @"\bthis\.month_embed\(", "this.month_embed.forward(");
            text = Regex.Replace(text, @"\bthis\.day_embed\(", "this.day_embed.forward(");
            text = Regex.Replace(text, @"\bthis\.hour_embed\(", "this.hour_embed.forward(");
            text = Regex.Replace(text, @"\bthis\.minute_embed\(", "this.minute_embed.forward(");
            text = Regex.Replace(text, @"\bthis\.weekday_embed\(", "this.weekday_embed.forward(");

            text = Regex.Replace(text, @"\bthis\.enc_embedding\(", "this.enc_embedding.forward(");
            text = Regex.Replace(text, @"\bthis\.encoder\(", "this.encoder.forward(");
            text = Regex.Replace(text, @"\bthis\.dec_embedding\(", "this.dec_embedding.forward(");
            text = Regex.Replace(text, @"\bthis\.decoder\(", "this.decoder.forward(");

            text = Regex.Replace(text, @"\bthis\.query_projection\(", "this.query_projection.forward(");
            text = Regex.Replace(text, @"\bthis\.key_projection\(", "this.key_projection.forward(");
            text = Regex.Replace(text, @"\bthis\.value_projection\(", "this.value_projection.forward(");
            text = Regex.Replace(text, @"\bthis\.out_projection\(", "this.out_projection.forward(");

            text = Regex.Replace(text, @"\bthis\.attn\(", "this.attn.forward(");
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
            text = Regex.Replace(text, @"\[([^\[\]]*?)\]", new MatchEvaluator(m => {
                if (m.Groups[1].Value.Contains(":") == false) {
                    return m.Value;
                }
                var strs = m.Groups[1].Value.Split(',');
                List<string> list = new List<string>();
                foreach (var str in strs) {
                    if (str.Trim() == "\":\"") {
                        list.Add("TensorIndex.Ellipsis");
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



    }
}
