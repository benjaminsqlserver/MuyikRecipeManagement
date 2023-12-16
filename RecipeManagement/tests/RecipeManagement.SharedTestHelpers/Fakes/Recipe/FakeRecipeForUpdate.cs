namespace RecipeManagement.SharedTestHelpers.Fakes.Recipe;

using AutoBogus;
using RecipeManagement.Domain.Recipes;
using RecipeManagement.Domain.Recipes.Models;
using RecipeManagement.Domain.Visibilities;

public sealed class FakeRecipeForUpdate : AutoFaker<RecipeForUpdate>
{
    public FakeRecipeForUpdate()
    {
        RuleFor(r => r.Visibility, f => f.PickRandom(Visibility.ListNames()));
    }
}