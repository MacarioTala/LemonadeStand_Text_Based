using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using System;
using UnityEngine;
using NUnit.Framework.Internal;

[TestFixture]
public class RecipeTests
{
    private Inventory test_inventory;
    private Good lemonade;
    private Good lemon;
    private Good sugar;
    private Good water;
    private Recipe basicLemonadeRecipe;
    private const int Period = 0;

    private readonly PriceBand price_band1= new(.5m, 1.0m);
    private readonly PriceBand price_band2= new(1.0m, 5.0m);
    private readonly PriceBand price_band3= new(6.0m, 8.0m);

    private readonly PriceBand price_band4 = new(10.0m, 15.0m);

    [SetUp]
    public void SetUp()
    {
        test_inventory = new Inventory();
        lemon = Good.CreateInstance("lemon", price_band2);
        sugar = Good.CreateInstance("sugar", price_band1);
        water = Good.CreateInstance("water", price_band3);
        var lemon_inventory_entry = new InventoryEntry(lemon, 10, 1, Period);
        var sugar_inventory_entry = new InventoryEntry(sugar, 10, 1, Period);
        var water_inventory_entry = new InventoryEntry(water, 10, 1, Period);
        test_inventory.AddGood(lemon_inventory_entry);
        test_inventory.AddGood(sugar_inventory_entry);
        test_inventory.AddGood(water_inventory_entry);

        lemonade = Good.CreateInstance("lemonade", price_band4);
        var lemonade_ingredients = new List<Ingredient> { new(lemon, 9),
                                                          new(sugar, 2), 
                                                          new(water, 7) };
        basicLemonadeRecipe = new Recipe(RecipeName:"Basic Lemonade",
                                     product: lemonade, 
                                     ingredients: lemonade_ingredients);
    }

