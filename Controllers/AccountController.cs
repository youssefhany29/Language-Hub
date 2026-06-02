using Microsoft.AspNetCore.Mvc;
using LanguageHub.Data;
using LanguageHub.Models;

namespace LanguageHub.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(string name, string email, string password, string role)
    {
        if (_context.Users.Any(u => u.Email == email))
        {
            ViewBag.Error = "Email is already registered.";
            return View();
        }

        var isTutor = role == "Tutor";
        var newUser = new User
        {
            Name = name,
            Email = email,
            PasswordHash = password,
            IsTutor = isTutor
        };

        _context.Users.Add(newUser);
        _context.SaveChanges();

        HttpContext.Session.SetInt32("UserId", newUser.Id);
        HttpContext.Session.SetString("UserName", newUser.Name);
        
        // NEW: Track if they are a tutor
        HttpContext.Session.SetString("IsTutor", newUser.IsTutor.ToString());

        return RedirectToAction(isTutor ? "Dashboard" : "Index", "Home");
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string email, string password)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == email && u.PasswordHash == password);
        
        if (user != null)
        {
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Name);
            
            // NEW: Track if they are a tutor
            HttpContext.Session.SetString("IsTutor", user.IsTutor.ToString());
            
            return RedirectToAction(user.IsTutor ? "Dashboard" : "Index", "Home");
        }

        ViewBag.Error = "Invalid email or password.";
        return View();
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear(); 
        return RedirectToAction("Index", "Home");
    }
}
