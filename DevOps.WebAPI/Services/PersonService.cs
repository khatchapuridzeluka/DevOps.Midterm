using DevOps.WebAPI.Models;

namespace DevOps.WebAPI.Services;

public class PersonService : IPersonService
{
    public PersonCategoryResult Categorize(PersonRequest request)
    {
        var category = request.Age switch
        {
            < 0 => throw new ArgumentException("Age cannot be negative."),
            < 13 => "Child",
            < 20 => "Teenager",
            < 65 => "Adult",
            _ => "Senior"
        };

        return new PersonCategoryResult
        {
            Username = request.Username,
            Name = request.Name,
            Age = request.Age,
            Category = category,
            Greeting = $"Hello {request.Name} (@{request.Username}), you are a {category}."
        };
    }
}