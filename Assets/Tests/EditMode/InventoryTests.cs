using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;

[TestFixture]
public class InventoryTests
{
    private EconAgent test_company;
    private Good lemon;
    private Good sugar;
    private Good water;

    private readonly TestHelpers testHelpers = new();

    private readonly PriceBand price_band1 = new(.05m, 2.0m);
    private readonly PriceBand price_band2 = new(1.0m, 3.0m);
    private readonly PriceBand price_band3 = new(2.0m, 5.0m);
    private readonly PriceBand price_band4 = new(5.0m, 10.0m);
    const int PeriodIsIrrelevant = 0;
    [SetUp]
    public void Setup()
    {
        //create a test company
        test_company = ScriptableObject.CreateInstance<EconAgent>();
        test_company.Name = "Test Company";

        //create test goods
        //Goods are created with a name, price, price_increment_rate, 
        //          price_increase_threshold, price_decrease_threshold
        //          in that order
         sugar = Good.CreateInstance("Sugar", price_band1);
         lemon = Good.CreateInstance("Lemon", price_band2);
         water = Good.CreateInstance("Water", price_band3);
        

    }
    // A Test behaves as an ordinary method
    [Test]
    public void Single_items_can_be_added()
    {
        //arrange
        var test_inventory = new Inventory();
        var test_inventory_entry = new InventoryEntry(lemon, 1, 1.0m, PeriodIsIrrelevant);

        test_inventory.AddGood(test_inventory_entry);
        var expected = lemon;
        //act
        var actual = test_inventory.GetInventoryEntries().First().good;
        //assert
        Assert.AreEqual(expected, actual);
    }
    [Test]
    public void If_good_with_same_price_already_exists_adds_quantity()
    {
        //arrange
        var test_inventory = new Inventory();
        var existing_test_inventory_entry = new InventoryEntry(lemon, 1, 1.0m, PeriodIsIrrelevant);
        test_inventory.AddGood(existing_test_inventory_entry);
        var new_test_inventory_entry = new InventoryEntry(lemon, 1, 1.0m, PeriodIsIrrelevant);
        var expected_quantity = 2;
        //act
        test_inventory.AddGood(new_test_inventory_entry);
        var actual_quantity = test_inventory.GetInventoryEntries().First().quantity;
        //assert
        Assert.AreEqual(expected_quantity, actual_quantity);

    }

    [Test]
    public void If_inventory_entries_is_not_empty_and_no_matching_good_at_that_price_is_found_adds_new_entry()
    {
        //arrange
        var test_inventory = new Inventory();
        var existing_test_inventory_entry = new InventoryEntry(lemon, 1, 1.0m, PeriodIsIrrelevant);
        test_inventory.AddGood(existing_test_inventory_entry);
        var new_test_inventory_entry = new InventoryEntry(lemon, 1, 2.0m, PeriodIsIrrelevant);
        var expected_number_of_entries = 2;
        var expected_quantity = 1;
        //act
        test_inventory.AddGood(new_test_inventory_entry);
        var actual_number_of_entries = test_inventory.GetInventoryEntries().Count;
        var actual_quantity = test_inventory.GetInventoryEntries().Last().quantity;
        //assert
        Assert.AreEqual(expected_number_of_entries, actual_number_of_entries);
        Assert.AreEqual(expected_quantity, actual_quantity);
    }

    [Test]
    public void If_enough_goods_are_available_at_price_return_valid_entry()
    {
        //arrange
        var test_inventory = new Inventory();
        var test_inventory_entry = new InventoryEntry(lemon, 10, 1.0m,PeriodIsIrrelevant);
        test_inventory.AddGood(test_inventory_entry);
        var lemons_to_sell = 5;
        var expected_entry = new InventoryEntry(lemon, 5, 1.0m, PeriodIsIrrelevant);
        
        //act
        test_inventory.Generate_goods_to_remove(lemon, lemons_to_sell, 1.0m);

        //assert
        var actual_entry = test_inventory.Generate_goods_to_remove(lemon, lemons_to_sell, 1.0m).First();
        Assert.AreEqual(expected_entry.good, actual_entry.good);
    }

    [Test]
    public void If_not_enough_goods_are_available_at_price_return_all_available()
    {
        //arrange
        var test_inventory = new Inventory();
        var test_inventory_entry = new InventoryEntry(lemon, 10, 1.0m,PeriodIsIrrelevant);
        test_inventory.AddGood(test_inventory_entry);
        var lemons_to_sell = 15;
        var expected_entry = new InventoryEntry(lemon, 10, 1.0m, PeriodIsIrrelevant);
        //act
        var actual_entry = test_inventory.Generate_goods_to_remove(lemon, lemons_to_sell, 1.0m).First();
        //assert
        Assert.AreEqual(expected_entry.good, actual_entry.good);
    }

