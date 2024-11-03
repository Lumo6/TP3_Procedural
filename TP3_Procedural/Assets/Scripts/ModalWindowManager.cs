using UnityEngine;
using TMPro;


public class ModalWindowManager : MonoBehaviour
{
    public GameObject modalWindow;
    public TextMeshProUGUI maillageTxt;
    public TP3_Terrain Terrain;

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
        if (modalWindow.activeSelf)
        {
            UpdateText();
        }
    }

    void UpdateText()
    {
        string text = "Propriétés du maillage en cours :\n" +
                      $"Dimension: {Terrain.GetDimension()}\n" +
                      $"Resolution: {Terrain.GetResolution()}\n" +
                      $"Brush Size: {Terrain.GetBrushSize()}\n" +
                      $"Amplitude Deformation: {Terrain.GetAmplitudeDeformation()}\n" +
                      $"Rayon Voisinage: {Terrain.GetRayonVoisinage()}\n" +
                      $"Nombre de Sommets: {Terrain.GetVerticesCount()}\n" +
                      $"Nombre de Normales: {Terrain.GetNormalsCount()}\n" +
                      $"Nombre de Triangles: {Terrain.GetTrianglesCount() / 3}\n" +
                      $"Nombre de Chunks: {TP3_Terrain.GetChunksCount()}\n" +
                      $"Num�ro du Pattern Curve en Cours: {Terrain.GetNumPatternCurveEnCours()}\n" +
                      $"Num�ro du Pattern Brush en Cours: {Terrain.GetNumPatternBrushEnCours()}\n";
        maillageTxt.text = text;
    }
}
