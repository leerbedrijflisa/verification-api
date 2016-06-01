using Lisa.Common.TableStorage;
using Lisa.Common.WebApi;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Lisa.Verification.Api
{
    public class Database
    {
        public Database(IOptions<TableStorageSettings> settings)
        {
            _settings = settings.Value;

            Connect();
        }

        // general functions
        public void Connect()
        {
            _account = CloudStorageAccount.Parse(_settings.ConnectionString);
            _client = _account.CreateCloudTableClient();
        }

        public async Task<CloudTable> GetTable(string tableName)
        {
            CloudTable table = _client.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();

            return table;
        }


        // verification functions
        public async Task<IEnumerable<DynamicModel>> FetchAll()
        {
            CloudTable table = await GetTable("verifications");

            var query = new TableQuery<DynamicEntity>();
            var unmappedResult = await table.ExecuteQuerySegmentedAsync(query, null);

            var result = unmappedResult.Select(a => VerificationMapper.ToModel(a));

            return result;
        }
        
        public async Task<DynamicModel> Fetch(string id)
        {
            CloudTable table = await GetTable("verifications");

            var query = TableOperation.Retrieve<DynamicEntity>(id, id);
            var result = (await table.ExecuteAsync(query)).Result;

            if (result == null)
                return null;

            return VerificationMapper.ToModel(result);
        }

        public async Task<DynamicModel> Post(dynamic verification)
        {
            CloudTable table = await GetTable("verifications");

            string guid = Guid.NewGuid().ToString();

            verification.SetMetadata(new { PartitionKey = guid, RowKey = guid });
            verification.Id = guid;
            verification.Status = "pending";
            if (!(verification.Expires is DateTime) && verification.Expires == "")
                verification.Expires = DateTime.MaxValue;

            var insertOperation = TableOperation.Insert(VerificationMapper.ToEntity(verification));

            var result = (await table.ExecuteAsync(insertOperation)).Result;

            return VerificationMapper.ToModel(result);
        }

        public async Task<DynamicModel> Patch(dynamic model)
        {
            CloudTable table = await GetTable("verifications");

            var patchOperation = TableOperation.InsertOrReplace(VerificationMapper.ToEntity(model));

            await table.ExecuteAsync(patchOperation);

            return model;
        }


        // application functions
        public async Task<DynamicModel> FetchApplication(string name)
        {
            CloudTable table = await GetTable("applications");

            var query = TableOperation.Retrieve<DynamicEntity>(name, name);
            var result = (await table.ExecuteAsync(query)).Result;

            if (result == null)
                return null;

            return ApplicationMapper.ToModel(result);
        }

        public async Task<DynamicModel> PostApplication(string application)
        {
            CloudTable table = await GetTable("applications");

            dynamic app = new DynamicModel();
            app.PartitionKey = application;
            app.RowKey = application;
            app.Name = application;
            app.Secret = "";

            var insertOperation = TableOperation.Insert(ApplicationMapper.ToEntity(app));

            var result = (await table.ExecuteAsync(insertOperation)).Result;

            return VerificationMapper.ToModel(result);
        }

        private TableStorageSettings _settings;
        private CloudStorageAccount _account;
        private CloudTableClient _client;
    }
}