using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node {

    public Vector3 pos;
    public Vector3 vel;
    public Vector3 force;
    public int index;

    public float mass;

    public bool isFixed = false;

    // Use this for initialization
    public Node(Vector3 position, int id, float m)
    {
        pos = position;
        index = id;
        mass = m;
        vel = Vector3.zero;
    }

    public void UpdateMass(float m)
    {
        mass = m;
    }

    public void ComputeForces(Vector3 gravity, float damping)
    {
        float d = damping * mass;
        force += mass * gravity;
        force -= d * vel;
    }
}
