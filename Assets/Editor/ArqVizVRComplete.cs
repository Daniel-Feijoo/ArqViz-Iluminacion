using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ArqViz / Setup VR Completo (Starter Assets)
///
/// Reemplaza el rig VR manual con los prefabs oficiales del Starter Assets
/// que ya tienen TODAS las acciones, bindings e interactores configurados.
///
/// Conserva: Casa_Moderna_ArqViz, Player, Mecanicas, GameManager, UI, Carteles.
/// Reemplaza: todo lo que empieza con VR_, XR_, XR_InteractionManager.
/// </summary>
public class ArqVizVRComplete
{
    // ── Rutas Starter Assets ──────────────────────────────────────────────────
    const string SA = "Assets/Samples/XR Interaction Toolkit/3.0.7/Starter Assets/";

    const string PREFAB_XR_RIG      = SA + "Prefabs/XR Origin (XR Rig).prefab";
    const string PREFAB_TELEPORT    = SA + "DemoSceneAssets/Prefabs/Teleport/Teleport Area.prefab";
    const string PREFAB_CUBE        = SA + "DemoSceneAssets/Prefabs/Interactables/Cube.prefab";

    // ── Nombres de objetos ────────────────────────────────────────────────────
    static readonly string[] OBJETOS_VR = {
        "VR_ArqViz_Root", "XR_InteractionManager", "XR_DeviceSimulator",
        "VR_Pelota_Interactuable", "VR_TeleportArea_SalaPrincipal",
        "VR_TeleportArea_SalaJuegos", "XR Origin (Rig)", "XR Origin (VR)",
        "DontDestroyOnLoad"
    };

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("ArqViz/Setup VR Completo — Starter Assets (USAR ESTE)")]
    static void SetupCompleto()
    {
        // ── 0. Limpiar todo lo anterior ───────────────────────────────────────
        foreach (var nombre in OBJETOS_VR)
        {
            var go = GameObject.Find(nombre);
            if (go != null) Undo.DestroyObjectImmediate(go);
        }

        // ── 1. XR Origin (XR Rig) — prefab completo con controllers ──────────
        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_XR_RIG);
        if (rigPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", $"No se encontró el prefab:\n{PREFAB_XR_RIG}\n\nVerifica que importaste los Starter Assets de XR Interaction Toolkit.", "OK");
            return;
        }

        var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        rig.name = "XR Origin (XR Rig)";
        rig.transform.position = new Vector3(0f, 0f, -3f); // entrada de la casa
        Undo.RegisterCreatedObjectUndo(rig, "XR Rig");
        Debug.Log("[ArqVizVR] ✓ XR Origin (XR Rig) instanciado.");

        // ── 2. Teleport Areas — prefab oficial con capa correcta ──────────────
        var teleportPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_TELEPORT);
        if (teleportPrefab == null)
        {
            Debug.LogWarning($"[ArqVizVR] Prefab Teleport Area no encontrado: {PREFAB_TELEPORT}");
        }
        else
        {
            // Sala Principal
            var area1 = (GameObject)PrefabUtility.InstantiatePrefab(teleportPrefab);
            area1.name = "VR_TeleportArea_SalaPrincipal";
            area1.transform.position   = new Vector3(0f, 0.01f, 3.5f);
            area1.transform.localScale = new Vector3(1.5f, 1f, 1.2f); // ~15x12m
            Undo.RegisterCreatedObjectUndo(area1, "Teleport Area 1");

            // Sala de Juegos
            var area2 = (GameObject)PrefabUtility.InstantiatePrefab(teleportPrefab);
            area2.name = "VR_TeleportArea_SalaJuegos";
            area2.transform.position   = new Vector3(9.5f, 0.01f, 2.5f);
            area2.transform.localScale = new Vector3(0.5f, 1f, 0.5f); // ~5x5m
            Undo.RegisterCreatedObjectUndo(area2, "Teleport Area 2");

            Debug.Log("[ArqVizVR] ✓ 2 Teleport Areas creadas.");
        }

        // ── 3. Objeto interactuable — Cube prefab (XRGrabInteractable) ────────
        var cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_CUBE);
        if (cubePrefab == null)
        {
            Debug.LogWarning($"[ArqVizVR] Prefab Cube no encontrado. Creando objeto manual.");
            CrearObjetoManual();
        }
        else
        {
            var cubo = (GameObject)PrefabUtility.InstantiatePrefab(cubePrefab);
            cubo.name = "VR_Cubo_Interactuable";
            cubo.transform.position   = new Vector3(9f, 1.0f, 2.5f); // Sala de Juegos
            cubo.transform.localScale = Vector3.one * 0.3f;
            Undo.RegisterCreatedObjectUndo(cubo, "VR Cubo");
            Debug.Log("[ArqVizVR] ✓ Cubo interactuable creado en Sala de Juegos.");
        }

        // ── 4. XR Device Simulator ────────────────────────────────────────────
        CrearDeviceSimulator();

        // ── 5. Desactivar FPS Player ──────────────────────────────────────────
        DesactivarPlayer();

        // ── 6. Desactivar Main Camera raíz (conflicto con cámara del rig) ─────
        DesactivarCamaraRaiz();

        // ── 7. Guardar ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[ArqVizVR] ✓ Setup completo con Starter Assets.");
        EditorUtility.DisplayDialog("ArqViz VR ✓ — Setup Completo",
            "Todo configurado con prefabs oficiales:\n\n" +
            "✓ XR Origin (XR Rig) — Camera Rig\n" +
            "✓ Left/Right Controllers — Controller Tracking\n" +
            "✓ Ray + Direct Interactors — Grab Interaction\n" +
            "✓ Teleportation — Locomotion/Teleport\n" +
            "✓ 2 Teleport Areas (Sala Principal + Sala Juegos)\n" +
            "✓ Cubo interactuable (Sala de Juegos)\n" +
            "✓ XR Device Simulator\n\n" +
            "CONTROLES en Play (click en Game View primero):\n" +
            "• Mouse = rotar cabeza\n" +
            "• WASD = mover\n" +
            "• Left Shift sostenido = controlar mano izquierda\n" +
            "• Space sostenido = controlar mano derecha\n" +
            "• G = Grip (agarrar cubo)\n" +
            "• T = Trigger (teleport — apunta zona azul primero)\n\n" +
            "Guarda la escena: Ctrl+S",
            "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void CrearObjetoManual()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "VR_Cubo_Interactuable";
        go.transform.position   = new Vector3(9f, 1.0f, 2.5f);
        go.transform.localScale = Vector3.one * 0.3f;
        Undo.RegisterCreatedObjectUndo(go, "VR Cubo Manual");

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.5f;

        var grabType = FindType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable");
        if (grabType != null) go.AddComponent(grabType);

        // Color naranja
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(1f, 0.45f, 0.1f);
            mr.sharedMaterial = mat;
        }
    }

    static void CrearDeviceSimulator()
    {
        var simType = FindType("UnityEngine.XR.Interaction.Toolkit.XRDeviceSimulator");
        if (simType == null)
        {
            Debug.LogWarning("[ArqVizVR] XRDeviceSimulator type not found.");
            return;
        }

        var simGO = new GameObject("XR_DeviceSimulator");
        Undo.RegisterCreatedObjectUndo(simGO, "XR Device Simulator");
        simGO.AddComponent(simType);

        // Aplicar preset del Starter Assets si existe
        var presetGuids = AssetDatabase.FindAssets("t:Preset", new[] { SA + "Presets" });
        foreach (var guid in presetGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Simulator") || path.Contains("Device"))
            {
                var preset = AssetDatabase.LoadAssetAtPath<UnityEditor.Presets.Preset>(path);
                var comp   = simGO.GetComponent(simType);
                if (preset != null && comp != null && preset.CanBeAppliedTo(comp))
                {
                    preset.ApplyTo(comp);
                    Debug.Log($"[ArqVizVR] Preset del simulador aplicado: {path}");
                    break;
                }
            }
        }

        Debug.Log("[ArqVizVR] ✓ XR Device Simulator creado.");
    }

    static void DesactivarPlayer()
    {
        string[] nombres = { "Player", "FPSPlayer", "PlayerController", "FirstPersonPlayer" };
        foreach (var n in nombres)
        {
            var go = GameObject.Find(n);
            if (go != null) { go.SetActive(false); Debug.Log($"[ArqVizVR] Player '{n}' desactivado."); return; }
        }
        var byTag = GameObject.FindGameObjectWithTag("Player");
        if (byTag != null) { byTag.SetActive(false); Debug.Log($"[ArqVizVR] Player '{byTag.name}' desactivado."); }
    }

    static void DesactivarCamaraRaiz()
    {
        // Desactivar la Main Camera de la raíz de la escena (no la del XR Rig)
        var allCams = GameObject.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCams)
        {
            // Si la cámara está en la raíz de la escena (sin parent) y no está dentro del XR Rig
            if (cam.transform.parent == null && cam.gameObject.name == "Main Camera")
            {
                cam.gameObject.SetActive(false);
                Debug.Log("[ArqVizVR] Main Camera raíz desactivada (evita conflicto con HMD).");
            }
        }
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
}
