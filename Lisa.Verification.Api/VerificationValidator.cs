using Lisa.Common.WebApi;

namespace Lisa.Verification.Api
{
    public class VerificationValidator : Validator
    {
        protected override void ValidateModel()
        {
            Required("Document", NotEmpty, TypeOf(DataTypes.String));
            Required("User", NotEmpty, TypeOf(DataTypes.String));
            Optional("Expires");

            Ignore("Id");
            Ignore("Signed");
        }

        protected override void ValidatePatch()
        {
            Allow("Status");
        }
    }
}