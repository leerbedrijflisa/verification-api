using Lisa.Common.TableStorage;
using Lisa.Common.WebApi;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Lisa.Verification.Api
{
    public class VerificationMapper
    {
        public static ITableEntity ToEntity(dynamic model)
        {
            if (model == null)
            {
                throw new ArgumentNullException("Model");
            }

            dynamic entity = new DynamicEntity();

            dynamic metadata = model.GetMetadata();
            if (metadata == null)
            {
                string guid = Guid.NewGuid().ToString();

                entity.PartitionKey = guid;
                entity.RowKey = guid;
                entity.Id = guid;
            }
            else
            {
                entity.PartitionKey = metadata.PartitionKey;
                entity.RowKey = metadata.RowKey;
                entity.Id = model.Id;
            }

            entity.Document = model.Document;
            entity.User = model.User;
            entity.Status = model.Status;
            entity.Signed = model.Signed ?? "";
            entity.Expires = model.Expires;

            return entity;
        }

        public static DynamicModel ToModel(dynamic entity)
        {
            if (entity == null)
            {
                throw new ArgumentException("Entity");
            }

            dynamic model = new DynamicModel();

            var metadata = new
            {
                PartitionKey = entity.PartitionKey,
                RowKey = entity.RowKey
            };
            model.SetMetadata(metadata);

            model.Id = entity.Id;
            model.Document = entity.Document;
            model.User = entity.User;
            model.Status = entity.Status;
            model.Signed = entity.Signed ?? "";
            model.Expires = entity.Expires;

            return model;
        }
    }
}