    [Test]
    public void If_not_enough_goods_are_available_at_price_return_all_available_and_most_profitable()
    {
        //arrange
        var test_inventory = new Inventory();
        var test_inventory_entry = new InventoryEntry(lemon, 10, 1.0m,PeriodIsIrrelevant);
        test_inventory.AddGood(test_inventory_entry);
        var test_inventory_entry2 = new InventoryEntry(lemon, 10, 1.9m,PeriodIsIrrelevant);
        test_inventory.AddGood(test_inventory_entry2);
        var lemons_to_sell = 15;
        var price_to_sell_at = 2.0m;
        var expected_inventory= new List<InventoryEntry>(){new(lemon, 10, 1.0m,PeriodIsIrrelevant), new(lemon, 5, 1.9m,PeriodIsIrrelevant)};
        //act
        var actual_inventory = test_inventory.Generate_goods_to_remove(lemon, lemons_to_sell, price_to_sell_at);
        //assert
        if(expected_inventory.Count != actual_inventory.Count)
        {
            // Generate string representations of both lists
            string expectedInventoryString = testHelpers.ListToString(expected_inventory);
            string actualInventoryString = testHelpers.ListToString(actual_inventory);

            Assert.Fail(
                $"Inventory counts do not match:\n" +
                $"Expected count: {expected_inventory.Count}, Actual count: {actual_inventory.Count}\n\n" +
                $"Expected Inventory:\n{expectedInventoryString}\n\n" +
                $"Actual Inventory:\n{actualInventoryString}"
        );
        }
    }
    [Test]
    public void If_no_profitable_goods_are_available_return_empty_list()
    {
        //arrange
        var test_inventory = new Inventory();
        var test_inventory_entry = new InventoryEntry(lemon, 10, 2.0m,PeriodIsIrrelevant);
        test_inventory.AddGood(test_inventory_entry);
        var lemons_to_sell = 15;
        var price_to_sell_at = 1.0m;
        var expected_inventory_count = 0;
        //act
        var actual_inventory = test_inventory.Generate_goods_to_remove(lemon, lemons_to_sell, price_to_sell_at);
        //assert
        Assert.AreEqual(expected_inventory_count, actual_inventory.Count);
    }

    [Test]
    public void If_no_goods_are_available_at_price_do_nothing()
    {
        //arrange
        var test_inventory = new Inventory();
        var test_inventory_entry = new InventoryEntry(lemon, 10, 1.0m,PeriodIsIrrelevant);
        test_inventory.AddGood(test_inventory_entry);
        var lemons_to_sell = 15;
        var price_to_sell_at = 2.0m;
        var expected_inventory_count = 1;
        //act
        var actual_inventory = test_inventory.Generate_goods_to_remove(lemon, lemons_to_sell, price_to_sell_at);
        //assert
        Assert.AreEqual(expected_inventory_count, actual_inventory.Count);
    }

