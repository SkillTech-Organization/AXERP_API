namespace AXERP.API.LogHelper.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class SystemAttribute : Attribute
    {
        public readonly string SystemName;

        public SystemAttribute(string systemName)
        {
            this.SystemName = systemName;
        }

        public string PositionalString
        {
            get { return SystemName; }
        }

        public string ParentSystemName { get; set; }
    }
}
