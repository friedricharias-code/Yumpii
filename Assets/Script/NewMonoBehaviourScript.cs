using UnityEngine;
using UnityEngine.InputSystem;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 5f;
    public float jumpForce = 1f;

    [Header("Cámara")]
    public Transform cameraTransform;  // Asignar en Inspector (o se autodescubre)
    public float mouseSensitivity = 15f;
    float xRotation = 0f;

    private Vector2 moveInput;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogError("Falta Rigidbody en el GameObject.");

        // Intento de autodescubrir la cámara si no la arrastraste en el Inspector
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
                cameraTransform = GetComponentInChildren<Camera>()?.transform;
        }

        Cursor.lockState = CursorLockMode.Locked; // opcional pero útil para pruebas
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && rb != null)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // Evita ejecutar en started/canceled (que es justo lo que te estaba golpeando)
        if (!context.performed) return;

        if (cameraTransform == null)
        {
            Debug.LogWarning("cameraTransform no asignado.");
            return;
        }

        Vector2 lookInput = context.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void Update()
    {
        // Nota: Translate mueve por Transform (no por física).
        // Si usas Rigidbody, lo ideal sería mover en FixedUpdate con rb.MovePosition.
        var movimiento = new Vector3(moveInput.x, 0f, moveInput.y);
        transform.Translate(movimiento * speed * Time.deltaTime, Space.Self);
    }
}
