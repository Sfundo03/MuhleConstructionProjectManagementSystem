using System.Web.Mvc;

namespace MuhleOP.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard");
            }
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        public ActionResult Dashboard()
        {
            // Both Admin and Candidate can access this dashboard
            // The view will handle role-based content display
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }
    }
}