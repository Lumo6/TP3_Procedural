using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(MeshRenderer))]

public class TP3_Terrain : MonoBehaviour
{
    private enum TypeAction {DEFORMATION_HAUT, DEFORMATION_BAS};
    private TypeAction typeAction = TypeAction.DEFORMATION_HAUT;

    private enum TypeDeformation {CURVE, BRUSH}
    private TypeDeformation typeDeformation = TypeDeformation.CURVE;

    public int dimension, resolution, brushSize;
    public bool CentrerPivot;
    public float amplitudeDeformation;
    public int rayonVoisinage;
    public List<AnimationCurve> patternCurves;
    public List<Texture2D> patternBrushs;

    private Mesh p_mesh;
    private MeshFilter p_meshFilter;
    private MeshCollider p_meshCollider;
    private MeshRenderer p_meshRenderer;

    private Vector3[] p_vertices, p_normals;
    private int[] p_triangles;
    private Camera p_cam;

    private Vector3 cible;
    private RaycastHit hit;
    public LayerMask maskPickingTerrain;

    private List<Voisin> listeVoisinsSel;

    private static int numPatternCurveEnCours = 0;
    private static int numPatternBrushEnCours = 0;

    private struct Voisin
    {
        public int indice;
        public float distance;
        public GameObject terrainAssocie;
        public Voisin(int ind, float dist, GameObject terrain)
            {
                indice = ind;
                distance = dist;
                terrainAssocie = terrain;
            }
    }

    private static List<GameObject> chunks = new List<GameObject>();
    private static int nbChunks = 0;

    private float randomColorTimer = 3.0f;

    private bool waitingForDirection = false; // Pour savoir si on attend une flèche
    private Vector3 newChunkDirection = Vector3.zero; // Pour stocker la direction du nouveau chunk

    private Stack<Vector3[]> undoStack;
    private Stack<Vector3[]> redoStack;


    public TP3_Terrain(
        int dimension, 
        int resolution, 
        bool CentrerPivot, 
        List<AnimationCurve> patternCurves,
        List<Texture2D> patternBrushs,
        float amplitudeDeformation,
        int rayonVoisinage,
        int brushSize,
        LayerMask maskPickingTerrain
        ) 
    {
        this.dimension = dimension;
        this.resolution = resolution;
        this.CentrerPivot = CentrerPivot;
        this.patternCurves = patternCurves;
        this.patternBrushs = patternBrushs;
        this.amplitudeDeformation = amplitudeDeformation;
        this.rayonVoisinage = rayonVoisinage;
        this.brushSize = brushSize;
        this.maskPickingTerrain = maskPickingTerrain;
    }

