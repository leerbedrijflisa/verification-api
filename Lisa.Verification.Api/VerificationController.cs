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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            IEnumerable<DynamicModel> models = await _db.FetchAll();

            if (models == null)
                return new NotFoundResult();

            return new OkObjectResult(models);
        }

        [HttpGet("{guid:guid}", Name = "getSingle")]
        public async Task<IActionResult> Get(Guid guid)
        {
            DynamicModel model = await _db.Fetch(guid.ToString());

            DynamicModel app = await _db.FetchApplication(((dynamic)model).Application);

            byte[] key = System.Text.ASCIIEncoding.ASCII.GetBytes(((dynamic)app).Secret);
            hmac = new HMACSHA256(key);

            string storedHash   = Request.Headers["Authorization"];
            string computedHash = ComputeHash(guid.ToString(), ((dynamic)app).Secret);

            if (storedHash != computedHash)
                return new StatusCodeResult(401);

            if (model == null)
                return new NotFoundResult();

            return new OkObjectResult(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DynamicModel verification)
        {
            if (verification == null)
                return new BadRequestResult();

            // expire date has already been passed, no point in storing the verification
            DateTime t1 = ((dynamic)verification).expires;
            if (DateTime.Compare(t1.ToUniversalTime(), DateTime.Now.ToUniversalTime()) < 0)
                return new StatusCodeResult(422);

            // new applications stuff
            string appName = ((dynamic)verification).application;
            DynamicModel app = await _db.FetchApplication(appName);
            if (app == null)
                await _db.PostApplication(appName);
            

            var validationResult = _validator.Validate(verification);
            if (validationResult.HasErrors)
                return new UnprocessableEntityObjectResult(validationResult.Errors) as IActionResult;

            dynamic result = await _db.Post(verification);

            string location = Url.RouteUrl("getSingle", new { guid = result.GetMetadata().RowKey }, Request.Scheme);
            return new CreatedResult(location, result);
        }

        [HttpPatch("{guid:guid}")]
        public async Task<IActionResult> Sign([FromBody] Patch[] patches, Guid guid)
        {
            if (patches == null)
                return new BadRequestResult();

            dynamic model = await _db.Fetch(guid.ToString());
            model.Signed = DateTime.Now;

            if (model == null)
                return new NotFoundResult();

            if (model.Status != "pending" ||
                DateTime.Compare(model.Expires.ToUniversalTime(), model.Signed.ToUniversalTime()) < 0)
                return new StatusCodeResult(403);

            ValidationResult validationResult = _validator.Validate(patches, model);
            if (validationResult.HasErrors)
                return new UnprocessableEntityObjectResult(validationResult.Errors) as IActionResult;

            _modelPatcher.Apply(patches, model);

            model = await _db.Patch(model);

            return new OkObjectResult(model);
        }


        private static string ComputeHash(string body, string secret)
        {
            // Authorization = base64(hmacsha256(base64(body) + secret))
            return Base64Encode(System.Text.Encoding.ASCII.GetString(
                hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(
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