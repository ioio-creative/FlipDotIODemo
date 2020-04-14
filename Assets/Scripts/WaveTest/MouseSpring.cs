using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseSpring : MonoBehaviour
{
    public Vector3 CursorPos
    {
        get; private set;
    }

    private Vector3 cursorVelocity;
    public Vector3 CursorVelocity
    {
        get
        {
            return cursorVelocity;
        }
    }


    public float CursorDX
    {
        get
        {
            return CursorVelocity.x;
        }
    }

    public float CursorDY
    {
        get
        {
            return CursorVelocity.y;
        }
    }

    [SerializeField]
    [Range(0f, 1f)]
    private float easingFactor = 0.2f;

    private Vector3 screenSizeOffset;

    private void Start()
    {
        CursorPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        screenSizeOffset = new Vector3(Screen.width / 2, Screen.height / 2);
    }

    private void FixedUpdate()
    {
        Vector3 newCursorPos = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - CursorPos) * easingFactor + CursorPos;

        CursorPos = Vector3.SmoothDamp(CursorPos, Camera.main.ScreenToWorldPoint(Input.mousePosition), ref cursorVelocity, easingFactor);

        //if (IsMouseInsideScreenWidth())
        //{
        //    CursorDX = Mathf.Abs((newCursorPos.x - CursorPos.x)) / Time.fixedDeltaTime;
        //    //Debug.Log("dX: " + CursorDX);
        //}
        //else
        //{
        //    CursorDX = 0;
        //}

        //if (IsMouseInsideScreenHeight())
        //{
        //    CursorDY = Mathf.Abs((newCursorPos.y - CursorPos.y)) / Time.fixedDeltaTime;
        //    //Debug.Log("dX: " + CursorDX);
        //}
        //else
        //{
        //    CursorDY = 0;
        //}

        //CursorPos = newCursorPos;
        GetComponentInChildren<SphereCollider>().transform.position = CursorPos;
    }

}