    private void Start()
    {
        undoStack = new Stack<Vector3[]>();
        redoStack = new Stack<Vector3[]>();

        p_meshFilter = GetComponent<MeshFilter>();
        p_meshCollider = GetComponent<MeshCollider>();
        p_meshRenderer = GetComponent<MeshRenderer>();
        
        p_cam = Camera.main;
        maskPickingTerrain = LayerMask.NameToLayer("Field");

        CreateField();
        SetHeightVertices();

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

        if (Input.GetKeyDown(KeyCode.L))
        {
            switch(typeDeformation)
            {
                case TypeDeformation.CURVE:
                    typeDeformation = TypeDeformation.BRUSH;
                    break;
                case TypeDeformation.BRUSH:
                    typeDeformation = TypeDeformation.CURVE;
                    break;
            }

            Debug.Log(typeDeformation);
        }

        if (Input.GetKeyDown(KeyCode.RightAlt)) {
            amplitudeDeformation = amplitudeDeformation - 0.01f > 0 ? amplitudeDeformation - 0.01f : 0.01f;
        }
        if (Input.GetKeyDown(KeyCode.LeftAlt)) {
            amplitudeDeformation = amplitudeDeformation + 0.01f < 5 ? amplitudeDeformation + 0.01f : 5.0f;
        }
        if (Input.GetKeyDown(KeyCode.KeypadPlus)) {
            rayonVoisinage = rayonVoisinage + 5 < 100 ? rayonVoisinage + 5 : 100;
        }
        if (Input.GetKeyDown(KeyCode.KeypadMinus)) {
            rayonVoisinage = rayonVoisinage - 5 > 0 ? rayonVoisinage - 5 : 1;
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            brushSize = brushSize + 5 < 100 ? brushSize + 5 : 100;
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            brushSize = brushSize - 5 > 0 ? brushSize - 5 : 1;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            for(int i = 1; i < chunks.Count; i++) {
                chunks[i].GetComponent<MeshRenderer>().material.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            }
            randomColorTimer = 3.0f;
        } 
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (typeDeformation == TypeDeformation.CURVE) {
                numPatternCurveEnCours = (numPatternCurveEnCours + 1) % patternCurves.Count; 
                Debug.Log("Curve sélectionné : " + numPatternCurveEnCours);
            } else {
                typeDeformation = TypeDeformation.CURVE;
            }
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            if (typeDeformation == TypeDeformation.BRUSH)
            {
                numPatternBrushEnCours = (numPatternBrushEnCours + 1) % patternBrushs.Count; 
                Debug.Log("Brush sélectionné : " + numPatternBrushEnCours);
            } else {
                typeDeformation = TypeDeformation.BRUSH;
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ChangeDistanceMetric();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            ChangeDistanceMetricFindVertices();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            recalculateAllNormals = !recalculateAllNormals;
            Debug.Log("Recalculate all normals: " + recalculateAllNormals);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            Undo();

        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            Redo();
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

        if (randomColorTimer <= 0.0f && chunks.Count > 0)
        {
            Color mainColor = chunks[0].GetComponent<MeshRenderer>().material.color;
            for(int i = 1; i < chunks.Count; i++) {
                chunks[i].GetComponent<MeshRenderer>().material.color = mainColor;
            }
        }

        if (!Physics.Raycast(p_cam.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, maskPickingTerrain)) return;

        if (Input.GetMouseButtonDown(0)) {
            CaptureCurrentState();
            Debug.Log("captured");
            redoStack.Clear();
        }

        if (Input.GetMouseButton(0)) {
            GameObject go = hit.collider.gameObject;
            TP3_Terrain terrainScript = go.GetComponent<TP3_Terrain>();

            Mesh mesh = go.GetComponent<MeshFilter>().mesh;
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;

            int triangleIndex = hit.triangleIndex * 3;
            Vector3[] triangleVertices = new Vector3[]
            {
                vertices[triangles[triangleIndex]],
                vertices[triangles[triangleIndex + 1]],
                vertices[triangles[triangleIndex + 2]]
            };

            Vector3 tempCible = terrainScript.FindClosestVertex(triangleVertices, hit.point);
            cible = go.transform.TransformPoint(tempCible);

            listeVoisinsSel = terrainScript.RechercherVoisins(cible);
                
            if (terrainScript != null)
            {
                typeAction = Input.GetKey(KeyCode.LeftControl) ? TypeAction.DEFORMATION_BAS : TypeAction.DEFORMATION_HAUT;

                Vector3 direction = typeAction == TypeAction.DEFORMATION_BAS ? Vector3.down : Vector3.up;

                switch (typeDeformation) {
                   case TypeDeformation.CURVE:
                        terrainScript.AppliquerDeformation(listeVoisinsSel, direction);
                        break;
                    case TypeDeformation.BRUSH:
                        terrainScript.AppliquerDeformationBrush(listeVoisinsSel);
                        break;
                }
            }
        }

        // PHASE 6
        if (Input.GetMouseButtonUp(0))
        {
            RecalculerMeshCollider();
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

    public void CreateField()
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

        undoStack.Push((Vector3[])p_vertices.Clone());
    }

    private void CreateChunk(Vector3 offset)
    {
        bool canBeCreated = true;

        foreach (GameObject c in chunks)
        {
            if (c.transform.position == this.transform.position + offset)
            {
                canBeCreated = false;
                break;
            }
        }

        if (canBeCreated)
        {
            GameObject chunk = new GameObject("TerrainChunk" + (++nbChunks));
            chunk.transform.position = this.transform.position + offset;

            MeshFilter newMeshFilter = chunk.AddComponent<MeshFilter>();
            MeshCollider newMeshCollider = chunk.AddComponent<MeshCollider>();
            MeshRenderer newMeshRenderer = chunk.AddComponent<MeshRenderer>();

            TP3_Terrain terrainChunk = chunk.AddComponent<TP3_Terrain>();

            terrainChunk.dimension = this.dimension;
            terrainChunk.resolution = this.resolution;
            terrainChunk.CentrerPivot = this.CentrerPivot;
            terrainChunk.patternCurves = this.patternCurves;
            terrainChunk.patternBrushs = this.patternBrushs;
            terrainChunk.amplitudeDeformation = this.amplitudeDeformation;
            terrainChunk.rayonVoisinage = this.rayonVoisinage;
            terrainChunk.brushSize = this.brushSize;
            terrainChunk.typeDeformation = this.typeDeformation;
            terrainChunk.currentDistanceMetric = this.currentDistanceMetric;
            terrainChunk.currentDistanceMetricFindVertices = this.currentDistanceMetricFindVertices;
            terrainChunk.recalculateAllNormals = this.recalculateAllNormals;

            terrainChunk.p_meshFilter = newMeshFilter;
            terrainChunk.p_meshCollider = newMeshCollider;
            terrainChunk.p_meshRenderer = newMeshRenderer;
        }
    }

    private void RecalculerMeshCollider()
    {
        this.p_meshCollider.sharedMesh = null;
        this.p_meshCollider.sharedMesh = this.p_meshFilter.mesh;
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

        foreach (GameObject chunk in chunks)
        {
            TP3_Terrain terrainScript = chunk.GetComponent<TP3_Terrain>();

            Vector3[] vertices = terrainScript.p_vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertexWorldPosition = chunk.transform.TransformPoint(vertices[i]);
                float distanceToCible = CalculateDistanceVertices(cible, vertexWorldPosition);

                if (distanceToCible <= terrainScript.rayonVoisinage)
                {
                    listV.Add(new Voisin(i, distanceToCible, terrainScript.gameObject)); 
                }
            }
        }
        return listV;
    }

    void AppliquerDeformation(List<Voisin> listeVoisinsSel, Vector3 orientation) 
    {
        if (patternCurves.Count == 0) return;

        Dictionary<TP3_Terrain, Vector3[]> terrainsModifies = new Dictionary<TP3_Terrain, Vector3[]>();
         Dictionary<TP3_Terrain, List<int>> modifiedVertices = new Dictionary<TP3_Terrain, List<int>>();

        foreach (Voisin voisin in listeVoisinsSel)
        {
            TP3_Terrain terrainScript = voisin.terrainAssocie.GetComponent<TP3_Terrain>();
            if (terrainScript == null) continue;

            if (!terrainsModifies.ContainsKey(terrainScript))
            {
                terrainsModifies[terrainScript] = terrainScript.p_vertices;
            }

            if (!modifiedVertices.ContainsKey(terrainScript)) {
                modifiedVertices[terrainScript] = new List<int>();
            }

            float _force = amplitudeDeformation * patternCurves[numPatternCurveEnCours].Evaluate(voisin.distance / rayonVoisinage);
            terrainsModifies[terrainScript][voisin.indice] += orientation * _force;
            modifiedVertices[terrainScript].Add(voisin.indice);
        }

        foreach (var pair in terrainsModifies)
        {
            TP3_Terrain terrain = pair.Key;
            Vector3[] vertices = pair.Value;

            terrain.p_mesh.vertices = vertices;
            
            if (modifiedVertices.ContainsKey(terrain))
            {
                terrain.RecalculateNormalsModifiedVertices(modifiedVertices[terrain]);
            }
            
            terrain.p_meshFilter.mesh = terrain.p_mesh;

        }
    }

    void AppliquerDeformationBrush(List<Voisin> voisins)
    {
        if (patternBrushs.Count == 0) return;

        Dictionary<TP3_Terrain, Vector3[]> terrainsModifies = new Dictionary<TP3_Terrain, Vector3[]>();

        Texture2D brushTexture = patternBrushs[numPatternBrushEnCours]; // Texture actuelle
        int brushPixelSize = brushTexture.width;  // Texture du mesh carré 

        Color[] brushPixels = brushTexture.GetPixels(); // Récupère tous les pixels une seule fois

        Vector3 brushCenter = cible; // Vertex sélectionné par le clic

        float brushWorldSize = brushSize * 2f; // Taille en espace 3D du brush (diamètre)
        float pixelPerVertex = (float)brushPixelSize / brushWorldSize; // Ratio pixels/vertex pour avoir la zone de moyenne

        foreach (var voisin in voisins)
        {
            TP3_Terrain terrainScript = voisin.terrainAssocie.GetComponent<TP3_Terrain>();
            if (terrainScript == null) return;

            if (!terrainsModifies.ContainsKey(terrainScript))
                terrainsModifies[terrainScript] = terrainScript.p_vertices;

            Vector3[] vertices = terrainScript.p_vertices;

            Vector3 vertexPosition = vertices[voisin.indice];

            // Calcul des coordonnées relatives du vertex par rapport au centre du brush
            float relativeX = voisin.terrainAssocie.transform.TransformPoint(vertexPosition).x - brushCenter.x;
            float relativeZ = voisin.terrainAssocie.transform.TransformPoint(vertexPosition).z - brushCenter.z;

            // Si le vertex est dans la zone du brush
            if (Mathf.Abs(relativeX) <= brushSize && Mathf.Abs(relativeZ) <= brushSize)
            {
                // On calcule les coordonnées normalisées (u, v) du vertex dans la zone du brush
                float u = Mathf.InverseLerp(-brushSize, brushSize, relativeX);
                float v = Mathf.InverseLerp(-brushSize, brushSize, relativeZ);

                // Conversion en coordonnées pixel dans la texture du brush
                int brushX = Mathf.FloorToInt(u * brushPixelSize);
                int brushY = Mathf.FloorToInt(v * brushPixelSize);

                // Calcul de la zone de pixel à considérer pour ce vertex
                int pixelAreaSize = Mathf.FloorToInt(pixelPerVertex);

                // Calcul de la moyenne des intensités de pixels autour du centre (brushX, brushY)
                float averageIntensity = 0f;

                // Zone de pixels à échantillonner autour du point central
                int startX = Mathf.Clamp(brushX - pixelAreaSize / 2, 0, brushPixelSize - 1);
                int endX = Mathf.Clamp(brushX + pixelAreaSize / 2, 0, brushPixelSize - 1);
                int startY = Mathf.Clamp(brushY - pixelAreaSize / 2, 0, brushPixelSize - 1);
                int endY = Mathf.Clamp(brushY + pixelAreaSize / 2, 0, brushPixelSize - 1);

                // Parcours des pixels voisins dans la zone du brush
                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        int pixelIndex = x + y * brushPixelSize;
                        averageIntensity += brushPixels[pixelIndex].grayscale; // Utilisation de GetPixels() pour éviter les appels à GetPixel()
                    }
                }

                // Calcul de l'intensité moyenne
                averageIntensity /= (endX - startX) * (endY - startY);

                // Application de la force de déformation sur le vertex
                float deformationAmount = averageIntensity * amplitudeDeformation;

                if (typeAction == TypeAction.DEFORMATION_HAUT)
                    terrainsModifies[terrainScript][voisin.indice].y += deformationAmount;
                
                else if (typeAction == TypeAction.DEFORMATION_BAS)
                    terrainsModifies[terrainScript][voisin.indice].y -= deformationAmount;
            }
        }

