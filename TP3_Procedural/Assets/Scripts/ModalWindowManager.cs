using UnityEngine;

public class ModalWindowManager : MonoBehaviour
{
    public GameObject modalWindow;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleModalWindow();
        }
    }

    void ToggleModalWindow()
    {
        modalWindow.SetActive(!modalWindow.activeSelf);
    }
}
