namespace SvgOutputSample.Models
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class SvgTextAnchorAttribute : Attribute
    {
        public const string End = "end";
        public const string Middle = "middle";
        public string TextAnchor { get; }

        public SvgTextAnchorAttribute(string textAnchor)
        {
            this.TextAnchor = textAnchor;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class SvgLengthAttribute : Attribute
    {
        public int Length { get; }

        public SvgLengthAttribute(int length)
        {
            this.Length = length;
        }
    }
}
