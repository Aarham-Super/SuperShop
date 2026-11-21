using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SuperShop.Data;
using SuperShop.Models;
using SuperShop.Services;

var builder = WebApplication.CreateBuilder(args);


/* ============================================================
   DATABASE
   ============================================================ */

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection") ??
    builder.Configuration["MYSQL_CONNECTION_STRING"];

// Hosts such as MonsterASP can provide MySQL values separately instead
// of a named ASP.NET connection string. Use those when no full string exists.
if (string.IsNullOrWhiteSpace(connectionString))
{
    var mysqlHost = builder.Configuration["MYSQL_HOST"];
    var mysqlDatabase = builder.Configuration["MYSQL_DATABASE"];
    var mysqlUser = builder.Configuration["MYSQL_USER"];
    var mysqlPassword = builder.Configuration["MYSQL_PASSWORD"];
    if (!string.IsNullOrWhiteSpace(mysqlHost) &&
        !string.IsNullOrWhiteSpace(mysqlDatabase) &&
        !string.IsNullOrWhiteSpace(mysqlUser) &&
        !string.IsNullOrWhiteSpace(mysqlPassword))
    {
        var mysqlPort = uint.TryParse(builder.Configuration["MYSQL_PORT"], out var parsedPort)
            ? parsedPort
            : 3306u;
        connectionString = new MySqlConnector.MySqlConnectionStringBuilder
        {
            Server = mysqlHost,
            Port = mysqlPort,
            Database = mysqlDatabase,
            UserID = mysqlUser,
            Password = mysqlPassword
        }.ConnectionString;
    }
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database configuration is missing. Set ConnectionStrings:DefaultConnection, MYSQL_CONNECTION_STRING, or MYSQL_HOST, MYSQL_DATABASE, MYSQL_USER, and MYSQL_PASSWORD.");
}


builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString)
        )
);


/* ============================================================
   IDENTITY
   ============================================================ */

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        /*
         * Password requirements for normal customer
         * registration.
         */

        options.Password.RequiredLength = 8;

        options.Password.RequireDigit = true;

        options.Password.RequireLowercase = true;

        options.Password.RequireUppercase = true;

        options.Password.RequireNonAlphanumeric = true;

        /*
         * Username rules.
         */

        options.User.RequireUniqueEmail = true;

        /*
         * We are not requiring email confirmation
         * at this stage.
         */

        options.SignIn.RequireConfirmedAccount = false;

        options.SignIn.RequireConfirmedEmail = false;

        options.SignIn.RequireConfirmedPhoneNumber = false;

        /*
         * Account lockout protection.
         */

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);

        options.Lockout.MaxFailedAccessAttempts = 5;

        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


/* ============================================================
   AUTHENTICATION COOKIE
   ============================================================ */

builder.Services.ConfigureApplicationCookie(
    options =>
    {
        /*
         * Dedicated SuperShop authentication cookie.
         */

        options.Cookie.Name =
            "SuperShop.Identity";


        /*
         * Login page.
         */

        options.LoginPath =
            "/Account/Login";


        /*
         * Unauthorized / insufficient-role page.
         */

        options.AccessDeniedPath =
            "/Account/AccessDenied";


        /*
         * Persistent Remember Me cookie lifetime.
         *
         * When Remember Me is selected, the user can
         * close the browser and return later without
         * logging in again until this cookie expires.
         */

        options.ExpireTimeSpan =
            TimeSpan.FromDays(30);


        /*
         * Renew the cookie when the user remains active.
         */

        options.SlidingExpiration =
            true;


        /*
         * Security settings.
         */

        options.Cookie.HttpOnly =
            true;


        options.Cookie.SameSite =
            SameSiteMode.Lax;


        /*
         * Works for both local HTTP development and
         * HTTPS production hosting.
         */

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.SameAsRequest;


        /*
         * Prevent unnecessary redirects for API/AJAX
         * requests in future development.
         */

        options.Events.OnRedirectToLogin =
            context =>
            {
                if (context.Request.Path.StartsWithSegments(
                        "/api"))
                {
                    context.Response.StatusCode =
                        StatusCodes.Status401Unauthorized;

                    return Task.CompletedTask;
                }

                context.Response.Redirect(
                    context.RedirectUri);

                return Task.CompletedTask;
            };


        options.Events.OnRedirectToAccessDenied =
            context =>
            {
                if (context.Request.Path.StartsWithSegments(
                        "/api"))
                {
                    context.Response.StatusCode =
                        StatusCodes.Status403Forbidden;

                    return Task.CompletedTask;
                }

                context.Response.Redirect(
                    context.RedirectUri);

                return Task.CompletedTask;
            };
    });


