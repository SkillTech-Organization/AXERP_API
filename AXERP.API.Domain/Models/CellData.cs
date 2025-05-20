using System.Diagnostics.CodeAnalysis;

namespace AXERP.API.Domain.Models;

public sealed record CellData
{
    public string? Value { get; init; }

    public int? Column { get; init; }

    public int? Row { get; init; }

    public sealed class CompareByValue : IEqualityComparer<CellData>
    {
        public bool Equals(CellData? x, CellData? y)
        {
            return x?.Value == y?.Value;
        }

        public int GetHashCode([DisallowNull] CellData obj)
        {
            return obj.Value?.GetHashCode() ?? 0;
        }
    }
}
