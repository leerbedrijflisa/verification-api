using Microsoft.WindowsAzure.Storage.Table;

namespace Lisa.Verification.AdminPanel
{
    public class ApplicationEntity : TableEntity
    {
        public ApplicationEntity() { }
        public ApplicationEntity(string name, string secret, string comment = "")
        {
            PartitionKey = name;
            RowKey = name;

            Name = name;
            Secret = secret;
            Comment = comment;
        }

        public string Name { get; set; }
        public string Secret { get; set; }
        public string Comment { get; set; }
    }
}