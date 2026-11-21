using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SuperShop.Models;
using SuperShop.Data;
using Microsoft.EntityFrameworkCore;

namespace SuperShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }


        // ============================================================
        // HOME
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Where(category => category.IsActive)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .ToListAsync();
            return View(categories);
        }


        // ============================================================
        // SHIPPING AND RETURNS INFORMATION
        [HttpGet]
        public IActionResult Shipping() => View();

        // ============================================================
        // PRIVACY
        // ============================================================

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet("/credits")]
        public IActionResult Credits()
        {
            return View();
        }


        // ============================================================
        // 404 NOT FOUND
        // ============================================================

        [HttpGet]
        public IActionResult NotFoundPage()
        {
            Response.StatusCode = StatusCodes.Status404NotFound;

            return View();
        }


        // ============================================================
        // APPLICATION ERROR
        // ============================================================

        [HttpGet]
        public IActionResult Error()
        {
            var requestId =
                Activity.Current?.Id ??
                HttpContext.TraceIdentifier;

            _logger.LogError(
                "An application error occurred. Request ID: {RequestId}",
                requestId);

            Response.StatusCode =
                StatusCodes.Status500InternalServerError;

            return View(
                new ErrorViewModel
                {
                    RequestId = requestId
                });
        }
    }
}
