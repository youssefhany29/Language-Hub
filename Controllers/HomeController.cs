using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LanguageHub.Models;
using LanguageHub.Models.ViewModels;
using LanguageHub.Data;

namespace LanguageHub.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context) 
    { 
        _context = context; 
    }

    public IActionResult Index() 
    { 
        return View(); 
    }

    [HttpGet]
    public async Task<IActionResult> BrowseCourses(string search, string language, string level)
    {
        if (HttpContext.Session.GetString("IsTutor") == "True")
        {
            return RedirectToAction("Dashboard");
        }

        var coursesQuery = _context.Courses
            .Include(c => c.Tutor)
            .Include(c => c.Reviews)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            coursesQuery = coursesQuery.Where(c =>
                c.Title.Contains(search) ||
                c.Description.Contains(search) ||
                c.TargetLanguage.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            coursesQuery = coursesQuery.Where(c => c.TargetLanguage == language);
        }

        if (!string.IsNullOrWhiteSpace(level))
        {
            coursesQuery = coursesQuery.Where(c => c.TargetLevel == level);
        }

        var courses = await coursesQuery
            .OrderBy(c => c.TargetLanguage)
            .ThenBy(c => c.Title)
            .ToListAsync();

        var currentUserId = HttpContext.Session.GetInt32("UserId");
        ViewBag.BookedCourseIds = currentUserId == null
            ? new List<int>()
            : await _context.Bookings
                .Where(b => b.StudentId == currentUserId.Value)
                .Select(b => b.CourseId)
                .ToListAsync();

        ViewBag.Languages = await _context.Courses
            .Select(c => c.TargetLanguage)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync();
        ViewBag.Levels = await _context.Courses
            .Select(c => c.TargetLevel)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync();
        ViewBag.Search = search;
        ViewBag.SelectedLanguage = language;
        ViewBag.SelectedLevel = level;
            
        return View(courses);
    }

    // --- DEMO PAYMENT LOGIC ---
    [HttpPost]
    public async Task<IActionResult> BookSession(int courseId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");
        if (HttpContext.Session.GetString("IsTutor") == "True") return RedirectToAction("Dashboard");

        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return RedirectToAction("BrowseCourses");

        var alreadyBooked = await _context.Bookings.AnyAsync(b => b.CourseId == courseId && b.StudentId == userId.Value);
        if (alreadyBooked)
        {
            TempData["SuccessMessage"] = "You already booked this course.";
            return RedirectToAction("Dashboard");
        }

        if (course.Price <= 0)
        {
            _context.Bookings.Add(new Booking
            {
                StudentId = userId.Value,
                CourseId = courseId,
                SessionDate = DateTime.Now.AddDays(7)
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Free course booked successfully!";
            return RedirectToAction("Dashboard");
        }

        return RedirectToAction("FakePayment", new { courseId });
    }

    [HttpGet]
    public async Task<IActionResult> FakePayment(int courseId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");
        if (HttpContext.Session.GetString("IsTutor") == "True") return RedirectToAction("Dashboard");

        var course = await _context.Courses
            .Include(c => c.Tutor)
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return RedirectToAction("BrowseCourses");

        var alreadyBooked = await _context.Bookings.AnyAsync(b => b.CourseId == courseId && b.StudentId == userId.Value);
        if (alreadyBooked)
        {
            TempData["SuccessMessage"] = "You already booked this course.";
            return RedirectToAction("Dashboard");
        }

        return View(course);
    }

    [HttpPost]
    public async Task<IActionResult> ProcessFakePayment(int courseId, string cardHolder, string cardNumber, string expiry, string cvv)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");
        if (HttpContext.Session.GetString("IsTutor") == "True") return RedirectToAction("Dashboard");

        var course = await _context.Courses
            .Include(c => c.Tutor)
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return RedirectToAction("BrowseCourses");

        var alreadyBooked = await _context.Bookings.AnyAsync(b => b.CourseId == courseId && b.StudentId == userId.Value);
        if (alreadyBooked)
        {
            TempData["SuccessMessage"] = "You already booked this course.";
            return RedirectToAction("Dashboard");
        }

        var cleanCardNumber = (cardNumber ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
        var validDemoPayment =
            !string.IsNullOrWhiteSpace(cardHolder) &&
            cleanCardNumber.Length >= 12 &&
            !string.IsNullOrWhiteSpace(expiry) &&
            !string.IsNullOrWhiteSpace(cvv);

        if (!validDemoPayment)
        {
            ViewBag.PaymentError = "Please enter demo card details to complete the payment.";
            return View("FakePayment", course);
        }

        var newBooking = new Booking 
        { 
            StudentId = userId.Value, 
            CourseId = courseId, 
            SessionDate = DateTime.Now.AddDays(7) 
        };
        
        _context.Bookings.Add(newBooking);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Demo payment successful! Your session is booked.";
        return RedirectToAction("Dashboard");
    }

    [HttpGet]
    public IActionResult PaymentSuccess(int courseId)
    {
        return RedirectToAction("FakePayment", new { courseId });
    }

    // --- DASHBOARD & COURSE LOGIC ---
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var isTutor = HttpContext.Session.GetString("IsTutor") == "True";
        
        if (userId == null) return RedirectToAction("Login", "Account");

        if (isTutor)
        {
            var tutorBookings = await _context.Bookings
                .Include(b => b.Course)
                .Include(b => b.Student)
                .Where(b => b.Course.TutorId == userId.Value)
                .OrderBy(b => b.SessionDate)
                .ToListAsync();

            var tutorCourseIds = await _context.Courses
                .Where(c => c.TutorId == userId.Value)
                .Select(c => c.Id)
                .ToListAsync();
            var tutorAttempts = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Where(a => a.Quiz != null && tutorCourseIds.Contains(a.Quiz.CourseId))
                .ToListAsync();

            ViewBag.TotalCourses = tutorCourseIds.Count;
            ViewBag.TotalStudents = tutorBookings.Select(b => b.StudentId).Distinct().Count();
            ViewBag.TotalSubmissions = tutorAttempts.Count;
            ViewBag.AverageGrade = tutorAttempts.Any() ? Math.Round(tutorAttempts.Average(a => a.Percentage), 1) : 0;

            return View(tutorBookings);
        }
        else
        {
            var studentBookings = await _context.Bookings
                .Include(b => b.Course)
                    .ThenInclude(c => c.Tutor)
                .Where(b => b.StudentId == userId.Value)
                .OrderBy(b => b.SessionDate)
                .ToListAsync();

            var studentAttempts = await _context.QuizAttempts
                .Where(a => a.StudentId == userId.Value)
                .ToListAsync();

            ViewBag.TotalBookings = studentBookings.Count;
            ViewBag.CompletedQuizzes = studentAttempts.Count;
            ViewBag.AverageGrade = studentAttempts.Any() ? Math.Round(studentAttempts.Average(a => a.Percentage), 1) : 0;
            ViewBag.NextSession = studentBookings.FirstOrDefault()?.SessionDate;

            return View(studentBookings);
        }
    }

    [HttpGet]
    public async Task<IActionResult> MyCourses()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");
        if (HttpContext.Session.GetString("IsTutor") != "True") return RedirectToAction("Dashboard");

        var courses = await _context.Courses
            .Include(c => c.Quizzes)
                .ThenInclude(q => q.Questions)
            .Include(c => c.Bookings)
                .ThenInclude(b => b.Student)
            .Where(c => c.TutorId == userId.Value)
            .OrderBy(c => c.Title)
            .ToListAsync();

        var courseIds = courses.Select(c => c.Id).ToList();
        var attempts = await _context.QuizAttempts
            .Include(a => a.Student)
            .Include(a => a.Quiz)
            .Where(a => a.Quiz != null && courseIds.Contains(a.Quiz.CourseId))
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        return View(new TutorCoursesViewModel
        {
            Courses = courses,
            QuizAttempts = attempts
        });
    }

    [HttpPost]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var booking = await _context.Bookings
            .Include(b => b.Course)
            .FirstOrDefaultAsync(b => b.Id == id && (b.StudentId == userId.Value || b.Course.TutorId == userId.Value));
            
        if (booking != null)
        {
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Session cancelled.";
        }
        return RedirectToAction("Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> CourseQuizzes(int courseId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");
        if (HttpContext.Session.GetString("IsTutor") == "True") return RedirectToAction("Dashboard");

        var hasBooking = await _context.Bookings
            .AnyAsync(b => b.CourseId == courseId && b.StudentId == userId.Value);
        if (!hasBooking) return RedirectToAction("Dashboard");

        var course = await _context.Courses
            .Include(c => c.Tutor)
            .Include(c => c.Quizzes)
                .ThenInclude(q => q.Questions)
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return RedirectToAction("Dashboard");

        var attempts = await _context.QuizAttempts
            .Include(a => a.Quiz)
            .Where(a => a.StudentId == userId.Value && a.Quiz != null && a.Quiz.CourseId == courseId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        return View(new StudentCourseQuizzesViewModel
        {
            Course = course,
            Attempts = attempts
        });
    }

    [HttpGet]
    public async Task<IActionResult> TakeQuiz(int quizId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");
        if (HttpContext.Session.GetString("IsTutor") == "True") return RedirectToAction("Dashboard");

        var quiz = await _context.Quizzes
            .Include(q => q.Course)
            .Include(q => q.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz?.Course == null) return RedirectToAction("Dashboard");

        var hasBooking = await _context.Bookings
            .AnyAsync(b => b.CourseId == quiz.CourseId && b.StudentId == userId.Value);
        if (!hasBooking) return RedirectToAction("Dashboard");

        return View(quiz);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitQuiz(int quizId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");
        if (HttpContext.Session.GetString("IsTutor") == "True") return RedirectToAction("Dashboard");

        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz == null) return RedirectToAction("Dashboard");

        var hasBooking = await _context.Bookings
            .AnyAsync(b => b.CourseId == quiz.CourseId && b.StudentId == userId.Value);
        if (!hasBooking) return RedirectToAction("Dashboard");

        var score = 0;
        foreach (var question in quiz.Questions)
        {
            var fieldName = $"answers[{question.Id}]";
            if (int.TryParse(Request.Form[fieldName], out var answerOptionId) &&
                question.AnswerOptions.Any(o => o.Id == answerOptionId && o.IsCorrect))
            {
                score++;
            }
        }

        var totalQuestions = quiz.Questions.Count;
        var attempt = new QuizAttempt
        {
            QuizId = quiz.Id,
            StudentId = userId.Value,
            Score = score,
            TotalQuestions = totalQuestions,
            Percentage = totalQuestions == 0 ? 0 : Math.Round(score * 100.0 / totalQuestions, 1),
            SubmittedAt = DateTime.Now
        };

        _context.QuizAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Quiz submitted. Your grade is {attempt.Score}/{attempt.TotalQuestions} ({attempt.Percentage:0.0}%).";
        return RedirectToAction("CourseQuizzes", new { courseId = quiz.CourseId });
    }

    [HttpGet]
    public IActionResult CreateCourse() 
    { 
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");
        if (HttpContext.Session.GetString("IsTutor") != "True") return RedirectToAction("Dashboard");

        return View(); 
    }

    [HttpPost]
    public IActionResult CreateCourse(Course newCourse)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");
        if (HttpContext.Session.GetString("IsTutor") != "True") return RedirectToAction("Dashboard");
        
        newCourse.TutorId = userId.Value;
        _context.Courses.Add(newCourse);
        _context.SaveChanges();
        
        TempData["SuccessMessage"] = "Course published!";
        return RedirectToAction("MyCourses");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var course = await _context.Courses.FindAsync(id);
        
        if (course != null && course.TutorId == userId)
        {
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Course deleted.";
        }
        return RedirectToAction("MyCourses");
    }

    // --- QUIZ LOGIC ---
    [HttpGet]
    public async Task<IActionResult> ManageEducationalTools(int courseId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        
        var course = await _context.Courses
            .Include(c => c.Quizzes)
                .ThenInclude(q => q.Questions)
                    .ThenInclude(o => o.AnswerOptions)
            .FirstOrDefaultAsync(c => c.Id == courseId && c.TutorId == userId);

        if (course == null) return RedirectToAction("MyCourses");
        return View(course);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuiz(int courseId, string quizTitle)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var ownsCourse = await _context.Courses.AnyAsync(c => c.Id == courseId && c.TutorId == userId.Value);
        if (!ownsCourse) return RedirectToAction("MyCourses");

        var quiz = new Quiz { CourseId = courseId, Title = quizTitle };
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "Quiz created! Now you can add questions to it.";
        return RedirectToAction("ManageEducationalTools", new { courseId = courseId });
    }

    [HttpPost]
    public async Task<IActionResult> AddQuestion(int courseId, int quizId, string questionText, string option1, string option2, string option3, int correctOption)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var ownsQuiz = await _context.Quizzes
            .Include(q => q.Course)
            .AnyAsync(q => q.Id == quizId && q.CourseId == courseId && q.Course != null && q.Course.TutorId == userId.Value);
        if (!ownsQuiz) return RedirectToAction("MyCourses");

        var question = new Question { QuizId = quizId, Text = questionText };
        _context.Questions.Add(question);
        await _context.SaveChangesAsync(); 

        var options = new List<AnswerOption>
        {
            new AnswerOption { QuestionId = question.Id, Text = option1, IsCorrect = correctOption == 1 },
            new AnswerOption { QuestionId = question.Id, Text = option2, IsCorrect = correctOption == 2 },
            new AnswerOption { QuestionId = question.Id, Text = option3, IsCorrect = correctOption == 3 }
        };
        
        _context.AnswerOptions.AddRange(options);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Question added successfully!";
        return RedirectToAction("ManageEducationalTools", new { courseId = courseId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteQuiz(int quizId, int courseId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var quiz = await _context.Quizzes
            .Include(q => q.Course)
            .FirstOrDefaultAsync(q => q.Id == quizId && q.CourseId == courseId && q.Course != null && q.Course.TutorId == userId.Value);
        if (quiz != null)
        {
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Quiz deleted successfully.";
        }
        return RedirectToAction("ManageEducationalTools", new { courseId = courseId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteQuestion(int questionId, int courseId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var question = await _context.Questions
            .Include(q => q.Quiz)
                .ThenInclude(q => q.Course)
            .FirstOrDefaultAsync(q => q.Id == questionId && q.Quiz != null && q.Quiz.CourseId == courseId && q.Quiz.Course != null && q.Quiz.Course.TutorId == userId.Value);
        if (question != null)
        {
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Question deleted successfully.";
        }
        return RedirectToAction("ManageEducationalTools", new { courseId = courseId });
    }

    // --- REVIEW LOGIC ---
    [HttpGet]
    public async Task<IActionResult> ReviewCourse(int courseId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return RedirectToAction("Dashboard");

        return View(course);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitReview(int courseId, int rating, string comment)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var review = new Review 
        { 
            CourseId = courseId, 
            StudentId = userId.Value, 
            Rating = rating, 
            Comment = comment 
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thank you! Your review has been published.";
        return RedirectToAction("Dashboard");
    }
}
