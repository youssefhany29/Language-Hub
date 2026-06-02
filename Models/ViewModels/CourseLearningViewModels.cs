namespace LanguageHub.Models.ViewModels;

public class TutorCoursesViewModel
{
    public List<Course> Courses { get; set; } = new();
    public List<QuizAttempt> QuizAttempts { get; set; } = new();
}

public class StudentCourseQuizzesViewModel
{
    public Course Course { get; set; }
    public List<QuizAttempt> Attempts { get; set; } = new();
}