        // Mise à jour du mesh
        
        foreach (var pair in terrainsModifies)
        {
            TP3_Terrain terrain = pair.Key;
            Vector3[] vertices = pair.Value;

            terrain.p_mesh.vertices = vertices;
            terrain.p_mesh.RecalculateNormals();
            terrain.p_meshFilter.mesh = terrain.p_mesh;
        }
    }

    void SetHeightVertices()
    {
        if (chunks.Count == 0) return;

        for(int i = 0; i < chunks.Count; i++)
        {
            TP3_Terrain chunk = chunks[i].GetComponent<TP3_Terrain>();
            if (gameObject.transform.position + (Vector3.left * dimension) == chunks[i].transform.position)
            {
                for (int j = 0; j < this.resolution; j++)
                {
                    int leftVertexIndex = j * this.resolution + (this.resolution - 1);
                    int rightVertexIndex = j * this.resolution;

                    this.p_vertices[rightVertexIndex].y = chunk.p_vertices[leftVertexIndex].y;                       
                } 
                continue;
            }

            if (gameObject.transform.position + (Vector3.right * dimension) == chunks[i].transform.position)
            {
                for (int j = 0; j < this.resolution; j++)
                {
                    int rightVertexIndex = j * this.resolution + (this.resolution - 1);
                    int leftVertexIndex = j * this.resolution;

                    this.p_vertices[rightVertexIndex].y = chunk.p_vertices[leftVertexIndex].y;                       
                }
                continue;
            }

            if (gameObject.transform.position + (Vector3.forward * dimension) == chunks[i].transform.position)
            {
                for (int j = 0; j < this.resolution; j++)
                {
                    int forwardVertexIndex = j;
                    int backVertexIndex = (this.resolution * (this.resolution-1)) + j;

                    this.p_vertices[backVertexIndex].y = chunk.p_vertices[forwardVertexIndex].y;                       
                }
                continue;
            }

            if (gameObject.transform.position + (Vector3.back * dimension) == chunks[i].transform.position)
            {
                for (int j = 0; j < this.resolution; j++)
                {
                    int backVertexIndex = j;
                    int forwardVertexIndex = (this.resolution * (this.resolution-1)) + j;

                    this.p_vertices[backVertexIndex].y = chunk.p_vertices[forwardVertexIndex].y;                       
                }
                continue;
            }       
        }

        this.p_mesh.vertices = p_vertices;
        this.p_mesh.RecalculateNormals();
    }

     public int GetDimension() => dimension;
    public int GetResolution() => resolution;
    public int GetBrushSize() => brushSize;
    public float GetAmplitudeDeformation() => amplitudeDeformation;
    public float GetRayonVoisinage() => rayonVoisinage;
    public int GetVerticesCount() => p_vertices != null ? p_vertices.Length : 0;
    public int GetNormalsCount() => p_normals != null ? p_normals.Length : 0;
    public int GetTrianglesCount() => p_triangles != null ? p_triangles.Length : 0;
    public int GetNumPatternCurveEnCours() => numPatternCurveEnCours;
    public int GetNumPatternBrushEnCours() => numPatternBrushEnCours;
    public static int GetChunksCount() => chunks.Count;
    public void SetDimension(int dimension) => this.dimension = dimension;
    public void SetResolution(int resolution) => this.resolution = resolution;
    public void ClearListChunks() {
        foreach(GameObject go in chunks) {
            Destroy(go);
        }
        chunks.Clear();
    }
    public List<GameObject> GetChunks() => chunks;

    public enum DistanceMetric { Euclidean, Manhattan, Chebyshev }
    public DistanceMetric currentDistanceMetric = DistanceMetric.Euclidean;
    // PHASE 1 : Changement formule calcul de distance
    void ChangeDistanceMetric()
    {
        currentDistanceMetric = (DistanceMetric)(((int)currentDistanceMetric + 1) % 3);
        Debug.Log("Distance métrique actuelle: " + currentDistanceMetric);
    }

    // PHASE 2
    Vector3 FindClosestVertex(Vector3[] vertices, Vector3 point)
    {
        Vector3 closestVertex = vertices[0];
        float minDistance = CalculateDistance(closestVertex, point);

        foreach (var vertex in vertices)
        {
            float distance = CalculateDistance(vertex, point);
            if (distance < minDistance)
            {
                closestVertex = vertex;
                minDistance = distance;
            }
        }
        return closestVertex;
    }

    // PHASE 2
    float CalculateDistance(Vector3 a, Vector3 b)
    {
        switch (currentDistanceMetric)
        {
            case DistanceMetric.Euclidean:
                return Vector3.Distance(a, b); // Distance Euclidienne
            case DistanceMetric.Manhattan:
                return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z); // Distance Manhattan 
            case DistanceMetric.Chebyshev:
                return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z)); // Distance Chebyshev
            default:
                return 0.0f;
        }
    }

    public enum DistanceMetricFindVertices { Euclidean, Manhattan, Chebyshev }
    public DistanceMetricFindVertices currentDistanceMetricFindVertices = DistanceMetricFindVertices.Euclidean;


    // PHASE 3 : Changement formule calcul de distance pour voisin
    void ChangeDistanceMetricFindVertices()
    {
        currentDistanceMetricFindVertices = (DistanceMetricFindVertices)(((int)currentDistanceMetricFindVertices + 1) % 3);
        Debug.Log("Distance métrique actuelle pour recherche voisin: " + currentDistanceMetricFindVertices);
    }

    // PHASE 3
    float CalculateDistanceVertices(Vector3 a, Vector3 b)
    {
        switch (currentDistanceMetricFindVertices)
        {
            case DistanceMetricFindVertices.Euclidean:
                return Vector3.Distance(a, b); // Distance Euclidienne
            case DistanceMetricFindVertices.Manhattan:
                return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z); // Distance Manhattan 
            case DistanceMetricFindVertices.Chebyshev:
                return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z)); // Distance Chebyshev
            default:
                return 0.0f;
        }
    }

    private bool recalculateAllNormals = true;
    public bool GetRecalculateMethod () => recalculateAllNormals;

    // PHASE 4
    void RecalculateNormalsModifiedVertices(List<int> modifiedVertices)
    {
        if (recalculateAllNormals)
        {
            this.p_mesh.RecalculateNormals();
        }
        else
        {
            Vector3[] vertices = this.p_mesh.vertices;
            Vector3[] normals = this.p_mesh.normals;
            int[] triangles = this.p_mesh.triangles;

            foreach (int vertexIndex in modifiedVertices)
            {
                Vector3 recalculatedNormal = Vector3.zero;

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    if (triangles[i] == vertexIndex || triangles[i + 1] == vertexIndex || triangles[i + 2] == vertexIndex)
                    {
                        Vector3 vertexA = vertices[triangles[i]];
                        Vector3 vertexB = vertices[triangles[i + 1]];
                        Vector3 vertexC = vertices[triangles[i + 2]];

                        Vector3 triangleNormal = Vector3.Cross(vertexB - vertexA, vertexC - vertexA).normalized;

                        recalculatedNormal += triangleNormal;
                    }
                }

                recalculatedNormal.Normalize();

                normals[vertexIndex] = recalculatedNormal;
            }

            this.p_mesh.normals = normals;
        }
    }

    private void CaptureCurrentState()
    {
        Vector3[] currentVertices = (Vector3[])p_vertices.Clone();
        undoStack.Push(currentVertices);
    }

    private void Undo()
    {
        if (undoStack.Count > 0)
        {
            Debug.Log("Undo");

            Vector3[] lastState = undoStack.Pop();
            redoStack.Push((Vector3[])p_vertices.Clone());

            p_vertices = lastState;
            p_mesh.vertices = lastState;
            p_mesh.RecalculateNormals();
            p_meshFilter.mesh = p_mesh;

            RecalculerMeshCollider();

            Debug.Log(redoStack.Count());
        }
    }

    private void Redo()
    {
        if (redoStack.Count > 0)
        {
            Debug.Log("Redo");
            
            Vector3[] nextState = (Vector3[])redoStack.Pop().Clone();
            undoStack.Push((Vector3[])p_vertices.Clone());

            p_vertices = nextState;
            p_mesh.vertices = nextState;
            p_mesh.RecalculateNormals();
            p_meshFilter.mesh = p_mesh;

            RecalculerMeshCollider();
        }
    }
}