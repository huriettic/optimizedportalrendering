using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ManagerMain : MonoBehaviour
{
    public string Name = "Tutorial";

    private float d;

    private int h;

    private int y;

    private bool t;

    private bool PointIn;

    private Mesh opaquemesh;

    private Mesh transparentmesh;

    private Vector3 p1; 

    private Vector3 p2;

    private float lerpX;
    private float lerpY;
    private float snap = 25f;
    private float rotationX;
    private float rotationY;
    private float lookAngle = 90f;
    private float sensitivityX = 10f;
    private float sensitivityY = 10f;

    public float speed = 6f;
    public float jumpHeight = 10f;
    public float gravity = 20f;

    private Vector3 moveDirection = Vector3.zero;

    private CharacterController Player;

    private bool isRender;

    private bool isTransparent;

    private bool isPortal;

    private Color[] LightColor;

    private int[] OneTriangle;

    private Camera Cam;

    private GameObject DirectionalLight;

    private RenderParams rp;

    private List<Plane> CamPlanes = new List<Plane>(6);

    private List<Plane> Planes = new List<Plane>();

    private RenderingData.Polyhedron CurrentSector;

    private GameObject CollisionObjects;

    private List<Vector4> outtex = new List<Vector4>();

    private List<Vector3> CombinedVertices = new List<Vector3>();

    private List<int> CombinedTriangles = new List<int>();

    private List<Vector3> OpaqueVertices = new List<Vector3>();

    private List<int> OpaqueTriangles = new List<int>();

    private List<Vector4> OpaqueTextures = new List<Vector4>();

    private List<Vector3> OpaqueNormals = new List<Vector3>();

    private List<Vector3> TransparentVertices = new List<Vector3>();

    private List<Vector4> TransparentTextures = new List<Vector4>();

    private List<Vector3> TransparentNormals = new List<Vector3>();

    private List<int> TransparentTriangles = new List<int>();

    private List<RenderingData.Polyhedron> Sectors = new List<RenderingData.Polyhedron>();

    private List<GameObject> CollisionSectors = new List<GameObject>();

    private List<Vector3> outvertices = new List<Vector3>();

    private List<Vector3> outnormals = new List<Vector3>();

    private List<float> m_Dists = new List<float>();

    private List<List<Plane>> ListsOfPlanes = new List<List<Plane>>();

    public List<(List<Vector3>, List<Vector4>, List<Vector3>)> ListsOfLists = new List<(List<Vector3>, List<Vector4>, List<Vector3>)>();

    private List<Vector3> Vertices = new List<Vector3>();

    private List<Vector4> Textures = new List<Vector4>();

    private List<Vector3> Normals = new List<Vector3>();

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

        matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

        CollisionObjects = new GameObject("CollisionMeshes");

        BuildCollsionSectors();

        CreatePolygonPlane();

        Cursor.lockState = CursorLockMode.Locked;

        Playerstart();

        Player.GetComponent<CharacterController>().enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        Controller();

        if (Cam.transform.hasChanged)
        {
            Sectors.Clear();

            GetPolyhedrons(CurrentSector);

            CamPlanes.Clear();

            ReadFrustumPlanes(Cam, CamPlanes);

            CamPlanes.RemoveAt(5);

            CamPlanes.RemoveAt(4);

            OpaqueVertices.Clear();

            OpaqueTextures.Clear();

            OpaqueTriangles.Clear();

            OpaqueNormals.Clear();

            TransparentVertices.Clear();

            TransparentTextures.Clear();

            TransparentNormals.Clear();

            TransparentTriangles.Clear();

            h = 0;

            y = 0;

            GetPortals(CamPlanes, CurrentSector);

            SetRenderMeshes();

            Cam.transform.hasChanged = false;
        }

        Renderit();
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

        for (int i = 0;i < Rendering.PolygonMeshes.Count; i++)
        {
            ListsOfLists.Add((new List<Vector3>(), new List<Vector4>(), new List<Vector3>()));
        }
    }

    public void CreateMaterial()
    {
        Shader shader = Resources.Load<Shader>("TexArray");

        opaquematerial = new Material(shader);

        for (int i = 0; i < Rendering.LightColor.Count; i++)
        {
            LightColor[i] = new Color(Rendering.LightColor[i].MeshLight.r, Rendering.LightColor[i].MeshLight.g, Rendering.LightColor[i].MeshLight.b, 1.0f);
        }

        opaquematerial.SetColorArray("_ColorArray", LightColor);

        Shader shaderT = Resources.Load<Shader>("TexArrayT");

        transparentmaterial = new Material(shaderT);

        transparentmaterial.SetColorArray("_ColorArray", LightColor);

        DirectionalLight.SetActive(false);

        opaquematerial.mainTexture = Resources.Load<Texture2DArray>("Textures");
        transparentmaterial.mainTexture = Resources.Load<Texture2DArray>("Textures");
    }

    public void Playerstart()
    {
        int random = UnityEngine.Random.Range(0, Rendering.PlayerPosition.Count);

        for (int i = 0; i < Rendering.Polyhedrons.Count; i++)
        {
            if (Rendering.PlayerPosition[random].Sector == i)
            {
                CurrentSector = Rendering.Polyhedrons[i];

                Player.transform.position = new Vector3(Rendering.PlayerPosition[random].Position.x, Rendering.PlayerPosition[random].Position.y + 1.10f, Rendering.PlayerPosition[random].Position.z);
            }
        }
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

    public void CreateClippingPlanes(List<Vector3> aVertices, List<Plane> aList, Vector3 aViewPos)
    {
        int count = aVertices.Count;
        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;
            var p1 = aVertices[i];
            var p2 = aVertices[j];
            var n = Vector3.Cross(p1 - p2, aViewPos - p2);
            var l = n.magnitude;
            if (l < 0.01f)
                continue;
            aList.Add(new Plane(n / l, aViewPos));
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

                CombinedVertices.AddRange(polygonMesh.Vertices);

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

    public void Controller()
    {
        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }

        if (Input.GetButton("Jump") && Player.isGrounded)
        {
            moveDirection.y = jumpHeight;
        }
        else
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        float mouseX = Input.GetAxis("Mouse X") * sensitivityX;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivityY;

        rotationY += mouseX;
        rotationX -= mouseY;

        rotationX = Mathf.Clamp(rotationX, -lookAngle, lookAngle);
        lerpX = Mathf.Lerp(lerpX, rotationX, snap * Time.deltaTime);
        lerpY = Mathf.Lerp(lerpY, rotationY, snap * Time.deltaTime);

        Cam.transform.rotation = Quaternion.Euler(lerpX, lerpY, 0);
        Player.transform.rotation = Quaternion.Euler(0, lerpY, 0);

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = Player.transform.right * x + Player.transform.forward * z;

        Player.Move(move * speed * Time.deltaTime);
        Player.Move(moveDirection * Time.deltaTime);
    }

    public (List<Vector3>, List<Vector4>, List<Vector3>) ClippingPlaneVertTexNorm((List<Vector3>, List<Vector4>, List<Vector3>) verttexnorm, Plane aPlane, float aEpsilon = 0.001f)
    {
        m_Dists.Clear();
        outvertices.Clear();
        outtex.Clear();
        outnormals.Clear();

        int count = verttexnorm.Item1.Count;
        if (m_Dists.Capacity < count)
            m_Dists.Capacity = count;
        if (outvertices.Capacity < count)
            outvertices.Capacity = count;
        if (outtex.Capacity < count)
            outtex.Capacity = count;
        if (outnormals.Capacity < count)
            outnormals.Capacity = count;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = verttexnorm.Item1[i];
            m_Dists.Add(aPlane.GetDistanceToPoint(p));
        }
        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;
            float d1 = m_Dists[i];
            float d2 = m_Dists[j];
            Vector3 p1 = verttexnorm.Item1[i];
            Vector3 p2 = verttexnorm.Item1[j];
            Vector4 t1 = verttexnorm.Item2[i];
            Vector4 t2 = verttexnorm.Item2[j];
            Vector3 faceNormal = verttexnorm.Item3[0];
            bool split = d1 > aEpsilon;
            if (split)
            {
                outvertices.Add(p1);
                outtex.Add(t1);
                outnormals.Add(faceNormal);
            }
            else if (d1 > -aEpsilon)
            {
                // point on clipping plane so just keep it
                outvertices.Add(p1);
                outtex.Add(t1);
                outnormals.Add(faceNormal);
                continue;
            }
            // both points are on the same side of the plane
            if ((d2 > -aEpsilon && split) || (d2 < aEpsilon && !split))
            {
                continue;
            }
            float d = d1 / (d1 - d2);
            outvertices.Add(p1 + (p2 - p1) * d);
            float x1 = t1.x + (t2.x - t1.x) * d;
            float y1 = t1.y + (t2.y - t1.y) * d;
            outtex.Add(new Vector4(x1, y1, t1.z, t1.w));
            outnormals.Add(faceNormal);
        }

        return (outvertices, outtex, outnormals);
    }

    public (List<Vector3>, List<Vector4>, List<Vector3>) ClippingPlanesVertTexNorm((List<Vector3>, List<Vector4>, List<Vector3>) verttexnorm, List<Plane> aPlanes)
    {
        for (int i = 0; i < aPlanes.Count; i++)
        {
            Vertices.Clear();

            Vertices.AddRange(verttexnorm.Item1);

            Textures.Clear();

            Textures.AddRange(verttexnorm.Item2);

            Normals.Clear();

            Normals.AddRange(verttexnorm.Item3);

            verttexnorm = ClippingPlaneVertTexNorm((Vertices, Textures, Normals), aPlanes[i]);
        }

        return verttexnorm;
    }

    public bool CheckRadius(RenderingData.Polyhedron asector, Vector3 campoint)
    {
        PointIn = true;

        for (int e = 0; e < asector.MeshPlanes.Count; e++)
        {
            if (Planes[asector.MeshPlanes[e]].GetDistanceToPoint(campoint) < -0.6f)
            {
                PointIn = false;
                break;
            }
        }
        return PointIn;
    }

    public bool CheckPolyhedron(RenderingData.Polyhedron asector, Vector3 campoint)
    {
        PointIn = true;

        for (int i = 0; i < asector.MeshPlanes.Count; i++)
        {
            if (Planes[asector.MeshPlanes[i]].GetDistanceToPoint(campoint) < 0)
            {
                PointIn = false;
                break;
            }
        }
        return PointIn;
    }

    public void GetPolyhedrons(RenderingData.Polyhedron ASector)
    {
        Vector3 CamPoint = Cam.transform.position;

        Sectors.Add(ASector);

        for (int i = 0; i < ASector.MeshPortals.Count; ++i)
        {
            RenderingData.PolygonData f = Rendering.PolygonInformation[ASector.MeshPortals[i]];

            if (Sectors.Contains(Rendering.Polyhedrons[f.Portal]))
            {
                continue;
            }

            t = CheckRadius(Rendering.Polyhedrons[f.Portal], CamPoint);

            if (t == true)
            {
                GetPolyhedrons(Rendering.Polyhedrons[f.Portal]);

                continue;
            }
        }

        t = CheckPolyhedron(ASector, CamPoint);

        if (t == true)
        {
            CurrentSector = ASector;

            IEnumerable<RenderingData.Polyhedron> except = Rendering.Polyhedrons.Except(Sectors);

            foreach (RenderingData.Polyhedron sector in except)
            {
                Physics.IgnoreCollision(Player, CollisionSectors[sector.PolyhedronNumber].GetComponent<MeshCollider>(), true);
            }

            foreach (RenderingData.Polyhedron sector in Sectors)
            {
                Physics.IgnoreCollision(Player, CollisionSectors[sector.PolyhedronNumber].GetComponent<MeshCollider>(), false);
            }
        }
    }

    public void SetRenderMeshes()
    {
        opaquemesh.Clear();

        opaquemesh.SetVertices(OpaqueVertices);

        opaquemesh.SetUVs(0, OpaqueTextures);

        opaquemesh.SetTriangles(OpaqueTriangles, 0);

        opaquemesh.SetNormals(OpaqueNormals);

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

        transparentmesh.SetNormals(TransparentNormals);
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

    public void GetPortals(List<Plane> APlanes, RenderingData.Polyhedron BSector)
    {
        Vector3 CamPoint = Cam.transform.position;

        for (int i = 0; i < BSector.MeshPlanes.Count; i++)
        {
            d = Planes[BSector.MeshPlanes[i]].GetDistanceToPoint(CamPoint);

            if (d < -0.1f || d <= 0)
            {
                continue;
            }

            isRender = BSector.MeshRenders.Contains(BSector.MeshPlanes[i]);

            isTransparent = BSector.MeshTransparent.Contains(BSector.MeshPlanes[i]);

            isPortal = BSector.MeshPortals.Contains(BSector.MeshPlanes[i]);

            RenderingData.PolygonMesh r = Rendering.PolygonMeshes[BSector.MeshPlanes[i]];

            RenderingData.PolygonData g = Rendering.PolygonInformation[BSector.MeshPlanes[i]];

            ListsOfLists[BSector.MeshPlanes[i]] = ClippingPlanesVertTexNorm((r.Vertices, r.Textures, r.Normals), APlanes);

            if (isRender)
            {
                OpaqueVertices.AddRange(ListsOfLists[BSector.MeshPlanes[i]].Item1);

                OpaqueTextures.AddRange(ListsOfLists[BSector.MeshPlanes[i]].Item2);

                OpaqueNormals.AddRange(ListsOfLists[BSector.MeshPlanes[i]].Item3);

                if (ListsOfLists[BSector.MeshPlanes[i]].Item1.Count > 2)
                {
                    for (int e = 0; e < ListsOfLists[BSector.MeshPlanes[i]].Item1.Count - 2; e++)
                    {
                        OpaqueTriangles.Add(0 + h);
                        OpaqueTriangles.Add(e + 1 + h);
                        OpaqueTriangles.Add(e + 2 + h);
                    }
                }

                h += ListsOfLists[BSector.MeshPlanes[i]].Item1.Count;
            }

            if (isTransparent)
            {
                TransparentVertices.AddRange(ListsOfLists[BSector.MeshPlanes[i]].Item1);

                TransparentTextures.AddRange(ListsOfLists[BSector.MeshPlanes[i]].Item2);

                TransparentNormals.AddRange(ListsOfLists[BSector.MeshPlanes[i]].Item3);

                if (ListsOfLists[BSector.MeshPlanes[i]].Item1.Count > 2)
                {
                    for (int e = 0; e < ListsOfLists[BSector.MeshPlanes[i]].Item1.Count - 2; e++)
                    {
                        TransparentTriangles.Add(0 + y);
                        TransparentTriangles.Add(e + 1 + y);
                        TransparentTriangles.Add(e + 2 + y);
                    }
                }

                y += ListsOfLists[BSector.MeshPlanes[i]].Item1.Count;
            }

            if (isPortal)
            {
                if (Sectors.Contains(Rendering.Polyhedrons[g.Portal]))
                {
                    ListsOfPlanes[g.PortalNumber].Clear();

                    ListsOfPlanes[g.PortalNumber].AddRange(APlanes);

                    GetPortals(ListsOfPlanes[g.PortalNumber], Rendering.Polyhedrons[g.Portal]);

                    continue;
                }

                if (d != 0)
                {
                    if (ListsOfLists[BSector.MeshPlanes[i]].Item1.Count > 2)
                    {
                        ListsOfPlanes[g.PortalNumber].Clear();

                        CreateClippingPlanes(ListsOfLists[BSector.MeshPlanes[i]].Item1, ListsOfPlanes[g.PortalNumber], CamPoint);

                        GetPortals(ListsOfPlanes[g.PortalNumber], Rendering.Polyhedrons[g.Portal]);
                    }
                }
            }
        }
    }
}
