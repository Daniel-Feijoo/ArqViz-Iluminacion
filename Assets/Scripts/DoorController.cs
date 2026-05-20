using UnityEngine;
using System.Collections;

/// <summary>
/// Puerta que se abre al acercarse el Player y se cierra al alejarse.
/// Debe estar en el GameObject pivote (borde bisagra de la puerta).
/// El trigger colisionador debe estar en un hijo de este objeto.
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Grados que rota la puerta al abrirse (positivo = hacia adentro)")]
    public float anguloApertura   = 90f;
    public float velocidadApertura = 2.5f;

    Quaternion rotCerrada;
    Quaternion rotAbierta;
    bool abierta  = false;
    bool animando = false;

    void Start()
    {
        rotCerrada = transform.localRotation;
        rotAbierta = rotCerrada * Quaternion.Euler(0f, anguloApertura, 0f);
    }

    void OnTriggerEnter(Collider otro)
    {
        if (otro.CompareTag("Player") && !abierta && !animando)
            StartCoroutine(Animar(abrir: true));
    }

    void OnTriggerExit(Collider otro)
    {
        if (otro.CompareTag("Player") && abierta && !animando)
            StartCoroutine(Animar(abrir: false));
    }

    IEnumerator Animar(bool abrir)
    {
        animando = true;
        abierta  = abrir;

        Quaternion desde  = transform.localRotation;
        Quaternion hasta  = abrir ? rotAbierta : rotCerrada;
        float      t      = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * velocidadApertura;
            transform.localRotation = Quaternion.Slerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        transform.localRotation = hasta;
        animando = false;
    }
}
