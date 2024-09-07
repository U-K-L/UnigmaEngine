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

    public class UnigmaRendererManager : MonoBehaviour
    {


        public static readonly int UnigmaRendererStride = (sizeof(float) * 4 * 4 * 4) + sizeof(int) * 4 + sizeof(float) * 3 * 4;
        public static UnigmaRendererManager Instance;
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
        }

        public void Initialize(UnigmaScene uScene)
        {
            AddObjectsToList(uScene);
            BuildTriangleList();
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

        void ReleaseBuffers()
        {
            if (_verticesObjectBuffer != null)
                _verticesObjectBuffer.Release();
            if (_indicesObjectBuffer != null)
                _indicesObjectBuffer.Release();
            if (_unigmaRendererObjectBuffer != null)
                _unigmaRendererObjectBuffer.Release();
        }

    }
}