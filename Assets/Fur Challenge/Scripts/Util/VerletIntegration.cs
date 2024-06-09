using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletIntegration : MonoBehaviour
{
    // GPU instancing 
    [Header("Debug View")]
    [SerializeField] private bool enableDebugRendering;
    [SerializeField] private Mesh debugMesh;
    [SerializeField] private Material debugMaterial;
    [SerializeField] private float debugDrawScale = 0.1f;

    private Vector3 scaleVector;
    private List<Matrix4x4> instanceDataBuffer = new List<Matrix4x4>();

    [Header("Simulation")]
    [SerializeField] private int iterations = 1;
    [SerializeField] private Vector3 gravity;

    // Custom constraints callback (used for moving fixed points)
    public delegate void ApplyCustomConstraintsDelegate(ref Point[] points, ref Connection[] connections);
    public event ApplyCustomConstraintsDelegate OnApplyCustomConstraints;

    public class Point
    {
        public Point(Vector3 pos, bool _locked, float _mass)
        {
            position = pos;
            previousPosition = pos;
            locked = _locked;
            accumulatedAcceleration = Vector3.zero;

            // TODO figure out if this needs better implementation
            inverseMass = 1 / _mass;
        }
        public Vector3 position, previousPosition, accumulatedAcceleration;
        public float inverseMass;
        public bool locked;
    }

    public class Connection
    {
        public Connection(Point _pointA, Point _pointB, float _length)
        {
            pointA = _pointA;
            pointB = _pointB;
            length = _length;
        }
        public Point pointA, pointB;
        public float length;
    }

    private Point[] points = new Point[0];
    private Connection[] connections = new Connection[0];

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Rendering
        if (enableDebugRendering)
        {
            if (!(points.Length > 0)) { return; }

            scaleVector = new Vector3(debugDrawScale, debugDrawScale, debugDrawScale);
            RenderParams renderParams = new RenderParams(debugMaterial);
            renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            UpdateInstanceData();
            Graphics.RenderMeshInstanced(renderParams, debugMesh, 0, instanceDataBuffer);
        }
    }

    public ref Point[] Simulate()
    {
        // Quickly make sure we always do at least 1 iteration
        iterations = Mathf.Max(1, iterations);

        AccumulateForces();
        Integrate();
        ApplyConstraints();

        return ref points;
    }

    public void SetPointData(List<Point> pointList)
    {
        points = new Point[pointList.Count];
        points = pointList.ToArray();
    }

    public void SetConnectionData(List<Connection> connectionList)
    {
        connections = new Connection[connectionList.Count];
        connections = connectionList.ToArray();
    }

    public ref Point[] GetPointData()
    {
        return ref points;
    }

    private void AccumulateForces()
    {
        foreach (var point in points)
        {
            // Reset forces (in this case just write in gravitational force)
            point.accumulatedAcceleration = gravity; // F = ma right?

            // for forces that aren't gravity divide?? by mass
        }
    }

    private void Integrate()
    {
        foreach (Point point in points)
        {
            if (point.locked) continue;

            Vector3 pos = point.position; // This frame's position
            point.position += pos - point.previousPosition + 0.5f * point.accumulatedAcceleration * Time.deltaTime * Time.deltaTime; // Verlet integration
            point.previousPosition = pos;
        }
    }

    private void ApplyConstraints()
    {
        for (int i = 0; i < iterations; i++)
        {
            OnApplyCustomConstraints(ref points, ref connections);
            // Solve connection constraints
            foreach (Connection connection in connections)
            {
                Vector3 axis = connection.pointB.position - connection.pointA.position;

                float distance = axis.magnitude; // Can approximate .magnitude to get rid of square root. This is overkill, do not.
                float diff = (distance - connection.length) / (connection.pointA.inverseMass + connection.pointB.inverseMass);

                // Apply offset, half on each point
                connection.pointA.position += axis.normalized * connection.pointA.inverseMass * diff;
                connection.pointB.position -= axis.normalized * connection.pointB.inverseMass * diff;
            }
        }
    }

    private void UpdateInstanceData()
    {
        instanceDataBuffer.Clear();

        foreach (Point point in points)
        {
            instanceDataBuffer.Add(Matrix4x4.TRS(point.position, Quaternion.identity, scaleVector));
        }
    }
}
