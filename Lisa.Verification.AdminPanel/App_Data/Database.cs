using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lisa.Verification.AdminPanel
{
    public class Database
    {
        public Database()
        {
            _connectionString = "UseDevelopmentStorage=true";

            Connect();
        }

        // general functions
        public void Connect()
        {
            _account = CloudStorageAccount.Parse(_connectionString);
            _client = _account.CreateCloudTableClient();
        }

        public CloudTable GetTable(string tableName)
        {
            return _client.GetTableReference(tableName);
        }

        public async Task<CloudTable> GetTableAsync(string tableName)
        {
            CloudTable table = _client.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();

            return table;
        }


        public async Task<List<ApplicationEntity>> RetrieveAll()
        {
            CloudTable table = await GetTableAsync("applications");

            TableQuery<ApplicationEntity> query = new TableQuery<ApplicationEntity>();

            List<ApplicationEntity> apps = (await table.ExecuteQuerySegmentedAsync(query, null)).Results;

            if (apps == null || apps.Count == 0)
                return null;

            return apps;
        }

        public async Task<ApplicationEntity> Retrieve(string appName)
        {
            if (appName == null)
                return null;

            CloudTable table = await GetTableAsync("applications");

            TableOperation retrieveOperation = TableOperation.Retrieve<ApplicationEntity>(appName, appName);

            ApplicationEntity app = (await table.ExecuteAsync(retrieveOperation)).Result as ApplicationEntity;

            return app;
        }

        public async Task<ApplicationEntity> Insert(ApplicationEntity app)
        {
            if (app == null || (await Retrieve(app.Name)) != null)
                return null;

            CloudTable table = await GetTableAsync("applications");

            var insertOperation = TableOperation.Insert(app);

            ApplicationEntity result = (await table.ExecuteAsync(insertOperation)).Result as ApplicationEntity;

            return result;
        }

        public async Task<ApplicationEntity> Replace(ApplicationEntity app)
        {
            CloudTable table = await GetTableAsync("applications");

            if (app == null || (await Retrieve(app.Name)) == null)
                return null;

            TableOperation updateOperation = TableOperation.Replace(app);

            ApplicationEntity result = (await table.ExecuteAsync(updateOperation)).Result as ApplicationEntity;

            return app;
        }

        public async Task<ApplicationEntity> Delete(ApplicationEntity app)
        {
            CloudTable table = await GetTableAsync("applications");

            if (app == null || (await Retrieve(app.Name)) == null)
                return null;

            TableOperation deleteOperation = TableOperation.Delete(app);

            ApplicationEntity result = (await table.ExecuteAsync(deleteOperation)).Result as ApplicationEntity;

            return result;
        }


        // user stuff
        public async Task<UserEntity> RetrieveUser(string userName, string password)
        {
            CloudTable table = GetTable("users");

            if (userName == "" || password == "")
                return null;

            TableQuery<UserEntity> query = new TableQuery<UserEntity>().Where(
                "(UserName eq '" + userName + "') and (Password eq '" + password + "')");

            UserEntity user = (await table.ExecuteQuerySegmentedAsync(query, null)).Results.SingleOrDefault();

            return user;
        }

        private string _connectionString;
        private CloudStorageAccount _account;
        private CloudTableClient _client;
    }
}