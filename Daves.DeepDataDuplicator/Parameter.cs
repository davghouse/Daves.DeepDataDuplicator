namespace Daves.DeepDataDuplicator
{
    public class Parameter
    {
        public Parameter(string name, string dataTypeName)
        {
            Name = name.StartsWith("@") ? name : $"@{name}";
            DataTypeName = dataTypeName;
        }

        public string Name { get; }
        public string DataTypeName { get; }
    }
}
