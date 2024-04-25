using UnityEngine;

public class PanelActivator : MonoBehaviour
{
	public GameObject Panel;
	bool isActive;
	public void OpenPanel()
	{
		if (Panel == null)
		{
			return;
		}
		isActive = Panel.activeSelf;
		Panel.SetActive(!isActive);
	}
}
