using UnityEngine;

/// <summary>
/// MECÁNICA 1 — TRIGGER
/// El jugador entra a la zona y tras 1.5s se teletransporta al destino.
/// Se muestra barra de progreso en pantalla.
/// </summary>
public class ZonaTeleporte : MonoBehaviour
{
    [Header("Destino")]
    public Vector3 posicionDestino = Vector3.zero;
    public string nombreDestino   = "Sala de Juegos";

    [Header("Configuración")]
    public float tiempoActivacion = 1.5f;

    bool  jugadorDentro   = false;
    float tiempoAcumulado = 0f;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro   = true;
            tiempoAcumulado = 0f;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro   = false;
            tiempoAcumulado = 0f;
        }
    }

    void Update()
    {
        if (!jugadorDentro) return;
        tiempoAcumulado += Time.deltaTime;
        if (tiempoAcumulado >= tiempoActivacion)
            Teleportar();
    }

    void Teleportar()
    {
        jugadorDentro   = false;
        tiempoAcumulado = 0f;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Desactivar CharacterController temporalmente para mover sin colisión
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.position = posicionDestino;
        if (cc != null) cc.enabled = true;
    }

    void OnGUI()
    {
        if (!jugadorDentro) return;

        float pct = Mathf.Clamp01(tiempoAcumulado / tiempoActivacion);
        float w = 320f, h = 58f;
        float x = (Screen.width  - w) / 2f;
        float y =  Screen.height - 120f;

        // Fondo oscuro
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(x - 4, y - 4, w + 8, h + 8), Texture2D.whiteTexture);

        // Texto
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.fontSize  = 14;
        st.alignment = TextAnchor.MiddleCenter;
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, w, 28), $"▶  Ir a {nombreDestino}", st);

        // Barra de progreso
        GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.7f);
        GUI.DrawTexture(new Rect(x, y + 30, w, 18), Texture2D.whiteTexture);
        GUI.color = new Color(0.25f, 0.65f, 1.0f, 0.9f);
        GUI.DrawTexture(new Rect(x, y + 30, w * pct, 18), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }
}
