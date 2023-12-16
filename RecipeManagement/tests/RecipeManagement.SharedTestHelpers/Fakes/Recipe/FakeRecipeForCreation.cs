namespace RecipeManagement.SharedTestHelpers.Fakes.Recipe;

using AutoBogus;
using RecipeManagement.Domain.Recipes;
using RecipeManagement.Domain.Recipes.Models;
using RecipeManagement.Domain.Visibilities;

public sealed class FakeRecipeForCreation : AutoFaker<RecipeForCreation>
{
    public FakeRecipeForCreation()
    {
        RuleFor(r => r.Visibility, f => f.PickRandom(Visibility.ListNames()));
    }
}