    [Test]
    public void Get_Ingredients_returns_list_of_ingredients()
    {
        // Arrange
        var expected = new List<String> { "lemon", "sugar", "water" };
        // Act
        var actual = basicLemonadeRecipe.GetIngredientNames();
        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Get_max_quantity_returns_goods_based_on_smallest_ingredient_available()
    {
        // Arrange
        var expected = 1;// need 9 lemon, 2 sugar, 7 water, can only make 1 lemonade
        // Act
        var actual = basicLemonadeRecipe.Get_max_quantity(test_inventory.GetInventoryEntries());
        // Assert 
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Make_recipe_throws_Recipe_Exception_if_not_enough_stock()
    {
        //arrange
        var test_inventory = new Inventory();
        var inventory_entry1 = new InventoryEntry(lemon, 10, 1.0m, Period);
        var inventory_entry2 = new InventoryEntry(sugar, 10, 1.0m, Period);
        var inventory_entry3 = new InventoryEntry(water, 10, 1.0m, Period);
        test_inventory.AddGood(inventory_entry1);
        test_inventory.AddGood(inventory_entry2);
        test_inventory.AddGood(inventory_entry3);
        var lemonade = Good.CreateInstance("Lemonade", price_band4);
        var lemonade_recipe = new Recipe(RecipeName: "Basic Lemonade",
                                         product: lemonade, 
                                         ingredients: new List<Ingredient> { new(lemon, 9), new(sugar, 2), new(water, 7) });
        var quantity = 2;
        //act
        //assert   
        Assert.Throws<RecipeException>(() => lemonade_recipe.Make_recipe(quantity, test_inventory));
    }

    [Test]
    public void Make_recipe_consume_stock_and_return_product_and_quantity()
    {
        //arrange
        var test_inventory = new Inventory();
        var inventory_entry1 = new InventoryEntry(lemon, 10, 1.0m, Period);
        var inventory_entry2 = new InventoryEntry(sugar, 10, 1.0m, Period);
        var inventory_entry3 = new InventoryEntry(water, 10, 1.0m, Period);
        test_inventory.AddGood(inventory_entry1);
        test_inventory.AddGood(inventory_entry2);
        test_inventory.AddGood(inventory_entry3);
        var lemonade = Good.CreateInstance("Lemonade", price_band4);
        var lemonade_recipe = new Recipe(RecipeName:"Basic Lemonade",
                                         product: lemonade, 
                                         ingredients:new List<Ingredient> { new(lemon, 9), new(sugar, 2), new(water, 7) });
        var quantity = 1;
        var expected_remaining_lemons = 1;
        var expected_remaining_sugar = 8;
        var expected_remaining_water = 3;
        //act
        var actual = lemonade_recipe.Make_recipe(quantity, test_inventory);
        //assert
        Assert.AreEqual((lemonade, quantity), actual);
        Assert.AreEqual(expected_remaining_lemons, test_inventory.GetInventoryEntries().Where(entry => entry.good == lemon).First().quantity);
        Assert.AreEqual(expected_remaining_sugar, test_inventory.GetInventoryEntries().Where(entry => entry.good == sugar).First().quantity);
        Assert.AreEqual(expected_remaining_water, test_inventory.GetInventoryEntries().Where(entry => entry.good == water).First().quantity);
    }
    [Test]
    public void Get_recipe_returns_list_of_ingredients_and_quantity_needed()
    {
     // Arrange
        var expected = new List<(string Name, int Quantity)>
        {
            ("lemon", 9),
            ("sugar", 2),
            ("water", 7)
        };

        // Act
        var actual = basicLemonadeRecipe.Get_recipe()
                                    .Select(ingredient => (ingredient.Good.GoodName, ingredient.Quantity_needed))
                                    .ToList();

        // Assert
        foreach (var (expectedName, expectedQuantity) in expected)
        {
            var match = actual.FirstOrDefault(a => a.GoodName == expectedName && a.Quantity_needed == expectedQuantity);
            Assert.IsNotNull(match, $"Expected ingredient '{expectedName}' with quantity {expectedQuantity} was not found in the recipe.");
        }
    }
    [Test]
    public void Recipe_can_be_made_if_ingredients_span_two_inventory_entries()
    {
        // Arrange
        var inventory = new Inventory();
        var lemonInventoryEntry = new InventoryEntry(lemon, 5, 3,Period);
        var sugarInventoryEntry = new InventoryEntry(sugar, 5, 2,Period);
        var waterInventoryEntry = new InventoryEntry(water, 7, 1,Period);
        var secondLemonInventoryEntry = new InventoryEntry(lemon, 5, 2,Period);
        inventory.AddGood(lemonInventoryEntry);
        inventory.AddGood(sugarInventoryEntry);
        inventory.AddGood(waterInventoryEntry);
        inventory.AddGood(secondLemonInventoryEntry);
        basicLemonadeRecipe = new Recipe(RecipeName:"Basic Lemonade",
                                    product: lemonade, 
                                    ingredients: new List<Ingredient> { new(lemon, 9), 
                                                                        new(sugar, 2), 
                                                                        new(water, 7) });
        var expected = (lemonade, 1);
        // Act
        var actual = basicLemonadeRecipe.Make_recipe(1, inventory);
        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void GetCostPerUnit_returns_average_cost_of_ingredients()
    {
        // Arrange
        var inventory = new Inventory();
        var lemon_inventory_entry = new InventoryEntry(lemon, 10, 3,Period);
        var sugar_inventory_entry = new InventoryEntry(sugar, 10, 2,Period);
        var water_inventory_entry = new InventoryEntry(water, 10, 1,Period);
        inventory.AddGood(lemon_inventory_entry);
        inventory.AddGood(sugar_inventory_entry);
        inventory.AddGood(water_inventory_entry);
        var expected = (float)(9*3 + 2*2 + 7*1);
        // Act
        var actual = basicLemonadeRecipe.GetCostPerUnit(inventory);
        // Assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void MakingRecipeCreatesAProducedGood()
    {
        //Arrange
        var inventory = new Inventory();
        var lemonInventoryEntry = new InventoryEntry(lemon, 10, 3,Period);
        var sugarInventoryEntry = new InventoryEntry(sugar, 10, 2,Period);
        var waterInventoryEntry = new InventoryEntry(water, 10, 1,Period);
        inventory.AddGood(lemonInventoryEntry);
        inventory.AddGood(sugarInventoryEntry);
        inventory.AddGood(waterInventoryEntry);
        basicLemonadeRecipe = new Recipe(RecipeName: "Basic Lemonade",
                                     product: lemonade, 
                                     ingredients: new List<Ingredient> { new(lemon, 9), 
                                                                        new(sugar, 2), 
                                                                        new(water, 7) });
        var expected = true;
        //Act
        basicLemonadeRecipe.Make_recipe(1, inventory);
        var actual = lemonade.IsProducedGood;
        //Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void MakingRecipeSetsRecipeOnProducedInventoryEntry()
    {
        //Arrange
        var TestCompany = EconAgent.Factory.Create(companyName: "TestCompany", 
                                                 company_level: AgentLevelEnum.Beginner, 
                                                 fixedCostStrategy: new BasicFixedCostStrategy(),
                                                 strategy: new BasicGrowthStrategy());
        var inventory = TestCompany.GetInventory();
        var lemonInventoryEntry = new InventoryEntry(lemon, 10, 3,Period);
        var sugarInventoryEntry = new InventoryEntry(sugar, 10, 2,Period);
        var waterInventoryEntry = new InventoryEntry(water, 10, 1,Period);
        inventory.AddGood(lemonInventoryEntry);
        inventory.AddGood(sugarInventoryEntry);
        inventory.AddGood(waterInventoryEntry);
        basicLemonadeRecipe = new Recipe("Basic Lemonade",
                                    product: lemonade, 
                                    ingredients: new List<Ingredient> { new(lemon, 9), 
                                                                        new(sugar, 2), 
                                                                        new(water, 7) });
        TestCompany.AddRecipe(basicLemonadeRecipe);
        var expected = basicLemonadeRecipe;
        //Act
        var context = new ActionContext{
                                        RecipeMaker = TestCompany
                                      , Recipe = basicLemonadeRecipe
                                      , QuantityToMake = 1
                                      , };
        TestCompany.MakeRecipe(context);
        var actual = inventory.GetInventoryEntries().Where(entry => entry.good == lemonade).First().GetRecipe();
        //Assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void MakingRecipeUpdatesAcquisitionCostToCostOfIngredients()
    {
        //Arrange
        var TestCompany = EconAgent.Factory.Create(companyName: "TestCompany", 
                                                 company_level: AgentLevelEnum.Beginner, 
                                                 fixedCostStrategy: new BasicFixedCostStrategy(),
                                                 strategy: new BasicGrowthStrategy());
        var inventory = TestCompany.GetInventory();
        var lemonInventoryEntry = new InventoryEntry(lemon, 10, 3,Period);
        var sugarInventoryEntry = new InventoryEntry(sugar, 10, 2,Period);
        var waterInventoryEntry = new InventoryEntry(water, 10, 1,Period);
        inventory.AddGood(lemonInventoryEntry);
        inventory.AddGood(sugarInventoryEntry);
        inventory.AddGood(waterInventoryEntry);
        basicLemonadeRecipe = new Recipe("Basic Lemonade",
                                    product: lemonade, 
                                    ingredients: new List<Ingredient> { new(lemon, 5), 
                                                                        new(sugar, 2), 
                                                                        new(water, 10) });
        TestCompany.AddRecipe(basicLemonadeRecipe);
        var expected = 5*3 + 2*2 + 10*1;
        //Act
        var context = new ActionContext{
                                        RecipeMaker = TestCompany
                                      , Recipe = basicLemonadeRecipe
                                      , QuantityToMake = 1
                                      , };
        TestCompany.MakeRecipe(context);
        var actual = inventory.GetInventoryEntries().Where(entry => entry.good == lemonade).First().Cost;
        //Assert
        Assert.AreEqual(expected, actual);

    }
}
