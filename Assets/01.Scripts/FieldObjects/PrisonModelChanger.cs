using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrisonModelChanger : MonoBehaviour
{
    [SerializeField] private PrisonManager prisonManager;

    [SerializeField] private GameObject model1;
    [SerializeField] private GameObject model2;

    [SerializeField] private Material redMat;

    public void Event_ChangeCellColor()
    {
        model1.GetComponent<Renderer>().material = redMat;
    }

    public void Event_ChangeModel()
    {
        model1.SetActive(false);
        model2.SetActive(true);

        prisonManager?.AddCapacity(10);
    }
}
