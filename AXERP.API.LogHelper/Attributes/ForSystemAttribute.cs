namespace AXERP.API.LogHelper.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ForSystemAttribute : Attribute
    {
        public readonly string SystemName;
        public readonly string DefaultFunctionName;

        public ForSystemAttribute(string systemName, string defaultFunctionName)
        {
            this.SystemName = systemName;
            this.DefaultFunctionName = defaultFunctionName;
        }
    }
}
