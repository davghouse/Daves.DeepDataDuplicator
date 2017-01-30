using System;

namespace Daves.DeepDataDuplicator.Helpers
{
    public static class Separators
    {
        public static string Nlw4 => $"{Environment.NewLine}    ";
        public static string Nlw8 => $"{Environment.NewLine}        ";
        public static string Nlw12 => $"{Environment.NewLine}            ";
        public static string Nlw16 => $"{Environment.NewLine}                ";
        public static string Cnlw4 => $",{Nlw4}";
        public static string Cnlw8 => $",{Nlw8}";
        public static string Cnlw12 => $",{Nlw12}";
        public static string Cnlw16 => $",{Nlw16}";
    }
}
