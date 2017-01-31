namespace Daves.DeepDataDuplicator
{
    public class Parameter
    {
        // TODO: Improve metadata in order to make dataTypeName an optional parameter.
        public Parameter(string name, string dataTypeName)
        {
            Name = ValidateName(name);
            DataTypeName = dataTypeName;
        }

        public string Name { get; }
        public string DataTypeName { get; }

        // '@' is considered part of the name: https://technet.microsoft.com/en-us/library/ms177436(v=sql.105).aspx.
        public static string ValidateName(string parameterName)
            => parameterName.StartsWith("@") ? parameterName : $"@{parameterName}";
    }
}
