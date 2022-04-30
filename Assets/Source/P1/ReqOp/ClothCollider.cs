using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothCollider : MonoBehaviour
{  

    public ClothCollider()
    {
        this.rigidity = 15;
        this.objectGeometry = Geometry.Plane;
    }

    #region InEditorVariables

    public Geometry objectGeometry;
    public float rigidity;

    #endregion

    #region OtherVariables   

    private Plane plane;
    private float radius;
    private Vector3 centre;

    #endregion

    #region Enums

    public enum Geometry
    {
        Plane = 0,
        Sphere = 1
    };

    #endregion    

    #region MonoBehaviour  

    // Start is called before the first frame update
    void Start()
    {
        if (objectGeometry == Geometry.Plane)
        {
            Vector3 normal = Vector3.zero;
            MeshFilter meshFilter = this.GetComponent<MeshFilter>();

            if (meshFilter && meshFilter.mesh.normals.Length > 0)
            {
                normal = meshFilter.transform.TransformDirection(meshFilter.mesh.normals[0]);
            }

            plane = new Plane(normal, 0.2f * normal + transform.position);
        }
        else if (objectGeometry == Geometry.Sphere)
        {
            centre = this.transform.position;
            radius = this.transform.localScale.x;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (objectGeometry == Geometry.Plane)
        {
            Vector3 normal = Vector3.zero;
            MeshFilter meshFilter = this.GetComponent<MeshFilter>();

            if (meshFilter && meshFilter.mesh.normals.Length > 0)
            {
                normal = meshFilter.transform.TransformDirection(meshFilter.mesh.normals[0]);
            }

            plane = new Plane(normal, 0.2f * normal + transform.position);        
        }
        else if (objectGeometry == Geometry.Sphere)
        {
            centre = this.transform.position;
            radius = this.transform.localScale.x;
        }
    }

    #endregion

    public void ComputePenaltyForce(Node node)
    {
        if (objectGeometry == Geometry.Plane)
        {
            float distance = -plane.GetDistanceToPoint(node.pos);

            if (!plane.GetSide(node.pos))
            {
                node.pos += distance * plane.normal;
                node.force += rigidity * distance * plane.normal;
            }            
        }
        else if (objectGeometry == Geometry.Sphere)
        {
            Vector3 vecDis = (node.pos - centre);
            float distance = vecDis.magnitude;

            if (distance < radius)
            {
                node.force += rigidity * (radius - distance) * vecDis.normalized;
            }
        }               
    }
}
