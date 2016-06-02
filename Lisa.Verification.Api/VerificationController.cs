using Lisa.Common.WebApi;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
            DynamicModel verification = await _db.Fetch(guid.ToString());
             
            if (!CompareTokens(await GetSecret(verification), guid.ToString()))
                return new UnauthorizedResult();

            if (verification == null)
                return new NotFoundResult();

            return new OkObjectResult(verification);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DynamicModel verification)
        {
            if (verification == null)
                return new BadRequestResult();

            if (!CompareTokens(await GetSecret(verification), Newtonsoft.Json.JsonConvert.SerializeObject(verification)))
                return new UnauthorizedResult();
            
            string guid = Guid.NewGuid().ToString();

            verification.SetMetadata(new { PartitionKey = guid, RowKey = guid });
            ((dynamic)verification).id = guid;
            ((dynamic)verification).status = "pending";
            if (!(((dynamic)verification).expires is DateTime) && ((dynamic)verification).expires == "")
                ((dynamic)verification).expires = DateTime.MaxValue;

            // expire date has already been passed, no point in storing the verification
            DateTime t1 = ((dynamic)verification).expires;
            if (DateTime.Compare(t1.ToUniversalTime(), DateTime.Now.ToUniversalTime()) < 0)
                return new UnprocessableEntityObjectResult(t1);

            // new applications stuff
            string appName = ((dynamic)verification).application;
            DynamicModel app = await _db.FetchApplication(appName);
            if (app == null)
                await _db.PostApplication(appName);

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
            verification.Signed = DateTime.Now;

            if (!CompareTokens(await GetSecret(verification), Newtonsoft.Json.JsonConvert.SerializeObject(patches)))
                return new UnauthorizedResult();

            if (verification == null)
                return new NotFoundResult();

            if (verification.Status != "pending" ||
                DateTime.Compare(verification.Expires.ToUniversalTime(), verification.Signed.ToUniversalTime()) < 0)
                return new StatusCodeResult(403);

            ValidationResult validationResult = _validator.Validate(patches, verification);
            if (validationResult.HasErrors)
                return new UnprocessableEntityObjectResult(validationResult.Errors);

            _modelPatcher.Apply(patches, verification);

            verification = await _db.Patch(verification);

            return new OkObjectResult(verification);
        }


        public async Task<string> GetSecret(dynamic verification)
        {
            dynamic app = await _db.FetchApplication(((dynamic)verification).application);
            return app.Secret;
        }

        public bool CompareTokens(string secret, string body)
        {
            // use the secret as key when instantiating an HMACSHA256 object
            byte[] key = System.Text.Encoding.ASCII.GetBytes(secret);
            hmac = new HMACSHA256(key);

            string storedHash = Request.Headers["Authorization"];
            string computedHash = ComputeHash(body, secret);

            return computedHash == storedHash;
        }

        private static string ComputeHash(string body, string secret)
        {
            //string bodySecret = Base64Encode(body) + secret;
            //byte[] bodySecretBytes = System.Text.Encoding.ASCII.GetBytes(bodySecret);
            //byte[] hash = hmac.ComputeHash(bodySecretBytes);
            //string hashString = System.Text.Encoding.ASCII.GetString(hash);
            //string hashed = Base64Encode(hashString);
            //return hashed;

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