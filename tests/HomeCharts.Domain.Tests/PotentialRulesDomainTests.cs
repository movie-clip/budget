using HomeCharts.Domain.Categorization;
using HomeCharts.Domain.Model;

namespace HomeCharts.Domain.Tests;

/// <summary>Logic moved down from MainWindowViewModel by the import-rules redesign.</summary>
[TestClass]
public sealed class DescriptionPrefixStripperTests
{
    [TestMethod]
    public void Strip_NoPrefixes_ReturnsDescriptionUnchanged()
    {
        Assert.AreEqual("COMPRA TARJ. OPENAI", DescriptionPrefixStripper.Strip("COMPRA TARJ. OPENAI", []));
    }

    [TestMethod]
    public void Strip_MatchingPrefix_RemovesItAndTrimsSeparators()
    {
        Assert.AreEqual("OPENAI", DescriptionPrefixStripper.Strip("COMPRA TARJ. OPENAI", ["COMPRA TARJ."]));
        Assert.AreEqual("SHOP", DescriptionPrefixStripper.Strip("TRF: - SHOP", ["TRF"]));
    }

    [TestMethod]
    public void Strip_LongestPrefixWinsOverShorterOverlappingPrefix()
    {
        // Shorter listed first: if order mattered the result would be "TARJ. OPENAI".
        var result = DescriptionPrefixStripper.Strip("COMPRA TARJ. OPENAI", ["COMPRA", "COMPRA TARJ."]);

        Assert.AreEqual("OPENAI", result);
    }

    [TestMethod]
    public void Strip_RepeatedPrefixes_AreAllRemoved()
    {
        Assert.AreEqual("SHOP", DescriptionPrefixStripper.Strip("PAGO - MOVIL: SHOP", ["MOVIL", "PAGO"]));
    }

    [TestMethod]
    public void Strip_IsCaseInsensitive()
    {
        Assert.AreEqual("MERCADONA", DescriptionPrefixStripper.Strip("COMPRA TARJ. MERCADONA", ["compra tarj."]));
    }

    [TestMethod]
    public void Strip_BlankPrefixes_AreIgnored()
    {
        Assert.AreEqual("MERCADONA", DescriptionPrefixStripper.Strip("MERCADONA", ["", "   "]));
    }

    [TestMethod]
    public void Strip_EverythingStripped_ReturnsOriginalDescription()
    {
        Assert.AreEqual("COMPRA TARJ.", DescriptionPrefixStripper.Strip("COMPRA TARJ.", ["COMPRA TARJ."]));
    }
}

[TestClass]
public sealed class ExpenseCategoryCatalogTests
{
    [TestMethod]
    public void OrderedNames_StartsWithNoneAndEndsWithOthers()
    {
        Assert.AreEqual("None", ExpenseCategoryCatalog.OrderedNames[0]);
        Assert.AreEqual("Others", ExpenseCategoryCatalog.OrderedNames[^1]);
        CollectionAssert.Contains(ExpenseCategoryCatalog.OrderedNames.ToList(), "Grocery");
    }

    [TestMethod]
    public void SelectAssignable_FollowsCatalogOrderNotInputOrder()
    {
        var input = new[] { "Car", "Grocery", "None" };

        var result = ExpenseCategoryCatalog.SelectAssignable(input, static name => name);

        CollectionAssert.AreEqual(new[] { "None", "Grocery", "Car" }, result.ToList());
    }

    [TestMethod]
    public void SelectAssignable_MatchesNamesCaseInsensitively()
    {
        var input = new[] { "grocery" };

        var result = ExpenseCategoryCatalog.SelectAssignable(input, static name => name);

        CollectionAssert.AreEqual(new[] { "grocery" }, result.ToList());
    }

    [TestMethod]
    public void SelectAssignable_DuplicateNames_KeepsOnlyTheFirst()
    {
        var first = new Category { Name = "Grocery" };
        var second = new Category { Name = "GROCERY" };

        var result = ExpenseCategoryCatalog.SelectAssignable([first, second], static category => category.Name);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(first.Id, result[0].Id);
    }

    [TestMethod]
    public void SelectAssignable_UnknownNames_AreSkipped()
    {
        var input = new[] { "Uncategorized", "Grocery", "Weird" };

        var result = ExpenseCategoryCatalog.SelectAssignable(input, static name => name);

        CollectionAssert.AreEqual(new[] { "Grocery" }, result.ToList());
    }
}
