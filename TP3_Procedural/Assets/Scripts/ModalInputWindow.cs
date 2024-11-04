using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

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
        Terrain.ClearListChunks();

        int dimension = Terrain.dimension;
        int resolution = Terrain.resolution;
        bool CentrerPivot = Terrain.CentrerPivot;
        List<AnimationCurve> patternCurves = Terrain.patternCurves;
        List<Texture2D> patternBrushs = Terrain.patternBrushs;
        float amplitudeDeformation = Terrain.amplitudeDeformation;
        int rayonVoisinage = Terrain.rayonVoisinage;
        int brushSize = Terrain.brushSize;

        Destroy(Terrain);

        GameObject chunk = new GameObject("Field");

        MeshFilter newMeshFilter = chunk.AddComponent<MeshFilter>();
        MeshCollider newMeshCollider = chunk.AddComponent<MeshCollider>();
        MeshRenderer newMeshRenderer = chunk.AddComponent<MeshRenderer>();

        TP3_Terrain terrainChunk = chunk.AddComponent<TP3_Terrain>();

        terrainChunk.dimension = dimension;
        terrainChunk.resolution = resolution;
        terrainChunk.CentrerPivot = CentrerPivot;
        terrainChunk.patternCurves = patternCurves;
        terrainChunk.patternBrushs = patternBrushs;
        terrainChunk.amplitudeDeformation = amplitudeDeformation;
        terrainChunk.rayonVoisinage = rayonVoisinage;
        terrainChunk.brushSize = brushSize;
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
