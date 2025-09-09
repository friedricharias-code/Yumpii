using UnityEngine;
using UnityEngine.InputSystem;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 4f;                      // Velocidad base en suelo
    [Range(0f, 1f)] public float airControl = 0.3f;  // Control en el aire
    public float acceleration = 12f;              // Qué tan rápido alcanza la vel. objetivo

    [Header("Salto")]
    public float jumpForce = 5f;                  // Impulso vertical
    public LayerMask groundMask = ~0;             // Capas de suelo
    public float groundCheckRadius = 0.25f;       // Radio del chequeo
    public float groundCheckDistance = 0.4f;      // Offset hacia abajo para el chequeo

    [Header("Cámara")]
    public Transform cameraTransform;             // Arrastra la cámara aquí (o se autodescubre)
    public float mouseSensitivityX = 80f;        // sensibilidad horizontal
    public float mouseSensitivityY = 55f;         // sensibilidad vertical
    float xRotation = 0f;

    // Input buffers (PlayerInput → Invoke Unity Events)
    Vector2 moveInput;
    Vector2 lookBuffer;

    // Interno
    Rigidbody rb;
    bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;
    }

    void Start()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null) cameraTransform = Camera.main.transform;
            else cameraTransform = GetComponentInChildren<Camera>()?.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
    }

    // ----------------- PlayerInput (Invoke Unity Events) -----------------
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed || !isGrounded) return;

        // Normaliza el salto (quita velocidad vertical previa)
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // Solo guardamos el input; la rotación se aplica en FixedUpdate con física
        if (!context.performed) return;
        lookBuffer = context.ReadValue<Vector2>();
    }

    // ----------------- Física -----------------
    void FixedUpdate()
    {
        GroundCheck();

        // ---- Movimiento XZ por física ----
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 desiredDir = transform.TransformDirection(input);
        if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

        float control = isGrounded ? 1f : airControl;
        Vector3 desiredVel = desiredDir * (speed * control);

        Vector3 currentVel = rb.linearVelocity;
        Vector3 velChange = new Vector3(desiredVel.x - currentVel.x, 0f, desiredVel.z - currentVel.z);

        float maxChange = acceleration * Time.fixedDeltaTime;
        velChange = Vector3.ClampMagnitude(velChange, maxChange);

        rb.AddForce(velChange, ForceMode.VelocityChange);

        // ---- Mirar/Girar ----
        // Yaw (izq/der) al cuerpo vía física
        float mouseX = lookBuffer.x * mouseSensitivityX * Time.fixedDeltaTime;
        if (Mathf.Abs(mouseX) > 0f)
        {
            Quaternion yaw = Quaternion.Euler(0f, mouseX, 0f);
            rb.MoveRotation(rb.rotation * yaw);
        }

        // Pitch (arriba/abajo) solo en la cámara (no afecta rigidbody)
        if (cameraTransform != null)
        {
            float mouseY = lookBuffer.y * mouseSensitivityY * Time.fixedDeltaTime;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        // Limpia el buffer para evitar acumulación si no hay input en el siguiente tick
        lookBuffer = Vector2.zero;
    }

    void GroundCheck()
    {
        Vector3 origin = transform.position + Vector3.down * groundCheckDistance;
        isGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 origin = transform.position + Vector3.down * groundCheckDistance;
        Gizmos.DrawWireSphere(origin, groundCheckRadius);
    }
}