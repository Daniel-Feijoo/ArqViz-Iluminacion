using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ArqViz / Fix VR — Configuración completa
///
/// Aplica en un click:
///   1. Reemplaza XR_DeviceSimulator manual con el prefab preconfigurado del Starter Assets
///   2. Conecta TeleportationProvider → LocomotionMediator
///   3. Verifica referencias del XR Origin
/// </summary>
public class ArqVizVRFix
{
    [MenuItem("ArqViz/Fix VR — Aplicar Configuración Completa")]
    static void FixAll()
    {
        Debug.Log("[ArqVizFix] Iniciando fix completo...");

        bool simFixed      = FixSimulator();
        bool teleportFixed = FixTeleportMediator();
        VerificarXROrigin();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        string simStatus      = simFixed      ? "✓" : "⚠ (manual requerido)";
        string teleportStatus = teleportFixed ? "✓" : "⚠ (manual requerido)";

        EditorUtility.DisplayDialog("ArqViz VR Fix",
            $"XR Device Simulator: {simStatus}\n" +
            $"Teleport Mediator:    {teleportStatus}\n\n" +
            "Dale Play y en el Game View:\n" +
            "• Click en la ventana Game → captura el mouse\n" +
            "• Mouse = rotar cabeza (HMD)\n" +
            "• WASD = mover cuerpo\n" +
            "• Tab = cambiar: HMD / mano izq / mano der\n" +
            "• G = Grip (agarrar objeto)\n" +
            "• T = Trigger (teleport sobre zona verde)",
            "OK");
    }

    // ── 1. Simulator ─────────────────────────────────────────────────────────
    static bool FixSimulator()
    {
        // Eliminar el GO manual creado por el builder
        var oldGO = GameObject.Find("XR_DeviceSimulator");

        // Buscar prefab del Starter Assets (nombre puede variar)
        string prefabPath = BuscarAsset("t:Prefab",
            new[] { "Device Simulator", "DeviceSimulator", "XR Device Sim" },
            "Assets/Samples/XR Interaction Toolkit");

        if (prefabPath != null)
        {
            if (oldGO != null) Undo.DestroyObjectImmediate(oldGO);

            var prefab   = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "XR_DeviceSimulator";
            Undo.RegisterCreatedObjectUndo(instance, "XR Device Simulator Prefab");
            Debug.Log($"[ArqVizFix] ✓ Prefab instanciado: {prefabPath}");
            return true;
        }

        // Sin prefab: activar el GO existente y aplicar preset
        if (oldGO != null)
        {
            oldGO.SetActive(true);
            EditorUtility.SetDirty(oldGO);

            var simType = FindType("UnityEngine.XR.Interaction.Toolkit.XRDeviceSimulator");
            if (simType != null)
            {
                var comp = oldGO.GetComponent(simType);
                if (comp != null && AplicarPreset(comp, "Simulator", "Device"))
                {
                    Debug.Log("[ArqVizFix] ✓ Preset aplicado al simulador.");
                    return true;
                }

                // Sin preset: asignar InputActionAsset via reflection
                AsignarInputActions(comp);
            }

            Debug.Log("[ArqVizFix] XR_DeviceSimulator activado (sin prefab ni preset).");
            return true;
        }

        Debug.LogWarning("[ArqVizFix] XR_DeviceSimulator no encontrado en la escena.");
        return false;
    }

