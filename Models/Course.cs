using System.ComponentModel.DataAnnotations;

namespace LanguageHub.Models;

public class Course
{
    public int Id { get; set; }
    [Required] public string Title { get; set; }
    [Required] public string Description { get; set; }
    [Required] public string TargetLanguage { get; set; }
    [Required] public string TargetLevel { get; set; }
    
    [Required]
    [Range(0, 1000)]
    public decimal Price { get; set; }
    
    public int TutorId { get; set; }
    public User? Tutor { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}