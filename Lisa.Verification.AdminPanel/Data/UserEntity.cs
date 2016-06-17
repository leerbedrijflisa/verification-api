using Microsoft.WindowsAzure.Storage.Table;

namespace Lisa.Verification.AdminPanel
{
    public class UserEntity : TableEntity
    {
        public UserEntity() { }
        public UserEntity(string name = "", string password = "")
        {
            PartitionKey = name;
            RowKey = name;

            UserName = name;
            Password = password;
        }

        public string UserName { get; set; }
        public string Password { get; set; }
    }
}