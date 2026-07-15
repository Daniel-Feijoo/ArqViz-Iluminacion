using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// MECÁNICA 2 — FÍSICA (Rigidbody)
/// Los objetos hijos permanecen congelados (kinematic) hasta que el jugador
/// entra al trigger. Al activarse se dispersan con física real.
/// Presiona R para resetear las posiciones originales.
/// </summary>
public class ObjetoFisico : MonoBehaviour
{
    [Header("Física")]
    public float fuerzaImpulso  = 3.0f;
    public bool  permitirReset  = true;

    Rigidbody[]  rbs;
    Vector3[]    posIniciales;
    Quaternion[] rotIniciales;
    bool activado = false;

    void Start()
    {
        rbs          = GetComponentsInChildren<Rigidbody>();
        posIniciales = new Vector3[rbs.Length];
        rotIniciales = new Quaternion[rbs.Length];

        for (int i = 0; i < rbs.Length; i++)
        {
            rbs[i].isKinematic = true;   // congelados al inicio
            posIniciales[i]    = rbs[i].transform.position;
            rotIniciales[i]    = rbs[i].transform.rotation;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (activado || !other.CompareTag("Player")) return;

        activado = true;

        // Notificar al GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.RegistrarObjetoFisico();

        // Liberar física y aplicar impulso aleatorio a cada objeto
        foreach (var rb in rbs)
        {
            rb.isKinematic = false;
            Vector3 dir = (rb.transform.position - transform.position).normalized;
            dir.y      += 0.5f;
            rb.AddForce(dir.normalized * fuerzaImpulso
                        + Random.insideUnitSphere * 2f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);
        }
    }

    void Update()
    {
        if (activado && permitirReset
            && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Resetear();
    }

    void Resetear()
    {
        for (int i = 0; i < rbs.Length; i++)
        {
            rbs[i].isKinematic      = true;
            rbs[i].linearVelocity   = Vector3.zero;
            rbs[i].angularVelocity  = Vector3.zero;
            rbs[i].transform.SetPositionAndRotation(posIniciales[i], rotIniciales[i]);
        }
        activado = false;
    }

    void OnGUI()
    {
        if (!activado || !permitirReset) return;

        float w = 210f, h = 30f;
        float x = Screen.width  - w - 14f;
        float y = 14f;

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x - 4, y - 4, w + 8, h + 8), Texture2D.whiteTexture);
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.fontSize  = 13;
        st.alignment = TextAnchor.MiddleCenter;
        GUI.color = new Color(1f, 0.85f, 0.3f);
        GUI.Label(new Rect(x, y, w, h), "[R]  Resetear objetos", st);
        GUI.color = Color.white;
    }
}
