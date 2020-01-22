namespace Pytocs.Core.TypeInference
{
    public enum BindingKind
    {
        ATTRIBUTE, // attr accessed with "." on some other object
        CLASS, // class definition
        CONSTRUCTOR, // __init__ functions in classes
        FUNCTION, // plain function
        METHOD, // static or instance method
        MODULE, // file
        PARAMETER, // function param
        SCOPE, // top-level variable ("scope" means we assume it can have attrs)
        VARIABLE // local variable
    }
}