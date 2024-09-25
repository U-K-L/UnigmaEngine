using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnigmaEngine
{
    public struct UnigmaRendererObjectStruct
    {
        public Matrix4x4 localToWorld;
        public uint indicesOffset;
        public uint indicesCount;
        public Vector3 position;
        public Vector3 AABBMin;
        public Vector3 AABBMax;
        public Vector3 color;
        public Matrix4x4 collisionForcesHigh;
        public Matrix4x4 collisionForcesLow;
        public uint id;
        public Matrix4x4 absorbtionMatrix;
        public int emitterType;
    };

    struct AABB
    {
        public Vector3 min;
        public Vector3 max;
    }

    public class UnigmaRendererManager : MonoBehaviour
    {


        public static readonly int UnigmaRendererStride = (sizeof(float) * 4 * 4 * 4) + sizeof(int) * 4 + sizeof(float) * 3 * 4;
        public static UnigmaRendererManager Instance;
        Dictionary<string, AABB> AABBs; 
        public struct Vertex
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector2 uv;
        };


        [HideInInspector]
        public UnigmaRendererObjectStruct[] unigmaRendererObjects;

        public List<Vertex> _vertices = new List<Vertex>();
        public List<int> _indices = new List<int>();

        public List<UnigmaRendererObject> _renderObjects = new List<UnigmaRendererObject>();

        public Dictionary<string, List<UnigmaRendererObject>> renderContainers;

        //Compute Buffers.
        [HideInInspector]
        public ComputeBuffer _unigmaRendererObjectBuffer { get; private set; }
        [HideInInspector]
        public ComputeBuffer _verticesObjectBuffer { get; private set; }
        [HideInInspector]
        public ComputeBuffer _indicesObjectBuffer { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            AABBs = new Dictionary<string, AABB>();
            renderContainers = new Dictionary<string, List<UnigmaRendererObject>>();
        }

        public void Initialize(UnigmaScene uScene)
        {
            AddObjectsToList(uScene);
            BuildTriangleList();
            CreateContainers(uScene);
        }

        void AddObjectsToList(UnigmaScene uScene)
        {
            for (int i = 0; i < uScene.unigmaGameObjects.Length; i++)
            {
                UnigmaRendererObject rObj = uScene.unigmaGameObjects[i].GetComponent<UnigmaRendererObject>();
                if (rObj != null)
                    _renderObjects.Add(rObj);
            }
            Debug.Log("Count of render objects: " + _renderObjects.Count);
        }

        private void Update()
        {
            UpdateRendererObject();

            /*
            string[] keys = renderContainers.Keys.ToArray();
            foreach(string key in keys)
                CombineAABB(key);
            */

        }

        void UpdateRendererObject()
        {
            if (_unigmaRendererObjectBuffer.count > 0)
            {
                _unigmaRendererObjectBuffer.SetData(unigmaRendererObjects);
            }
        }

        void BuildTriangleList()
        {
            _vertices.Clear();
            _indices.Clear();

            int indexMeshObject = 0;
            unigmaRendererObjects = new UnigmaRendererObjectStruct[_renderObjects.Count];
            for (int rIndex = 0; rIndex < _renderObjects.Count; rIndex++)
            {
                int urobjIndex = 0;
                UnigmaRendererObject urobj = GetComponent<UnigmaRendererObject>();
                Renderer r = _renderObjects[rIndex]._renderer;
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf)
                {
                    Mesh m = mf.sharedMesh;
                    int startVert = _vertices.Count;
                    int startIndex = _indices.Count;

                    for (int i = 0; i < m.vertices.Length; i++)
                    {
                        Vertex v = new Vertex();
                        v.position = m.vertices[i];
                        if (i < m.normals.Length)
                            v.normal = m.normals[i];
                        if (i < m.uv.Length)
                            v.uv = m.uv[i];
                        _vertices.Add(v);
                    }
                    var indices = m.GetIndices(0);
                    _indices.AddRange(indices.Select(index => index + startVert));

                    // Add the object itself
                    unigmaRendererObjects[indexMeshObject] = new UnigmaRendererObjectStruct()
                    {
                        localToWorld = r.transform.localToWorldMatrix,
                        indicesOffset = (uint)startIndex,
                        indicesCount = (uint)indices.Length,
                        id = (uint)rIndex

                    };

                    if (urobj != null)
                    {
                        urobj._renderID = urobjIndex;
                        urobjIndex++;
                    }
                    indexMeshObject++;
                }
            }
            if (unigmaRendererObjects.Length > 0)
            {
                _unigmaRendererObjectBuffer = new ComputeBuffer(unigmaRendererObjects.Length, UnigmaRendererStride);
                _verticesObjectBuffer = new ComputeBuffer(_vertices.Count, 32);
                _indicesObjectBuffer = new ComputeBuffer(_indices.Count, 4);
                _verticesObjectBuffer.SetData(_vertices);
                _indicesObjectBuffer.SetData(_indices);
            }

        }

        public void UpdateRenderArray(int index, UnigmaRendererObjectStruct robj)
        {
            unigmaRendererObjects[index] = robj;
        }

        void ReleaseBuffers()
        {
            if (_verticesObjectBuffer != null)
                _verticesObjectBuffer.Release();
            if (_indicesObjectBuffer != null)
                _indicesObjectBuffer.Release();
            if (_unigmaRendererObjectBuffer != null)
                _unigmaRendererObjectBuffer.Release();
        }

        void CreateContainers(UnigmaScene uscene)
        {

            renderContainers.Clear();
            //Find the container. Then get all the game objects under it.
            foreach (UnigmaGameObject uobj in UnigmaSceneManager.currentScene.unigmaGameObjects)
            {
                if (uobj.isContainer)
                {
                    List<UnigmaRendererObject> gobjsToMerge = new List<UnigmaRendererObject>();
                    UnigmaGameObject[] uobjInContainer = uobj.GetComponentsInChildren<UnigmaGameObject>();

                    foreach (UnigmaGameObject uobjC in uobjInContainer)
                    {
                        UnigmaRendererObject urobj = GetComponent<UnigmaRendererObject>();
                        if (uobjC.GroupingTag == uobj.GroupingId && !uobjC.isContainer && urobj != null)
                        {
                            gobjsToMerge.Add(urobj);
                            break;
                        }
                    }
                    renderContainers.Add(uobj.GroupingId, gobjsToMerge);
                }
            }
        }

        void CombineAABB(string containerId)
        {
            List<UnigmaRendererObject> gobjsToMerge = renderContainers[containerId];
            AABB finalMergedAABB = new AABB();
            finalMergedAABB.min = Vector3.positiveInfinity;
            finalMergedAABB.max = Vector3.negativeInfinity;

            //Check if they have the same grouping Id, if so merge.
            foreach (UnigmaRendererObject urobj in gobjsToMerge)
            {
                finalMergedAABB = MergeAABB(finalMergedAABB.min, finalMergedAABB.max, urobj.unigmaRendererObject.AABBMin, urobj.unigmaRendererObject.AABBMax);
            }

            if (AABBs.ContainsKey(containerId))
            {
                AABBs[containerId] = finalMergedAABB;
            }
            else
                AABBs.Add(containerId, finalMergedAABB);
        }

        AABB MergeAABB(Vector3 amin, Vector3 amax, Vector3 bmin, Vector3 bmax) {

            AABB result = new AABB();
            result.min.x = Mathf.Min(amin.x, bmin.x); 
            result.min.y = Mathf.Min(amin.y, bmin.y); 
            result.min.z = Mathf.Min(amin.z, bmin.z); 
            result.max.x = Mathf.Max(amax.x, bmax.x); 
            result.max.y = Mathf.Max(amax.y, bmax.y);
            result.max.z = Mathf.Max(amax.z, bmax.z);
            return result;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            if (AABBs.ContainsKey("SunnyHome"))
            {
                AABB aabb = AABBs["SunnyHome"];

                Debug.Log("Contains sunny room: " + aabb.min + " | " + aabb.max);

                // Assuming min and max are part of the AABB class or structure
                Vector3 center = (aabb.min + aabb.max) / 2; // Calculate the center
                Vector3 size = aabb.max - aabb.min;         // Calculate the size

                Gizmos.DrawWireCube(center, size);
            }
        }

    }
}