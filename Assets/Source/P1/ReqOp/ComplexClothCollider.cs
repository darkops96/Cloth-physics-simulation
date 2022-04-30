using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComplexClothCollider : MonoBehaviour
{
    public ComplexClothCollider()
    {
        this.rigidity = 15;
    }

    #region InEditorVariables

    public float rigidity;

    #endregion

    #region OtherVariables   

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    #endregion    

    #region Structs
    private struct CollisionInfo
    {
        public float distance { get; }
        public Vector3 triangleNormal { get; }

        public CollisionInfo(Vector3 normal, float dist)
        {
            distance = dist;
            triangleNormal = new Vector3(normal.x, normal.y, normal.z);
        }
    }
    #endregion

    #region MonoBehaviour  

    // Start is called before the first frame update
    void Start()
    {
        mesh = this.GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        triangles = mesh.triangles;
    }

    // Update is called once per frame
    void Update()
    {
    }

    #endregion

    public void ComputePenaltyForce(Node node)
    {
        //Getting closest triangle plane, its normal and the distance
        float minDist = float.PositiveInfinity;
        float maxDist = float.NegativeInfinity;
        Vector3 normal = Vector3.zero;
        CollisionInfo result;

        for (int i = 0; i < triangles.Length-1; i += 3)
        {
            result = GetBoundingPlanes(node.pos, triangles[i], triangles[i + 1], triangles[i + 2]);
            if (minDist > result.distance)
            {
                minDist = result.distance;
                normal.Set(result.triangleNormal.x, result.triangleNormal.y, result.triangleNormal.z);
            }
            if (maxDist < result.distance)
            {
                maxDist = result.distance;
            }
        }

        //Computing the penalty force if the node is inside the object
        if (maxDist < 0)
        {
            node.force += rigidity * (-minDist) * normal;
        }
    }

    private CollisionInfo GetBoundingPlanes(Vector3 point, int indexA, int indexB, int indexC)
    {
        //Triangle vertex
        Vector3 vA = transform.TransformPoint(vertices[triangles[indexA]]);
        Vector3 vB = transform.TransformPoint(vertices[triangles[indexB]]);
        Vector3 vC = transform.TransformPoint(vertices[triangles[indexC]]);

        //Plane vectors
        Vector3 cb = vB - vC;
        Vector3 ca = vA - vC;

        Vector3 normalC = Vector3.Cross(cb, ca).normalized;

        Vector3 ba = vA - vB;
        Vector3 bc = vC - vB;

        Vector3 normalB = Vector3.Cross(ba, bc).normalized;

        Vector3 ab = vB - vA;
        Vector3 ac = vC - vA;

        Vector3 normalA = Vector3.Cross(ac, ab).normalized;

        Vector3 normal = ((normalC + normalB + normalA) / 3).normalized;

        Vector3 barycentre = new Vector3((vA.x + vB.x + vC.x) / 3, (vA.y + vB.y + vC.y) / 3, (vA.z + vB.z + vC.z) / 3);
        Vector3 dotC = point - barycentre; 

        float proyection = Vector3.Dot(dotC, normal); //If proyection sign is positive, the point is outside the triangle

        return new CollisionInfo(transform.InverseTransformDirection(normal), proyection);
    }
}

