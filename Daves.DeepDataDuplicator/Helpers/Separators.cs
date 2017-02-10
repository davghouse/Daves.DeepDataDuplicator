using System;

namespace Daves.DeepDataDuplicator.Helpers
{
    public static class Separators
    {
        public static readonly string Nlw4 = $"{Environment.NewLine}    ";
        public static readonly string Nlw8 = $"{Environment.NewLine}        ";
        public static readonly string Nlw12 = $"{Environment.NewLine}            ";
        public static readonly string Nlw16 = $"{Environment.NewLine}                ";
        public static readonly string Cnlw4 = $",{Nlw4}";
        public static readonly string Cnlw8 = $",{Nlw8}";
        public static readonly string Cnlw12 = $",{Nlw12}";
        public static readonly string Cnlw16 = $",{Nlw16}";
    }
}
