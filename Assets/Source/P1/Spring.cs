using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spring {

    private Node nodeA, nodeB;

    public float Length0;
    public float Length;

    public float stiffness;

    private Vector3 u;

    // Use this for initialization
    public Spring (Node nA, Node nB, float stiff) {
        nodeA = nA;
        nodeB = nB;
        stiffness = stiff;
        UpdateLength();
        Length0 = Length;
    }

    public void UpdateStiffness(float stiff)
    {
        stiffness = stiff;
    }

    public void UpdateLength ()
    {
        Length = (nodeA.pos - nodeB.pos).magnitude;
        u = nodeA.pos - nodeB.pos;
        u.Normalize();
    }

    public void ComputeForces(float damping)
    {
        Vector3 force = - stiffness * (Length - Length0) * u;

        float d = damping * stiffness;
        Vector3 dampingForce = -d * Vector3.Dot(u, (nodeA.vel - nodeB.vel)) * u;

        nodeA.force += force;
        nodeA.force += dampingForce;

        nodeB.force -= force;
        nodeB.force -= dampingForce;
    }

    public bool Equals(Spring spring)
    {
        if ((this.nodeA.index == spring.nodeA.index) && (this.nodeB.index == spring.nodeB.index))
        {
            return true;
        }
        else if ((this.nodeB.index == spring.nodeA.index) && (this.nodeA.index == spring.nodeB.index))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
