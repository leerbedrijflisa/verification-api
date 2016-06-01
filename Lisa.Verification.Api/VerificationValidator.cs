using Lisa.Common.WebApi;

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
            Optional("expires");

            Ignore("id");
            Ignore("signed");
        }

        protected override void ValidatePatch()
        {
            Allow("status");
        }
    }
}