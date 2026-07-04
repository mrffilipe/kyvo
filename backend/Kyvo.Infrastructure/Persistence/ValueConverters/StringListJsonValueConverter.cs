using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kyvo.Infrastructure.Persistence.ValueConverters;

public static class StringListJsonValueConverter
{
    public static ValueComparer<ICollection<string>> Comparer { get; } = new(
        (left, right) => ListsEqual(left, right),
        list => ListHashCode(list),
        list => list.ToList());

    public static ValueConverter<ICollection<string>, string> Converter { get; } = new(
        list => Serialize(list),
        json => Deserialize(json));

    private static bool ListsEqual(ICollection<string>? left, ICollection<string>? right)
    {
        return (left ?? new List<string>()).SequenceEqual(right ?? new List<string>(), StringComparer.Ordinal);
    }

    private static int ListHashCode(ICollection<string> list)
    {
        return list.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode(StringComparison.Ordinal)));
    }

    private static string Serialize(ICollection<string> list)
    {
        return JsonSerializer.Serialize(list);
    }

    private static List<string> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
