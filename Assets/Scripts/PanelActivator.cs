using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelActivator : MonoBehaviour
{
    public GameObject Panel;
    bool isActive;
    public void OpenPanel()
    {
        if (Panel != null)
        {
            isActive = Panel.activeSelf;

            Panel.SetActive(!isActive);
        }
    }
}
