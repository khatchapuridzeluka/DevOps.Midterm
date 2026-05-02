using DevOps.WebAPI.Models;

namespace DevOps.WebAPI.Services;

public interface IPersonService
{
    PersonCategoryResult Categorize(PersonRequest request);
}