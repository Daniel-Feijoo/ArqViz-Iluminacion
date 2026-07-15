using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador FPS — usa New Input System (com.unity.inputsystem).
/// WASD para moverse, Mouse para mirar, Shift para correr.
/// Escape para liberar cursor, clic izquierdo para bloquearlo de nuevo.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad       = 4.0f;
    public float velocidadCorrer = 7.0f;
    public float gravedad        = -12.0f;

    [Header("Cámara")]
    public Transform camara;
    public float sensibilidad   = 0.15f;   // New Input System: delta en píxeles, escala baja
    public float limiteVertical = 85.0f;

    CharacterController cc;
    Vector3 velVertical  = Vector3.zero;
    float   rotX         = 0f;
    bool    cursorLocked = false;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        BloquearCursor();
    }

    void Update()
    {
        GestionarCursor();
        if (!cursorLocked) return;
        MoverPersonaje();
        MoverCamara();
    }

    // ─── Movimiento WASD ────────────────────────────────────────────────────
    void MoverPersonaje()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float h = 0f, v = 0f;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1f;

        float spd = kb.leftShiftKey.isPressed ? velocidadCorrer : velocidad;

        Vector3 dir = transform.right * h + transform.forward * v;
        if (dir.magnitude > 1f) dir.Normalize();
        cc.Move(dir * spd * Time.deltaTime);

        // Gravedad
        if (cc.isGrounded && velVertical.y < 0f)
            velVertical.y = -2f;

        velVertical.y += gravedad * Time.deltaTime;
        cc.Move(velVertical * Time.deltaTime);
    }

    // ─── Mouse look ─────────────────────────────────────────────────────────
    void MoverCamara()
    {
        if (camara == null) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 delta = mouse.delta.ReadValue();

        rotX -= delta.y * sensibilidad;
        rotX  = Mathf.Clamp(rotX, -limiteVertical, limiteVertical);

        camara.localRotation = Quaternion.Euler(rotX, 0f, 0f);
        transform.Rotate(Vector3.up * delta.x * sensibilidad);
    }

    // ─── Cursor ─────────────────────────────────────────────────────────────
    void GestionarCursor()
    {
        var kb    = Keyboard.current;
        var mouse = Mouse.current;

        if (kb != null && kb.escapeKey.wasPressedThisFrame && cursorLocked)
            DesbloquearCursor();

        if (mouse != null && mouse.leftButton.wasPressedThisFrame && !cursorLocked)
            BloquearCursor();
    }

    void BloquearCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        cursorLocked     = true;
    }

    void DesbloquearCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        cursorLocked     = false;
    }
}
