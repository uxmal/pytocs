using System.Reflection;
using System.Text.RegularExpressions;
using TorchSharp;

namespace TorchCs
{
    public class TorchSharpInfo
    {
        private Dictionary<string, string> dict=new Dictionary<string, string>() {
            {"Int64","long" },
            {"Int32","int" },
            {"String","string" },
            {"Single","float" },
            {"Double","double" },
        };
        public Type nnType;
        public MethodInfo[] nnMethods;
        public List<string> nnModelNames;

        public Type torchType;
        public MethodInfo[] torchMethods;

        public Type TensorType;

        public MethodInfo[] TensorMethods;
        public string TensorFieldRegex;
        public string TensorMethodRegex;

        private TorchSharpMethodList nn_methods;
        private TorchSharpMethodList torch_methods;

        public static TorchSharpInfo Instance = new TorchSharpInfo();

        private TorchSharpInfo()
        {
            nnType = typeof(TorchSharp.torch.nn);
            nnMethods = nnType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            nnModelNames = new List<string>();
            foreach (var method in nnMethods) {
                if (method.Name == "ModuleDict" || method.Name == "ModuleList") { continue; }
                nnModelNames.Add(method.ReturnType.Name);
            }
            nnModelNames = nnModelNames.Distinct().ToList();

            TensorType = typeof(TorchSharp.torch.Tensor);
            var fields = TensorType.GetFields();
            var properties = TensorType.GetProperties();
            HashSet<string> fs = new HashSet<string>();
            foreach (var fieldInfo in fields) { fs.Add(fieldInfo.Name); }
            foreach (var fieldInfo in properties) { fs.Add(fieldInfo.Name); }
            fs.Remove("device");
            TensorFieldRegex = "(" + string.Join("|", fs) + ")";
            TensorMethods = TensorType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            fs.Clear();
            foreach (var fieldInfo in TensorMethods) { fs.Add(fieldInfo.Name); }
            TensorMethodRegex = "(" + string.Join("|", fs) + ")";

            var torchType = typeof(TorchSharp.torch);
            torchMethods = torchType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            nn_methods = new TorchSharpMethodList(nnMethods);
            torch_methods = new TorchSharpMethodList(torchMethods);
        }

        public string FindTypeBy_nn(string code, string text)
        {
            var names = nn_methods.Select(q => q.MethodName).Distinct().ToList();
            string reg = $@"\bnn\.({string.Join("|", names)})\((((?<BR>\()|(?<-BR>\))|[^()])+)\)";
            var ms = Regex.Matches(code, reg);
            foreach (Match m in ms) {
                if (m.Value.Contains(text) == false) { continue; }
                var p = m.Groups[2].Value;
                var type = FindTypeBy_nn(p, text);
                if (type != null) { return type; }
                var ps = TorchUtil.splitParamenters(p.Trim());
                if (ps.Contains(text) == false) { continue; }

                var methodName = m.Groups[1].Value;
                var methods = nn_methods.Where(q => q.MethodName == methodName).ToList();
                foreach (var method in methods) {
                    if (method.Check(ps)) {
                        var index = ps.IndexOf(text);
                        var pi = method.Paramenters[index];
                        if (pi.IsGenericType == false) {
                            if (dict.TryGetValue(pi.TypeName,out string t)) {
                                return t;
                            }
                            return pi.TypeName;
                        }
                    }
                }
            }
            return null;
        }
        public string FindTypeBy_torch(string code, string text)
        {
            var names = torch_methods.Select(q => q.MethodName).Distinct().ToList();
            string reg = $@"\btorch\.({string.Join("|", names)})\((((?<BR>\()|(?<-BR>\))|[^()])+)\)";
            var ms = Regex.Matches(code, reg);
            foreach (Match m in ms) {
                if (m.Value.Contains(text) == false) { continue; }
                var p = m.Groups[2].Value;
                var type = FindTypeBy_nn(p, text);
                if (type != null) { return type; }
                var ps = TorchUtil.splitParamenters(p.Trim());
                if (ps.Contains(text) == false) { continue; }

                var methodName = m.Groups[1].Value;
                var methods = torch_methods.Where(q => q.MethodName == methodName).ToList();
                foreach (var method in methods) {
                    if (method.Check(ps)) {
                        var index = ps.IndexOf(text);
                        var pi = method.Paramenters[index];
                        if (pi.IsGenericType == false) {
                            if (dict.TryGetValue(pi.TypeName, out string t)) {
                                return t;
                            }
                            return pi.TypeName;
                        }
                    }
                }
            }
            return null;
        }



    }

    public class TorchSharpMethodList : List<TorchSharpMethod>
    {
        public TorchSharpMethodList(MethodInfo[] methods)
        {
            foreach (var method in methods) {
                Add(new TorchSharpMethod(method));
            }
        }
    }

    public class TorchSharpMethod
    {
        public string MethodName { get; set; }
        public string ReturnType { get; set; }
        public List<MethodParamenter> Paramenters { get; set; }

        public TorchSharpMethod() { }
        public TorchSharpMethod(MethodInfo methodInfo)
        {
            MethodName = methodInfo.Name;
            ReturnType = methodInfo.ReturnType.Name;
            Paramenters = new List<MethodParamenter>();
            var ps = methodInfo.GetParameters();
            for (int i = 0; i < ps.Length; i++) {
                Paramenters.Add(new MethodParamenter(i, ps[i]));
            }
        }
        public bool Check(List<string> ps)
        {
            if (Paramenters.Count < ps.Count) { return false; }
            foreach (var p in ps) {
                if (p.Contains(":")) {
                    var name = p.Substring(0, p.IndexOf(':'));
                    if (Paramenters.Any(q => q.Name == name) == false) {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    public class MethodParamenter
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public bool IsGenericType { get; set; }
        public bool IsOptional { get; set; }

        public MethodParamenter() { }
        public MethodParamenter(int index, ParameterInfo parameter)
        {
            Index = index;
            Name = parameter.Name;
            TypeName = parameter.ParameterType.Name;
            IsOptional = parameter.IsOptional;
            IsGenericType = parameter.ParameterType.IsGenericType;
        }
    }

}
