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
            SeedRolesAndAdmin();
        }

        private void SeedRolesAndAdmin()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var userStore = new UserStore<ApplicationUser>(context);
                    var userManager = new ApplicationUserManager(userStore);
                    var roleStore = new RoleStore<IdentityRole>(context);
                    var roleManager = new RoleManager<IdentityRole>(roleStore);

                    // Create all roles
                    string[] roleNames = {
                        "Admin",
                        "Candidate",
                        "Crew Member",
                        "Project Manager",
                        "Finance Officer",
                        "Site Manager"
                    };

                    foreach (var roleName in roleNames)
                    {
                        if (!roleManager.RoleExists(roleName))
                        {
                            var role = new IdentityRole(roleName);
                            roleManager.Create(role);
                            System.Diagnostics.Debug.WriteLine($"Created role: {roleName}");
                        }
                    }

                    // Create Admin user
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
                            userManager.AddToRole(user.Id, "Admin");
                            System.Diagnostics.Debug.WriteLine("Admin user created successfully.");
                        }
                        else
                        {
                            foreach (var error in result.Errors)
                            {
                                System.Diagnostics.Debug.WriteLine($"Admin creation error: {error}");
                            }
                        }
                    }
                    else
                    {
                        if (!userManager.IsInRole(adminUser.Id, "Admin"))
                        {
                            userManager.AddToRole(adminUser.Id, "Admin");
                            System.Diagnostics.Debug.WriteLine("Admin role assigned to existing admin user.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error seeding roles and admin: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
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