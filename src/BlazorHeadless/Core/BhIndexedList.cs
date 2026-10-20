using System.Collections;

namespace BlazorHeadless;

/// <summary>A value paired with its position in a list (used for virtualized option lists).</summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly record struct BhIndexed<T>(int Index, T Value);

/// <summary>
/// A zero-allocation, index-preserving view over a list, used to feed
/// <c>Virtualize</c> while keeping each item's position (for ids and
/// <c>aria-posinset</c>).
/// </summary>
internal sealed class BhIndexedList<T> : IReadOnlyList<BhIndexed<T>>, ICollection<BhIndexed<T>>
{
    private readonly IReadOnlyList<T> _items;

    public BhIndexedList(IReadOnlyList<T> items) => _items = items;

    public int Count => _items.Count;

    public bool IsReadOnly => true;

    public BhIndexed<T> this[int index] => new(index, _items[index]);

    public IEnumerator<BhIndexed<T>> GetEnumerator()
    {
        for (var i = 0; i < _items.Count; i++)
            yield return new BhIndexed<T>(i, _items[i]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Contains(BhIndexed<T> item) => item.Index >= 0 && item.Index < _items.Count;

    public void CopyTo(BhIndexed<T>[] array, int arrayIndex)
    {
        for (var i = 0; i < _items.Count; i++)
            array[arrayIndex + i] = new BhIndexed<T>(i, _items[i]);
    }

    public void Add(BhIndexed<T> item) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public bool Remove(BhIndexed<T> item) => throw new NotSupportedException();
}
