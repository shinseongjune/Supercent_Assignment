using System.Collections.Generic;
using UnityEngine;

public enum StorageLayoutType
{
    SingleVertical,     // 한 자리에서 위로만
    TwoColumnsVertical, // 광석 2자리
    Rect6Grid           // 돈 6칸
}

public class ItemStorage : MonoBehaviour
{
    [Header("Accept")]
    [SerializeField] private Carriable.Type acceptedType;
    [SerializeField] private int capacity = 10;

    [Header("Layout")]
    [SerializeField] private Transform layoutRoot;
    [SerializeField] private StorageLayoutType layoutType;
    [SerializeField] private Vector3 itemEulerRotation = Vector3.zero;
    [SerializeField] private Vector3 cellSpacing = new Vector3(0.4f, 0.25f, 0.4f);

    private readonly List<Carriable> storedItems = new();

    public Carriable.Type AcceptedType => acceptedType;
    public int Capacity => capacity;
    public int Count => storedItems.Count;
    public bool IsFull => storedItems.Count >= capacity;
    public bool IsEmpty => storedItems.Count == 0;
    public IReadOnlyList<Carriable> StoredItems => storedItems;

    private Vector3 GetLocalPosition(int index)
    {
        switch (layoutType)
        {
            case StorageLayoutType.SingleVertical:
                {
                    int layer = index;
                    return new Vector3(0f, cellSpacing.y * layer, 0f);
                }

            case StorageLayoutType.TwoColumnsVertical:
                {
                    int column = index % 2;
                    int layer = index / 2;

                    float x = (column == 0) ? -cellSpacing.x * 0.5f : cellSpacing.x * 0.5f;
                    float y = cellSpacing.y * layer;
                    return new Vector3(x, y, 0f);
                }

            case StorageLayoutType.Rect6Grid:
                {
                    int cols = 3;
                    int col = index % cols;
                    int row = index / cols;

                    return new Vector3(
                        (col - 1) * cellSpacing.x,
                        0f,
                        row * cellSpacing.z
                    );
                }
        }

        return Vector3.zero;
    }

    public bool CanStore(Carriable item)
    {
        if (item == null) return false;
        if (IsFull) return false;
        if (item.type != acceptedType) return false;
        return true;
    }

    public bool TryStore(Carriable item)
    {
        if (!CanStore(item))
            return false;

        storedItems.Add(item);
        RefreshLayout();
        return true;
    }

    public bool TryTakeLast(out Carriable item)
    {
        item = null;

        if (storedItems.Count == 0)
            return false;

        int lastIndex = storedItems.Count - 1;
        item = storedItems[lastIndex];
        storedItems.RemoveAt(lastIndex);

        RefreshLayout();
        return true;
    }

    private void RefreshLayout()
    {
        Transform root = layoutRoot != null ? layoutRoot : transform;

        for (int i = 0; i < storedItems.Count; i++)
        {
            Carriable item = storedItems[i];
            Vector3 localPos = GetLocalPosition(i);
            Vector3 localEuler = itemEulerRotation;

            item.Taken(root, localPos, localEuler, false);
        }
    }
}