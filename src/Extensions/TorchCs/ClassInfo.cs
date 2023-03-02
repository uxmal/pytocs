using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace TorchCs
{
    public class ClassFile
    {
        public string FileName { get; set; }
        public string Code { get; set; }
        public List<ClassInfo> ClassInfos { get; set; }

        public List<ClassMethod> StaticMethods { get; set; }

        public static List<ClassFile> LoadFiles(string folder)
        {
            var files = new List<ClassFile>();
            var files2 = Directory.GetFiles(folder, "*.py.cs", SearchOption.AllDirectories);
            foreach (var file in files2) {
                var text = File.ReadAllText(file);
                ClassFile classFile = new ClassFile();
                classFile.FileName = file;
                classFile.Code = text;
                classFile.ClassInfos = ClassInfo.AnalysisCode(text);
                classFile.StaticMethods = ClassMethod.AnalysisCodeForStaticMethod(text);
            }
            return files;
        }
    }


    public class ClassInfo
    {
        private const string classRegex = @"public class ([a-zA-Z_][a-zA-Z0-9_]*)([\s\S]*?)\{(((?<BR>\()|(?<-BR>\))|(?<BR2>\{)|(?<-BR2>\})|[^(){}])+)\}";
        private const string classRegex2 = @"public class {name}([\s\S]*?)\{(((?<BR>\()|(?<-BR>\))|(?<BR2>\{)|(?<-BR2>\})|[^(){}])+)\}";

        public ClassFile File { get; set; }
        public string FullClassName { get; set; }
        public string ClassName { get; set; }
        public bool HasForwardMethod { get; set; }
        public ClassConstructor Constructor { get; set; }
        public List<ClassField> Fields { get; set; }
        public List<ClassMethod> Methods { get; set; }

        public static List<ClassInfo> AnalysisCode(string code)
        {
            List<ClassInfo> classInfos = new List<ClassInfo>();
            var match = Regex.Match(code, @"namespace ([a-zA-Z_][a-zA-Z0-9._]*) ");
            var match2 = Regex.Match(code, @"public static class ([a-zA-Z_][a-zA-Z0-9._]*) ");
            var prefix = match.Groups[1].Value + "." + match2.Groups[1].Value;

            var ms = Regex.Matches(code, classRegex);
            foreach (Match m in ms) {
                ClassInfo classInfo = new ClassInfo();

                classInfo.FullClassName = prefix + "." + m.Groups[1].Value;
                classInfo.ClassName = m.Groups[1].Value;
                var bodyCode = m.Groups[3].Value;
                classInfo.Constructor = ClassConstructor.AnalysisCode(bodyCode, classInfo.ClassName);
                classInfo.Fields = ClassField.AnalysisCode(bodyCode);
                classInfo.Methods = ClassMethod.AnalysisCode(bodyCode);
                classInfo.HasForwardMethod = classInfo.Methods.Any(q => q.MethodName == "forward");

                classInfos.Add(classInfo);
            }
            var fclass = classInfos.Where(q => q.HasForwardMethod).Select(q => q.ClassName).ToList();
            foreach (var info in classInfos) {
                foreach (var item in info.Fields) {
                    if (fclass.Contains(item.NewType ?? item.Type)) {
                        item.HasForwardMethod = true;
                    }
                }
            }
            return classInfos;
        }
        public string AddNewField(string code)
        {
            if (Fields.Any(q => q.IsNewField)) {
                code = Regex.Replace(code, classRegex2.Replace("{name}", ClassName), new MatchEvaluator(m => {
                    var bodyCode = m.Groups[2].Value;
                    var baseClass = m.Groups[1].Value;
                    foreach (var field in Fields) {
                        bodyCode = field.AddNewField(bodyCode);
                    }
                    return $"public class {ClassName}{baseClass}{{{bodyCode}}}";
                }));
            }
            return code;
        }

        public string ReplaceNewConstructor(string code, List<ClassInfo> classInfos)
        {
            foreach (var classInfo in classInfos) {
                code = Regex.Replace(code, $@"\b{classInfo.ClassName}\(", $"new {classInfo.ClassName}(");
            }
            code = Regex.Replace(code, @"\bnew new ", "new ");
            return code;
        }

        public string ReplaceCodes(string code)
        {
            code = Regex.Replace(code, classRegex2.Replace("{name}", ClassName), new MatchEvaluator(m => {
                var bodyCode = m.Groups[2].Value;
                var baseClass = m.Groups[1].Value;
                foreach (var field in Fields) {
                    bodyCode = field.ReplaceCodes(bodyCode);
                }
                bodyCode = Constructor.ReplaceCodes(bodyCode);
                foreach (var method in Methods) {
                    bodyCode = method.ReplaceCodes(bodyCode, Fields);
                }
                return $"public class {ClassName}{baseClass}{{{bodyCode}}}";
            }));
            return code;
        }
        public override string ToString()
        {
            return $"class: {ClassName}";
        }
    }
    public class ClassConstructor
    {
        private const string constructorRegex = @"public {name}\(([^)]*?)\)(.*?)\{(((?<BR>\()|(?<-BR>\))|(?<BR2>\{)|(?<-BR2>\})|[^(){}])+)\}";

        public string ClassName { get; set; }

        public List<ClassMethodParamenter> Paramenters { get; set; }
        public List<ClassMethodVariable> Variables { get; set; }

        public static ClassConstructor AnalysisCode(string code, string className)
        {
            ClassConstructor classConstructor = new ClassConstructor();
            classConstructor.ClassName = className;
            var reg = constructorRegex.Replace("{name}", className);
            var m = Regex.Match(code, reg);
            classConstructor.Paramenters = ClassMethodParamenter.AnalysisCode(m.Groups[1].Value, m.Groups[3].Value);
            classConstructor.Variables = ClassMethodVariable.AnalysisCode(m.Groups[3].Value, classConstructor.Paramenters);
            return classConstructor;
        }



        public string ReplaceCodes(string code)
        {
            code = Regex.Replace(code, constructorRegex.Replace("{name}", ClassName), new MatchEvaluator(m => {
                var ParamenterCode = m.Groups[1].Value;
                foreach (var paramenter in Paramenters) {
                    ParamenterCode = paramenter.ReplaceCodes(ParamenterCode);
                }
                var BodyCode = m.Groups[3].Value;
                foreach (var variable in Variables) {
                    BodyCode = variable.ReplaceCodes(BodyCode);
                }
                return $"public {ClassName}({ParamenterCode}){m.Groups[2].Value}{{{BodyCode}}}";
            }));
            return code;
        }
    }

    public class ClassField
    {
        public string Type { get; set; }
        public string NewType { get; set; }
        public string FieldName { get; set; }
        public bool IsNewField { get; set; }
        public bool HasForwardMethod { get; set; }

        public static List<ClassField> AnalysisCode(string code)
        {
            List<ClassField> classFields = new List<ClassField>();
            HashSet<string> fields = new HashSet<string>();
            var ms = Regex.Matches(code, "public ([a-zA-Z_][a-zA-Z0-9_<>]*) ([a-zA-Z_][a-zA-Z0-9_]*);");
            foreach (Match match in ms) {
                ClassField field = new ClassField();
                field.Type = match.Groups[1].Value;
                field.FieldName = match.Groups[2].Value;
                fields.Add(field.FieldName);
                classFields.Add(field);
            }
            ms = Regex.Matches(code, @"\bthis\.([a-zA-Z_][a-zA-Z0-9_]*)[ \t\r\n,;)\[]");
            foreach (Match m in ms) {
                if (fields.Add(m.Groups[1].Value)) {
                    ClassField field = new ClassField();
                    field.Type = "object";
                    field.FieldName = m.Groups[1].Value;
                    field.IsNewField = true;
                    classFields.Add(field);
                }
            }

            var nnMethods = TorchSharpInfo.Instance.nnMethods;
            foreach (var method in nnMethods) {
                var fieldType = method.ReturnType.Name;
                var methodName = method.Name;
                if (methodName == "ModuleDict" || methodName == "ModuleList") { continue; }

                var r = $@"this\.(\S+) = nn\.{methodName}\(";
                var ms3 = Regex.Matches(code, r);
                foreach (Match m in ms3) {
                    var name = m.Groups[1].Value;
                    var f = classFields.FirstOrDefault(q => q.FieldName == name);
                    if (f != null) { f.NewType = fieldType; f.HasForwardMethod = true; }
                }
            }

            var ms2 = Regex.Matches(code, @"this\.(\S+) = new ([a-zA-Z_][a-zA-Z0-9_]+)\(");
            foreach (Match m2 in ms2) {
                var name = m2.Groups[1].Value;
                var typeName = m2.Groups[2].Value;
                var f = classFields.FirstOrDefault(q => q.FieldName == name);
                if (f != null) { f.NewType = typeName; }
            }

            foreach (var field1 in classFields) {
                if (field1.NewType != null) { continue; }
                var name = field1.FieldName;
                if (code.Contains($"if (this.{name})") || code.Contains($"if (!this.{name})")) {
                    field1.NewType = "bool";
                } else if (Regex.IsMatch(code, $@"this\.{name} (=|==|!=) (false|true)") || Regex.IsMatch(code, $@"(\(|&& |\|\| )!?this\.{name} (&&|\|\|)") || Regex.IsMatch(code, $@"(&&|\|\|) !?this\.{name}(\)| &&| \|\|)")) {
                    field1.NewType = "bool";
                } else if (Regex.IsMatch(code, $@"this\.{name} (=|==|!=|\+=) """) || Regex.IsMatch(code, $@"this\.{name}\.(startswith|endswith|upper|lower|replace|strip|lstrip|rstrip)\(")) {
                    field1.NewType = "string";
                } else if (Regex.IsMatch(code, $@"this\.{name} (=|==|!=|>|<|>=|<=|\+=|\-=|\*=|/=|%=) \d+\.\d+")) {
                    field1.NewType = "double";
                } else if (Regex.IsMatch(code, $@"this\.{name} (=|==|!=|>|<|>=|<=|\+=|\-=|\*=|/=|%=) \d+")) {
                    field1.NewType = "int";
                } else if (Regex.IsMatch(code, $@"this\.{name}\[[^\]]*?TensorIndex\.")) {
                    field1.NewType = "Tensor";
                } else if (field1.Type == "object" && Regex.IsMatch(name, "^(dropout|.*_dropout)$")) {
                    field1.NewType = "double";
                } else if (field1.Type == "object" && Regex.IsMatch(name, "^(channels|index|length|step|epoch|(num_|n_).*|.*(_len|_in|_model|_out|_channels|_size|_dims|_count|_index))$")) {
                    field1.NewType = "int";
                } else if (field1.Type == "object" && Regex.IsMatch(name, "^(name|path|dir|.*(_path|_name|_dir))$")) {
                    field1.NewType = "string";
                    //} else if (classMethodParamenter.Type == "object" && Regex.IsMatch(text, $@" [\+\-\*\/] {name}[ ,;)]")) {
                    //    classMethodParamenter.NewType = "double";
                    //} else if (classMethodParamenter.Type == "object" && Regex.IsMatch(text, $@"[(, ]{name} [\+\-\*\/] ")) {
                    //    classMethodParamenter.NewType = "double";
                } else {
                    var type = TorchSharpInfo.Instance.FindTypeBy_nn(code, "this." + field1.FieldName);
                    if (type == null) {
                        type = TorchSharpInfo.Instance.FindTypeBy_torch(code, "this." + field1.FieldName);
                    }
                    if (type != null) {
                        field1.NewType = type;
                    }
                }
            }
            return classFields;
        }
        public string AddNewField(string code)
        {
            if (IsNewField) {
                return $"\r\n\t\t\tpublic {NewType ?? Type} {FieldName};{code}";
            }
            return code;
        }

        public string ReplaceCodes(string code)
        {
            if (NewType == null || NewType == Type) { return code; }
            return code.Replace($"public {Type} {FieldName};", $"public {NewType} {FieldName};");
        }

        public override string ToString()
        {
            return $"field: {NewType ?? Type} {FieldName}";
        }

    }

    public class ClassMethod
    {
        private const string methodRegex = @"public (virtual) ([a-zA-Z_][a-zA-Z0-9_]*|Tuple<[a-zA-Z_][a-zA-Z0-9_<>, ]*>|\([^)]+?\)) ([a-zA-Z_@][a-zA-Z0-9_]*)\(([^)]*?)\) ?\{(((?<BR>\()|(?<-BR>\))|(?<BR2>\{)|(?<-BR2>\})|[^(){}])+)\}";
        private const string methodRegex2 = @"public (virtual|static) ([a-zA-Z_][a-zA-Z0-9_]*|Tuple<[a-zA-Z_][a-zA-Z0-9_<>, ]*>|\([^)]+?\)) {name}\(([^)]*?)\) ?\{(((?<BR>\()|(?<-BR>\))|(?<BR2>\{)|(?<-BR2>\})|[^(){}])+)\}";
        private const string methodRegex3 = @"public (static) ([a-zA-Z_][a-zA-Z0-9_]*|Tuple<[a-zA-Z_][a-zA-Z0-9_<>, ]*>|\([^)]+?\)) ([a-zA-Z_@][a-zA-Z0-9_]*)\(([^)]*?)\) ?\{(((?<BR>\()|(?<-BR>\))|(?<BR2>\{)|(?<-BR2>\})|[^(){}])+)\}";
        // public static int get_q_k(int input_size, int window_size, object stride, object device)

        public string MethodName { get; set; }
        public string ReturnType { get; set; }
        public string NewReturnType { get; set; }
        public bool IsForwardMethod { get; set; }

        public List<ClassMethodParamenter> Paramenters { get; set; } = new List<ClassMethodParamenter>();
        public List<ClassMethodVariable> Variables { get; set; } = new List<ClassMethodVariable>();

        public static List<ClassMethod> AnalysisCodeForStaticMethod(string code)
        {
            List<ClassMethod> classMethods = new List<ClassMethod>();
            var ms = Regex.Matches(code, methodRegex3);
            foreach (Match m in ms) {
                ClassMethod classMethod = new ClassMethod();

                classMethod.ReturnType = m.Groups[2].Value;
                classMethod.MethodName = m.Groups[3].Value;
                classMethod.Paramenters = ClassMethodParamenter.AnalysisCode(m.Groups[4].Value, m.Groups[5].Value);
                classMethod.Variables = ClassMethodVariable.AnalysisCode(m.Groups[5].Value, classMethod.Paramenters);
                classMethods.Add(classMethod);
            }
            return classMethods;
        }
        public static List<ClassMethod> AnalysisCode(string code)
        {
            List<ClassMethod> classMethods = new List<ClassMethod>();
            var ms = Regex.Matches(code, methodRegex);
            foreach (Match m in ms) {
                ClassMethod classMethod = new ClassMethod();
                classMethod.ReturnType = m.Groups[2].Value;
                classMethod.MethodName = m.Groups[3].Value;
                classMethod.Paramenters = ClassMethodParamenter.AnalysisCode(m.Groups[4].Value, m.Groups[5].Value);
                classMethod.Variables = ClassMethodVariable.AnalysisCode(m.Groups[5].Value, classMethod.Paramenters);
                classMethod.IsForwardMethod = classMethod.MethodName == "forward";
                classMethods.Add(classMethod);
            }
            return classMethods;
        }

        public string ReplaceCodes(string code, List<ClassField> fields = null)
        {
            code = Regex.Replace(code, methodRegex2.Replace("{name}", MethodName), new MatchEvaluator(m => {
                var ParamenterCode = m.Groups[3].Value;
                foreach (var paramenter in Paramenters) {
                    ParamenterCode = paramenter.ReplaceCodes(ParamenterCode);
                }
                var bodyCode = m.Groups[4].Value;
                foreach (var variable in Variables) {
                    bodyCode = variable.ReplaceCodes(bodyCode);
                }
                if (fields != null) {
                    foreach (var field in fields) {
                        if (field.HasForwardMethod || IsForwardMethod) {
                            bodyCode = Regex.Replace(bodyCode, @$"\bthis\.{field.FieldName}\(", $"this.{field.FieldName}.forward(");
                            bodyCode = Regex.Replace(bodyCode, @$"\bthis\.{field.FieldName}(\[([a-zA-Z_][a-zA-Z_0-9]*|\^?[0-9]+)\])\(", $"this.{field.FieldName}$1.forward(");
                        }
                    }
                }
                if (NewReturnType == null) {
                    if (ReturnType.StartsWith("Tuple<")) {
                        NewReturnType = ReturnType.Replace("Tuple<", "(");
                        NewReturnType = NewReturnType.Substring(0, NewReturnType.Length - 1) + ")";
                        if (IsForwardMethod) {
                            NewReturnType = NewReturnType.Replace("object", "Tensor");
                            NewReturnType = NewReturnType.Replace("void", "Tensor");
                        }
                    } else if (ReturnType == "void" || ReturnType == "object") {
                        var ms = Regex.Matches(bodyCode, "return ([^;]*);");
                        var max = 0;
                        foreach (Match item in ms) {
                            if (item.Groups[1].Value.StartsWith('(')) {
                                var t= item.Groups[1].Value.Substring(1, item.Groups[1].Value.Length-2);
                                var ms2 = TorchUtil.splitParamenters(t);
                                max = Math.Max(max, ms2.Count);
                            } else {
                                max = Math.Max(max, 1);
                            }
                        }
                        if (max == 1) {
                            NewReturnType = "object";
                        } else if (max > 1) {
                            NewReturnType = "(";
                            for (int i = 0; i < max; i++) {
                                if (i > 0) { NewReturnType += ","; }
                                NewReturnType += "object";
                            }
                            NewReturnType += ")";
                        }
                        if (IsForwardMethod) {
                            NewReturnType = (NewReturnType?? ReturnType).Replace("object", "Tensor");
                            NewReturnType = NewReturnType.Replace("void", "Tensor");
                        }
                    }
                }
                return $"public {m.Groups[1].Value} {NewReturnType ?? ReturnType} {MethodName}({ParamenterCode}){{{bodyCode}}}";
            }));
            return code;
        }
        public override string ToString()
        {
            if (NewReturnType != null) {
                return $"method: {NewReturnType} {MethodName}";
            }
            return $"method: {ReturnType} {MethodName}";
        }
    }

    public class ClassMethodParamenter
    {
        public string ParamenterName { get; set; }
        public string Type { get; set; }
        public string NewType { get; set; }
        public string DefaultValue { get; set; }

        public static List<ClassMethodParamenter> AnalysisCode(string code, string text)
        {
            var fieldsRegex = TorchSharpInfo.Instance.TensorFieldRegex;
            var methodRegex = TorchSharpInfo.Instance.TensorMethodRegex;


            List<ClassMethodParamenter> classMethodParamenters = new List<ClassMethodParamenter>();
            if (string.IsNullOrEmpty(code)) { return classMethodParamenters; }

            var strs = Regex.Matches(code, "(.*?) ([a-zA-Z_][a-zA-Z_0-9]*)( = ([^,]+))?(,|$)");

            foreach (Match str in strs) {
                ClassMethodParamenter classMethodParamenter = new ClassMethodParamenter();
                classMethodParamenters.Add(classMethodParamenter);
                classMethodParamenter.Type = str.Groups[1].Value.Trim();
                classMethodParamenter.ParamenterName = str.Groups[2].Value.Trim();
                var name = classMethodParamenter.ParamenterName;
                //if (name == "inputs") {

                //}
                if (str.Groups[3].Success) {
                    classMethodParamenter.DefaultValue = str.Groups[4].Value.Trim();

                    if (classMethodParamenter.DefaultValue == "true" || classMethodParamenter.DefaultValue == "false") {
                        classMethodParamenter.NewType = "bool";
                    } else if (classMethodParamenter.DefaultValue.StartsWith("\"")) {
                        classMethodParamenter.NewType = "string";
                    } else if (Regex.IsMatch(classMethodParamenter.DefaultValue, @"\-?\d+\.\d+")) {
                        classMethodParamenter.NewType = "double";
                    } else if (Regex.IsMatch(classMethodParamenter.DefaultValue, @"\-?\d+")) {
                        classMethodParamenter.NewType = "int";
                    } else if (classMethodParamenter.DefaultValue == "null") {
                        if (Regex.IsMatch(text, @$"{name} = {name} \?\? [a-zA-Z_][a-zA-Z_0-9]* [\+\-\*\/] [a-zA-Z_][a-zA-Z_0-9]*;")) {
                            classMethodParamenter.NewType = "int?";
                        }
                    }
                    if (classMethodParamenter.NewType != null) { continue; }
                }
                if (text.Contains($"if ({name})") || text.Contains($"if (!{name})")) {
                    classMethodParamenter.NewType = "bool";
                } else if (Regex.IsMatch(text, $@"(^|[ \t(,;\[]){name} (=|==|!=) (false|true)") || Regex.IsMatch(text, $@"(\(|&& |\|\| )!?{name} (&&|\|\|)") || Regex.IsMatch(text, $@"(&&|\|\|) !?{name}(\)| &&| \|\|)")) {
                    classMethodParamenter.NewType = "bool";
                } else if (Regex.IsMatch(text, $@"(^|[ \t(,;\[]){name} (=|==|!=|\+=) """) || Regex.IsMatch(text, $@"(^|[ \t(,;\[]){name}\.(split|startswith|endswith|upper|lower|replace|strip|lstrip|rstrip)\(")) {
                    classMethodParamenter.NewType = "string";
                } else if (Regex.IsMatch(text, $@"(^|[ \t(,;\[]){name} (=|==|!=|>|<|>=|<=|\+=|\-=|\*=|/=|%=) \d+\.\d+")) {
                    classMethodParamenter.NewType = "doulbe";
                } else if (Regex.IsMatch(text, $@"(^|[ \t(,;\[]){name} (=|==|!=|>|<|>=|<=|\+=|\-=|\*=|/=|%=) \d+")) {
                    classMethodParamenter.NewType = "int";
                } else if (Regex.IsMatch(text, $@"(^|[ \t(,;\[]){name}\[[^\]]*?TensorIndex\.")) {
                    classMethodParamenter.NewType = "Tensor";
                } else if (Regex.IsMatch(text, $@"(^|[ \t(,;\[]){name}\.{fieldsRegex}[ ,;)\[]")) {
                    classMethodParamenter.NewType = "Tensor";
                } else if (Regex.IsMatch(text, $@"(^|[ \t(,;\[]){name}\.{methodRegex}\(")) {
                    classMethodParamenter.NewType = "Tensor";
                } else if (classMethodParamenter.Type == "object" && Regex.IsMatch(name, "^(dropout|.*_dropout)$")) {
                    classMethodParamenter.NewType = "double";
                } else if (classMethodParamenter.Type == "object" && Regex.IsMatch(name, "^(channels|index|length|step|epoch|(num_|n_).*|.*(_len|_in|_model|_out|_channels|_size|_dims|_count|_index))$")) {
                    classMethodParamenter.NewType = "int";
                } else if (classMethodParamenter.Type == "object" && Regex.IsMatch(name, "^(name|path|dir|.*(_path|_name|_dir))$")) {
                    classMethodParamenter.NewType = "string";
                    //} else if (classMethodParamenter.Type == "object" && Regex.IsMatch(text, $@" [\+\-\*\/] {name}[ ,;)]")) {
                    //    classMethodParamenter.NewType = "double";
                    //} else if (classMethodParamenter.Type == "object" && Regex.IsMatch(text, $@"[(, ]{name} [\+\-\*\/] ")) {
                    //    classMethodParamenter.NewType = "double";
                } else {
                    var type = TorchSharpInfo.Instance.FindTypeBy_nn(text, classMethodParamenter.ParamenterName);
                    if (type == null) {
                        type = TorchSharpInfo.Instance.FindTypeBy_torch(text, classMethodParamenter.ParamenterName);
                    }
                    if (type != null) {
                        classMethodParamenter.NewType = type;
                    }
                }

            }
            return classMethodParamenters;
        }

        public string ReplaceCodes(string code)
        {
            if (NewType == null || NewType == Type) { return code; }
            return Regex.Replace(code, $@"\b{Type} {ParamenterName}\b", $"{NewType} {ParamenterName}");
        }

        public override string ToString()
        {
            if (NewType != null) {
                return $"paramenter: {NewType} {ParamenterName}";
            }
            return $"paramenter: {Type} {ParamenterName}";
        }
    }

    public class ClassMethodVariable
    {
        public string Type { get; set; }
        public string NewType { get; set; }
        public string HiddenType { get; set; }
        public string VariableName { get; set; }

        public static List<ClassMethodVariable> AnalysisCode(string code, List<ClassMethodParamenter> paramenters)
        {
            List<ClassMethodVariable> classMethodVariables = new List<ClassMethodVariable>();
            var texts = code.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            HashSet<string> names = new HashSet<string>();
            names.Add("_");
            foreach (var paramenter in paramenters) {
                names.Add(paramenter.ParamenterName);
            }
            foreach (var text in texts) {
                var m = Regex.Match(text, @"\t*([a-zA-Z_][a-zA-Z0-9_]*) ([a-zA-Z_][a-zA-Z0-9_]*) = ");
                if (m.Success) {
                    if (names.Add(m.Groups[1].Value)) {
                        ClassMethodVariable classMethodVariable = new ClassMethodVariable();
                        classMethodVariable.Type = m.Groups[1].Value;
                        classMethodVariable.VariableName = m.Groups[2].Value;

                        classMethodVariables.Add(classMethodVariable);
                    }
                    continue;
                }
                m = Regex.Match(text, @"\t*([a-zA-Z_][a-zA-Z0-9_]*) = ");
                if (m.Success) {
                    if (names.Add(m.Groups[1].Value)) {
                        ClassMethodVariable classMethodVariable = new ClassMethodVariable();
                        classMethodVariable.VariableName = m.Groups[1].Value;
                        classMethodVariables.Add(classMethodVariable);
                    }
                    continue;
                }

                m = Regex.Match(text, @"\t*\(([^)]+)\) = ");
                if (m.Success) {
                    var str = m.Groups[1].Value;
                    var sp = str.Split(',');
                    foreach (var sp1 in sp) {
                        var s = sp1.Trim();
                        if (names.Add(s)) {
                            ClassMethodVariable classMethodVariable = new ClassMethodVariable();
                            classMethodVariable.VariableName = m.Groups[1].Value;
                            classMethodVariables.Add(classMethodVariable);
                        }
                    }
                    continue;
                }
            }
            return classMethodVariables;
        }

        public string ReplaceCodes(string code)
        {
            return code;
            //if (Type != null && NewType != Type) {
            //    code = Regex.Replace(code, $@"\b{Type} {VariableName}", $"{NewType} {VariableName}");
            //}
            //return code;
        }

        public override string ToString()
        {
            if (NewType != null) {
                return $"variable: {NewType} {VariableName}";
            }
            return $"variable: {Type} {VariableName}";
        }
    }



}
