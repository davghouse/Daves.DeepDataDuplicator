namespace Daves.DeepDataDuplicator
{
    public class Parameter
    {
        public Parameter(string name, string dataTypeDescription)
        {
            Name = ValidateName(name);
            DataTypeDescription = dataTypeDescription;
        }

        public string Name { get; }
        public string DataTypeDescription { get; }

        // '@' is considered part of the name: https://technet.microsoft.com/en-us/library/ms177436(v=sql.105).aspx.
        public static string ValidateName(string parameterName)
            => parameterName == null ? parameterName
            : parameterName.StartsWith("@") ? parameterName
            : $"@{parameterName}";
    }
}
