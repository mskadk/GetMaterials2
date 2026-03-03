using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Input = UnityEngine.Input;

public class CameraEventControll : MonoBehaviour
{
    public int 滚轮速度 = 1;
    public int mb = (int)MouseButton.MiddleMouse;
    [Header("镜头缩放大小")]
    public int ScaleMin = 200;
    public int ScaleMax = 8000;
    Camera cam;
    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        updateMouseDown();
    }


    private Vector3 dragOrigin;
    private Vector3 pos;
    private Vector3 move;
    private float scroll;
    void updateMouseDown()
    {
        //拾取位置
        if (Input.GetMouseButtonDown(mb))
        {
            dragOrigin = Input.mousePosition;
        }

        if (Input.GetMouseButton(mb))
        {
            pos = cam.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
            move = new(pos.x * cam.orthographicSize * 2 * cam.aspect, pos.y * cam.orthographicSize * 2, 0);
            transform.Translate(-move, Space.World);
            dragOrigin = Input.mousePosition;
        }

        scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            cam.orthographicSize -= scroll * 滚轮速度 * 500;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, ScaleMin, ScaleMax);
        }
    }
}
