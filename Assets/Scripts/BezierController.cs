using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PathInfo
{
    public Vector3 pos;
    public Vector3 dir;
}

public class BezierController : MonoBehaviour
{
    [SerializeField]
    private GameObject spherePoint;
    private GameObject nextControlPoint;
    private Dictionary<GameObject, int> controlPoints = new Dictionary<GameObject, int>();
    private List<GameObject> controlPointsList = new List<GameObject>();
    private GameObject controlPointToUpdate = null;
    private List<GameObject> bezierPoints = new List<GameObject>();

    const int numBezierPoints = 100;
    private double bezierCurveLength;

    private enum BezierUpdateChange
    {
        ControlPointAdded,
        ControlPointRemoved,
        ControlPointTransformed
    };

    // Start is called before the first frame update
    void Start()
    {
        nextControlPoint = Instantiate(spherePoint);
        nextControlPoint.SetActive(false);
        nextControlPoint.transform.position = Vector3.zero;
        nextControlPoint.GetComponent<MeshRenderer>().material.color = Color.green;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateNextControlPoint();
        SpawnControlPoint();
        UpdateControlPoints();
        UpdateUndoPoints();
    }

    public PathInfo GetPathInfoAt(float t)
    {
        double distFromStart = bezierCurveLength * t;

        int i = 0;
        for (i = 0; i < bezierPoints.Count - 1 && distFromStart > 0.0f; i++)
        {
            distFromStart -= (bezierPoints[i + 1].transform.position - bezierPoints[i].transform.position).magnitude;
        }

        PathInfo pathInfo = new PathInfo();
        Vector3 lastDir = (bezierPoints[i].transform.position - bezierPoints[i - 1].transform.position);
        pathInfo.dir = lastDir.normalized;
        pathInfo.pos = bezierPoints[i].transform.position + ((float)(distFromStart / lastDir.magnitude) * lastDir);
        return pathInfo;
    }

