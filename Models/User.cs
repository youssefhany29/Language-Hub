using System.ComponentModel.DataAnnotations;

namespace LanguageHub.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string PasswordHash { get; set; }

    public bool IsTutor { get; set; } 
    
    public List<Booking> Bookings { get; set; } = new List<Booking>();
}