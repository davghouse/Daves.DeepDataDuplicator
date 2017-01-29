using System.Linq;

namespace Daves.DankDataDuplicator.Helpers
{
    internal static class NameExtensions
    {
        internal static string ToLowercaseName(this string name)
            => name.ToUpper() == name ? name.ToLower()
            : char.ToLower(name[0]) + name.Substring(1);

        internal static string ToSingularName(this string name)
            => name.EndsWith("ss") ? name
            : name.EndsWith("s") ? name.Substring(0, name.Length - 1)
            : name;

        internal static string ToSpacelessName(this string name)
            => new string(name
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());

        internal static string ToLowercaseSpacelessName(this string name)
            => name.ToLowercaseName().ToSpacelessName();

        internal static string ToSingularSpacelessName(this string name)
            => name.ToSingularName().ToSpacelessName();
    }
}
