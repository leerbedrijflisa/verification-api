using Lisa.Common.WebApi;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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

            if (model == null)
                return new NotFoundResult();

            return new OkObjectResult(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DynamicModel verification)
        {
            if (verification == null)
                return new BadRequestResult();

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

        private Database _db;
        private ModelPatcher _modelPatcher;
        private VerificationValidator _validator;
    }
}