using Lisa.Common.TableStorage;
using Lisa.Common.WebApi;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Lisa.Verification.Api
{
    public class ApplicationMapper
    {
        public static ITableEntity ToEntity(dynamic model)
        {
            if (model == null)
            {
                throw new ArgumentNullException("Model");
            }

            dynamic entity = new DynamicEntity();

            entity.PartitionKey = model.PartitionKey;
            entity.RowKey = model.RowKey;

            entity.name = model.name;
            entity.secret = model.secret;
            entity.comment = model.comment;

            return entity;
        }

        public static DynamicModel ToModel(dynamic entity)
        {
            if (entity == null)
            {
                throw new ArgumentException("Entity");
            }

            dynamic model = new DynamicModel();

            model.SetMetadata(new { PartitionKey = entity.PartitionKey, RowKey = entity.RowKey });

            model.Name = entity.Name;
            model.Secret = entity.Secret;
            model.Comment = entity.Comment;

            return model;
        }
    }
}