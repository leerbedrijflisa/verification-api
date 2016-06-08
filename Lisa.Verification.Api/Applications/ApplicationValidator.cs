using Lisa.Common.WebApi;

namespace Lisa.Verification.Api
{
    public class ApplicationValidator : Validator
    {
        protected override void ValidateModel()
        {
            Required("name", NotEmpty, TypeOf(DataTypes.String));
            Required("secret", NotEmpty, TypeOf(DataTypes.String));
            Optional("comment", TypeOf(DataTypes.String));
        }

        protected override void ValidatePatch()
        {
        }
    }
}