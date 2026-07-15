using UnityEngine;

/// <summary>
/// VRMano — Mano virtual estilo FPS para el XR Device Simulator
///
/// Se coloca como hijo de la Main Camera del XR Rig.
/// Se mueve con el personaje (WASD) y apunta con el mouse (vista).
///
/// CONTROLES:
///   WASD          = mover personaje (la mano sigue)
///   Mouse         = girar vista (la mano apunta a donde miras)
///   G (mantener)  = agarrar objeto que el rayo toca
///   G (soltar)    = soltar objeto (cae con gravedad)
/// </summary>
public class VRMano : MonoBehaviour
{
    // ── Config ────────────────────────────────────────────────────────────────
    [Header("Posición de la mano (local respecto a cámara)")]
    public Vector3 posicionLocal    = new Vector3(0.35f, -0.25f, 0.6f);

    [Header("Alcance del rayo de interacción")]
    public float   alcanceRayo      = 5f;

    [Header("Tecla para agarrar")]
    public KeyCode teclaAgarrar     = KeyCode.G;

    [Header("Visual de la mano (asignado automático)")]
    public GameObject visualMano;

    // ── Estado interno ────────────────────────────────────────────────────────
    Rigidbody   _rbObjeto;          // rigidbody del objeto agarrado
    GameObject  _objetoAgarrado;    // referencia al objeto agarrado
    Vector3     _offsetAgarre;      // offset hand-objeto al momento de agarrar
    Camera      _cam;

    // Feedback visual — color del rayo
    LineRenderer _lr;
    readonly Color _colorLibre    = new Color(0.4f, 0.9f, 1f, 0.7f);
    readonly Color _colorDetecta  = new Color(0f,   1f,  0.4f, 1f);
    readonly Color _colorAgarra   = new Color(1f,   0.5f, 0f,  1f);

    // ── Inicialización ────────────────────────────────────────────────────────
    void Start()
    {
        _cam = Camera.main;
        if (_cam == null) { enabled = false; return; }

        // Posicionar la mano como hijo de la cámara
        transform.SetParent(_cam.transform);
        transform.localPosition = posicionLocal;
        transform.localRotation = Quaternion.identity;

        // Crear visual de la mano si no existe
        if (visualMano == null)
        {
            visualMano = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualMano.name = "Visual_Mano";
            visualMano.transform.SetParent(transform);
            visualMano.transform.localPosition = Vector3.zero;
            visualMano.transform.localScale    = Vector3.one * 0.06f;
            Destroy(visualMano.GetComponent<Collider>());

            var mr  = visualMano.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = Color.white;
            mr.sharedMaterial = mat;
        }

        // Línea del rayo visual
        _lr = gameObject.AddComponent<LineRenderer>();
        _lr.startWidth   = 0.005f;
        _lr.endWidth     = 0.002f;
        _lr.positionCount = 2;
        _lr.useWorldSpace = true;
        _lr.material = new Material(Shader.Find("Sprites/Default"));
        _lr.SetColors(_colorLibre, _colorLibre);
    }

    // ── Update: lógica principal ──────────────────────────────────────────────
    void Update()
    {
        // La mano siempre sigue la cámara (ya es hijo, pero forzamos rotación)
        transform.localPosition = posicionLocal;

        // Rayo desde la mano hacia adelante (dirección de la cámara)
        var origen    = transform.position;
        var direccion = _cam.transform.forward;

        // Detectar objeto interactuable
        GameObject objetoDetectado = null;
        RaycastHit hit;
        if (Physics.Raycast(origen, direccion, out hit, alcanceRayo))
        {
            // Buscar script VRCuboInteractuable en el objeto tocado o su padre
            var script = hit.collider.GetComponentInParent<VRCuboInteractuable>();
            if (script != null) objetoDetectado = script.gameObject;
        }

        // ── Agarrar con G ─────────────────────────────────────────────────────
        if (Input.GetKeyDown(teclaAgarrar))
        {
            if (_objetoAgarrado == null && objetoDetectado != null)
                Agarrar(objetoDetectado);
            else if (_objetoAgarrado != null)
                Soltar();
        }

        // ── Dibujar rayo visual ───────────────────────────────────────────────
        var puntoFin = _objetoAgarrado != null
            ? _objetoAgarrado.transform.position
            : origen + direccion * alcanceRayo;

        _lr.SetPosition(0, origen);
        _lr.SetPosition(1, puntoFin);

        Color color = _objetoAgarrado != null ? _colorAgarra
                    : objetoDetectado  != null ? _colorDetecta
                    : _colorLibre;
        _lr.startColor = color;
        _lr.endColor   = color;
    }

    // ── FixedUpdate: mover objeto agarrado ────────────────────────────────────
    void FixedUpdate()
    {
        if (_objetoAgarrado == null || _rbObjeto == null) return;

        // Posición objetivo: la mano + offset guardado
        var posObjetivo = transform.position + _offsetAgarre;
        _rbObjeto.MovePosition(Vector3.Lerp(
            _rbObjeto.position, posObjetivo, Time.fixedDeltaTime * 20f));
    }

    // ── Agarrar ───────────────────────────────────────────────────────────────
    void Agarrar(GameObject obj)
    {
        _objetoAgarrado = obj;
        _rbObjeto       = obj.GetComponent<Rigidbody>();
        if (_rbObjeto != null)
        {
            _rbObjeto.useGravity  = false;
            _rbObjeto.isKinematic = true;
        }
        // Offset: diferencia entre la mano y el objeto al momento de agarrar
        _offsetAgarre = obj.transform.position - transform.position;

        // Notificar al script del cubo (si existe)
        var cubo = obj.GetComponent<VRCuboInteractuable>();
        if (cubo != null) cubo.AlAgarrarDesdeVRMano();

        Debug.Log($"[VRMano] Agarrado: {obj.name} — WASD para moverte. G para soltar.");
    }

    // ── Soltar ────────────────────────────────────────────────────────────────
    void Soltar()
    {
        if (_objetoAgarrado == null) return;

        if (_rbObjeto != null)
        {
            _rbObjeto.isKinematic = false;
            _rbObjeto.useGravity  = true;
            _rbObjeto.linearVelocity     = Vector3.zero;
            _rbObjeto.angularVelocity = Vector3.zero;
        }

        var cubo = _objetoAgarrado.GetComponent<VRCuboInteractuable>();
        if (cubo != null) cubo.AlSoltarDesdeVRMano();

        Debug.Log($"[VRMano] Soltado: {_objetoAgarrado.name}");
        _objetoAgarrado = null;
        _rbObjeto       = null;
    }
}
