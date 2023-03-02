using System.Reflection;

namespace TorchCs
{
    public class TorchSharpInfo
    {
        //public TorchSharpMethodList TorchSharpMethods;
        public Type nnType;
        public MethodInfo[] nnMethods;
        public List<string> nnModelNames;

        public Type torchType;
        public MethodInfo[] torchMethods;

        public Type TensorType;

        public MethodInfo[] TensorMethods;
        public string TensorFieldRegex;
        public string TensorMethodRegex;

        public static TorchSharpInfo Instance=new TorchSharpInfo();

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
        }
    }

    //public class TorchSharpMethodList : List<TorchSharpMethod>
    //{
    //    private static TorchSharpMethodList _TorchSharpMethods;
    //    private static TorchSharpMethodList GetTorchSharpMethods()
    //    {
    //        if (_TorchSharpMethods == null) {

    //        }
    //        return _TorchSharpMethods;
    //    }
    //    public TorchSharpMethod GetMethod(string methodName, List<string> paramenters)
    //    {
    //        return null;
    //    }

    //}

    //public class TorchSharpMethod
    //{
    //    public string MethodName { get; set; }
    //    public string TypeName { get; set; }
    //    public List<MethodParamenter> Paramenters { get; set; }
    //    public string ReplaceCodes(string code)
    //    {

    //        return code;
    //    }
    //}

    //public class MethodParamenter
    //{
    //    public int Index { get; set; }
    //    public string Name { get; set; }
    //    public string TypeName { get; set; }
    //    public bool IsOptional { get; set; }

    //    public string ReplaceCodes(string code)
    //    {

    //        return code;
    //    }
    //}

}