    private void UpdateNextControlPoint()
    {
        if (!Input.GetMouseButton(1))
        {
            nextControlPoint.SetActive(false);
            return;
        }

        Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(r, out hit))
        {
            if (hit.collider.gameObject != nextControlPoint)
            {
                Vector3 worldPosition = hit.point;
                nextControlPoint.SetActive(true);
                nextControlPoint.transform.position = worldPosition;
            }
        }
        else
        {
            nextControlPoint.SetActive(false);
        }
    }

    private void SpawnControlPoint()
    {
        if (!Input.GetMouseButton(1) || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        GameObject newControlPoint = Instantiate(spherePoint);
        newControlPoint.SetActive(true);
        newControlPoint.transform.position = nextControlPoint.transform.position;

        if (controlPointsList.Count > 0) 
        {
            Vector3 distDiff = newControlPoint.transform.position - controlPointsList.Last().transform.position;
            GameObject lastControlPoint = controlPointsList.Last();

            for (int i = 0; i < 2; i++)
            {
                GameObject newMiddleControlPoint = Instantiate(spherePoint);
                newMiddleControlPoint.SetActive(true);
                newMiddleControlPoint.transform.position = lastControlPoint.transform.position + (distDiff * ((float)(i + 1)/3.0f));
                newMiddleControlPoint.transform.localScale *= 0.5f;
                newMiddleControlPoint.GetComponent<MeshRenderer>().material.color = Color.black;
                controlPoints.Add(newMiddleControlPoint, controlPointsList.Count);
                controlPointsList.Add(newMiddleControlPoint);
            }
        }

        controlPoints.Add(newControlPoint, controlPointsList.Count);
        controlPointsList.Add(newControlPoint);

        UpdateBezier(BezierUpdateChange.ControlPointAdded);
    }

    private void UpdateControlPoints()
    {
        if (!Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            controlPointToUpdate = null;
            return;
        }

        if(!controlPointToUpdate)
        {
            foreach (var controlPoint in controlPoints)
            {
                controlPoint.Key.GetComponent<Collider>().enabled = true;
            }

            Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(r, out hit) && controlPoints.ContainsKey(hit.collider.gameObject))
            {
                controlPointToUpdate = hit.collider.gameObject;
            }

            foreach (var controlPoint in controlPoints)
            {
                controlPoint.Key.GetComponent<Collider>().enabled = false;
            }
        }

        if(controlPointToUpdate)
        {
            Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(r, out hit))
            {
                Vector3 worldPosition = hit.point;
                controlPointToUpdate.transform.position = worldPosition;

                UpdateBezier(BezierUpdateChange.ControlPointTransformed);
            }
        }
    }

    private void UpdateBezier(BezierUpdateChange bezierChangeFlag)
    {
        if (controlPointsList.Count <= 1) return;

        if (bezierChangeFlag == BezierUpdateChange.ControlPointAdded)
        {
            Vector3 distDiff = controlPointsList.Last().transform.position - controlPointsList[^4].transform.position;
            GameObject startPoint = controlPointsList[^4];

            for (int i = 0; i < numBezierPoints; i++)
            {
                GameObject bezierPoint = Instantiate(spherePoint);
                bezierPoint.SetActive(true);
                bezierPoint.transform.position = startPoint.transform.position + (distDiff * ((float)(i) / (float)(numBezierPoints - 1)));
                bezierPoint.transform.localScale *= 0.08f;
                bezierPoint.GetComponent<MeshRenderer>().material.color = Color.red;
                bezierPoints.Add(bezierPoint);
            }
        }
        else if(bezierChangeFlag  == BezierUpdateChange.ControlPointTransformed)
        {
            controlPoints.TryGetValue(controlPointToUpdate, out int controlPointIndex);
            int firstControlPointIndex = controlPointIndex - (controlPointIndex % 3);
            if ((controlPointIndex % 3) == 0) firstControlPointIndex = Math.Max(controlPointIndex - 3, 0);
            
            int bezierPointsChunck = Math.Max(Mathf.FloorToInt((float)controlPointIndex / 3.0f - 0.0001f), 0);

            int firstBezierPointToUpdate = bezierPointsChunck * numBezierPoints;
            float t = 0.0f;
            for (int i = firstBezierPointToUpdate; i < firstBezierPointToUpdate + numBezierPoints; i++)
            {
                bezierPoints[i].transform.position = CalculateBezierPoint(t,
                    controlPointsList[firstControlPointIndex].transform.position,
                    controlPointsList[firstControlPointIndex + 1].transform.position,
                    controlPointsList[firstControlPointIndex + 2].transform.position,
                    controlPointsList[firstControlPointIndex + 3].transform.position);

                t += (float)(1) / (float)(numBezierPoints - 1);
            }

            if (controlPointIndex != 0 && controlPointIndex != (controlPointsList.Count - 1) && (controlPointIndex % 3) == 0)
            {
                firstControlPointIndex += 3;
                firstBezierPointToUpdate += numBezierPoints;

                t = 0.0f;
                for (int i = firstBezierPointToUpdate; i < firstBezierPointToUpdate + numBezierPoints; i++)
                {
                    bezierPoints[i].transform.position = CalculateBezierPoint(t,
                        controlPointsList[firstControlPointIndex].transform.position,
                        controlPointsList[firstControlPointIndex + 1].transform.position,
                        controlPointsList[firstControlPointIndex + 2].transform.position,
                        controlPointsList[firstControlPointIndex + 3].transform.position);

                    t += (float)(1) / (float)(numBezierPoints - 1);
                }
            }
        }
        else
        {
            for (int i = 0; i < numBezierPoints; i++)
            {
                Destroy(bezierPoints.Last());
                bezierPoints.RemoveAt(bezierPoints.Count-1);
            }
        }

        bezierCurveLength = 0.0f;
        for (int i = 0; i < bezierPoints.Count - 1; i++)
        {
            bezierCurveLength += (bezierPoints[i + 1].transform.position - bezierPoints[i].transform.position).magnitude;
        }
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3)
    {
        // Parametric form of cubic Bezier
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 point = uuu * P0; // (1 - t)^3 * P0
        point += 3 * uu * t * P1; // 3 * (1 - t)^2 * t * P1
        point += 3 * u * tt * P2; // 3 * (1 - t) * t^2 * P2
        point += ttt * P3;        // t^3 * P3

        return point;
    }

    void UpdateUndoPoints()
    {
        if (!(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))) return;

        if (controlPoints.Count == 0) return;

        if(controlPoints.Count == 1)
        {
            controlPoints.Remove(controlPointsList.Last());
            Destroy(controlPointsList.Last());
            controlPointsList.RemoveAt(controlPointsList.Count - 1);
        }
        else
        {
            UpdateBezier(BezierUpdateChange.ControlPointRemoved);

            for (int i = 0; (i) < 3; i++)
            {
                controlPoints.Remove(controlPointsList.Last());
                Destroy(controlPointsList.Last());
                controlPointsList.RemoveAt(controlPointsList.Count - 1);
            }
        }

        controlPointToUpdate = null;
    }
}
