using UnityEngine;

public class SimpleFlyCam : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float lookSpeed = 2.0f;
    public float sprintMultiplier = 2.5f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    private void Start()
    {
        Vector3 rot = transform.localEulerAngles;
        rotationX = rot.x;
        rotationY = rot.y;
    }

    private void Update()
    {
        // 1. 按住滑鼠右鍵旋轉視角
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked; // 鎖定並隱藏滑鼠
            Cursor.visible = false;

            rotationX -= Input.GetAxis("Mouse Y") * lookSpeed;
            rotationY += Input.GetAxis("Mouse X") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f); // 避免相機翻轉

            transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 2. 移動相機 (WASD + QE)
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1.0f);

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;    // 向上升
        if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;    // 向下降

        transform.position += move.normalized * speed * Time.deltaTime;
    }
}
