using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Lisa.Verification.AdminPanel
{
    public class CustomAuthorize : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var authenCookie = httpContext.Request.Cookies.Get(FormsAuthentication.FormsCookieName);
            if (authenCookie == null) return false;

            return true;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectResult("/");
        }
    }
}