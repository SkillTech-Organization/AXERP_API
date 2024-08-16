namespace AXERP.API.LogHelper.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ForFunctionAttribute : Attribute
    {
        public readonly string FunctionName;

        public ForFunctionAttribute(string functionName)
        {
            this.FunctionName = functionName;
        }
    }
}
