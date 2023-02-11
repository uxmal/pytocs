using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TorchCs
{
    public class TorchUtil
    {
        public static void ReplaceFolder(string folder)
        {
            var files = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                File.WriteAllText(file, ReplaceCodes(text));
            }
        }
        public static void ReplaceFile(string file)
        {
            var text = File.ReadAllText(file);
            File.WriteAllText(file, ReplaceCodes(text));
        }

        public static string ReplaceCodes(string text)
        {
            text = replaceNamespace(text);
            text = replaceConstructor(text);
            text = replaceFieldType(text);
            text = replaceMethodParameterName(text);
            text = replaceMathMethod(text);

            text = replaceForwardMethod(text);
            text = replaceCallForwardMethod(text);

            text = replaceListSlice(text);

            text = replaceTensorList(text);

            #region other
            text = text.Replace("using (var torch.no_grad()) {", "using (var _temp= torch.no_grad()) {");
            text = Regex.Replace(text, @"\bos\.makedirs\(", "Directory.CreateDirectory(");
            text = Regex.Replace(text, @"\bos\.path\.join\(", "Path.Combine(");

            #endregion

            return text;
        }
        #region replaceNamespace
        static string replaceNamespace(string text)
        {
            text = text.Replace("using np = numpy;", "using NumpyDotNet;");
            text = text.Replace("using torch;", "using static TorchSharp.torch;\r\nusing torch = TorchSharp.torch;\r\nusing TorchSharp.Modules;");
            text = text.Replace("using nn = torch.nn;", "using nn = TorchSharp.torch.nn;");
            text = text.Replace("using F = torch.nn.functional;", "using F = TorchSharp.torch.nn.functional;");

            text = text.Replace("using math;", "");
            text = text.Replace("using os;", "");
            return text;
        }
        #endregion

        #region replaceConstructor
        static string replaceConstructor(string text)
        {
            var ms = Regex.Matches(text, @"public class (\S+)[\s \t]*: nn.Module");
            if (ms.Count > 0)
            {
                foreach (Match item in ms)
                {
                    var name = item.Groups[1].Value.Trim();
                    text = Regex.Replace(text, $@"(public {name}\([^)]*\))", $"$1:base(\"{name}\")");
                    text = text.Replace($":base(\"{name}\"):base(\"{name}\")", $":base(\"{name}\")");
                }
            }
            return text;
        }
        #endregion

        #region replaceFieldType
        static string replaceFieldType(string text)
        {
            var nnType = typeof(TorchSharp.torch.nn);
            var nnMethods = nnType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            foreach (var method in nnMethods)
            {
                var fieldType = method.ReturnType.Name;
                var methodName = method.Name;
                if (methodName == "ModuleDict" || methodName == "ModuleList")
                {
                    continue;
                }
                var r = $@"this\.(\S+) = nn\.{methodName}\(";
                var ms = Regex.Matches(text, r);
                if (ms.Count > 0)
                {
                    foreach (Match m in ms)
                    {
                        var name = m.Groups[1].Value;
                        text = text.Replace($"public object {name};", $"public {fieldType} {name};");
                        text = text.Replace($"public void {name};", $"public {fieldType} {name};");
                        text = Regex.Replace(text, @$"\bthis\.{name}\(", $"this.{name}.forward(");
                    }
                }
            }
            text = replaceFieldType3(text);
            return text;
        }

        static string replaceFieldType3(string text)
        {
            var ms = Regex.Matches(text, @"public (object|void) (\S+);");
            if (ms.Count > 0)
            {
                foreach (Match m in ms)
                {
                    var name = m.Groups[2].Value;
                    if (text.Contains($"this.{name} = {name};"))
                    {
                        if (text.Contains($"int {name} ="))
                        {
                            text = text.Replace($"public object {name};", $"public int {name};");
                            text = text.Replace($"public void {name};", $"public int {name};");
                        } else if (text.Contains($"long {name} ="))
                        {
                            text = text.Replace($"public object {name};", $"public long {name};");
                            text = text.Replace($"public void {name};", $"public long {name};");
                        } else if (text.Contains($"doulbe {name} ="))
                        {
                            text = text.Replace($"public object {name};", $"public doulbe {name};");
                            text = text.Replace($"public void {name};", $"public doulbe {name};");
                        } else if (text.Contains($"string {name} ="))
                        {
                            text = text.Replace($"public object {name};", $"public string {name};");
                            text = text.Replace($"public void {name};", $"public string {name};");
                        } else if (text.Contains($"bool {name} ="))
                        {
                            text = text.Replace($"public object {name};", $"public bool {name};");
                            text = text.Replace($"public void {name};", $"public bool {name};");
                        }
                    }
                }
            }
            return text;
        }

        #endregion

        #region replaceMethodParameterName
        static string replaceMethodParameterName(string text)
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

            foreach (var methodInfo in nnMethods)
            {
                var ps = methodInfo.GetParameters();
                foreach (var p in ps)
                {
                    text = replaceMethodParameterName(text, "nn." + methodInfo.Name, getPythonParameterName(p.Name), p.Name);
                    if (parameters.ContainsKey(p.Name))
                    {
                        text = replaceMethodParameterName(text, "nn." + methodInfo.Name, parameters[p.Name], p.Name);
                    }
                }
            }
            foreach (var methodInfo in torchMethods)
            {
                var ps = methodInfo.GetParameters();
                foreach (var p in ps)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        text = replaceMethodParameterName(text, "torch." + methodInfo.Name, getPythonParameterName(p.Name), p.Name);
                        if (parameters.ContainsKey(p.Name))
                        {
                            text = replaceMethodParameterName(text, "torch." + methodInfo.Name, parameters[p.Name], p.Name);
                        }
                    }
                }
            }
            return text;
        }

        static string replaceMethodParameterName(string text, string methodName, string oldName, string newName)
        {
            if (oldName == newName) { return text; }

            var r = $"({methodName}\\([^;]*?){oldName}:";
            return Regex.Replace(text, r, new MatchEvaluator((m) => {
                return m.Groups[1].Value + newName + ":";
            }));
        }
        static string getPythonParameterName(string text)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (i == 0)
                {
                    stringBuilder.Append(char.ToLower(c));
                } else if (c >= 'A' && c <= 'Z')
                {
                    stringBuilder.Append('_');
                    stringBuilder.Append(char.ToLower(c));
                } else
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString();
        }

        #endregion

        #region replaceMathMethod
        static string replaceMathMethod(string text)
        {
            var mathType = typeof(Math);
            var mathMethods = mathType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            foreach (var methodInfo in mathMethods)
            {
                var name = methodInfo.Name;
                var nameL = name.ToLower();
                text = Regex.Replace(text, @$"\bmath\.{nameL}\(", $"Math.{name}(");
            }
            return text;
        }

        #endregion

        #region replaceForwardMethod
        static string replaceForwardMethod(string text)
        {
            text = text.Replace(" Tuple<object, object> forward(", " (Tensor, Tensor) forward(");
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
        #endregion

        #region replaceCallForwardMethod
        static string replaceCallForwardMethod(string text)
        {
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
        #endregion

        #region replaceTensorList
        static string replaceTensorList(string text)
        {
            text = text.Replace(" torch.cat(new List<object>", " torch.cat(new List<Tensor>");
            text = text.Replace(" torch.ones(new List<object>", " torch.ones(new List<Tensor>");
            text = text.Replace(" torch.zeros(new List<object>", " torch.zeros(new List<Tensor>");

            text = text.Replace("var attns = new List<object>();", "var attns = new List<Tensor>();");
            text = text.Replace("attns.append(attn);", "attns.Add(attn);");
            return text;
        }
        #endregion

        #region replaceListSlice
        static string replaceListSlice(string text)
        {
            text = Regex.Replace(text, @"\[([^\[\]]*?)\]", new MatchEvaluator(m => {
                if (m.Groups[1].Value.Contains(":") == false)
                {
                    return m.Value;
                }
                var strs = m.Groups[1].Value.Split(',');
                List<string> list = new List<string>();
                foreach (var str in strs)
                {
                    if (str.Trim() == "\":\"")
                    {
                        list.Add("TensorIndex.Ellipsis");
                    } else if (str.Trim() == "")
                    {
                        list.Add("TensorIndex.Null");
                    } else if (str.Contains(":"))
                    {
                        var ss = str.Trim().Split(':');
                        string r = "TensorIndex.Slice(";
                        for (int i = 0; i < ss.Length; i++)
                        {
                            var s = ss[i];
                            if (i > 0) { r += ","; }
                            if (s.Trim() == "")
                            {
                                r += "null";
                            } else
                            {
                                if (s.StartsWith("self."))
                                {
                                    r += s.Replace("self.", "this.");
                                } else
                                {
                                    r += s;
                                }
                            }
                        }
                        r += ")";
                        list.Add(r);
                    } else
                    {
                        list.Add(str);
                    }
                }
                return "[" + string.Join(",", list) + "]";
            }));
            return text;
        }
        #endregion


    }
}