    // ── 2. Teleport Mediator ──────────────────────────────────────────────────
    static bool FixTeleportMediator()
    {
        var xrOriginGO = GameObject.Find("XR Origin (Rig)");
        if (xrOriginGO == null)
        {
            Debug.LogWarning("[ArqVizFix] 'XR Origin (Rig)' no encontrado.");
            return false;
        }

        var medType = FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionMediator")
                   ?? FindType("UnityEngine.XR.Interaction.Toolkit.LocomotionSystem");
        var tpType  = FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider")
                   ?? FindType("UnityEngine.XR.Interaction.Toolkit.TeleportationProvider");

        if (medType == null || tpType == null)
        {
            Debug.LogWarning("[ArqVizFix] Tipos de Locomotion/Teleport no encontrados.");
            return false;
        }

        var med = xrOriginGO.GetComponent(medType);
        var tp  = xrOriginGO.GetComponent(tpType);

        if (med == null || tp == null)
        {
            Debug.LogWarning("[ArqVizFix] Componentes Mediator/TeleportProvider no encontrados en XR Origin (Rig).");
            return false;
        }

        // Intentar con varios nombres de campo (varía entre versiones)
        bool assigned = false;
        foreach (var name in new[] { "m_Mediator", "mediator", "locomotionMediator", "m_LocomotionSystem", "locomotionSystem" })
        {
            if (TryAssign(tp, name, med)) { assigned = true; break; }
        }

        EditorUtility.SetDirty(xrOriginGO);

        if (assigned) Debug.Log("[ArqVizFix] ✓ TeleportationProvider.Mediator conectado.");
        else          Debug.LogWarning("[ArqVizFix] No se pudo asignar Mediator via reflection.");

        return assigned;
    }

    // ── 3. Verificar XR Origin ────────────────────────────────────────────────
    static void VerificarXROrigin()
    {
        var xrOriginGO = GameObject.Find("XR Origin (Rig)");
        if (xrOriginGO == null) return;

        var xrOriginType = FindType("Unity.XR.CoreUtils.XROrigin");
        if (xrOriginType == null) return;

        var comp = xrOriginGO.GetComponent(xrOriginType);
        if (comp == null) return;

        var camOffset = xrOriginGO.transform.Find("Camera Offset");
        if (camOffset == null) return;

        var camTr = camOffset.Find("Main Camera");
        if (camTr == null) return;

        var cam = camTr.GetComponent<Camera>();
        if (cam != null)
        {
            TryAssign(comp, "Camera", cam);
            TryAssign(comp, "camera", cam);
        }
        TryAssign(comp, "CameraFloorOffsetObject", camOffset.gameObject);
        TryAssign(comp, "cameraFloorOffsetObject", camOffset.gameObject);

        EditorUtility.SetDirty(xrOriginGO);
        Debug.Log("[ArqVizFix] ✓ XR Origin verificado.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static bool AplicarPreset(Component comp, params string[] keywords)
    {
        var guids = AssetDatabase.FindAssets("t:Preset",
            new[] { "Assets/Samples/XR Interaction Toolkit" });

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var kw in keywords)
            {
                if (path.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var preset = AssetDatabase.LoadAssetAtPath<UnityEditor.Presets.Preset>(path);
                    if (preset != null && preset.CanBeAppliedTo(comp))
                    {
                        preset.ApplyTo(comp);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    static void AsignarInputActions(Component comp)
    {
        if (comp == null) return;

        var guids = AssetDatabase.FindAssets("XRI Default Input Actions t:InputActionAsset",
            new[] { "Assets/Samples" });
        if (guids.Length == 0) return;

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
            AssetDatabase.GUIDToAssetPath(guids[0]));
        if (asset == null) return;

        foreach (var name in new[] { "m_ActionAsset", "actionAsset", "m_DeviceSimulatorActionAsset",
                                     "controlsAsset", "inputActions", "m_InputActionAsset" })
            TryAssign(comp, name, asset);

        EditorUtility.SetDirty(comp);
        Debug.Log("[ArqVizFix] XRI Default Input Actions asignado al simulador.");
    }

    static string BuscarAsset(string filter, string[] keywords, string searchFolder)
    {
        var guids = AssetDatabase.FindAssets(filter, new[] { searchFolder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var kw in keywords)
                if (path.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                    return path;
        }
        return null;
    }

    static Type FindType(string typeName)
    {
        var t = Type.GetType(typeName);
        if (t != null) return t;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            t = asm.GetType(typeName);
            if (t != null) return t;
        }
        return null;
    }

    static bool TryAssign(object obj, string memberName, object value)
    {
        if (obj == null) return false;
        var type = obj.GetType();

        var field = type.GetField(memberName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            try { field.SetValue(obj, value); return true; }
            catch { }
        }

        var prop = type.GetProperty(memberName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanWrite)
        {
            try { prop.SetValue(obj, value); return true; }
            catch { }
        }
        return false;
    }
}
