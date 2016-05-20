using Lisa.Common.WebApi;
using Microsoft.AspNet.Mvc;
using Microsoft.Net.Http.Server;
using System;
using System.Collections.Generic;
using System.Net;
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
                return new HttpNotFoundResult();

            return new HttpOkObjectResult(models);
        }

        [HttpGet("{guid:guid}", Name = "getSingle")]
        public async Task<IActionResult> Get(Guid guid)
        {
            DynamicModel model = await _db.Fetch(guid.ToString());

            if (model == null)
                return new HttpNotFoundResult();

            return new HttpOkObjectResult(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DynamicModel verification)
        {
            if (verification == null)
                return new BadRequestResult();

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

            dynamic model = await _db.Fetch(guid.ToString());
            model.Signed = DateTime.Now;

            if (model == null)
                return new HttpNotFoundResult();

            if (model.Status != "pending" ||
                DateTime.Compare(model.Expires.ToUniversalTime(), model.Signed.ToUniversalTime()) < 0)
                return new HttpStatusCodeResult(403);

            ValidationResult validationResult = _validator.Validate(patches, model);
            if (validationResult.HasErrors)
                return new UnprocessableEntityObjectResult(validationResult.Errors);

            _modelPatcher.Apply(patches, model);

            model = await _db.Patch(model);

            return new HttpOkObjectResult(model);
        }

        private Database _db;
        private ModelPatcher _modelPatcher;
        private VerificationValidator _validator;
    }
}