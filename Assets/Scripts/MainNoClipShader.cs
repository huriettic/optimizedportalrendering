using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class MainNoClipShader : MonoBehaviour
{
    public string Name = "Tutorial";

    private float lerpX;
    private float lerpY;
    private float snap = 25f;
    private float rotationX;
    private float rotationY;
    private float lookAngle = 90f;
    private float sensitivityX = 10f;
    private float sensitivityY = 10f;
    public float speed = 6f;
    public float jumpHeight = 8f;
    public float gravity = 20f;
    public CharacterController Player;
    private Vector3 moveDirection = Vector3.zero;

    public Camera Cam;

    public GameObject DirectionalLight;

    public bool YouHaveTextures = true;

    public bool EmissionFullBright = true;

    public RenderParams rp;

    public List<Plane> CamPlanes = new List<Plane>(6);

    public List<Plane> Planes = new List<Plane>();

    private RenderingData.Polyhedron CurrentSector;

    private GameObject CollisionObjects;

    private List<Vector2> outtex = new List<Vector2>();

    private List<Vector3> CombinedVertices = new List<Vector3>();

    private List<int> CombinedTriangles = new List<int>();

    private List<Vector2> CombinedTextures = new List<Vector2>();

    private List<Vector3> CombinedNormals = new List<Vector3>();

    public List<List<int>> ListsOfTriangles = new List<List<int>>();

    public List<List<Vector3>> ListsOfNormals = new List<List<Vector3>>();

    private List<RenderingData.Polyhedron> Sectors = new List<RenderingData.Polyhedron>();

    private List<RenderingData.Polyhedron> VisitedSector = new List<RenderingData.Polyhedron>();

    private List<GameObject> CollisionSectors = new List<GameObject>();

    private List<Vector3> outvertices = new List<Vector3>();

    private List<Vector3> outnormals = new List<Vector3>();

    private List<float> m_Dists = new List<float>();

    public List<List<Plane>> ListsOfPlanes = new List<List<Plane>>();

    public List<List<Vector3>> ListsOfVertices = new List<List<Vector3>>();

    public List<List<Vector2>> ListsOfTextures = new List<List<Vector2>>();

    public List<List<Mesh>> ListsOfMeshes = new List<List<Mesh>>();

    private List<Material> materials = new List<Material>();

    private List<Mesh> CollisionMesh = new List<Mesh>();

    private List<Mesh> MeshSectors = new List<Mesh>();

    private Mesh CurrentMesh;

    private RenderingData Rendering;

    private Matrix4x4 matrix;

    [System.Serializable]
    public class RenderingData
    {
        public List<Polyhedron> Polyhedrons = new List<Polyhedron>();

        public List<PolygonData> PolygonInformation = new List<PolygonData>();

        public List<PolygonMesh> PolygonMeshes = new List<PolygonMesh>();

        public List<PlayerStarts> PlayerPosition = new List<PlayerStarts>();

        [System.Serializable]
        public class PolygonData
        {
            public int Plane;
            public int Portal;
            public int Render;
            public int Collision;
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

            public List<Vector2> Textures = new List<Vector2>();

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

            public int PolyhedronNumber;
        }

        [System.Serializable]
        public class PlayerStarts
        {
            public Vector3 Position;
            public int Sector;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Load();

        MakeLists();

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

        Sectors.Clear();

        GetPolyhedrons(CurrentSector);

        CamPlanes.Clear();

        ReadFrustumPlanes(Cam, CamPlanes);

        CamPlanes.RemoveAt(5);

        CamPlanes.RemoveAt(4);

        VisitedSector.Clear();

        GetPortals(CamPlanes, CurrentSector, CurrentMesh);
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
        for (int i = 0; i < 26; i++)
        {
            ListsOfTriangles.Add(new List<int>());
        }

        for (int i = 0; i < Rendering.PolygonInformation.Count; i++)
        {
            if (Rendering.PolygonInformation[i].Portal != -1)
            {
                ListsOfPlanes.Add(new List<Plane>());
            }
        }

        for (int i = 0; i < 20; i++)
        {
            ListsOfVertices.Add(new List<Vector3>());
        }

        for (int i = 0; i < 20; i++)
        {
            ListsOfTextures.Add(new List<Vector2>());
        }

        for (int i = 0; i < 20; i++)
        {
            ListsOfNormals.Add(new List<Vector3>());
        }

        for (int i = 0; i < Rendering.Polyhedrons.Count; i++)
        {
            MeshSectors.Add(new Mesh());
        }

        for (int i = 0; i < Rendering.PolygonInformation.Count; i++)
        {
            if (Rendering.PolygonInformation[i].Portal != -1)
            {
                ListsOfMeshes.Add(new List<Mesh>()
                {
                  new Mesh(), new Mesh(), new Mesh(), new Mesh(),
                  new Mesh(), new Mesh(), new Mesh(), new Mesh(),
                  new Mesh(), new Mesh(), new Mesh(), new Mesh(),
                  new Mesh(), new Mesh(), new Mesh(), new Mesh(),
                  new Mesh(), new Mesh(), new Mesh(), new Mesh(),
                  new Mesh(), new Mesh(), new Mesh(), new Mesh(),
                  new Mesh(), new Mesh()
                });
            }
        }

        Shader shader = Resources.Load<Shader>("Clipping");

        if (EmissionFullBright)
        {
            DirectionalLight.SetActive(false);
        }

        for (int e = 0; e < Rendering.PolygonInformation.Count; ++e)
        {
            if (Rendering.PolygonInformation[e].Render != -1)
            {
                Material material = new Material(shader);

                if (YouHaveTextures)
                {
                    material.mainTexture = Resources.Load<Texture2D>(Rendering.PolygonInformation[e].MeshTextureCollection + "/" + Rendering.PolygonInformation[e].MeshTexture);
                }

                if (EmissionFullBright)
                {
                    Color hdrWhite = Color.white * 1.0f;
                    material.SetColor("_Emission", hdrWhite);
                    material.SetFloat("_Glossiness", 0.0f);
                }

                materials.Add(material);
            }
        }
    }

    public void Playerstart()
    {
        int random = UnityEngine.Random.Range(0, Rendering.PlayerPosition.Count);

        for (int i = 0; i < Rendering.Polyhedrons.Count; i++)
        {
            if (Rendering.PlayerPosition[random].Sector == i)
            {
                CurrentSector = Rendering.Polyhedrons[i];

                CurrentMesh = MeshSectors[i];

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
            Vector3 p1 = Rendering.PolygonMeshes[i].Normals[0];
            Vector3 p2 = Rendering.PolygonMeshes[i].Vertices[0];

            Planes.Add(new Plane(p1, p2));
        }
    }

    public float PointDistanceToPlane(Plane plane, Vector3 point)
    {
        return plane.normal.x * point.x + plane.normal.y * point.y + plane.normal.z * point.z + plane.distance;
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

            int h = 0;

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

    public List<Vector3> ClippingPlane(List<Vector3> invertices, Plane aPlane, float aEpsilon = 0.001f)
    {
        m_Dists.Clear();
        outvertices.Clear();

        int count = invertices.Count;
        if (m_Dists.Capacity < count)
            m_Dists.Capacity = count;
        if (outvertices.Capacity < count)
            outvertices.Capacity = count;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = invertices[i];
            m_Dists.Add(PointDistanceToPlane(aPlane, p));
        }
        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;
            float d1 = m_Dists[i];
            float d2 = m_Dists[j];
            Vector3 p1 = invertices[i];
            Vector3 p2 = invertices[j];
            bool split = d1 > aEpsilon;
            if (split)
            {
                outvertices.Add(p1);
            }
            else if (d1 > -aEpsilon)
            {
                // point on clipping plane so just keep it
                outvertices.Add(p1);
                continue;
            }
            // both points are on the same side of the plane
            if ((d2 > -aEpsilon && split) || (d2 < aEpsilon && !split))
            {
                continue;
            }
            float d = d1 / (d1 - d2);
            outvertices.Add(p1 + (p2 - p1) * d);
        }
        return outvertices;
    }

    public List<Vector3> ClippingPlanes(List<Vector3> invertices, List<Plane> aPlanes)
    {
        for (int i = 0; i < aPlanes.Count; i++)
        {
            ListsOfVertices[i].Clear();

            ListsOfVertices[i].AddRange(invertices);

            invertices = ClippingPlane(ListsOfVertices[i], aPlanes[i]);
        }

        return invertices;
    }

    public (List<Vector3>, List<Vector2>, List<Vector3>) ClippingPlaneVertTexNorm((List<Vector3>, List<Vector2>, List<Vector3>) verttexnorm, Plane aPlane, float aEpsilon = 0.001f)
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
            m_Dists.Add(PointDistanceToPlane(aPlane, p));
        }
        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;
            float d1 = m_Dists[i];
            float d2 = m_Dists[j];
            Vector3 p1 = verttexnorm.Item1[i];
            Vector3 p2 = verttexnorm.Item1[j];
            Vector2 t1 = verttexnorm.Item2[i];
            Vector2 t2 = verttexnorm.Item2[j];
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
            outtex.Add(t1 + (t2 - t1) * d);
            outnormals.Add(faceNormal);
        }

        return (outvertices, outtex, outnormals);
    }

    public (List<Vector3>, List<Vector2>, List<Vector3>) ClippingPlanesVertTexNorm((List<Vector3>, List<Vector2>, List<Vector3>) verttexnorm, List<Plane> aPlanes)
    {
        for (int i = 0; i < aPlanes.Count; i++)
        {
            ListsOfVertices[i].Clear();

            ListsOfVertices[i].AddRange(verttexnorm.Item1);

            ListsOfTextures[i].Clear();

            ListsOfTextures[i].AddRange(verttexnorm.Item2);

            ListsOfNormals[i].Clear();

            ListsOfNormals[i].AddRange(verttexnorm.Item3);

            verttexnorm = ClippingPlaneVertTexNorm((ListsOfVertices[i], ListsOfTextures[i], ListsOfNormals[i]), aPlanes[i]);
        }

        return verttexnorm;
    }

    public bool CheckRadius(RenderingData.Polyhedron asector, Vector3 campoint)
    {
        bool PointIn = true;

        for (int e = 0; e < asector.MeshPlanes.Count; e++)
        {
            if (PointDistanceToPlane(Planes[asector.MeshPlanes[e]], campoint) < -0.6f)
            {
                PointIn = false;
                break;
            }
        }
        return PointIn;
    }

    public bool CheckPolyhedron(RenderingData.Polyhedron asector, Vector3 campoint)
    {
        bool PointIn = true;

        for (int i = 0; i < asector.MeshPlanes.Count; i++)
        {
            if (PointDistanceToPlane(Planes[asector.MeshPlanes[i]], campoint) < 0)
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

            bool r = CheckRadius(Rendering.Polyhedrons[f.Portal], CamPoint);

            if (r == true)
            {
                GetPolyhedrons(Rendering.Polyhedrons[f.Portal]);

                continue;
            }
        }

        bool t = CheckPolyhedron(ASector, CamPoint);

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

    public void GetPortals(List<Plane> APlanes, RenderingData.Polyhedron BSector, Mesh MeshCombined)
    {
        Vector3 CamPoint = Cam.transform.position;

        int SectorCount = 0;

        foreach (RenderingData.Polyhedron VSector in VisitedSector)
        {
            if (VSector == BSector)
            {
                SectorCount++;
            }
        }

        VisitedSector.Add(BSector);

        CombinedVertices.Clear();

        CombinedTextures.Clear();

        CombinedNormals.Clear();

        int h = 0;

        for (int i = 0; i < BSector.MeshRenders.Count; i++)
        {
            RenderingData.PolygonMesh r = Rendering.PolygonMeshes[BSector.MeshRenders[i]];

            (List<Vector3>, List<Vector2>, List<Vector3>) outverttexnorm = ClippingPlanesVertTexNorm((r.Vertices, r.Textures, r.Normals), APlanes);

            CombinedVertices.AddRange(outverttexnorm.Item1);

            CombinedTextures.AddRange(outverttexnorm.Item2);

            CombinedNormals.AddRange(outverttexnorm.Item3);

            ListsOfTriangles[i].Clear();

            if (outverttexnorm.Item1.Count > 2)
            {
                for (int e = 2; e < outverttexnorm.Item1.Count; e++)
                {
                    int a = 0;
                    int b = e - 1;
                    int c = e;

                    ListsOfTriangles[i].Add(a + h);
                    ListsOfTriangles[i].Add(b + h);
                    ListsOfTriangles[i].Add(c + h);
                }
            }

            h += outverttexnorm.Item1.Count;
        }

        MeshCombined.Clear();

        MeshCombined.subMeshCount = BSector.MeshRenders.Count;

        MeshCombined.SetVertices(CombinedVertices);

        MeshCombined.SetUVs(0, CombinedTextures);

        for (int k = 0; k < BSector.MeshRenders.Count; k++)
        {
            MeshCombined.SetTriangles(ListsOfTriangles[k], k);
        }

        MeshCombined.SetNormals(CombinedNormals);

        for (int i = 0; i < MeshCombined.subMeshCount; i++)
        {
            rp.material = materials[Rendering.PolygonInformation[BSector.MeshRenders[i]].MeshNumber];

            Graphics.RenderMesh(rp, MeshCombined, i, matrix);
        }

        for (int i = 0; i < BSector.MeshPortals.Count; ++i)
        {
            RenderingData.PolygonData g = Rendering.PolygonInformation[BSector.MeshPortals[i]];

            RenderingData.PolygonMesh v = Rendering.PolygonMeshes[BSector.MeshPortals[i]];

            List<Mesh> PortMesh = ListsOfMeshes[g.PortalNumber];

            float d = PointDistanceToPlane(Planes[BSector.MeshPortals[i]], CamPoint);

            if (d < -0.1f)
            {
                continue;
            }

            if (VisitedSector.Contains(Rendering.Polyhedrons[g.Portal]) && d <= 0)
            {
                continue;
            }

            if (Sectors.Contains(Rendering.Polyhedrons[g.Portal]))
            {
                ListsOfPlanes[g.PortalNumber].Clear();

                ListsOfPlanes[g.PortalNumber].AddRange(APlanes);

                GetPortals(ListsOfPlanes[g.PortalNumber], Rendering.Polyhedrons[g.Portal], PortMesh[SectorCount]);

                continue;
            }

            if (d != 0)
            {
                List<Vector3> verticesout = ClippingPlanes(v.Vertices, APlanes);

                if (verticesout.Count > 2)
                {

                    ListsOfPlanes[g.PortalNumber].Clear();

                    CreateClippingPlanes(verticesout, ListsOfPlanes[g.PortalNumber], CamPoint);

                    GetPortals(ListsOfPlanes[g.PortalNumber], Rendering.Polyhedrons[g.Portal], PortMesh[SectorCount]);
                }
            }
        }
    }
}

