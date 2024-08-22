namespace AXERP.API.Domain.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class GridPropsAttribute : Attribute
    {
        public int Order { get; set; }

        public int MinWidth { get; set; }

        public int MaxWidth { get; set; }

        public GridPropsAttribute(int order = 0, int minWidth = int.MinValue, int maxWidth = int.MaxValue)
        {
            Order = order;
            MinWidth = minWidth;
            MaxWidth = maxWidth;
        }
    }
}
