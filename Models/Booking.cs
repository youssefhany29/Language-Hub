using System;
using System.ComponentModel.DataAnnotations;

namespace LanguageHub.Models;

public class Booking
{
    public int Id { get; set; }

    public DateTime SessionDate { get; set; }

    public int StudentId { get; set; }
    public User Student { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; }
}