namespace HomeCharts.Domain.Model;

public static class ExpenseCategoryCatalog
{
    private static readonly string[] Names =
    [
        "None",
        "Cafe",
        "Shopping",
        "Grocery",
        "Education",
        "Car",
        "Entertainment",
        "House",
        "Medicine",
        "Rent",
        "Utility",
        "Smoke",
        "Travel",
        "Transport",
        "Services",
        "Income",
        "Transfer",
        "Others"
    ];

    public static IReadOnlyList<string> OrderedNames => Names;

    public static IReadOnlyList<T> SelectAssignable<T>(IEnumerable<T> categories, Func<T, string> nameOf)
    {
        var source = categories.ToArray();
        var selected = new List<T>();
        foreach (var name in Names)
        {
            var match = source.FirstOrDefault(item =>
                string.Equals(nameOf(item), name, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                continue;
            }

            if (selected.Any(existing => string.Equals(nameOf(existing), nameOf(match), StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            selected.Add(match);
        }

        return selected;
    }
}
