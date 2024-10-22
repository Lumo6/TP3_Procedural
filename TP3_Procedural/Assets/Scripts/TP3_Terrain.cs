using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(MeshRenderer))]

public class TP3_Terrain : MonoBehaviour
{
    private enum TypeAction {DEFORMATION_HAUT, DEFORMATION_BAS};
    private TypeAction typeAction;

    public int dimension, resolution;
    public bool CentrerPivot;
    public float amplitudeDeformation, rayonVoisinage;
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
    public LayerMask maskPickingTerrain;

    private List<Voisin> listeVoisinsSel;

    private int numPatternCurveEnCours;

    private struct Voisin
    {
        public int indice;
        public float distance;

        public Voisin(int ind, float dist)
            {
                indice = ind;
                distance = dist;
            }
    }

    private static List<GameObject> chunks = new List<GameObject>();

    private float randomColorTimer = 3.0f;

    private bool waitingForDirection = false; // Pour savoir si on attend une flèche
    private Vector3 newChunkDirection = Vector3.zero; // Pour stocker la direction du nouveau chunk


    private void Start()
    {
        p_meshFilter = GetComponent<MeshFilter>();
        p_meshCollider = GetComponent<MeshCollider>();
        p_meshRenderer = GetComponent<MeshRenderer>();
        
        p_cam = Camera.main;
        maskPickingTerrain = LayerMask.NameToLayer("Field");

        CreateField();

        chunks.Add(this.gameObject);
    }

