using Lisa.Common.WebApi;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Lisa.Verification.Api
{
    [Route("/verifications/")]
    public class VerificationController : Controller
    {
        public VerificationController(Database database)
        {
            _db = database;
            _modelPatcher = new ModelPatcher();
            _validator = new VerificationValidator();
        }

        [HttpGet("{guid:guid}", Name = "getSingle")]
        public async Task<IActionResult> Get(Guid guid)
        {
            dynamic verification = await _db.Fetch(guid.ToString());
             
            if (verification == null)
                return new NotFoundResult();

            if (!CompareTokens(await GetSecret(verification.application), guid.ToString(), Request.Headers["Authorization"]))
                return new UnauthorizedResult();

            return new OkObjectResult(verification);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DynamicModel verificationModel)
        {
            // cast to a dynamic for easier assigning of fields
            dynamic verification = verificationModel;

            if (verification == null)
                return new BadRequestResult();

            if (verification.application == null)
                return new UnauthorizedResult();

            if (!CompareTokens(await GetSecret(verification.application), Newtonsoft.Json.JsonConvert.SerializeObject(verification), Request.Headers["Authorization"]))
                return new UnauthorizedResult();
            
            string guid = Guid.NewGuid().ToString();
            verification.SetMetadata(new { PartitionKey = guid, RowKey = guid });

            // temporary dynamic object so you dont have to cast the DynamicModel to a dynamic object 
            // each time you want to assign or retrieve a property
            verification.id = guid;
            verification.status = "pending";
            if (!(verification.expires is DateTime) && verification.expires == "")
                verification.expires = DateTime.MaxValue;

            // expire date has already been passed, no point in storing the verification
            DateTime t1 = verification.expires;
            if (DateTime.Compare(t1.ToUniversalTime(), DateTime.Now.ToUniversalTime()) < 0)
                return new UnprocessableEntityObjectResult("Invalid field \'expires\': " + t1);

            string appName = verification.application;
            DynamicModel app = await _db.FetchApplication(appName);
            if (app == null)
                return new UnauthorizedResult();

            var validationResult = _validator.Validate(verification);
            if (validationResult.HasErrors)
                return new UnprocessableEntityObjectResult(validationResult.Errors);

            dynamic result = await _db.Post(verification);

            string location = Url.RouteUrl("getSingle", new { guid = result.GetMetadata().RowKey }, Request.Scheme);
            return new CreatedResult(location, result);
        }

        [HttpPatch("{guid:guid}")]
        public async Task<IActionResult> Sign([FromBody] Patch[] patches, Guid guid)
        {
            if (patches == null)
                return new BadRequestResult();

            dynamic verification = await _db.Fetch(guid.ToString());

            if (verification == null)
                return new NotFoundResult();

            verification.Signed = DateTime.Now;

            if (!CompareTokens(await GetSecret(verification.application), Newtonsoft.Json.JsonConvert.SerializeObject(patches), Request.Headers["Authorization"]))
                return new UnauthorizedResult();

            if (verification.Status != "pending" || DateTime.Compare(verification.Expires.ToUniversalTime(), verification.Signed.ToUniversalTime()) < 0)
                return new ForbidResult();

            ValidationResult validationResult = _validator.Validate(patches, verification);
            if (validationResult.HasErrors)
                return new UnprocessableEntityObjectResult(validationResult.Errors);

            _modelPatcher.Apply(patches, verification);

            verification = await _db.Patch(verification);

            return new OkObjectResult(verification);
        }

        public async Task<string> GetSecret(string applicationName)
        {
            dynamic app = await _db.FetchApplication(applicationName);

            if (app == null)
                return null;
            return app.Secret;
        }

        public bool CompareTokens(string secret, string body, string storedHash)
        {
            if (secret == null)
                return false;

            string computedHash = ComputeHash(body, secret);

            // compare the stored hash (the has the user sends with the header) to the newly computed hash. 
            // if they dont match it means that OR the user has send the wrong hash (wrong secret) 
            // OR someone changed data in the body or secret
            return computedHash == storedHash;
        }

        private static string ComputeHash(string body, string secret)
        {
            // use the secret as key when instantiating an HMACSHA256 object
            byte[] key = System.Text.Encoding.ASCII.GetBytes(secret);
            hmac = new HMACSHA256(key);

            // formula = base64(hmacsha256(base64(body) + secret))
            return Base64Encode(System.Text.Encoding.ASCII.GetString(
                hmac.ComputeHash(System.Text.Encoding.ASCII.GetBytes(
                    Base64Encode(body) + secret))));
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        private static HMACSHA256 hmac;
        private Database _db;
        private ModelPatcher _modelPatcher;
        private VerificationValidator _validator;
    }
}