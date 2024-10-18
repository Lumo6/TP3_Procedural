using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(MeshRenderer))]

public class TP3_Terrain : MonoBehaviour
{
    private enum TypeAction {DEFORMATION_HAUT, DEFORMATION_BAS};
    private TypeAction typeAction;

    public int dimension, resolution;
    public bool CentrerPivot;
    public float amplitudeDeformation;
    public float rayonVoisinage;
    public List<AnimationCurve> patternCurves;

    private Mesh p_mesh;
    private MeshFilter p_meshFilter;
    private MeshCollider p_meshCollider;
    private MeshRenderer p_meshRenderer;

    private Vector3[] p_vertices, p_normals;
    private int[] p_triangles;
    public Camera p_cam;

    private Vector3 cible;
    private RaycastHit hit;
    private int maskPickingTerrain;

    private List<Vector3> voisins;
    private List<Voisin> listeVoisinsSel;

    private int numPatternCurveEnCours;

    private struct Voisin
    {
        public int indice;
        public float distance;
    }

    private void Start()
    {
        p_meshFilter = GetComponent<MeshFilter>();
        p_meshCollider = GetComponent<MeshCollider>();
        p_meshRenderer = GetComponent<MeshRenderer>();
        
        p_cam = Camera.main;
        maskPickingTerrain = LayerMask.GetMask("Terrain");

        CreateField();
    }

    void Update()
    {
        // si pas de picking sur le terrain, pas de déformation, on quitte sans rien faire
        if (!Physics.Raycast(p_cam.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, maskPickingTerrain)) return;

        // RECHERCHE du VERTEx sélectionné par le picking = CIBLE
        cible = RechercherVertexCible(hit);
        // RECHERCHE des voisins du vertex CIBLE , ceux à une distance <= voisinnage
        // (paramètre public qui sera modifiable en temps réel)
        listeVoisinsSel = RechercherVoisins(cible);

        switch (typeAction) // typeAction modifié en TR par traitement des évenements claviers
        { // permettra de choisir entre creuser ou élever le terrain
            case TypeAction.DEFORMATION_HAUT:
                AppliquerDeformation(listeVoisinsSel, Vector3.up); // appliquer la modification locale du terrain
                break;
            case TypeAction.DEFORMATION_BAS:
                AppliquerDeformation(listeVoisinsSel, Vector3.down);
                break;
        }
        majHUD(); // maj des informations affichées en temps réel } 
    }

    void CreateField()
    {
        p_mesh = new Mesh();
        p_mesh.Clear();
        p_mesh.name = "ProceduralTerrainMESH";

        p_vertices = new Vector3[resolution * resolution];
        p_normals = new Vector3[p_vertices.Length];
        p_triangles = new int[3 * 2 * (resolution - 1) * (resolution - 1)];

        int indice_vertex = 0;
        for (int j = 0; j < resolution; j++)
        {
            for (int i = 0; i < resolution; i++)
            {
                p_vertices[indice_vertex] = new Vector3(i * ((float)dimension / (resolution - 1)), 0, j * ((float)dimension / (resolution - 1)));
                p_normals[indice_vertex] = new Vector3(0, 1, 0);
                indice_vertex++;
            }
        }

        if (CentrerPivot)
        {
            Vector3 decalCentrage = new Vector3(dimension / 2f, 0, dimension / 2f);
            for (int k = 0; k < p_vertices.Length; k++)
                p_vertices[k] -= decalCentrage;
        }

        int indice_triangle = 0;
        for (int j = 0; j < resolution - 1; j++)
        {
            for (int i = 0; i < resolution - 1; i++)
            {
                int num_vertex = j * resolution + i;

                p_triangles[indice_triangle] = num_vertex;
                p_triangles[indice_triangle + 1] = num_vertex + resolution;
                p_triangles[indice_triangle + 2] = num_vertex + 1;

                indice_triangle += 3;

                p_triangles[indice_triangle] = num_vertex + 1;
                p_triangles[indice_triangle + 1] = num_vertex + resolution;
                p_triangles[indice_triangle + 2] = num_vertex + resolution + 1;

                indice_triangle += 3;
            }
        }

        p_mesh.vertices = p_vertices;
        p_mesh.normals = p_normals;
        p_mesh.triangles = p_triangles;

        p_meshFilter.mesh = p_mesh;

        p_meshCollider.sharedMesh = null;
        p_meshCollider.sharedMesh = p_mesh;

        p_meshRenderer.material.color = Color.green;
    }

    private void RecalculerMeshCollider()
    {
        p_meshCollider.sharedMesh = null;
        p_meshCollider.sharedMesh = p_meshFilter.mesh;
    }
    private void AppliquerDeformation(Vector3 orientation)
    { // orientation nous indique s'il faut creuser(y=-1) ou élever(y=1) Vector3.UP ou DOWN

        float _force;
        foreach (Voisin softSel in listeVoisinsSel)
        {
            _force = amplitudeDeformation * patternCurves[numPatternCurveEnCours].Evaluate(softSel.distance / rayonVoisinage);
            p_vertices[softSel.indice] += orientation * _force;
        }
        // à ce stade , on connait les voisins sélectionnés (appel à RechercherVoisins ()…
        // ce sont les vertices qui sont dans le rayon de voisinage r autour du vertex cible
        // Pour chaque voisin (foreach), on connait sa distance d au vertex cible
        // le rapport c = d/r renvoie une valeur entre 0 et 1
        // c devient une abscisse à utiliser avec la courbe d'animation avec evaluate(c) pour
        // obtenir une force de déformation fonction de la distance au vertex sélectionné
        // rem : je gère ici un tableau publix d'AnimationCurve : patternCurves[]
        // rem : ce qui permet d'un pattern à un autre (modificaion de numPatternCurveEnCours)
        // cette force est multiplié par une amplitudeDeformation
        // rem c'est un paramètre public de la classe qui pourra être modifié en temps réel // cette force est multiplié par orientation (bas ou haut) selon qu'on creuse ou éléve
        p_mesh.vertices = p_vertices;
        p_mesh.RecalculateNormals(); // pourrait être optimisé / voir plus tard p_meshFilter.mesh = p_mesh;
        RecalculerMeshCollider(); // pourrait être optimisé / voir plus tard}
    }

    void majHUD()
    {

    }

    Vector3 RechercherVertexCible(RaycastHit hit)
    {
        float min = Mathf.Infinity;
        Vector3 vmin = new Vector3();
        for (int i = 0; i < 3; i++)
        {
            if (min >= Vector3.Distance(p_vertices[p_triangles[hit.triangleIndex * 3 + i]], hit.point)) {
                vmin = p_vertices[p_triangles[hit.triangleIndex * 3 + i]];
                min = Vector3.Distance(p_vertices[p_triangles[hit.triangleIndex * 3 + i]], hit.point);
            }
        }

        return vmin;
    }

    List<Voisin> RechercherVoisins(Vector3 cible)
    {
        return new List<Voisin>();
    }

    void AppliquerDeformation(List<Voisin> listeVoisinsSel, Vector3 direction) 
    {
    
    }
}