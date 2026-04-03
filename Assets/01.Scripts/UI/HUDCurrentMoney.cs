using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HUDCurrentMoney : MonoBehaviour
{
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private TextMeshProUGUI text;

    void Update()
    {
        if (playerInventory == null || text == null)
            return;

        int moneyCount = playerInventory.GetCount(Carriable.Type.Money);
        int money = moneyCount * 5;

        text.text = money.ToString();
    }
}
