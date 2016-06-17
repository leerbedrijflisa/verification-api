using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Security;

namespace Lisa.Verification.AdminPanel.Controllers
{
    public class LoginController : Controller
    {
        public LoginController()
        {
            _db = new Database();
        }

        public ActionResult Index(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View("Index");
        }

        public async Task<ActionResult> Login(UserEntity user, string returnUrl)
        {
            if (await IsValid(user.UserName, user.Password))
            { 
                FormsAuthentication.SetAuthCookie(user.UserName, false);
                return Redirect("/Application/Index");
            }

            ModelState.AddModelError("", "The user name or password provided is incorrect.");
            return View("Index", user);
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Abandon();

            return RedirectToAction("index");
        }

        public async Task<bool> IsValid(string name, string password)
        {
            if (name == "" || password == "")
                return false;

            string hashedPassword = sha256(password);

            UserEntity user = await _db.RetrieveUser(name, hashedPassword);
            bool valid = true;

            if (user == null)
                valid = false;

            return valid;
        }

        public string sha256(string password)
        {
            SHA256Managed crypt = new SHA256Managed();
            StringBuilder hash = new StringBuilder();

            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] crypto = crypt.ComputeHash(bytes, 0, Encoding.UTF8.GetByteCount(password));


            return Convert.ToBase64String(crypto);
    }

        private Database _db;
    }
}