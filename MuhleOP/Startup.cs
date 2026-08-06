using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using MuhleOP.Models;
using Owin;
using System;

[assembly: OwinStartup(typeof(MuhleOP.Startup))]

namespace MuhleOP
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            SeedAdminUserAndRole();
        }

        private void SeedAdminUserAndRole()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var userStore = new UserStore<ApplicationUser>(context);
                    var userManager = new ApplicationUserManager(userStore);
                    var roleStore = new RoleStore<IdentityRole>(context);
                    var roleManager = new RoleManager<IdentityRole>(roleStore);

                    // Create Admin role if it doesn't exist
                    if (!roleManager.RoleExists("Admin"))
                    {
                        var role = new IdentityRole("Admin");
                        roleManager.Create(role);
                    }

                    // Create Admin user if it doesn't exist
                    var adminUser = userManager.FindByEmail("admin@gmail.com");
                    if (adminUser == null)
                    {
                        var user = new ApplicationUser
                        {
                            UserName = "admin@gmail.com",
                            Email = "admin@gmail.com",
                            EmailConfirmed = true
                        };

                        var result = userManager.Create(user, "Admin123!");
                        if (result.Succeeded)
                        {
                            // Assign Admin role to the user
                            userManager.AddToRole(user.Id, "Admin");
                        }
                    }
                    else
                    {
                        // If user exists but doesn't have Admin role, add it
                        if (!userManager.IsInRole(adminUser.Id, "Admin"))
                        {
                            userManager.AddToRole(adminUser.Id, "Admin");
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Handle or log exception if needed
            }
        }

        private void ConfigureAuth(IAppBuilder app)
        {
            app.CreatePerOwinContext<ApplicationDbContext>(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });

            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);
        }
    }
}