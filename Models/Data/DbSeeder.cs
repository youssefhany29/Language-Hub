using LanguageHub.Models;

namespace LanguageHub.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        if (context.Courses.Any())
        {
            return;
        }

        var dummyTutor = new User
        {
            Name = "Ahmet Yılmaz",
            Email = "ahmet@languagehub.test",
            PasswordHash = "dummyhash123", 
            IsTutor = true
        };

        context.Users.Add(dummyTutor);
        context.SaveChanges(); 

        var courses = new List<Course>
        {
            new Course
            {
                Title = "Survival Turkish for Travelers",
                Description = "Learn the essential phrases for navigating the city, ordering tea, and shopping.",
                TargetLanguage = "Turkish",
                TargetLevel = "A1 Beginner",
                Price = 20,
                TutorId = dummyTutor.Id
            },
            new Course
            {
                Title = "Advanced English Grammar",
                Description = "Master complex sentence structures and professional vocabulary.",
                TargetLanguage = "English",
                TargetLevel = "C1 Advanced",
                Price = 35,
                TutorId = dummyTutor.Id
            }
        };

        context.Courses.AddRange(courses);
        context.SaveChanges();
    }
}
