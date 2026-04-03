using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UICurrentPrisoner : MonoBehaviour
{
    [SerializeField] private PrisonManager prisonManager;
    [SerializeField] private TextMeshProUGUI text;

    private void Update()
    {
        if (prisonManager == null || text == null)
            return;

        int currentCount = prisonManager.OccupantCount;
        int capacity = prisonManager.Capacity;

        string str = string.Format("{0} / {1}", currentCount, capacity);
        text.text = str;
    }
}
