using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using MuhleOP.Models;

namespace MuhleOP.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            System.Diagnostics.Debug.WriteLine("=== LOGIN PAGE LOADED ===");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            System.Diagnostics.Debug.WriteLine("=== LOGIN ATTEMPT STARTED ===");
            System.Diagnostics.Debug.WriteLine($"Email: {model.Email}");
            System.Diagnostics.Debug.WriteLine($"RememberMe: {model.RememberMe}");

            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("Model State is Invalid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Model Error: {error.ErrorMessage}");
                }
                return View(model);
            }

            try
            {
                // First, check if the user exists
                var user = await UserManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine($"User not found with email: {model.Email}");
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"User found: {user.UserName}, ID: {user.Id}");
                    System.Diagnostics.Debug.WriteLine($"Email confirmed: {user.EmailConfirmed}");
                }

                // Try to sign in
                System.Diagnostics.Debug.WriteLine("Attempting password sign in...");
                var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);

                System.Diagnostics.Debug.WriteLine($"Sign-in result: {result}");

                switch (result)
                {
                    case SignInStatus.Success:
                        System.Diagnostics.Debug.WriteLine("=== LOGIN SUCCESSFUL ===");
                        return RedirectToLocal(returnUrl);
                    case SignInStatus.LockedOut:
                        System.Diagnostics.Debug.WriteLine("Account is locked out");
                        return View("Lockout");
                    case SignInStatus.RequiresVerification:
                        System.Diagnostics.Debug.WriteLine("Requires verification");
                        return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                    case SignInStatus.Failure:
                    default:
                        System.Diagnostics.Debug.WriteLine("LOGIN FAILED - Invalid credentials");
                        ModelState.AddModelError("", "Invalid login attempt.");
                        return View(model);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EXCEPTION during login: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            System.Diagnostics.Debug.WriteLine("=== LOGGING OUT ===");
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Dashboard", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}