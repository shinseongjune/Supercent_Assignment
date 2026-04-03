using UnityEngine;

public class InventoryMaxPopupTarget : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private Carriable.Type itemType;
    [SerializeField] private MaxPopupBillboard popup;
    [SerializeField] private Transform popupAnchor;
    [SerializeField] private Vector3 popupOffset = new Vector3(0f, 1.0f, 0f);
    [SerializeField] private float popupDuration = 1.0f;

    public void ShowIfFull()
    {
        if (inventory == null || popup == null)
            return;

        if (inventory.GetCount(itemType) < inventory.GetCapacity(itemType))
            return;

        Transform target = popupAnchor != null ? popupAnchor : transform;
        popup.ShowForSeconds(target, popupOffset, popupDuration);
    }
}