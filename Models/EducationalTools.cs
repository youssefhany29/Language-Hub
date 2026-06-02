using System.ComponentModel.DataAnnotations;

namespace LanguageHub.Models;

public class Quiz
{
    public int Id { get; set; }
    [Required] public string Title { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}

public class Question
{
    public int Id { get; set; }
    [Required] public string Text { get; set; }
    public int QuizId { get; set; }
    public Quiz? Quiz { get; set; }
    public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
}

public class AnswerOption
{
    public int Id { get; set; }
    [Required] public string Text { get; set; }
    public bool IsCorrect { get; set; }
    public int QuestionId { get; set; }
    public Question? Question { get; set; }
}

public class QuizAttempt
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; }
    public int StudentId { get; set; }
    public User Student { get; set; }
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public double Percentage { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.Now;
}
