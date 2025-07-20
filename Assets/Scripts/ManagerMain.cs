using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ManagerMain : MonoBehaviour
{
    public string Name = "Tutorial";

    public string Textures = "Textures";

    private float d;

    private int h;

    private int y;

    private bool t; 

    private Mesh opaquemesh;

    private Mesh transparentmesh;

    private Vector3 p1; 

    private Vector3 p2;

    public float speed = 7f;
    public float jumpHeight = 2f;
    public float gravity = 5f;
    public float sensitivity = 10f;
    public float clampAngle = 90f;
    public float smoothFactor = 25f;

    private Vector2 targetRotation;
    private Vector3 targetMovement;
    private Vector2 currentRotation;
    private Vector3 currentForce;

    private CharacterController Player;

    private bool isOpaque;

    private bool isTransparent;

    private bool isPortal;

    private Color[] LightColor;

    private int[] OneTriangle;

    private Camera Cam;

    private Vector3 CamPoint;

    private GameObject DirectionalLight;

    private RenderParams rp;

    private List<Plane> CamPlanes = new List<Plane>(6);

    private List<Plane> Planes = new List<Plane>();

    private RenderingData.Polyhedron CurrentSector;

    private GameObject CollisionObjects;

    private List<Vector3> CombinedVertices = new List<Vector3>();

    private List<int> CombinedTriangles = new List<int>();

    private List<Vector3> ClippedVertices = new List<Vector3>();

    private List<Vector4> ClippedTextures = new List<Vector4>();

    private List<Vector3> OpaqueVertices = new List<Vector3>();

    private List<int> OpaqueTriangles = new List<int>();

    private List<Vector4> OpaqueTextures = new List<Vector4>();

    private List<Vector3> TransparentVertices = new List<Vector3>();

    private List<Vector4> TransparentTextures = new List<Vector4>();

    private List<int> TransparentTriangles = new List<int>();

    private List<RenderingData.Polyhedron> Sectors = new List<RenderingData.Polyhedron>();

    private List<RenderingData.Polyhedron> OldSectors = new List<RenderingData.Polyhedron>();

    private List<GameObject> CollisionSectors = new List<GameObject>();

    private List<Vector3> outvertices = new List<Vector3>();

    private List<Vector4> outtex = new List<Vector4>();

    private List<List<Plane>> ListsOfPlanes = new List<List<Plane>>();

    private Matrix4x4 matrix;

    private Material opaquematerial;

    private Material transparentmaterial;

    private List<Mesh> CollisionMesh = new List<Mesh>();

    private RenderingData Rendering;

    [System.Serializable]
    public class RenderingData
    {
        public List<Polyhedron> Polyhedrons = new List<Polyhedron>();

        public List<PolygonData> PolygonInformation = new List<PolygonData>();

        public List<PolygonMesh> PolygonMeshes = new List<PolygonMesh>();

        public List<PlayerStarts> PlayerPosition = new List<PlayerStarts>();

        public List<PolygonLight> LightColor = new List<PolygonLight>();

        [System.Serializable]
        public class PolygonData
        {
            public int Plane;
            public int Portal;
            public int Render;
            public int Collision;
            public int Transparent;
            public int CollisionNumber;
            public int PortalNumber;
            public int MeshNumber;
            public int MeshTexture;
            public int MeshTextureCollection;
        }

        [System.Serializable]
        public class PolygonMesh
        {
            public List<Vector3> Vertices = new List<Vector3>();

            public List<Vector4> Textures = new List<Vector4>();

            public List<int> Triangles = new List<int>();

            public List<Vector3> Normals = new List<Vector3>();
        }

        [System.Serializable]
        public class Polyhedron
        {
            public List<int> MeshPlanes = new List<int>();

            public List<int> MeshPortals = new List<int>();

            public List<int> MeshRenders = new List<int>();

            public List<int> MeshCollisions = new List<int>();

            public List<int> MeshTransparent = new List<int>();

            public int PolyhedronNumber;
        }

        [System.Serializable]
        public class PlayerStarts
        {
            public Vector3 Position;
            public int Sector;
        }

        [System.Serializable]
        public class PolygonLight
        {
            public Color MeshLight;
        }
    }

    void Awake()
    {
        Player = GameObject.Find("Player").GetComponent<CharacterController>();

        Cam = Camera.main;

        DirectionalLight = GameObject.Find("Directional Light");
    }

    // Start is called before the first frame update
    void Start()
    {
        Load();

        MakeLists();

        LightColor = new Color[Rendering.LightColor.Count];

        OneTriangle = new int[3];

        CreateMaterial();

        opaquemesh = new Mesh();

        transparentmesh = new Mesh();

        rp = new RenderParams();

        matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

        CollisionObjects = new GameObject("CollisionMeshes");

        BuildCollsionSectors();

        CreatePolygonPlane();

        Cursor.lockState = CursorLockMode.Locked;

        Playerstart();

        Player.GetComponent<CharacterController>().enabled = true;

        foreach (RenderingData.Polyhedron sector in Rendering.Polyhedrons)
        {
            Physics.IgnoreCollision(Player, CollisionSectors[sector.PolyhedronNumber].GetComponent<MeshCollider>(), true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        PlayerInput();

        if (Cam.transform.hasChanged)
        {
            CamPoint = Cam.transform.position;

            Sectors.Clear();

            GetPolyhedrons(CurrentSector);

            CamPlanes.Clear();

            ReadFrustumPlanes(Cam, CamPlanes);

            CamPlanes.RemoveAt(5);

            CamPlanes.RemoveAt(4);

            OpaqueVertices.Clear();

            OpaqueTextures.Clear();

            OpaqueTriangles.Clear();

            TransparentVertices.Clear();

            TransparentTextures.Clear();

            TransparentTriangles.Clear();

            h = 0;

            y = 0;

            GetPolygons(CamPlanes, CurrentSector);

            SetRenderMeshes();

            Cam.transform.hasChanged = false;
        }

        Renderit();
    }

    void FixedUpdate()
    {
        if (!Player.isGrounded)
        {
            currentForce.y -= gravity * Time.deltaTime;
        }
    }

    public void Load()
    {
        TextAsset LoadLevel = Resources.Load<TextAsset>(Name);

        if (LoadLevel != null)
        {
            Rendering = JsonUtility.FromJson<RenderingData>(LoadLevel.text);
        }
    }

    public void MakeLists()
    {
        for (int i = 0; i < Rendering.PolygonInformation.Count; i++)
        {
            if (Rendering.PolygonInformation[i].Portal != -1)
            {
                ListsOfPlanes.Add(new List<Plane>());
            }
        }
    }

    public void CreateMaterial()
    {
        Shader shader = Resources.Load<Shader>("TexArray");

        Shader shaderT = Resources.Load<Shader>("TexArrayT");

        for (int i = 0; i < Rendering.LightColor.Count; i++)
        {
            LightColor[i] = new Color(Rendering.LightColor[i].MeshLight.r, Rendering.LightColor[i].MeshLight.g, Rendering.LightColor[i].MeshLight.b, 1.0f);
        }

        opaquematerial = new Material(shader);

        opaquematerial.mainTexture = Resources.Load<Texture2DArray>(Textures);

        opaquematerial.SetColorArray("_ColorArray", LightColor);

        transparentmaterial = new Material(shaderT);

        transparentmaterial.mainTexture = Resources.Load<Texture2DArray>(Textures);

        transparentmaterial.SetColorArray("_ColorArray", LightColor);
    }

    public void Playerstart()
    {
        if (Rendering.PlayerPosition.Count == 0)
        {
            Debug.LogError("No player starts available.");

            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, Rendering.PlayerPosition.Count);

        RenderingData.PlayerStarts selectedPosition = Rendering.PlayerPosition[randomIndex];

        CurrentSector = Rendering.Polyhedrons[selectedPosition.Sector];

        Player.transform.position = new Vector3(selectedPosition.Position.x, selectedPosition.Position.y + 1.10f, selectedPosition.Position.z);
    }

    private Plane FromVec4(Vector4 aVec)
    {
        Vector3 n = aVec;
        float l = n.magnitude;
        return new Plane(n / l, aVec.w / l);
    }

    public void SetFrustumPlanes(List<Plane> planes, Matrix4x4 m)
    {
        if (planes == null)
            return;
        var r0 = m.GetRow(0);
        var r1 = m.GetRow(1);
        var r2 = m.GetRow(2);
        var r3 = m.GetRow(3);

        planes.Add(FromVec4(r3 - r0)); // Right
        planes.Add(FromVec4(r3 + r0)); // Left
        planes.Add(FromVec4(r3 - r1)); // Top
        planes.Add(FromVec4(r3 + r1)); // Bottom
        planes.Add(FromVec4(r3 - r2)); // Far
        planes.Add(FromVec4(r3 + r2)); // Near
    }

    public void ReadFrustumPlanes(Camera cam, List<Plane> planes)
    {
        SetFrustumPlanes(planes, cam.projectionMatrix * cam.worldToCameraMatrix);
    }

    public void CreatePolygonPlane()
    {
        for (int i = 0; i < Rendering.PolygonMeshes.Count; i++)
        {
            p1 = Rendering.PolygonMeshes[i].Normals[0];
            p2 = Rendering.PolygonMeshes[i].Vertices[0];

            Planes.Add(new Plane(p1, p2));
        }
    }

    public void CreateClippingPlanes(List<Vector3> vertices, List<Plane> planes, Vector3 viewPos)
    {
        int count = vertices.Count;
        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;

            Vector3 p1 = vertices[i];
            Vector3 p2 = vertices[j];
            Vector3 normal = Vector3.Cross(p1 - p2, viewPos - p2);
            float magnitude = normal.magnitude;

            if (magnitude > 0.01f)
            {
                planes.Add(new Plane(normal / magnitude, p1));
            }
        }
    }

    public void BuildCollsionSectors()
    {
        for (int i = 0; i < Rendering.Polyhedrons.Count; i++)
        {
            CombinedVertices.Clear();

            CombinedTriangles.Clear();

            h = 0;

            for (int e = 0; e < Rendering.Polyhedrons[i].MeshCollisions.Count; e++)
            {
                RenderingData.PolygonMesh polygonMesh = Rendering.PolygonMeshes[Rendering.Polyhedrons[i].MeshCollisions[e]];

                for (int f = 0; f < polygonMesh.Vertices.Count; f++)
                {
                    CombinedVertices.Add(polygonMesh.Vertices[f]);
                }

                for (int j = 0; j < polygonMesh.Triangles.Count; j++)
                {
                    CombinedTriangles.Add(polygonMesh.Triangles[j] + h);
                }

                h += polygonMesh.Vertices.Count;
            }

            Mesh combinedmesh = new Mesh();

            CollisionMesh.Add(combinedmesh);

            combinedmesh.SetVertices(CombinedVertices);

            combinedmesh.SetTriangles(CombinedTriangles, 0);

            GameObject meshObject = new GameObject("Collision " + i);

            CollisionSectors.Add(meshObject);

            meshObject.AddComponent<MeshCollider>();

            meshObject.GetComponent<MeshCollider>().sharedMesh = combinedmesh;

            meshObject.transform.SetParent(CollisionObjects.transform);
        }
    }

    public void PlayerInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        if (Input.GetKeyDown(KeyCode.Space) && Player.isGrounded)
        {
            currentForce.y = jumpHeight;
        }

        float mousex = Input.GetAxisRaw("Mouse X");
        float mousey = Input.GetAxisRaw("Mouse Y");

        targetRotation.x -= mousey * sensitivity;
        targetRotation.y += mousex * sensitivity;

        targetRotation.x = Mathf.Clamp(targetRotation.x, -clampAngle, clampAngle);

        currentRotation = Vector2.Lerp(currentRotation, targetRotation, smoothFactor * Time.deltaTime);

        Cam.transform.localRotation = Quaternion.Euler(currentRotation.x, 0f, 0f);
        Player.transform.rotation = Quaternion.Euler(0f, currentRotation.y, 0f);

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        targetMovement = (Player.transform.right * horizontal + Player.transform.forward * vertical).normalized;

        Player.Move((targetMovement + currentForce) * speed * Time.deltaTime);
    }

    public (List<Vector3>, List<Vector4>) ClipThePolygon((List<Vector3>, List<Vector4>) verttexnorm, Plane plane, float epsilon = 0.00001f)
    {
        outvertices.Clear();
        outtex.Clear();

        int count = verttexnorm.Item1.Count;

        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;

            Vector3 p1 = verttexnorm.Item1[i];
            Vector3 p2 = verttexnorm.Item1[j];
            Vector4 t1 = verttexnorm.Item2[i];
            Vector4 t2 = verttexnorm.Item2[j];

            float d1 = plane.GetDistanceToPoint(p1);
            float d2 = plane.GetDistanceToPoint(p2);

            if (d1 >= -epsilon)
            {
                outvertices.Add(p1);
                outtex.Add(t1);
            }

            if (d1 * d2 < 0)
            {
                float d = d1 / (d1 - d2);
                outvertices.Add(Vector3.Lerp(p1, p2, d));
                outtex.Add(Vector4.Lerp(t1, t2, d));
            }
        }

        return (outvertices, outtex);
    }

    public (List<Vector3>, List<Vector4>) ClippingPlanesForPolygon((List<Vector3>, List<Vector4>) verttexnorm, List<Plane> planes)
    {
        foreach (Plane plane in planes)
        {
            ClippedVertices.Clear();

            ClippedVertices.AddRange(verttexnorm.Item1);

            ClippedTextures.Clear();

            ClippedTextures.AddRange(verttexnorm.Item2);

            verttexnorm = ClipThePolygon((ClippedVertices, ClippedTextures), plane);
        }

        return verttexnorm;
    }

    public bool CheckRadius(RenderingData.Polyhedron asector, Vector3 campoint)
    {
        foreach (var planeIndex in asector.MeshPlanes)
        {
            if (Planes[planeIndex].GetDistanceToPoint(campoint) < -0.6f)
            {
                return false;
            }
        }
        return true;
    }

    public bool CheckPolyhedron(RenderingData.Polyhedron asector, Vector3 campoint)
    {
        foreach (var planeIndex in asector.MeshPlanes)
        {
            if (Planes[planeIndex].GetDistanceToPoint(campoint) < 0)
            {
                return false;
            }
        }
        return true;
    }

    public void GetPolyhedrons(RenderingData.Polyhedron ASector)
    {
        Sectors.Add(ASector);

        foreach (int ASectors in ASector.MeshPortals)
        {
            RenderingData.PolygonData polygonData = Rendering.PolygonInformation[ASectors];

            RenderingData.Polyhedron polygonPortal = Rendering.Polyhedrons[polygonData.Portal];

            if (Sectors.Contains(polygonPortal))
            {
                continue;
            }

            t = CheckRadius(polygonPortal, CamPoint);

            if (t == true)
            {
                GetPolyhedrons(polygonPortal);

                continue;
            }
        }

        t = CheckPolyhedron(ASector, CamPoint);

        if (t == true)
        {
            CurrentSector = ASector;

            if (!OldSectors.SequenceEqual(Sectors))
            {
                foreach (RenderingData.Polyhedron sector in OldSectors)
                {
                    Physics.IgnoreCollision(Player, CollisionSectors[sector.PolyhedronNumber].GetComponent<MeshCollider>(), true);
                }

                foreach (RenderingData.Polyhedron sector in Sectors)
                {
                    Physics.IgnoreCollision(Player, CollisionSectors[sector.PolyhedronNumber].GetComponent<MeshCollider>(), false);
                }

                OldSectors.Clear();

                OldSectors.AddRange(Sectors);
            } 
        }
    }

    public void SetRenderMeshes()
    {
        opaquemesh.Clear();

        opaquemesh.SetVertices(OpaqueVertices);

        opaquemesh.SetUVs(0, OpaqueTextures);

        opaquemesh.SetTriangles(OpaqueTriangles, 0);

        transparentmesh.Clear();

        transparentmesh.subMeshCount = TransparentTriangles.Count / 3;

        transparentmesh.SetVertices(TransparentVertices);

        transparentmesh.SetUVs(0, TransparentTextures);

        for (int i = 0; i < TransparentTriangles.Count; i += 3)
        {
            OneTriangle[0] = TransparentTriangles[i];
            OneTriangle[1] = TransparentTriangles[i + 1];
            OneTriangle[2] = TransparentTriangles[i + 2];

            transparentmesh.SetTriangles(OneTriangle, i / 3);
        }
    }

    public void Renderit()
    {
        rp.material = opaquematerial;

        Graphics.RenderMesh(rp, opaquemesh, 0, matrix);

        rp.material = transparentmaterial;

        for (int i = TransparentTriangles.Count - 1; i >= 0; i -= 3)
        {
            Graphics.RenderMesh(rp, transparentmesh, i / 3, matrix);
        }
    }

    public void GetPolygons(List<Plane> APlanes, RenderingData.Polyhedron BSector)
    {
        foreach (var planeIndex in BSector.MeshPlanes)
        {
            d = Planes[planeIndex].GetDistanceToPoint(CamPoint);

            if (d < -0.1f || d <= 0)
            {
                continue;
            }

            RenderingData.PolygonMesh renderData = Rendering.PolygonMeshes[planeIndex];

            RenderingData.PolygonData polygonData = Rendering.PolygonInformation[planeIndex];

            isOpaque = polygonData.Render != -1;
            isTransparent = polygonData.Transparent != -1;
            isPortal = polygonData.Portal != -1;

            (List<Vector3>, List<Vector4>) clippedData = ClippingPlanesForPolygon((renderData.Vertices, renderData.Textures), APlanes);

            int count = clippedData.Item1.Count;

            List<Vector3> vertices = clippedData.Item1;

            List<Vector4> textures = clippedData.Item2;

            if (isOpaque)
            {
                if (count > 2)
                {
                    for (int i = 0; i < count; i++)
                    {
                        OpaqueVertices.Add(vertices[i]);
                        OpaqueTextures.Add(textures[i]);
                    }

                    for (int e = 0; e < count - 2; e++)
                    {
                        OpaqueTriangles.Add(0 + h);
                        OpaqueTriangles.Add(e + 1 + h);
                        OpaqueTriangles.Add(e + 2 + h);
                    }

                    h += count;
                }
            }

            if (isTransparent)
            {
                if (count > 2)
                {
                    for (int i = 0; i < count; i++)
                    {
                        TransparentVertices.Add(vertices[i]);
                        TransparentTextures.Add(textures[i]);
                    }

                    for (int e = 0; e < count - 2; e++)
                    {
                        TransparentTriangles.Add(0 + y);
                        TransparentTriangles.Add(e + 1 + y);
                        TransparentTriangles.Add(e + 2 + y);
                    }

                    y += count;
                }
            }

            if (isPortal)
            {
                RenderingData.Polyhedron polygonPortal = Rendering.Polyhedrons[polygonData.Portal];

                if (Sectors.Contains(polygonPortal))
                {
                    GetPolygons(APlanes, polygonPortal);

                    continue;
                }

                if (d != 0)
                {
                    if (count > 2)
                    {
                        ListsOfPlanes[polygonData.PortalNumber].Clear();

                        CreateClippingPlanes(vertices, ListsOfPlanes[polygonData.PortalNumber], CamPoint);

                        GetPolygons(ListsOfPlanes[polygonData.PortalNumber], polygonPortal);
                    }
                }
            }
        }
    }
}
