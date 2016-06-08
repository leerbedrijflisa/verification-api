using Lisa.Common.WebApi;
using System;

namespace Lisa.Verification.Api
{
    public class VerificationValidator : Validator
    {
        protected override void ValidateModel()
        {
            Required("document", NotEmpty, TypeOf(DataTypes.String));
            Required("application", NotEmpty, TypeOf(DataTypes.String));
            Required("user", NotEmpty, TypeOf(DataTypes.String));
            Optional("status", OneOf("pending", "accepted", "rejected"));
            Optional("expires", NotExpired);

            Ignore("id");
            Ignore("signed");
        }

        protected override void ValidatePatch()
        {
            Allow("status");
        }

        private void NotExpired(string fieldName, object value)
        {
            if (value is DateTime)
            {
                DateTime expires = (DateTime)value;
                if (DateTime.Compare(expires.ToUniversalTime(), DateTime.Now.ToUniversalTime()) < 0)
                {
                    var error = new Error
                    {
                        Code = 1,
                        Message = $"Expiration date has already been passed: {expires}",
                        Values = new
                        {
                            Field = fieldName,
                        }
                    };
                    Result.Errors.Add(error);
                }
            }
        }
    }
}