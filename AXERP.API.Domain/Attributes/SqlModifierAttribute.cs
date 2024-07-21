namespace AXERP.API.Domain.Attributes
{
    public enum SqlModifiers
    {
        None,

        /// <summary>
        /// Numerals having string as pre/sufix: 123b, b23432...
        /// </summary>
        StringNumeral
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SqlModifierAttribute : Attribute
    {
        public SqlModifiers Modifier;

        public SqlModifierAttribute(SqlModifiers modifier) { Modifier = modifier; }
    }
}
