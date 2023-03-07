using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrajectoryPrediction : ImmediateModeShapeDrawer
{
    // Start is called before the first frame update
    private Scene predictionScene;
    private Scene currentScene;
    private PhysicsScene predictionPhysics;
    private PhysicsScene currentPhysics;
    private List<Vector3> points;
    void Start()
    {
        points = new List<Vector3>();
        CreateSceneParameters parameters = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
        predictionScene = SceneManager.CreateScene("Prediction", parameters);
        predictionPhysics = predictionScene.GetPhysicsScene();
        Physics.autoSimulation = false;

        currentScene = SceneManager.GetActiveScene();
        currentPhysics = currentScene.GetPhysicsScene();
    }

    private void FixedUpdate()
    {
        if (currentPhysics.IsValid())
        {
            currentPhysics.Simulate(Time.fixedDeltaTime);
        }
    }

    public void trajectoryPrediction(GameObject subject, GameObject map, int MaxIterations)
    {
        points.Clear();
        GameObject dummy = Instantiate(subject);
        GameObject dummyMap = Instantiate(map);
        SceneManager.MoveGameObjectToScene(dummyMap, predictionScene);
        SceneManager.MoveGameObjectToScene(dummy, predictionScene);
        dummyMap.transform.position = map.transform.position;
        dummy.transform.position = subject.transform.position;
        dummy.GetComponent<Rigidbody>().AddForce(Vector3.forward * 30f + Vector3.up * 50f, ForceMode.Impulse);

        for (int i = 0; i < MaxIterations; i++)
        {
            predictionPhysics.Simulate(Time.fixedDeltaTime);
            points.Add(dummy.transform.position);
           
        }
        Destroy(dummy);
        Destroy(dummyMap);
    }
    public override void DrawShapes(Camera cam)
    {
        foreach (Vector3 point in points)
            DrawMenuDot(cam, point);
    }
    void DrawMenuDot(Camera cam, Vector3 position)
    {
        using (Draw.Command(cam))
        {
            Draw.Radius = 0.02f;
            Draw.BlendMode = ShapesBlendMode.Transparent;
            Draw.Color = new Color(0.75f, 0.42f, 0.55f, 0.65f);
            Draw.Sphere(position);

        }
    }
}
