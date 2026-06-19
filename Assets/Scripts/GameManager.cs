using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// GAME MANAGER — lleva el estado de la experiencia ArqViz.
/// Trackea: zonas visitadas, tiempo transcurrido, objetos físicos activados.
/// Expone UnityEvents para conectar acciones desde el Inspector.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Estado ───────────────────────────────────────────────────────────────
    [Header("Estado de la experiencia")]
    public int   zonasVisitadas        = 0;
    public int   objetosFisicosActivados = 0;
    public float tiempoTranscurrido    = 0f;
    public bool  experienciaCompleta   = false;

    [Header("Configuración de logro")]
    [Tooltip("Número de zonas necesarias para completar la experiencia")]
    public int zonasParaCompletar = 2;

    // ── Referencia al cartel world space ─────────────────────────────────────
    [Header("UI World Space")]
    public CartelWorldSpace cartelEstado;

    // ── UnityEvents ──────────────────────────────────────────────────────────
    [Header("Eventos")]
    public UnityEvent onZonaVisitada;          // se dispara cada vez que se visita una zona
    public UnityEvent onExperienciaCompleta;   // se dispara al cumplir el logro

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (!experienciaCompleta)
            tiempoTranscurrido += Time.deltaTime;

        ActualizarCartel();
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>Llamado por ZonaTeleporte al completar el teletransporte.</summary>
    public void RegistrarZonaVisitada()
    {
        zonasVisitadas++;
        onZonaVisitada?.Invoke();

        if (!experienciaCompleta && zonasVisitadas >= zonasParaCompletar)
            ActivarLogro();
    }

    /// <summary>Llamado por ObjetoFisico cuando el jugador activa los bloques.</summary>
    public void RegistrarObjetoFisico()
    {
        objetosFisicosActivados++;
    }

    // ── Logro ─────────────────────────────────────────────────────────────────
    void ActivarLogro()
    {
        experienciaCompleta = true;
        onExperienciaCompleta?.Invoke();

        if (cartelEstado != null)
            cartelEstado.MostrarLogro();

        Debug.Log("[GameManager] ¡Experiencia completa!");
    }

    // ── Cartel ────────────────────────────────────────────────────────────────
    void ActualizarCartel()
    {
        if (cartelEstado == null) return;

        string mins = Mathf.FloorToInt(tiempoTranscurrido / 60f).ToString("00");
        string secs = Mathf.FloorToInt(tiempoTranscurrido % 60f).ToString("00");

        cartelEstado.ActualizarEstado(
            zonasVisitadas,
            objetosFisicosActivados,
            $"{mins}:{secs}",
            experienciaCompleta
        );
    }
}
