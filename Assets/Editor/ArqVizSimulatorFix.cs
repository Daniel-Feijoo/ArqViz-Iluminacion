using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ArqViz / Fix Simulator — Reemplaza XR_DeviceSimulator con el prefab oficial
///
/// Requiere: Sample "XR Device Simulator" ya importado en
/// Assets/Samples/XR Interaction Toolkit/3.0.7/XR Device Simulator/
///
/// El prefab oficial tiene todos los InputActionReferences ya conectados:
///   - XR Device Simulator Controls.inputactions  → teclado/mouse del simulador
///   - XR Device Controller Controls.inputactions → botones de los controladores
/// </summary>
public class ArqVizSimulatorFix
{
    const string SIM_PREFAB = "Assets/Samples/XR Interaction Toolkit/3.0.7/XR Device Simulator/XR Device Simulator.prefab";

    [MenuItem("ArqViz/Fix Simulator — Instalar Prefab Oficial (CORRER AHORA)")]
    static void FixSimulator()
    {
        // ── 1. Verificar que el sample fue importado ──────────────────────────
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SIM_PREFAB);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("ArqViz — Sample no importado",
                "No se encontró el prefab del simulador.\n\n" +
                "Espera que Unity termine de importar los archivos del sample\n" +
                "y vuelve a correr este menú.",
                "OK");
            return;
        }

        // ── 2. Eliminar cualquier XR_DeviceSimulator existente ────────────────
        string[] nombres = { "XR_DeviceSimulator", "XR Device Simulator" };
        foreach (var n in nombres)
        {
            var old = GameObject.Find(n);
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old);
                Debug.Log($"[SimFix] Eliminado: {n}");
            }
        }

        // ── 3. Instanciar el prefab oficial con todos los bindings ────────────
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "XR_DeviceSimulator";
        instance.SetActive(true);
        Undo.RegisterCreatedObjectUndo(instance, "XR Device Simulator Oficial");

        // ── 4. Guardar escena ─────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[SimFix] ✓ XR Device Simulator oficial instanciado con todos los controles.");

        EditorUtility.DisplayDialog("ArqViz — Simulator ✓",
            "XR Device Simulator instalado correctamente.\n\n" +
            "CONTROLES (en Play Mode, click en Game View primero):\n\n" +
            "MOVER CUERPO:\n" +
            "  W/A/S/D = mover\n" +
            "  Q/E     = bajar/subir\n\n" +
            "ROTAR CABEZA:\n" +
            "  Botón Derecho Mouse (sostener) + mover mouse\n\n" +
            "MANO IZQUIERDA (sostener Left Shift):\n" +
            "  Mouse       = mover mano izq\n" +
            "  G           = Grip (agarrar objeto)\n" +
            "  Click Izq   = Trigger (teleport)\n\n" +
            "MANO DERECHA (sostener Space):\n" +
            "  Mouse       = mover mano der\n" +
            "  G           = Grip (agarrar objeto)\n" +
            "  Click Izq   = Trigger (teleport)\n\n" +
            "Tab = ciclar HMD / mano izq / mano der\n" +
            "T   = fijar mano izq  |  Y = fijar mano der\n" +
            "H   = cambiar modo Hand/Controller\n\n" +
            "PARA TELEPORT:\n" +
            "  Sostén Space → apunta al área verde → Click Izq\n\n" +
            "Guarda la escena: Ctrl+S",
            "Entendido");
    }

    [MenuItem("ArqViz/Fix Simulator — Instalar Prefab Oficial (CORRER AHORA)", true)]
    static bool ValidarFixSimulator()
    {
        // Solo habilitar si no estamos en Play Mode
        return !EditorApplication.isPlaying;
    }
}
