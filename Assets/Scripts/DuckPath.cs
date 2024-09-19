using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

public class DuckPath : MonoBehaviour
{
    private GameObject bezierController;
    private float t = 0.0f;
    private float speed = 0.25f;
    private delegate float EasingFunction(float x);
    private EasingFunction easeFunc;

    // Start is called before the first frame update
    void Start()
    {
        SetVisibility(false);
        gameObject.transform.localScale *= 0.4f;

        bezierController = GameObject.FindWithTag("BezierController");

        easeFunc = ConstantSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.D))
        {
            t += Time.deltaTime * speed;
            t = Mathf.Clamp01(t);

            float easeT = easeFunc(t);

            SetVisibility(true);
            PathInfo pathInfo = bezierController.GetComponent<BezierController>().GetPathInfoAt(easeT);
            gameObject.transform.position = pathInfo.pos;
            gameObject.transform.forward = -pathInfo.dir;
        }
        else
        {
            t = 0.0f;
            SetVisibility(false);
        }

        if (Input.GetKey(KeyCode.Alpha1))
            easeFunc = ConstantSpeed;
        else if (Input.GetKey(KeyCode.Alpha2))
            easeFunc = EaseInOutCubic;
        else if (Input.GetKey(KeyCode.Alpha3))
            easeFunc = EaseOutQuart;
    }

    private void SetVisibility(bool isVisible)
    {
        Renderer[] childRenderers = gameObject.GetComponentsInChildren<Renderer>();

        // Loop through all renderers and set their visibility
        foreach (Renderer renderer in childRenderers)
        {
            renderer.enabled = isVisible;
        }
    }

    private float ConstantSpeed(float x)
    {
        return x;
    }

    private float EaseInOutCubic(float x)
    {
        return x < 0.5f ? 4.0f * x * x * x : 1.0f - ((float)Math.Pow(-2.0f * x + 2.0f, 3.0f) / 2.0f);
    }

    private float EaseOutQuart(float x)
    {
        return 1.0f - (float)Math.Pow(1.0f - x, 4.0f);
    }
}
