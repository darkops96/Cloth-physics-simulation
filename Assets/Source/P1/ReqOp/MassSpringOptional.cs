using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassSpringOptional : MonoBehaviour
{
    public MassSpringOptional()
    {
        this.Paused = true;
        this.TimeStep = 0.01f;
        this.Gravity = new Vector3(0.0f, -9.81f, 0.0f);
        this.IntegrationMethod = Integration.Symplectic;
        this.tractionStiffness = 100;
        this.flexionStiffness = 50;
        this.mass = 100;
        this.substeps = 1;
        this.airFriction = 0.25f;
        this.springDamping = 0.15f;
        this.airVelocity = Vector3.zero;
        this.airBehaviour = AirType.Constant;
        this.clothFriction = 0.5f;
    }

    #region Enums

    /// <summary>
    /// Integration method.
    /// </summary>
    public enum Integration
    {
        Explicit = 0,
        Symplectic = 1,
    };

    /// <summary>
    /// Air Behaviour.
    /// </summary>
    public enum AirType
    {
        Constant = 0,
        Changing = 1,
    };

    #endregion

    #region Structs

    /// <summary>
    /// Struct to storage triangle's edges
    /// </summary>
    private struct Edge
    {
        public int vertexA { get; set; }
        public int vertexB { get; set; }
        public int otherVertex { get; set; }

        public Edge(Node a, Node b, Node other)
        {
            vertexA = a.index;
            vertexB = b.index;
            otherVertex = other.index;
        }

        public override string ToString() => $"({vertexA}, {vertexB}, {otherVertex})";
        public bool Equals(Edge edge)
        {
            if ((this.vertexA == edge.vertexA) && (this.vertexB == edge.vertexB))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    #endregion

    #region InEditorVariables

    public bool Paused;
    public float TimeStep;
    public Vector3 Gravity;
    public Integration IntegrationMethod;
    public float tractionStiffness;
    public float flexionStiffness;
    public float mass; //Mass of whole cloth
    public int substeps;
    public float airFriction;
    public float springDamping;
    public Vector3 airVelocity;
    public AirType airBehaviour;
    public float clothFriction;
    public List<ClothCollider> obstacles;
    public List<ComplexClothCollider> complexObstacles;

    #endregion

    #region OtherVariables

    public List<Node> nodes;
    public List<Spring> tractionSprings;
    public List<Spring> flexionSprings;

    private List<Edge> edges;

    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    #endregion    

    #region MonoBehaviour

    public void Start()
    {
        mesh = this.GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        triangles = mesh.triangles;

        nodes = new List<Node>();
        tractionSprings = new List<Spring>();
        flexionSprings = new List<Spring>();
        edges = new List<Edge>();

        //Getting all cloth nodes
        for (int i = 0; i < vertices.Length; i++)
        {
            nodes.Add(new Node(transform.TransformPoint(vertices[i]), i, (mass / vertices.Length)));
        }

        //Creating all the edges
        for (int i = 0; i < triangles.Length; i++)
        {
            Edge[] newEdges = new Edge[3];

            newEdges[0].vertexA = triangles[i];
            newEdges[1].vertexB = triangles[i];
            newEdges[2].otherVertex = triangles[i];
            i++;


            newEdges[0].vertexB = triangles[i];
            newEdges[1].otherVertex = triangles[i];
            newEdges[2].vertexA = triangles[i];
            i++;

            newEdges[0].otherVertex = triangles[i];
            newEdges[1].vertexA = triangles[i];
            newEdges[2].vertexB = triangles[i];

            edges.Add(newEdges[0]);
            edges.Add(newEdges[1]);
            edges.Add(newEdges[2]);
        }
        
        //Vertex A must have a lower index than vertex B in all edges
        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i].vertexA > edges[i].vertexB)
            {
                Edge edgeAux = edges[i];
                int nodeAux = edgeAux.vertexA;
                edgeAux.vertexA = edgeAux.vertexB;
                edgeAux.vertexB = nodeAux;
                edges[i] = edgeAux;
            }
        }

        //Deleting duplicated edges and creating flexion springs
        for (int i = 0; i < edges.Count; i++)
        {
            for (int j = i + 1; j < edges.Count; j++)
            {
                if (edges[i].Equals(edges[j]))
                {
                    flexionSprings.Add(new Spring(nodes[edges[i].otherVertex], nodes[edges[j].otherVertex], flexionStiffness));
                    edges.Remove(edges[j]);
                }
            }
        }

        //Creating traction springs
        foreach (Edge edge in edges)
        {
            tractionSprings.Add(new Spring(nodes[edge.vertexA], nodes[edge.vertexB], tractionStiffness));
        }

    }

    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.P))
            this.Paused = !this.Paused;

        foreach (Node node in nodes)
        {
            node.UpdateMass((mass / vertices.Length));
        }

        foreach (Spring spring in tractionSprings)
        {
            spring.UpdateStiffness(tractionStiffness);
        }

        foreach (Spring spring in flexionSprings)
        {
            spring.UpdateStiffness(flexionStiffness);
        }

        //Update node's positions
        Vector3[] newVertices = new Vector3[mesh.vertexCount];

        foreach (Node node in nodes)
        {
            newVertices[node.index] = transform.InverseTransformPoint(node.pos);
        }

        mesh.vertices = newVertices;
        mesh.RecalculateNormals();
    }

    public void FixedUpdate()
    {
        if (this.Paused)
            return; // Not simulating

        for (int i = 0; i < substeps; i++)
        {
            // Select integration method
            switch (this.IntegrationMethod)
            {
                case Integration.Explicit: this.stepExplicit(); break;
                case Integration.Symplectic: this.stepSymplectic(); break;
                default:
                    throw new System.Exception("[ERROR] Should never happen!");
            }
        }

    }

    #endregion

    /// <summary>
    /// Performs a simulation step in 1D using Explicit integration.
    /// </summary>
    private void stepExplicit()
    {
        float subTimeStep = TimeStep / ((float)substeps);

        //Computing forces that affect nodes separately
        foreach (Node node in nodes)
        {
            node.force = Vector3.zero;
            node.ComputeForces(Gravity, airFriction);

            foreach (ClothCollider obstacle in obstacles)
            {
                obstacle.ComputePenaltyForce(node);
            }

            foreach (ComplexClothCollider obstacle in complexObstacles)
            {
                obstacle.ComputePenaltyForce(node);
            }
        }

        for (int i = 2; i < triangles.Length; i += 3)
        {
            AirForce(nodes[triangles[i - 2]], nodes[triangles[i - 1]], nodes[triangles[i]]);
        }

        foreach (Spring spring in tractionSprings)
        {
            spring.ComputeForces(springDamping);
        }

        foreach (Spring spring in flexionSprings)
        {
            spring.ComputeForces(springDamping);
        }

        //Computing new position and velocity
        foreach (Node node in nodes)
        {
            if (!node.isFixed)
            {
                node.pos += subTimeStep * node.vel;
                node.vel += subTimeStep / node.mass * node.force;
            }
        }

        foreach (Spring spring in tractionSprings)
        {
            spring.UpdateLength();
        }

        foreach (Spring spring in flexionSprings)
        {
            spring.UpdateLength();
        }
    }

    /// <summary>
    /// Performs a simulation step in 1D using Symplectic integration.
    /// </summary>
    private void stepSymplectic()
    {
        float subTimeStep = TimeStep / ((float)substeps);

        //Computing forces that affect nodes separately
        foreach (Node node in nodes)
        {
            node.force = Vector3.zero;
            node.ComputeForces(Gravity, airFriction);

            foreach (ClothCollider obstacle in obstacles)
            {
                obstacle.ComputePenaltyForce(node);
            }

            foreach (ComplexClothCollider obstacle in complexObstacles)
            {
                obstacle.ComputePenaltyForce(node);
            }
        }

        for (int i = 2; i < triangles.Length; i += 3)
        {
            AirForce(nodes[triangles[i - 2]], nodes[triangles[i - 1]], nodes[triangles[i]]);
        }

        foreach (Spring spring in tractionSprings)
        {
            spring.ComputeForces(springDamping);
        }

        foreach (Spring spring in flexionSprings)
        {
            spring.ComputeForces(springDamping);
        }

        //Computing new position and velocity
        foreach (Node node in nodes)
        {            
            if (!node.isFixed)
            {                
                node.vel += subTimeStep / node.mass * node.force;
                node.pos += subTimeStep * node.vel;
            }                
        }

        foreach (Spring spring in tractionSprings)
        {
            spring.UpdateLength();
        }

        foreach (Spring spring in flexionSprings)
        {
            spring.UpdateLength();
        }
    }

    private void AirForce(Node a, Node b, Node c)
    {
        //Getting plane vectors
        Vector3 cb = (b.pos - c.pos);
        Vector3 ca = (a.pos - c.pos);

        //Getting triangle's properties
        Vector3 normal = Vector3.Cross(cb, ca).normalized;
        Vector3 triangleVel = (a.vel + b.vel + c.vel) / 3;
        float area = (Vector3.Cross(cb, ca).magnitude) / 2;

        //Computing air velocity
        if (airBehaviour == AirType.Changing)
        {
            float sinTime = Mathf.Sin(Time.time / 2);
            float cosTime = Mathf.Cos(Time.time / 2);
            airVelocity.x = 10 * sinTime;
            airVelocity.y = 10 * sinTime * (-cosTime);
            airVelocity.z = 20 * cosTime;
        }

        Vector3 airForce = (clothFriction * area * (Vector3.Dot(normal, (airVelocity - triangleVel))) * normal) / 3;

        a.force += airForce;
        b.force += airForce;
        c.force += airForce;
    }
}