    [Test]
    public void Sell_goods_removes_goods_from_inventory_price_matches_stock_exactly()
    {
        //arrange
        var test_inventory = new Inventory();
        var test_inventory_entry = new InventoryEntry(lemon, 10, 1.0m,PeriodIsIrrelevant);
        test_inventory.AddGood(test_inventory_entry);
        var lemons_to_sell = 10;
        var price_to_sell_at = 1.0m;
        var expected_inventory_count = 0;
        //act
        test_inventory.RemoveGood(lemon, lemons_to_sell, price_to_sell_at);
        var actual_inventory_count = test_inventory.GetInventoryEntries().Count;
        //assert
        Assert.AreEqual(expected_inventory_count, actual_inventory_count);
    }
    [Test]
    public void Sell_goods_removes_goods_from_inventory_takes_from_multiple_stocks()
    {
        //arrange
        var test_inventory = new Inventory();
        var test_inventory_entry = new InventoryEntry(lemon, 10, 1.0m,PeriodIsIrrelevant);
        test_inventory.AddGood(test_inventory_entry);
        var test_inventory_entry2 = new InventoryEntry(lemon, 10, 1.9m,PeriodIsIrrelevant);
        test_inventory.AddGood(test_inventory_entry2);
        var lemons_to_sell = 15;
        var price_to_sell_at = 2.0m;
        var expected_remaining_inventory  = new List<InventoryEntry>(){new(lemon, 5, 1.9m,PeriodIsIrrelevant)};
        //act
        test_inventory.RemoveGood(lemon, lemons_to_sell, price_to_sell_at);
        var actual_remaining_inventory = test_inventory.GetInventoryEntries();
        //assert
        if(expected_remaining_inventory.Count != actual_remaining_inventory.Count)
        {
            // Generate string representations of both lists
            string expectedInventoryString = testHelpers.ListToString(expected_remaining_inventory);
            string actualInventoryString = testHelpers.ListToString(actual_remaining_inventory);
            Assert.Fail(
                $"Inventory counts do not match:\n" +
                $"Expected count: {expected_remaining_inventory.Count}, Actual count: {actual_remaining_inventory.Count}\n\n" +
                $"Expected Inventory:\n{expectedInventoryString}\n\n" +
                $"Actual Inventory:\n{actualInventoryString}"
                    );
        }
    }
    [Test]
    public void Consume_for_recipe_consumes_ingredients_in_inventory()
    {
        //arrange
        var test_inventory = new Inventory();
        var inventory_entry1 = new InventoryEntry(lemon, 10, 1.0m,PeriodIsIrrelevant);
        var inventory_entry2 = new InventoryEntry(sugar, 10, 1.0m,PeriodIsIrrelevant);
        var inventory_entry3 = new InventoryEntry(water, 10, 1.0m,PeriodIsIrrelevant);
        test_inventory.AddGood(inventory_entry1);
        test_inventory.AddGood(inventory_entry2);
        test_inventory.AddGood(inventory_entry3);
        var lemonade = Good.CreateInstance("Lemonade", price_band4);
        
        var lemonade_recipe = new Recipe(RecipeName: "Basic Lemonade",
                                         product: lemonade, 
                                        ingredients: new List<Ingredient> { new(lemon, 9), new(sugar, 2), new(water, 7) });
        var quantity = 1;
        var expected_inventory = new List<InventoryEntry> { new(lemon, 1, 1.0m,PeriodIsIrrelevant), new(sugar, 8, 1.0m,PeriodIsIrrelevant), new(water, 3, 1.0m,PeriodIsIrrelevant) };
        //act
        test_inventory.Consume_for_recipe(lemonade_recipe, quantity);
        var actual_inventory = test_inventory.GetInventoryEntries();
        //assert
        if(expected_inventory.Count != actual_inventory.Count)
        {
            // Generate string representations of both lists
            string expectedInventoryString = testHelpers.ListToString(expected_inventory);
            string actualInventoryString = testHelpers.ListToString(actual_inventory);
            Assert.Fail(
                $"Inventory counts do not match:\n" +
                $"Expected count: {expected_inventory.Count}, Actual count: {actual_inventory.Count}\n\n" +
                $"Expected Inventory:\n{expectedInventoryString}\n\n" +
                $"Actual Inventory:\n{actualInventoryString}"
                    );
        }
    }

    [Test]
    public void TryConsumeGood_returns_0_if_good_is_available_at_specified_quantity()
    {
        //arrange
        var test_inventory = new Inventory();
        var test_inventory_entry = new InventoryEntry(lemon, 10, 1.0m,PeriodIsIrrelevant);
        test_inventory.AddGood(test_inventory_entry);
        var quantity = 10;
        var expected = 0;
        //act
        var actual = test_inventory.TryConsumeGood("Lemon", quantity);
        //assert
        Assert.AreEqual(expected, actual);
    }
    
    [Test]
    public void TryConsumeGood_returns_remaining_quantity_if_good_is_not_available_at_specified_quantity()
    {
        //arrange
        var test_inventory = new Inventory();
        var test_inventory_entry = new InventoryEntry(lemon, 10, 1.0m,PeriodIsIrrelevant);
        test_inventory.AddGood(test_inventory_entry);
        var quantity = 15;
        var expected = 5;
        //act
        var actual = test_inventory.TryConsumeGood("Lemon", quantity);
        //assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void GetInventoryEntryByGood_returns_correct_entry_if_only_one_entry_for_good_exists()
    {
        //arrange
        var test_inventory = new Inventory();
        var test_inventory_entry = new InventoryEntry(lemon, 10, 1.0m,PeriodIsIrrelevant);
        var test_inventory_entry2 = new InventoryEntry(sugar, 10, 1.0m,PeriodIsIrrelevant);
        test_inventory.AddGood(test_inventory_entry);
        test_inventory.AddGood(test_inventory_entry2);
        var expected = test_inventory_entry;
        //act
        var actual = test_inventory.GetInventoryEntriesByGood("Lemon").FirstOrDefault();
        //assert
        Assert.AreEqual(expected, actual);
    }

#region Perishability
[Test]
public void Perishable_goods_are_removed_from_inventory_after_expiry()
{
    //arrange
    var test_inventory = new Inventory();
    lemon.ExpiresAfterPeriods = 1;
    var test_inventory_entry = new InventoryEntry(lemon, 10, 1.0m, 1);
    test_inventory.AddGood(test_inventory_entry);
    const int expectedItemCount = 0;
    //act
    test_inventory.ExpireGoods(2);
    var actualItemCount = test_inventory.GetInventoryEntries().Count;
    //assert
    Assert.AreEqual(expectedItemCount, actualItemCount);
}   

#endregion
}
