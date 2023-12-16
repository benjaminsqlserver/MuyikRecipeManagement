namespace RecipeManagement.SharedTestHelpers.Fakes.Recipe;

using AutoBogus;
using RecipeManagement.Domain.Recipes;
using RecipeManagement.Domain.Recipes.Dtos;
using RecipeManagement.Domain.Visibilities;

public sealed class FakeRecipeForCreationDto : AutoFaker<RecipeForCreationDto>
{
    public FakeRecipeForCreationDto()
    {
        RuleFor(r => r.Visibility, f => f.PickRandom(Visibility.ListNames()));
    }
}