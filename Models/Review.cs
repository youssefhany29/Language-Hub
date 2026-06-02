using System.ComponentModel.DataAnnotations;

namespace LanguageHub.Models;

public class Review
{
    public int Id { get; set; }
    
    public int CourseId { get; set; }
    public Course? Course { get; set; }

    public int StudentId { get; set; }
    public User? Student { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; } // 1 to 5 stars

    [Required]
    public string Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}