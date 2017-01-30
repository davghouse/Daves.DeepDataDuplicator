using System.Linq;

namespace Daves.DankDataDuplicator.Helpers
{
    public static class NameExtensions
    {
        public static string ToLowercaseName(this string name)
            => name.ToUpper() == name ? name.ToLower()
            : char.ToLower(name[0]) + name.Substring(1);

        public static string ToSingularName(this string name)
            => name.EndsWith("ss") ? name
            : name.EndsWith("s") ? name.Substring(0, name.Length - 1)
            : name;

        public static string ToSpacelessName(this string name)
            => new string(name
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());

        public static string ToLowercaseSpacelessName(this string name)
            => name.ToLowercaseName().ToSpacelessName();

        public static string ToSingularSpacelessName(this string name)
            => name.ToSingularName().ToSpacelessName();
    }
}
