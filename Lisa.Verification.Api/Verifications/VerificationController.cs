using Lisa.Common.WebApi;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var verifications = await _db.FetchAll();

            return new OkObjectResult(verifications);
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
        public async Task<IActionResult> Post()
        {
            // get the raw body stream
            string rawBody = new StreamReader(Request.Body).ReadToEnd();

            // Deserialize the raw body to a dynamic verification
            dynamic verification = Newtonsoft.Json.JsonConvert.DeserializeObject<DynamicModel>(rawBody);

            if (verification == null)
                return new BadRequestResult();

            if (verification.application == null)
                return new UnauthorizedResult();

            if (!CompareTokens(await GetSecret(verification.application), rawBody, Request.Headers["Authorization"]))
                return new UnauthorizedResult();
            
            string guid = Guid.NewGuid().ToString();
            verification.SetMetadata(new { PartitionKey = guid, RowKey = guid });

            verification.id = guid;
            verification.status = "pending";
            if (!(verification.expires is DateTime) && verification.expires == "")
                verification.expires = DateTime.MaxValue;

            var validationResult = _validator.Validate(verification);
            if (validationResult.HasErrors)
                return new UnprocessableEntityObjectResult(validationResult.Errors);

            dynamic result = await _db.Post(verification);

            string location = Url.RouteUrl("getSingle", new { guid = result.GetMetadata().RowKey }, Request.Scheme);
            return new CreatedResult(location, result);
        }

        [HttpPatch("{guid:guid}")]
        public async Task<IActionResult> Sign(Guid guid)
        {
            // get the raw body stream
            string rawBody = new StreamReader(Request.Body).ReadToEnd();

            // Deserialize the raw body to a Patch array
            Patch[] patches = Newtonsoft.Json.JsonConvert.DeserializeObject<Patch[]>(rawBody);


            if (patches == null)
                return new BadRequestResult();

            dynamic verification = await _db.Fetch(guid.ToString());

            if (verification == null)
                return new NotFoundResult();

            verification.Signed = DateTime.Now;

            if (!CompareTokens(await GetSecret(verification.application), rawBody, Request.Headers["Authorization"]))
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
            return app.secret;
        }

        public bool CompareTokens(string secret, string body, string storedHash)
        {
            if (secret == null)
                return false;

            string computedHash = ComputeHash(body, secret);

            // compare the stored hash (the has the user sends with the header) to the newly computed hash. 
            // if they dont match it means that OR the user has send the wrong hash (hashed incorectly or wrong secret)
            // OR someone changed data in the body
            return computedHash == storedHash;
        }

        private string ComputeHash(string body, string secret)
        {
            // use the secret as key when instantiating a new HMACSHA256 object
            byte[] key = System.Text.Encoding.ASCII.GetBytes(secret);
            _hmac = new HMACSHA256(key);

            // formula = base64(hmacsha256(body))
            return Convert.ToBase64String(_hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(body)));
        }

        private HMACSHA256 _hmac;
        private Database _db;
        private ModelPatcher _modelPatcher;
        private VerificationValidator _validator;
    }
}