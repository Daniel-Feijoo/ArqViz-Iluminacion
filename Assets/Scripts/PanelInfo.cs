using UnityEngine;

/// <summary>
/// MECÁNICA 3 — GÉNERO (Visualización Arquitectónica)
/// Punto de información que muestra datos de la habitación al acercarse.
/// El panel aparece y desaparece con fundido suave.
/// </summary>
public class PanelInfo : MonoBehaviour
{
    [Header("Contenido")]
    public string titulo      = "Sala Principal";
    public string area        = "80 m²";
    public string estilo      = "Moderno Minimalista";
    public string descripcion = "Espacio integrado con luz natural.";

    [Header("Color acento")]
    public Color colorAcento = new Color(0.9f, 0.75f, 0.3f);

    float alpha   = 0f;
    bool  mostrar = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) mostrar = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) mostrar = false;
    }

    void Update()
    {
        float objetivo = mostrar ? 1f : 0f;
        alpha = Mathf.MoveTowards(alpha, objetivo, Time.deltaTime * 3f);
    }

    void OnGUI()
    {
        if (alpha < 0.01f) return;

        float w = 270f, h = 120f;
        float x = 16f;
        float y = (Screen.height - h) / 2f;

        // Fondo principal
        GUI.color = new Color(0.05f, 0.05f, 0.05f, 0.80f * alpha);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

        // Borde izquierdo de color acento
        GUI.color = new Color(colorAcento.r, colorAcento.g, colorAcento.b, alpha);
        GUI.DrawTexture(new Rect(x, y, 5f, h), Texture2D.whiteTexture);

        // Título
        GUIStyle sTit = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 16,
            fontStyle = FontStyle.Bold
        };
        GUI.color = new Color(colorAcento.r, colorAcento.g, colorAcento.b, alpha);
        GUI.Label(new Rect(x + 14, y + 10, w - 20, 26), titulo, sTit);

        // Separador
        GUI.color = new Color(0.35f, 0.35f, 0.35f, alpha);
        GUI.DrawTexture(new Rect(x + 14, y + 38, w - 28, 1), Texture2D.whiteTexture);

        // Datos
        GUIStyle sDat = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        GUI.color = new Color(0.85f, 0.85f, 0.85f, alpha);
        GUI.Label(new Rect(x + 14, y + 46, w - 20, 20), $"▪ Área:   {area}",   sDat);
        GUI.Label(new Rect(x + 14, y + 64, w - 20, 20), $"▪ Estilo: {estilo}", sDat);

        // Descripción
        GUIStyle sDesc = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
        GUI.color = new Color(0.60f, 0.60f, 0.60f, alpha);
        GUI.Label(new Rect(x + 14, y + 88, w - 20, 28), descripcion, sDesc);

        GUI.color = Color.white;
    }
}
