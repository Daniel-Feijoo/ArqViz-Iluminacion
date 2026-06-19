using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI EN WORLD SPACE — Panel 3D flotante dentro del entorno.
/// Muestra el estado del GameManager: zonas visitadas, tiempo, objetos físicos.
/// Al completar la experiencia muestra el mensaje de logro.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CartelWorldSpace : MonoBehaviour
{
    [Header("Textos del panel")]
    public Text txtTitulo;
    public Text txtZonas;
    public Text txtObjetos;
    public Text txtTiempo;
    public Text txtLogro;

    [Header("Fondo del logro")]
    public Image fondoLogro;

    Canvas canvas;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        if (txtLogro  != null) txtLogro.gameObject.SetActive(false);
        if (fondoLogro != null) fondoLogro.gameObject.SetActive(false);
    }

    /// <summary>Actualizado cada frame por GameManager.</summary>
    public void ActualizarEstado(int zonas, int objetos, string tiempo, bool completa)
    {
        if (txtZonas   != null) txtZonas.text   = $"Zonas exploradas: {zonas}";
        if (txtObjetos != null) txtObjetos.text = $"Objetos activados: {objetos}";
        if (txtTiempo  != null) txtTiempo.text  = $"Tiempo: {tiempo}";
    }

    /// <summary>Llamado por GameManager al completar la experiencia.</summary>
    public void MostrarLogro()
    {
        if (txtLogro   != null) txtLogro.gameObject.SetActive(true);
        if (fondoLogro != null) fondoLogro.gameObject.SetActive(true);
    }
}