    void Update()
    {
        randomColorTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.F5)) 
        {
            waitingForDirection = true; // On entre en mode d'attente pour une direction
            newChunkDirection = Vector3.zero; // Réinitialise la direction
            Debug.Log("Appuyez sur une flèche pour créer un chunk.");
        }

        // Si on attend une direction, on vérifie les touches de direction
        if (waitingForDirection)
        {
            newChunkDirection = GetArrowDirection();

            // Dès qu'on a une direction valide, on crée le chunk
            if (newChunkDirection != Vector3.zero) 
            {
                CreateChunk(newChunkDirection);
                waitingForDirection = false; // On sort du mode attente après la création du chunk
            }
        }

        if (Input.GetKeyDown(KeyCode.RightAlt)) {
            amplitudeDeformation = amplitudeDeformation - 0.01f > 0 ? amplitudeDeformation - 0.01f : 0;
        }
        if (Input.GetKeyDown(KeyCode.LeftAlt)) {
            amplitudeDeformation += 0.01f;
        }
        if (Input.GetKeyDown(KeyCode.Plus)) {
            rayonVoisinage += 5;
        }
        if (Input.GetKeyDown(KeyCode.Minus)) {
            rayonVoisinage = rayonVoisinage - 5 > 0 ? rayonVoisinage - 5 : 0;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            for(int i = 1; i < chunks.Count; i++) {
                chunks[i].GetComponent<MeshRenderer>().material.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            }
            randomColorTimer = 3.0f;
        } 
        
        if (randomColorTimer <= 0.0f && chunks.Count > 0)
        {
            Color mainColor = chunks[0].GetComponent<MeshRenderer>().material.color;
            for(int i = 1; i < chunks.Count; i++) {
                chunks[i].GetComponent<MeshRenderer>().material.color = mainColor;
            }
        }

        majHUD(); // maj des informations affich�es en temps r�el

        // si pas de picking sur le terrain, pas de d�formation, on quitte sans rien faire
        if (!Physics.Raycast(p_cam.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, maskPickingTerrain)) return;

        // RECHERCHE du VERTEx s�lectionn� par le picking = CIBLE
        cible = RechercherVertexCible(hit);
        // RECHERCHE des voisins du vertex CIBLE , ceux � une distance <= voisinnage
        // (param�tre public qui sera modifiable en temps r�el)
        listeVoisinsSel = RechercherVoisins(cible);

        if (Input.GetMouseButton(0)) {
            if (Input.GetKey(KeyCode.LeftControl)) {
                typeAction = TypeAction.DEFORMATION_BAS;
            } else {
                typeAction = TypeAction.DEFORMATION_HAUT;
            }

            switch (typeAction) {
                case TypeAction.DEFORMATION_HAUT:
                    AppliquerDeformation(listeVoisinsSel, Vector3.up);
                    break;
                case TypeAction.DEFORMATION_BAS:
                    AppliquerDeformation(listeVoisinsSel, Vector3.down);
                    break;
            }
        }
    }

    private Vector3 GetArrowDirection()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            return new Vector3(-dimension, 0, 0);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            return new Vector3(dimension, 0, 0);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            return new Vector3(0, 0, dimension);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            return new Vector3(0, 0, -dimension);
        }

        return Vector3.zero; // Retourne zéro si aucune touche fléchée n'est pressée
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

    private void CreateChunk(Vector3 offset)
    {
        bool canBeCreate = true;
        foreach(GameObject c in chunks)
        {
            if (c.transform.position == this.transform.position + offset) {
                canBeCreate = false;
                break;
            }
        }

        if(canBeCreate) 
        {
            GameObject chunk = new GameObject("TerrainChunk");
            TP3_Terrain terrainChunk = chunk.AddComponent<TP3_Terrain>();
            terrainChunk.dimension = this.dimension;
            terrainChunk.resolution = this.resolution; 
            terrainChunk.CentrerPivot = this.CentrerPivot;
            terrainChunk.patternCurves = this.patternCurves;

            chunk.transform.position = this.transform.position + offset;
        }
    }

    private void RecalculerMeshCollider()
    {
        p_meshCollider.sharedMesh = null;
        p_meshCollider.sharedMesh = p_meshFilter.mesh;
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
        List<Voisin> listV = new List<Voisin>();
        for(int i = 0; i < p_vertices.Length; i++){
            if(Vector3.Distance(cible,p_vertices[i]) < rayonVoisinage){
                listV.Add(new Voisin(i, Vector3.Distance(cible, p_vertices[i])));
            }
        }

        return listV;
    }

    void AppliquerDeformation(List<Voisin> listeVoisinsSel, Vector3 orientation) 
    {
        float _force = amplitudeDeformation * patternCurves[numPatternCurveEnCours].Evaluate(0);
        for(int i = 0;i < p_vertices.Length; i++){
            if(p_vertices[i] == cible){
                p_vertices[i] += orientation * _force;
            }
        }
        foreach (Voisin softSel in listeVoisinsSel){
            _force = amplitudeDeformation * patternCurves[numPatternCurveEnCours].Evaluate(softSel.distance / rayonVoisinage);
            p_vertices[softSel.indice] += orientation * _force;
        }
        
        p_mesh.vertices = p_vertices;
        p_mesh.RecalculateNormals();
        p_meshFilter.mesh = p_mesh;
        RecalculerMeshCollider();


        // � ce stade , on connait les voisins s�lectionn�s (appel � RechercherVoisins ()�
        // ce sont les vertices qui sont dans le rayon de voisinage r autour du vertex cible
        // Pour chaque voisin (foreach), on connait sa distance d au vertex cible
        // le rapport c = d/r renvoie une valeur entre 0 et 1
        // c devient une abscisse � utiliser avec la courbe d'animation avec evaluate(c) pour
        // obtenir une force de d�formation fonction de la distance au vertex s�lectionn�
        // rem : je g�re ici un tableau publix d'AnimationCurve : patternCurves[]
        // rem : ce qui permet d'un pattern � un autre (modificaion de numPatternCurveEnCours)
        // cette force est multipli� par une amplitudeDeformation
        // rem c'est un param�tre public de la classe qui pourra �tre modifi� en temps r�el // cette force est multipli� par orientation (bas ou haut) selon qu'on creuse ou �l�ve
    }
}