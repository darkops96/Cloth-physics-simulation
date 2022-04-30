using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixerReq3 : MonoBehaviour
{
    #region InEditorVariables

    public List<MassSpringReq3> cloths;

    #endregion

    #region OtherVariables

    public List<Node> fixedNodes;
    private Vector3 oldPosition;
    private Bounds bound;

    #endregion

    #region MonoBehaviour

    // Start is called before the first frame update
    void Start()
    {
        fixedNodes = new List<Node>();
        oldPosition = this.transform.position;

        BoxCollider collider = this.GetComponent<BoxCollider>();
        bound = collider.bounds;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount == 1)
        {
            //Getting nodes inside the fixer
            foreach (MassSpringReq3 cloth in cloths)
            {
                foreach (Node node in cloth.nodes)
                {
                    if (bound.Contains(node.pos))
                    {
                        node.isFixed = true;
                        fixedNodes.Add(node);
                    }
                }
            }
        }

        //Computing fixer tranformation and applying it to fixed nodes
        foreach (Node node in fixedNodes)
        {
            node.pos -= (oldPosition - this.transform.position);
        }
        oldPosition = this.transform.position;
    }

    #endregion
}
