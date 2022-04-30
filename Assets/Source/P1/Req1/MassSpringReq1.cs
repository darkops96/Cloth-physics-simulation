using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sample code for accessing MeshFilter data.
/// </summary>
public class MassSpring : MonoBehaviour
{
    /// <summary>
    /// Default constructor. Zero all. 
    /// </summary>
    public MassSpring()
    {
        this.Paused = true;
        this.TimeStep = 0.01f;
        this.Gravity = new Vector3(0.0f, -9.81f, 0.0f);
        this.IntegrationMethod = Integration.Symplectic;
        this.stiffness = 100;
        this.mass = 100;
        this.airFriction = 0;
        this.springDamping = 0;
    }

    /// <summary>
	/// Integration method.
	/// </summary>
	public enum Integration
    {
        Explicit = 0,
        Symplectic = 1,
    };

    #region InEditorVariables

    public bool Paused;
    public float TimeStep;
    public Vector3 Gravity;
    public Integration IntegrationMethod;
    public float stiffness;
    public float mass;
    public float airFriction;
    public float springDamping;

    public List<Node> nodes;
    public List<Spring> springs;

    #endregion

    #region OtherVariables

    #endregion

    #region MonoBehaviour

    public void Start()
    {
        Mesh mesh = this.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        nodes = new List<Node>();
        springs = new List<Spring>();

        //For simulation purposes, transform the points to global coordinates
        for (int i = 0; i < vertices.Length; i++)
        {
            nodes.Add(new Node(transform.TransformPoint(vertices[i]), i, (mass / vertices.Length)));
        }

        //Fijar nodos
        for(int i = 0; i < 11; i++)
        {
            nodes[i].isFixed = true;
        }

        for (int i = 0; i < triangles.Length; i++)
        {
            springs.Add(new Spring(nodes[triangles[i]], nodes[triangles[i + 1]], stiffness));
            i++;

            springs.Add(new Spring(nodes[triangles[i]], nodes[triangles[i + 1]], stiffness));
            i++;

            springs.Add(new Spring(nodes[triangles[i]], nodes[triangles[i - 2]], stiffness));
        }
    }

    public void Update()
    {
        //Procedure to update vertex positions
        Mesh mesh = this.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = new Vector3[mesh.vertexCount];

        if (Input.GetKeyUp(KeyCode.P))
            this.Paused = !this.Paused;

        foreach(Node node in nodes)
        {
            node.UpdateMass((mass / vertices.Length));
        }

        foreach (Spring spring in springs)
        {
            spring.UpdateStiffness(stiffness);
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

        foreach (Spring spring in springs)
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

        foreach (Spring spring in springs)
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

        foreach (Spring spring in springs)
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

        foreach (Spring spring in springs)
        {
            spring.UpdateLength();
        }
    }
}
