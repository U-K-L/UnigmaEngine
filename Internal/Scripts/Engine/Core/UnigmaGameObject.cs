using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnigmaEngine
{
    public class UnigmaGameObject : MonoBehaviour
    {

        public uint id { get; set; }

        public struct UnigmaGameObjectStruct
        {
            public uint objectId;
            public uint physicsId;
            public uint renderId;
        }

        uint UnigmaGameObjectStride = sizeof(int) * 4;

        public UnigmaGameObjectStruct unigmaGameObject;

        //Goes in "Editor" folder.
        //Creates this item from the editor and calls the function under this declartion.
        [MenuItem("GameObject/Unigma/Unigma Game Object", false, 10)]
        static void CreateUnigmaObject(MenuCommand menuCommand)
        {
            // Create a new GameObject
            GameObject customObject = new GameObject("UnigmaObject");

            //Add the components to this object.
            customObject.AddComponent<UnigmaPhysicsObject>();
            customObject.AddComponent<UnigmaRendererObject>();

            // Ensure the new GameObject is placed correctly in the hierarchy
            GameObjectUtility.SetParentAndAlign(customObject, menuCommand.context as GameObject);

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(customObject, "Create My Custom Object");
            Selection.activeObject = customObject;
        }

        //Creates this item from the editor and calls the function under this declartion.
        [MenuItem("GameObject/Unigma/Change To Unigma Game Object", false, 10)]
        static void TransformToUnigmaObject(MenuCommand menuCommand)
        {
            MeshRenderer[] meshes = Selection.activeGameObject.GetComponentsInChildren<MeshRenderer>();

            for (int i = 0; i < meshes.Length; i++)
            {
                GameObject gobj = meshes[i].gameObject;
                AddUnigmaComponent<UnigmaGameObject>(gobj);
                AddUnigmaComponent<UnigmaPhysicsObject>(gobj);
                AddUnigmaComponent<UnigmaRendererObject>(gobj);

            }
        }
        private void Awake()
        {
            AddUnigmaComponent<UnigmaPhysicsObject>(gameObject);
            AddUnigmaComponent<UnigmaRendererObject>(gameObject);
        }

        public void Initialize()
        {
            unigmaGameObject = new UnigmaGameObjectStruct();

            //Initialize to default settings. This is changed via each manager, within scene managers.
            unigmaGameObject.objectId = id;
            unigmaGameObject.physicsId = id;
            unigmaGameObject.renderId = id;

        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public static void AddUnigmaComponent<T>(GameObject gobj) where T : Component
        {
            if (gobj.GetComponent<T>() == null)
                gobj.AddComponent<T>();
        }
    }
}