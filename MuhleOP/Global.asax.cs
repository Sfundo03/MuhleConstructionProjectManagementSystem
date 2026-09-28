using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using MuhleOP.Models;
using MuhleOP.Helpers;

namespace MuhleOP
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            try
            {
                InspectionTemplateSeeder.Seed();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InspectionTemplateSeeder skipped: {ex.Message}");
            }
        }
    }
}