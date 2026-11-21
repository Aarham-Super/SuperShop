using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SuperShop.Models;
using SuperShop.ViewModels;
using SuperShop.Data;
using SuperShop.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace SuperShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly EmailSender _emailSender;
        private readonly PasswordResetRequestLimiter _resetLimiter;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            ApplicationDbContext context,
            EmailSender emailSender,
            PasswordResetRequestLimiter resetLimiter)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            _emailSender = emailSender;
            _resetLimiter = resetLimiter;
        }


        // ============================================================
        // LOGIN
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // If the user is already authenticated, do not show
            // the login page again.
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // ----------------------------------------------------
                // IMPORTANT
                //
                // Login is performed by Identity.
                //
                // The username determines which Identity account
                // is being authenticated.
                //
                // The role is NOT selected by the user.
                // ----------------------------------------------------

                var result = await _signInManager.PasswordSignInAsync(
                    model.Username.Trim(),
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation(
                        "User {Username} logged in successfully.",
                        model.Username);

                    // Prevent an external or unsafe return URL.
                    if (!string.IsNullOrWhiteSpace(model.ReturnUrl) &&
                        Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return LocalRedirect(model.ReturnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "This account is temporarily locked. Please try again later.");

                    return View(model);
                }

                if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "This account is not currently allowed to sign in.");

                    return View(model);
                }

                // ----------------------------------------------------
                // Generic message.
                //
                // Do not reveal whether a privileged account exists.
                // ----------------------------------------------------

                ModelState.AddModelError(
                    string.Empty,
                    "Invalid username or password.");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while logging in user {Username}.",
                    model.Username);

                ModelState.AddModelError(
                    string.Empty,
                    "An error occurred while trying to sign in. Please try again.");

                return View(model);
            }
        }


        // ============================================================
        // REGISTER
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // ----------------------------------------------------
                // RESERVED USERNAME
                //
                // Customers must never be able to register the
                // SuperAdmin username.
                // ----------------------------------------------------

                if (string.Equals(
                    model.Username.Trim(),
                    "SuperAdmin-shop",
                    StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        nameof(model.Username),
                        "This username is not available.");

                    return View(model);
                }


                // ----------------------------------------------------
                // CHECK USERNAME
                // ----------------------------------------------------

                var existingUsername =
                    await _userManager.FindByNameAsync(
                        model.Username.Trim());

                if (existingUsername != null)
                {
                    ModelState.AddModelError(
                        nameof(model.Username),
                        "This username is already in use.");

                    return View(model);
                }


                // ----------------------------------------------------
                // CHECK EMAIL
                // ----------------------------------------------------

                var existingEmail =
                    await _userManager.FindByEmailAsync(
                        model.Email.Trim());

                if (existingEmail != null)
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "This email address is already in use.");

                    return View(model);
                }


                // ----------------------------------------------------
                // CREATE NORMAL CUSTOMER
                //
                // No role is assigned here.
                //
                // Therefore a newly registered user is NOT a
                // SuperAdmin.
                // ----------------------------------------------------

                var user = new ApplicationUser
                {
                    UserName = model.Username.Trim(),
                    Email = model.Email.Trim(),
                    FullName = model.FullName.Trim(),
                    EmailConfirmed = false
                };


                var createResult =
                    await _userManager.CreateAsync(
                        user,
                        model.Password);


                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return View(model);
                }


                _logger.LogInformation(
                    "New customer account created for {Username}.",
                    user.UserName);


                // ----------------------------------------------------
                // SIGN IN NEW CUSTOMER
                //
                // This is a normal customer.
                // No SuperAdmin role is assigned.
                // ----------------------------------------------------

                await _signInManager.SignInAsync(
                    user,
                    isPersistent: false);


                return RedirectToAction(
                    "Index",
                    "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while registering user {Username}.",
                    model.Username);

                ModelState.AddModelError(
                    string.Empty,
                    "The account could not be created. Please try again.");

                return View(model);
            }
        }


        // ============================================================
        // ONE-TIME EMAIL PASSWORD RESET CODE
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown";
            if (!_resetLimiter.TryAcquire(ip))
            {
                _logger.LogWarning("Password reset request throttled.");
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                var now = DateTime.UtcNow;
                var previous = await _context.PasswordResetCodes
                    .Where(code => code.UserId == user.Id && code.UsedAt == null)
                    .ToListAsync();
                foreach (var codeRecord in previous) codeRecord.UsedAt = now;

                var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
                var salt = RandomNumberGenerator.GetBytes(16);
                var hash = HashResetCode(code, salt);
                var resetCodeRecord = new PasswordResetCode
                {
                    UserId = user.Id,
                    CodeHash = Convert.ToHexString(hash),
                    Salt = Convert.ToBase64String(salt),
                    CreatedAt = now,
                    ExpiresAt = now.AddMinutes(10)
                };
                _context.PasswordResetCodes.Add(resetCodeRecord);
                await _context.SaveChangesAsync();

                try
                {
                    await _emailSender.SendPasswordResetCodeAsync(user.Email, code);
                }
                catch (Exception ex)
                {
                    // Never log the code or expose account existence in the response.
                    _logger.LogError(ex, "Password reset email delivery failed for user ID {UserId}.", user.Id);
                    resetCodeRecord.UsedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword() => View(new ResetPasswordViewModel());

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null) return ResetCodeInvalid();

            var resetCode = await _context.PasswordResetCodes
                .Where(code => code.UserId == user.Id && code.UsedAt == null)
                .OrderByDescending(code => code.CreatedAt)
                .FirstOrDefaultAsync();
            var now = DateTime.UtcNow;
            if (resetCode == null || resetCode.ExpiresAt <= now || resetCode.FailedAttempts >= 5)
                return ResetCodeInvalid();

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(resetCode.Salt);
                expected = Convert.FromHexString(resetCode.CodeHash);
            }
            catch (FormatException)
            {
                resetCode.UsedAt = now;
                await _context.SaveChangesAsync();
                return ResetCodeInvalid();
            }

            var actual = HashResetCode(model.Code, salt);
            if (actual.Length != expected.Length || !CryptographicOperations.FixedTimeEquals(actual, expected))
            {
                resetCode.FailedAttempts++;
                if (resetCode.FailedAttempts >= 5) resetCode.UsedAt = now;
                await _context.SaveChangesAsync();
                return ResetCodeInvalid();
            }

            var passwordErrors = new List<IdentityError>();
            foreach (var validator in _userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(_userManager, user, model.NewPassword);
                if (!result.Succeeded) passwordErrors.AddRange(result.Errors);
            }
            if (passwordErrors.Count > 0)
            {
                foreach (var error in passwordErrors) ModelState.AddModelError(nameof(model.NewPassword), error.Description);
                return View(model);
            }

            var claimed = await _context.PasswordResetCodes
                .Where(code => code.Id == resetCode.Id && code.UsedAt == null && code.ExpiresAt > now && code.FailedAttempts < 5)
                .ExecuteUpdateAsync(update => update.SetProperty(code => code.UsedAt, DateTime.UtcNow));
            if (claimed != 1) return ResetCodeInvalid();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!resetResult.Succeeded)
            {
                foreach (var error in resetResult.Errors) ModelState.AddModelError(nameof(model.NewPassword), error.Description);
                return View(model);
            }

            _logger.LogInformation("Password reset completed for user ID {UserId}.", user.Id);
            return RedirectToAction(nameof(Login));
        }

        private IActionResult ResetCodeInvalid()
        {
            ModelState.AddModelError(string.Empty, "That code is invalid, expired, or already used. Request a new code and try again.");
            return View("ResetPassword", new ResetPasswordViewModel());
        }

        private static byte[] HashResetCode(string code, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(code), salt, 100_000, HashAlgorithmName.SHA256, 32);


        // ============================================================
        // PROFILE
        // ============================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                await _signInManager.SignOutAsync();

                return RedirectToAction(
                    nameof(Login));
            }

            return View(user);
        }


        // ============================================================
        // UPDATE PROFILE
        // ============================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            string fullName)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                await _signInManager.SignOutAsync();

                return RedirectToAction(
                    nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                TempData["ErrorMessage"] =
                    "Full name is required.";

                return RedirectToAction(
                    nameof(Profile));
            }

            fullName = fullName.Trim();

            if (fullName.Length > 100)
            {
                TempData["ErrorMessage"] =
                    "Full name cannot exceed 100 characters.";

                return RedirectToAction(
                    nameof(Profile));
            }

            user.FullName = fullName;

            var result =
                await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Refresh the authentication cookie so the updated
                // user information is reflected immediately.
                await _signInManager.RefreshSignInAsync(user);

                TempData["SuccessMessage"] =
                    "Your profile was updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Your profile could not be updated.";
            }

            return RedirectToAction(
                nameof(Profile));
        }


        // ============================================================
        // ACCOUNT SETTINGS
        // ============================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Settings()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                await _signInManager.SignOutAsync();

                return RedirectToAction(
                    nameof(Login));
            }

            var model = new AccountSettingsViewModel
            {
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Username = user.UserName ?? string.Empty
            };

            return View(model);
        }


        // ============================================================
        // CHANGE EMAIL
        // ============================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(
            string email)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                await _signInManager.SignOutAsync();

                return RedirectToAction(
                    nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] =
                    "Email address is required.";

                return RedirectToAction(
                    nameof(Settings));
            }

            email = email.Trim();


            // ----------------------------------------------------
            // Check whether another account already uses email.
            // ----------------------------------------------------

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null &&
                existingUser.Id != user.Id)
            {
                TempData["ErrorMessage"] =
                    "This email address is already in use.";

                return RedirectToAction(
                    nameof(Settings));
            }


            var result =
                await _userManager.SetEmailAsync(
                    user,
                    email);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    "The email address could not be changed.";

                return RedirectToAction(
                    nameof(Settings));
            }


            user.EmailConfirmed = false;

            await _userManager.UpdateAsync(user);

            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] =
                "Your email address was updated successfully.";

            return RedirectToAction(
                nameof(Settings));
        }


        // ============================================================
        // CHANGE PASSWORD
        // ============================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            string currentPassword,
            string newPassword,
            string confirmPassword)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                await _signInManager.SignOutAsync();

                return RedirectToAction(
                    nameof(Login));
            }


            // ----------------------------------------------------
            // VALIDATION
            // ----------------------------------------------------

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                TempData["ErrorMessage"] =
                    "Current password is required.";

                return RedirectToAction(
                    nameof(Settings));
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["ErrorMessage"] =
                    "New password is required.";

                return RedirectToAction(
                    nameof(Settings));
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] =
                    "The new passwords do not match.";

                return RedirectToAction(
                    nameof(Settings));
            }


            // ----------------------------------------------------
            // CHANGE PASSWORD THROUGH IDENTITY
            // ----------------------------------------------------

            var result =
                await _userManager.ChangePasswordAsync(
                    user,
                    currentPassword,
                    newPassword);


            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                TempData["ErrorMessage"] =
                    "The password could not be changed.";

                return RedirectToAction(
                    nameof(Settings));
            }


            // ----------------------------------------------------
            // Refresh login after password change.
            // ----------------------------------------------------

            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] =
                "Your password was changed successfully.";

            return RedirectToAction(
                nameof(Settings));
        }


        // ============================================================
        // LOGOUT
        // ============================================================
        //
        // IMPORTANT:
        //
        // This method ONLY signs the current user out.
        //
        // It does NOT:
        // - delete the user
        // - delete the SuperAdmin
        // - change the password
        // - remove the SuperAdmin role
        // - recreate the account
        // - modify Identity database records
        //
        // Therefore logging out SuperAdmin will NOT destroy
        // the SuperAdmin account.
        // ============================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var username =
                    User.Identity?.Name;

                _logger.LogInformation(
                    "User {Username} is logging out.",
                    username);


                // ------------------------------------------------
                // SIGN OUT FROM ASP.NET CORE IDENTITY
                // ------------------------------------------------

                await _signInManager.SignOutAsync();


                // ------------------------------------------------
                // CLEAR APPLICATION SESSION
                //
                // This does NOT delete the Identity user.
                // ------------------------------------------------

                HttpContext.Session.Clear();


                // ------------------------------------------------
                // RETURN TO HOME PAGE
                // ------------------------------------------------

                return RedirectToAction(
                    "Index",
                    "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while logging out user {Username}.",
                    User.Identity?.Name);

                // Even if something unexpected happens,
                // send the user back to the public home page.
                return RedirectToAction(
                    "Index",
                    "Home");
            }
        }


        // ============================================================
        // ACCESS DENIED
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            Response.StatusCode =
                StatusCodes.Status403Forbidden;

            return View();
        }
    }
}
