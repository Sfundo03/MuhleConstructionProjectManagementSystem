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
            System.Diagnostics.Debug.WriteLine("=== STARTUP CONFIGURATION STARTED ===");
            ConfigureAuth(app);

            // Seed default admin user on startup
            SeedAdminUser();
            System.Diagnostics.Debug.WriteLine("=== STARTUP CONFIGURATION COMPLETED ===");
        }

        private void SeedAdminUser()
        {
            System.Diagnostics.Debug.WriteLine("=== SEEDING ADMIN USER STARTED ===");
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    System.Diagnostics.Debug.WriteLine("Database context created.");

                    var userStore = new UserStore<ApplicationUser>(context);
                    var userManager = new ApplicationUserManager(userStore);

                    System.Diagnostics.Debug.WriteLine("Checking if admin user exists...");

                    // Check if admin user already exists
                    var adminUser = userManager.FindByEmail("admin@gmail.com");
                    if (adminUser == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Admin user not found. Creating new admin user...");

                        var user = new ApplicationUser
                        {
                            UserName = "admin@gmail.com",
                            Email = "admin@gmail.com",
                            EmailConfirmed = true
                        };

                        var result = userManager.Create(user, "Admin123!");
                        if (result.Succeeded)
                        {
                            System.Diagnostics.Debug.WriteLine("✓✓✓ ADMIN USER CREATED SUCCESSFULLY!");
                            System.Diagnostics.Debug.WriteLine($"Email: admin@gmail.com");
                            System.Diagnostics.Debug.WriteLine($"Password: Admin123!");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("✗✗✗ FAILED to create admin user:");
                            foreach (var error in result.Errors)
                            {
                                System.Diagnostics.Debug.WriteLine($"  - {error}");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("✓ Admin user already exists.");
                        System.Diagnostics.Debug.WriteLine($"Admin User ID: {adminUser.Id}");
                        System.Diagnostics.Debug.WriteLine($"Admin Email: {adminUser.Email}");
                        System.Diagnostics.Debug.WriteLine($"Email Confirmed: {adminUser.EmailConfirmed}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗✗✗ ERROR seeding admin user: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            System.Diagnostics.Debug.WriteLine("=== SEEDING ADMIN USER COMPLETED ===");
        }

        private void ConfigureAuth(IAppBuilder app)
        {
            System.Diagnostics.Debug.WriteLine("=== CONFIGURING AUTHENTICATION ===");

            // Configure the db context, user manager and signin manager to use a single instance per request
            app.CreatePerOwinContext<ApplicationDbContext>(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
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

            System.Diagnostics.Debug.WriteLine("=== AUTHENTICATION CONFIGURED ===");
        }
    }
}