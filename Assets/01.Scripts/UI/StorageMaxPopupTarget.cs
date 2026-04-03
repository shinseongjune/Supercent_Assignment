using UnityEngine;

public class StorageMaxPopupTarget : MonoBehaviour
{
    [SerializeField] private ItemStorage storage;
    [SerializeField] private MaxPopupBillboard popup;
    [SerializeField] private Transform popupAnchor;
    [SerializeField] private Vector3 popupOffset = new Vector3(0f, 0.4f, 0f);
    [SerializeField] private float popupDuration = 1.0f;

    private void Reset()
    {
        if (storage == null)
            storage = GetComponent<ItemStorage>();
    }

    public void ShowIfFull()
    {
        if (storage == null || popup == null)
            return;

        if (!storage.IsFull)
            return;

        Transform target = popupAnchor != null ? popupAnchor : transform;
        popup.ShowForSeconds(target, popupOffset, popupDuration);
    }
}