using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Lisa.Verification.AdminPanel.Controllers
{
    [CustomAuthorize]
    [Route("")]
    public class ApplicationController : Controller
    {
        public ApplicationController()
        {
            _db = new Database();
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            return await Home();
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View(new ApplicationEntity());
        }

        [HttpPost]
        public async Task<ActionResult> Save(FormCollection collection)
        {
            string nameVal = collection[0];
            string commentVal = collection[1];

            dynamic app = new ApplicationEntity(nameVal, GenerateSecret(), commentVal);

            var appExist = await _db.Retrieve(nameVal);
            if (appExist != null)
            {
                ModelState.AddModelError("", "The application '"+nameVal+"' already exists.");
                return View("Create", (ApplicationEntity)app);
            }

            app = await _db.Insert(app);

            return await Home();
        }

        [HttpGet]
        public async Task<ActionResult> Delete(string name)
        {
            ApplicationEntity app = await _db.Retrieve(name);

            if (app == null)
                return await Home();

            await _db.Delete(app);

            var apps = await _db.RetrieveAll();

            return View("Index", apps);
        }

        [HttpGet]
        public async Task<ActionResult> UpdateSecret(string name)
        {
            ApplicationEntity app = await _db.Retrieve(name);

            if (app == null)
                return await Home();

            app.Secret = GenerateSecret();

            app = (await _db.Replace(app));

            return await Home();
        }

        public string GenerateSecret()
        {
            int size = 8;
            byte[] data = new byte[size];
            RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
            crypto.GetBytes(data);

            string secret = BitConverter.ToString(data).Replace("-", "").ToLower();

            secret = Regex.Replace(secret, ".{"+ (size*2/4) + "}", "$0-").Trim('-');

            return secret;
        }


        // views
        private async Task<ActionResult> Home()
        {
            return View("Index", await _db.RetrieveAll());
        }

        private Database _db;
    }
}