/* ============================================================
   MVC
   ============================================================ */

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<PasswordResetRequestLimiter>();
builder.Services.AddTransient<EmailSender>();


/* ============================================================
   SESSION
   ============================================================ */

builder.Services.AddSession(
    options =>
    {
        options.IdleTimeout =
            TimeSpan.FromHours(2);

        options.Cookie.HttpOnly =
            true;

        options.Cookie.IsEssential =
            true;

        options.Cookie.SameSite =
            SameSiteMode.Lax;
    });


/* ============================================================
   ANTI-FORGERY
   ============================================================ */

builder.Services.AddAntiforgery(
    options =>
    {
        options.Cookie.Name =
            "SuperShop.AntiForgery";

        options.Cookie.HttpOnly =
            true;

        options.Cookie.SameSite =
            SameSiteMode.Strict;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.SameAsRequest;
    });


/* ============================================================
   BUILD APPLICATION
   ============================================================ */

var app = builder.Build();


/* ============================================================
   DATABASE MIGRATION / SEEDING
   ============================================================ */

using (var scope = app.Services.CreateScope())
{
    var services =
        scope.ServiceProvider;

    var logger =
        services.GetRequiredService<
            ILogger<Program>>();

    try
    {
        var context =
            services.GetRequiredService<
                ApplicationDbContext>();


        /*
         * Apply any pending EF Core migrations.
         */

        await context.Database.MigrateAsync();


        /*
         * Identity managers.
         */

        var roleManager =
            services.GetRequiredService<
                RoleManager<IdentityRole>>();

        var userManager =
            services.GetRequiredService<
                UserManager<ApplicationUser>>();


        /* ====================================================
           SUPERADMIN ROLE
           ==================================================== */

        const string superAdminRole =
            "SuperAdmin";


        if (!await roleManager.RoleExistsAsync(
                superAdminRole))
        {
            var roleResult =
                await roleManager.CreateAsync(
                    new IdentityRole(
                        superAdminRole));

            if (!roleResult.Succeeded)
            {
                var errors =
                    string.Join(
                        "; ",
                        roleResult.Errors.Select(
                            e => e.Description));

                throw new InvalidOperationException(
                    "Unable to create SuperAdmin role: " +
                    errors);
            }
        }


        /* ====================================================
           SUPERADMIN ACCOUNT
           ==================================================== */

        const string superAdminUsername =
            "SuperAdmin-shop";

        const string superAdminEmail =
            "SuperAdmin-shop@shopapp.local";


        /*
         * IMPORTANT:
         *
         * For local development only, the password may
         * currently be supplied through configuration.
         *
         * Do NOT place the production password directly
         * into source code before deployment.
         */

        var superAdminPassword =
            builder.Configuration[
                "SuperAdmin:Password"];


        /*
         * If the password has not been configured,
         * do not silently create an account with a
         * weak/default password.
         */

        if (string.IsNullOrWhiteSpace(
                superAdminPassword))
        {
            logger.LogWarning(
                "SuperAdmin:Password is not configured. " +
                "The SuperAdmin account was not created or modified.");
        }
        else
        {
            var superAdmin =
                await userManager.FindByNameAsync(
                    superAdminUsername);


            if (superAdmin == null)
            {
                superAdmin =
                    new ApplicationUser
                    {
                        UserName =
                            superAdminUsername,

                        Email =
                            superAdminEmail,

                        EmailConfirmed =
                            true,

                        FullName =
                            "Super Administrator"
                    };


                var createResult =
                    await userManager.CreateAsync(
                        superAdmin,
                        superAdminPassword);


                if (!createResult.Succeeded)
                {
                    var errors =
                        string.Join(
                            "; ",
                            createResult.Errors.Select(
                                e => e.Description));

                    throw new InvalidOperationException(
                        "Unable to create SuperAdmin account: " +
                        errors);
                }
            }
            else if (!await userManager.CheckPasswordAsync(
                         superAdmin,
                         superAdminPassword))
            {
                // The configured secret is the source of truth for
                // this dedicated bootstrap account. Keep it usable if
                // the secret was rotated after the account was created.
                var resetToken =
                    await userManager.GeneratePasswordResetTokenAsync(
                        superAdmin);

                var resetResult =
                    await userManager.ResetPasswordAsync(
                        superAdmin,
                        resetToken,
                        superAdminPassword);

                if (!resetResult.Succeeded)
                {
                    var errors =
                        string.Join(
                            "; ",
                            resetResult.Errors.Select(
                                e => e.Description));

                    throw new InvalidOperationException(
                        "Unable to apply the configured SuperAdmin password: " +
                        errors);
                }
            }


            if (superAdmin.FullName != "Shop Owner")
            {
                superAdmin.FullName = "Shop Owner";

                var updateResult =
                    await userManager.UpdateAsync(superAdmin);

                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Unable to update the SuperAdmin account profile.");
                }
            }


            /*
             * Ensure the SuperAdmin account has the
             * correct role.
             */

            if (!await userManager.IsInRoleAsync(
                    superAdmin,
                    superAdminRole))
            {
                var roleResult =
                    await userManager.AddToRoleAsync(
                        superAdmin,
                        superAdminRole);


                if (!roleResult.Succeeded)
                {
                    var errors =
                        string.Join(
                            "; ",
                            roleResult.Errors.Select(
                                e => e.Description));

                    throw new InvalidOperationException(
                        "Unable to assign SuperAdmin role: " +
                        errors);
                }
            }
        }



        /* ====================================================
           TEST CUSTOMER
           ==================================================== */

        var testCustomerPassword =
            builder.Configuration["TestCustomer:Password"];

        if (!string.IsNullOrWhiteSpace(testCustomerPassword))
        {
            const string testUsername = "test1234";
            const string testEmail = "test1234@shopapp.local";
            var testUser = await userManager.FindByNameAsync(testUsername);

            if (testUser == null)
            {
                testUser = new ApplicationUser
                {
                    UserName = testUsername,
                    Email = testEmail,
                    EmailConfirmed = true,
                    FullName = "Test Customer"
                };

                var createTestUserResult = await userManager.CreateAsync(
                    testUser,
                    testCustomerPassword);

                if (!createTestUserResult.Succeeded)
                {
                    var errors = string.Join(
                        "; ",
                        createTestUserResult.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        "Unable to create test customer: " + errors);
                }
            }

            // Keep the test account a regular customer even if it was
            // previously assigned a privileged role by mistake.
            if (await userManager.IsInRoleAsync(testUser, superAdminRole))
            {
                var removeRoleResult = await userManager.RemoveFromRoleAsync(
                    testUser,
                    superAdminRole);

                if (!removeRoleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Unable to remove the SuperAdmin role from the test customer.");
                }
            }
        }

        /* ====================================================
           DEFAULT CATEGORIES
           ==================================================== */

        var defaultCategories =
            new[]
            {
                new
                {
                    Name = "Men",
                    DisplayOrder = 1
                },

                new
                {
                    Name = "Women",
                    DisplayOrder = 2
                },

                new
                {
                    Name = "Kids",
                    DisplayOrder = 3
                },

                new
                {
                    Name = "Electronics",
                    DisplayOrder = 4
                },

                new
                {
                    Name = "Home & Living",
                    DisplayOrder = 5
                },

                new
                {
                    Name = "Beauty",
                    DisplayOrder = 6
                },

                new
                {
                    Name = "Sports",
                    DisplayOrder = 7
                },

                new
                {
                    Name = "Shoes",
                    DisplayOrder = 8
                },

                new
                {
                    Name = "Accessories",
                    DisplayOrder = 9
                },

                new
                {
                    Name = "More",
                    DisplayOrder = 10
                }
            };


        foreach (var defaultCategory
                 in defaultCategories)
        {
            var categoryExists =
                await context.Categories
                    .AnyAsync(
                        c =>
                            c.Name.ToLower() ==
                            defaultCategory.Name.ToLower());


            if (!categoryExists)
            {
                context.Categories.Add(
                    new Category
                    {
                        Name =
                            defaultCategory.Name,

                        Description =
                            null,

                        IsActive =
                            true,

                        DisplayOrder =
                            defaultCategory.DisplayOrder,

                        CreatedAt =
                            DateTime.UtcNow
                    });
            }
        }


        await context.SaveChangesAsync();


        logger.LogInformation(
            "SuperShop database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex,
            "An error occurred while initializing the SuperShop database.");

        throw;
    }
}


/* ============================================================
   HTTP REQUEST PIPELINE
   ============================================================ */

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(
        "/Home/Error");

    app.UseHsts();
}


/* ============================================================
   HTTPS
   ============================================================ */

app.UseHttpsRedirection();


/* ============================================================
   STATIC FILES
   ============================================================ */

app.UseStaticFiles();


/* ============================================================
   ROUTING
   ============================================================ */

app.UseRouting();


/* ============================================================
   SESSION
   ============================================================ */

app.UseSession();


/* ============================================================
   AUTHENTICATION
   ============================================================ */

app.UseAuthentication();


/* ============================================================
   AUTHORIZATION
   ============================================================ */

app.UseAuthorization();


/* ============================================================
   DEFAULT MVC ROUTE
   ============================================================ */

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}"
);


/* ============================================================
   CUSTOM 404 FALLBACK
   ============================================================ */

app.MapFallbackToController(
    "NotFoundPage",
    "Home"
);


/* ============================================================
   RUN
   ============================================================ */

app.Run();
