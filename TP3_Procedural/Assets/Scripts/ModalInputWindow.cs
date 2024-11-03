using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ModalInputWindow : MonoBehaviour
{
    public GameObject modalWindow;
    public TextMeshProUGUI maillageTxt;
    public TMP_InputField dimensionInputField;
    public TMP_InputField resolutionInputField;
    public TP3_Terrain Terrain;

    void Start()
    {
        dimensionInputField.text = Terrain.GetDimension().ToString();
        resolutionInputField.text = Terrain.GetResolution().ToString();

        dimensionInputField.onEndEdit.AddListener(OnDimensionEndEdit);
        resolutionInputField.onEndEdit.AddListener(OnResolutionEndEdit);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F10))
        {
            ToggleModalWindow();
        }
    }

    void ToggleModalWindow()
    {
        modalWindow.SetActive(!modalWindow.activeSelf);
        if (modalWindow.activeSelf)
        {
            UpdateText();
        }
    }

    void UpdateText()
    {
        string text = "Propriétés du maillage en cours :\n" +
                      $"Dimension: {Terrain.GetDimension()}\n" +
                      $"Resolution: {Terrain.GetResolution()}\n";
        maillageTxt.text = text;

        dimensionInputField.text = Terrain.GetDimension().ToString();
        resolutionInputField.text = Terrain.GetResolution().ToString();
    }

    void OnDimensionEndEdit(string input)
    {
        if (int.TryParse(input, out int newDimension))
        {
            Terrain.SetDimension(newDimension);
            UpdateText();
        }
        else
        {
            dimensionInputField.text = Terrain.GetDimension().ToString();
        }
    }

    void OnResolutionEndEdit(string input)
    {
        if (int.TryParse(input, out int newResolution))
        {
            Terrain.SetResolution(newResolution);
            UpdateText();
        }
        else
        {
            resolutionInputField.text = Terrain.GetResolution().ToString();
        }
    }
}
