using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ArqViz / Configurar VR — Building Blocks
///
/// Monta en la escena los 4 Building Blocks equivalentes a Meta XR SDK:
///   Block 1 — Camera Rig      → XR Origin con Camera Offset
///   Block 2 — Controller Track → Action Based Controllers (izquierda/derecha)
///   Block 3 — Grab Interaction → XR Ray Interactor + XR Grab Interactable
///   Block 4 — Teleportation    → Teleportation Provider + Teleportation Areas
///
/// Requiere previo en manifest.json:
///   com.unity.xr.management, com.unity.xr.openxr, com.unity.xr.interaction.toolkit
/// </summary>
public class ArqVizVRBuilder
{
    const string VR_ROOT   = "VR_ArqViz_Root";
    const string SIM_ROOT  = "XR_DeviceSimulator";
    const string MGR_ROOT  = "XR_InteractionManager";

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("ArqViz/Configurar VR — Building Blocks")]
    static void Build()
    {
        // ── Verificar paquetes ────────────────────────────────────────────────
        var xrOriginType = FindType("Unity.XR.CoreUtils.XROrigin");
        if (xrOriginType == null)
        {
            EditorUtility.DisplayDialog("ArqViz VR — Paquetes no encontrados",
                "Los paquetes XR aún no están instalados.\n\n" +
                "1. Revisa que manifest.json tenga los 3 paquetes XR.\n" +
                "2. Espera que Unity termine de descargarlos (barra inferior).\n" +
                "3. Vuelve a ejecutar ArqViz > Configurar VR.",
                "OK");
            return;
        }

        // ── Limpiar instancias previas ─────────────────────────────────────
        RemoveIfExists(VR_ROOT);
        RemoveIfExists(SIM_ROOT);
        RemoveIfExists(MGR_ROOT);
        RemoveIfExists("VR_Pelota_Interactuable");
        RemoveIfExists("VR_TeleportArea_SalaPrincipal");
        RemoveIfExists("VR_TeleportArea_SalaJuegos");

        // ── XR Interaction Manager ────────────────────────────────────────────
        var mgrGO = CreateGO(MGR_ROOT);
        AddComp(mgrGO, "UnityEngine.XR.Interaction.Toolkit.XRInteractionManager");

        // ── BLOCK 1: Camera Rig (XR Origin) ──────────────────────────────────
        var vrRoot = CreateGO(VR_ROOT);
        Undo.RegisterCreatedObjectUndo(vrRoot, VR_ROOT);

        var xrOriginGO = CreateGO("XR Origin (Rig)");
        xrOriginGO.transform.SetParent(vrRoot.transform);
        xrOriginGO.transform.localPosition = new Vector3(0f, 0f, -3f); // entrada casa

        var xrOriginComp = xrOriginGO.AddComponent(xrOriginType);

        // Camera Offset
        var camOffset = CreateGO("Camera Offset");
        camOffset.transform.SetParent(xrOriginGO.transform);
        camOffset.transform.localPosition = new Vector3(0f, 1.75f, 0f);

        // Main Camera (HMD)
        var camGO = CreateGO("Main Camera");
        camGO.transform.SetParent(camOffset.transform);
        camGO.transform.localPosition = Vector3.zero;
        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.01f;
        cam.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        // Asignar Camera y CameraOffset al XROrigin via reflection
        AssignMember(xrOriginComp, "Camera", cam);
        AssignMember(xrOriginComp, "CameraFloorOffsetObject", camOffset);

        // ── BLOCK 2: Controller Tracking (Action Based Controllers) ───────────
        var leftCtrl  = BuildController("LeftHand Controller",  camOffset.transform, true);
        var rightCtrl = BuildController("RightHand Controller", camOffset.transform, false);

        // ── BLOCK 3: Grab Interaction (XR Ray Interactor) ─────────────────────
        AttachRayInteractor(leftCtrl,  true);
        AttachRayInteractor(rightCtrl, false);
        CreateGrabInteractable(); // pelota naranja en Sala de Juegos

        // ── BLOCK 4: Teleportation (Locomotion + Teleport Areas) ──────────────
        SetupTeleportation(xrOriginGO);

        // ── XR Device Simulator ───────────────────────────────────────────────
        SetupDeviceSimulator();

        // ── Desactivar FPS Player para no chocar con XR Origin ────────────────
        DisableFPSPlayer();

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[ArqVizVR] ✓ Building Blocks configurados correctamente.");

        EditorUtility.DisplayDialog("ArqViz VR ✓  —  Building Blocks creados",
            "COMPLETADO:\n" +
            "✓ Block 1 — XR Origin (Camera Rig)\n" +
            "✓ Block 2 — Action Based Controllers L/R\n" +
            "✓ Block 3 — XR Ray Interactor + Grab Interactable\n" +
            "✓ Block 4 — Teleportation Provider + 2 Teleport Areas\n" +
            "✓ XR Device Simulator (inactivo por defecto)\n\n" +
            "PASOS SIGUIENTES:\n" +
            "1.  Edit > Project Settings > XR Plugin Management\n" +
            "    → Activar OpenXR  (pestaña PC y pestaña Android)\n" +
            "2.  En OpenXR Settings: añadir\n" +
            "    'Meta Quest Touch Pro Controller Profile'\n" +
            "3.  Guarda la escena (Ctrl+S)\n" +
            "4.  Para probar sin headset:\n" +
            "    → Activa el GO 'XR_DeviceSimulator'\n" +
            "    → Play — usa WASD/Mouse para simular HMD\n" +
            "5.  Para build Android/Quest:\n" +
            "    File > Build Settings > Android > Switch Platform",
            "Entendido");
    }

    // ── Controller ────────────────────────────────────────────────────────────
    static GameObject BuildController(string name, Transform parent, bool isLeft)
    {
        var go = CreateGO(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = isLeft
            ? new Vector3(-0.2f, -0.1f, 0.05f)
            : new Vector3( 0.2f, -0.1f, 0.05f);

        // Action Based Controller — input del controlador físico / simulado
        AddComp(go, "UnityEngine.XR.Interaction.Toolkit.ActionBasedController");

        // Modelo visual del controlador (cubo pequeño)
        var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.name = "Model";
        model.transform.SetParent(go.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localScale    = new Vector3(0.05f, 0.05f, 0.1f);
        UnityEngine.Object.DestroyImmediate(model.GetComponent<BoxCollider>());

        var mr  = model.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = isLeft ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.5f, 0.3f);
            mr.sharedMaterial = mat;
        }

        return go;
    }

    // ── Ray Interactor ────────────────────────────────────────────────────────
    static void AttachRayInteractor(GameObject ctrl, bool isLeft)
    {
        // XR Ray Interactor — permite seleccionar objetos y áreas de teleport a distancia
        var rayType = FindType("UnityEngine.XR.Interaction.Toolkit.XRRayInteractor");
        if (rayType != null) ctrl.AddComponent(rayType);

        // Visualizador del rayo
        var lineVisType = FindType("UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual");
        if (lineVisType != null) ctrl.AddComponent(lineVisType);

        // Line Renderer para rayo visible
        var lr = ctrl.AddComponent<LineRenderer>();
        lr.startWidth    = 0.01f;
        lr.endWidth      = 0.002f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        var lrMat = new Material(Shader.Find("Sprites/Default"));
        lrMat.color = isLeft ? new Color(0.4f, 0.8f, 1f, 0.8f) : new Color(1f, 0.65f, 0.2f, 0.8f);
        lr.material = lrMat;
    }

    // ── Grab Interactable Object ───────────────────────────────────────────────
    static void CreateGrabInteractable()
    {
        // Pelota naranja en Sala de Juegos — objeto interactuable con VR
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(sphere, "VR Pelota");
        sphere.name = "VR_Pelota_Interactuable";
        sphere.transform.position   = new Vector3(9f, 1.2f, 2.5f); // Sala de Juegos
        sphere.transform.localScale = Vector3.one * 0.18f;

        var rb = sphere.AddComponent<Rigidbody>();
        rb.mass = 0.4f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // XR Grab Interactable — permite agarrar con el controlador
        AddComp(sphere, "UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable");

        var mr = sphere.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(1f, 0.4f, 0.1f);
            mr.sharedMaterial = mat;
        }

        Debug.Log("[ArqVizVR] Pelota interactuable creada en Sala de Juegos (9, 1.2, 2.5).");
    }

    // ── Teleportation ─────────────────────────────────────────────────────────
    static void SetupTeleportation(GameObject xrOriginGO)
    {
        // Locomotion Provider (XRI 3.x usa LocomotionMediator; 2.x usa LocomotionSystem)
        var mediatorType = FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionMediator")
                        ?? FindType("UnityEngine.XR.Interaction.Toolkit.LocomotionSystem");
        if (mediatorType != null)
            xrOriginGO.AddComponent(mediatorType);

        // Teleportation Provider
        var tpType = FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider")
                  ?? FindType("UnityEngine.XR.Interaction.Toolkit.TeleportationProvider");
        if (tpType != null)
            xrOriginGO.AddComponent(tpType);

        // ── Area de teleport 1: Sala Principal ────────────────────────────────
        var area1 = CreateTeleportPlane(
            "VR_TeleportArea_SalaPrincipal",
            new Vector3(0f, 0.005f, 3.5f),
            new Vector3(1.8f, 1f, 1.5f));  // ~18x15m

        // ── Area de teleport 2: Sala de Juegos ────────────────────────────────
        var area2 = CreateTeleportPlane(
            "VR_TeleportArea_SalaJuegos",
            new Vector3(11f, 0.005f, 2.5f),
            new Vector3(0.8f, 1f, 0.8f));  // ~8x8m

        // Asignar TeleportationArea a ambas
        var taType = FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea")
                  ?? FindType("UnityEngine.XR.Interaction.Toolkit.TeleportationArea");
        if (taType != null)
        {
            area1.AddComponent(taType);
            area2.AddComponent(taType);
        }

        Debug.Log("[ArqVizVR] 2 Teleportation Areas creadas (Sala Principal + Sala Juegos).");
    }

    static GameObject CreateTeleportPlane(string name, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(go, name);
        go.name = name;
        go.transform.position   = pos;
        go.transform.localScale = scale;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.25f, 0.85f, 0.35f, 0.35f);
            // Transparencia en URP
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend",   0f); // Alpha
            mat.renderQueue = 3000;
            mr.sharedMaterial = mat;
        }
        return go;
    }

    // ── XR Device Simulator ───────────────────────────────────────────────────
    static void SetupDeviceSimulator()
    {
        var simGO = CreateGO(SIM_ROOT);
        Undo.RegisterCreatedObjectUndo(simGO, SIM_ROOT);
        var simType = FindType("UnityEngine.XR.Interaction.Toolkit.XRDeviceSimulator");
        if (simType != null)
            simGO.AddComponent(simType);

        simGO.SetActive(false); // Activar manualmente para probar sin headset
        Debug.Log("[ArqVizVR] XR Device Simulator creado (desactivado). Actívalo para probar en editor.");
    }

    // ── Desactivar FPS Player ─────────────────────────────────────────────────
    static void DisableFPSPlayer()
    {
        // Buscar por nombres comunes
        string[] names = { "Player", "FPSPlayer", "PlayerController",
                           "FirstPersonPlayer", "FPS Player", "PlayerCapsule" };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null)
            {
                go.SetActive(false);
                Debug.Log($"[ArqVizVR] Player FPS '{n}' desactivado (modo VR activo).");
                return;
            }
        }

        // Fallback: buscar por tag
        var byTag = GameObject.FindGameObjectWithTag("Player");
        if (byTag != null)
        {
            byTag.SetActive(false);
            Debug.Log($"[ArqVizVR] Player '{byTag.name}' desactivado (tag Player).");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static GameObject CreateGO(string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"ArqVizVR: {name}");
        return go;
    }

    static void RemoveIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Undo.DestroyObjectImmediate(go);
    }

    static Component AddComp(GameObject go, string typeName)
    {
        var t = FindType(typeName);
        if (t == null)
        {
            Debug.LogWarning($"[ArqVizVR] Tipo no encontrado: {typeName}");
            return null;
        }
        return go.AddComponent(t);
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

    static void AssignMember(Component comp, string memberName, object value)
    {
        if (comp == null) return;
        var type = comp.GetType();

        // Intenta propiedad pública primero
        var prop = type.GetProperty(memberName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(comp, value);
            return;
        }

        // Fallback: campo
        var field = type.GetField(memberName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(comp, value);
    }
}
