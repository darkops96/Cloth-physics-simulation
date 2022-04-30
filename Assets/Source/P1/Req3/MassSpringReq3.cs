using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassSpringReq3 : MonoBehaviour
{
    public MassSpringReq3()
    {
        this.Paused = true;
        this.TimeStep = 0.01f;
        this.Gravity = new Vector3(0.0f, -9.81f, 0.0f);
        this.IntegrationMethod = Integration.Symplectic;
        this.tractionStiffness = 100;
        this.flexionStiffness = 50;
        this.mass = 100;
        this.airFriction = 0.25f;
        this.springDamping = 0.15f;
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

    #endregion

    #region Structs
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
    public float mass;
    public float airFriction;
    public float springDamping;

    #endregion

    #region OtherVariables

    public List<Node> nodes;
    public List<Spring> tractionSprings;
    public List<Spring> flexionSprings;

    private List<Edge> edges;

    #endregion    

    #region MonoBehaviour

    public void Start()
    {
        Mesh mesh = this.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        nodes = new List<Node>();
        tractionSprings = new List<Spring>();
        flexionSprings = new List<Spring>();
        edges = new List<Edge>();

        //For simulation purposes, transform the points to global coordinates
        for (int i = 0; i < vertices.Length; i++)
        {
            nodes.Add(new Node(transform.TransformPoint(vertices[i]), i, (mass / vertices.Length)));
        }        

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

        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i].vertexA > edges[i].vertexB)
            {
                Edge edgeAux = edges[i];
                int  nodeAux = edgeAux.vertexA;
                edgeAux.vertexA = edgeAux.vertexB;
                edgeAux.vertexB = nodeAux;
                edges[i] = edgeAux;
            }            
        }

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

        foreach (Edge edge in edges)
        {
            tractionSprings.Add(new Spring(nodes[edge.vertexA], nodes[edge.vertexB], tractionStiffness));
        }

    }

    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.P))
            this.Paused = !this.Paused;

        //Procedure to update vertex positions
        Mesh mesh = this.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = new Vector3[mesh.vertexCount];

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

        foreach (Node node in nodes)
        {
            vertices[node.index] = transform.InverseTransformPoint(node.pos);
        }

        mesh.vertices = vertices;
    }

    public void FixedUpdate()
    {
        if (this.Paused)
            return; // Not simulating

        // Select integration method
        switch (this.IntegrationMethod)
        {
            case Integration.Explicit: this.stepExplicit(); break;
            case Integration.Symplectic: this.stepSymplectic(); break;
            default:
                throw new System.Exception("[ERROR] Should never happen!");
        }

    }

    #endregion

    /// <summary>
    /// Performs a simulation step in 1D using Explicit integration.
    /// </summary>
    private void stepExplicit()
    {
        foreach (Node node in nodes)
        {
            node.force = Vector3.zero;
            node.ComputeForces(Gravity, airFriction);
        }

        foreach (Spring spring in tractionSprings)
        {
            spring.ComputeForces(springDamping);
        }

        foreach (Spring spring in flexionSprings)
        {
            spring.ComputeForces(springDamping);
        }

        foreach (Node node in nodes)
        {
            if (!node.isFixed)
            {
                node.pos += TimeStep * node.vel;
                node.vel += TimeStep / node.mass * node.force;
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

        foreach (Node node in nodes)
        {
            node.force = Vector3.zero;
            node.ComputeForces(Gravity, airFriction);
        }

        foreach (Spring spring in tractionSprings)
        {
            spring.ComputeForces(springDamping);
        }

        foreach (Spring spring in flexionSprings)
        {
            spring.ComputeForces(springDamping);
        }

        foreach (Node node in nodes)
        {
            if (!node.isFixed)
            {
                node.vel += TimeStep / node.mass * node.force;
                node.pos += TimeStep * node.vel;
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
}
