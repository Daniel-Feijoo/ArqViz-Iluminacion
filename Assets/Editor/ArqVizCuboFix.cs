using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ArqViz / Fix Cubo VR + Mano
///
/// 1. Crea el cubo interactuable naranja (con VRCuboInteractuable)
/// 2. Instala VRMano en la Main Camera del XR Rig
///
/// CONTROLES resultantes:
///   WASD = mover personaje (la mano naranja sigue)
///   Mouse = girar vista (la mano apunta a donde miras)
///   G = agarrar / soltar el cubo (cuando el rayo verde lo toca)
/// </summary>
public class ArqVizCuboFix
{
    [MenuItem("ArqViz/Fix Cubo VR — Reemplazar objeto interactuable")]
    static void Setup()
    {
        bool cuboOk = CrearCubo();
        bool manoOk = InstalarVRMano();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        string status = $"Cubo VR: {(cuboOk ? "✓" : "⚠")}\nVRMano en cámara: {(manoOk ? "✓" : "⚠ asígnala manualmente")}";

        EditorUtility.DisplayDialog("ArqViz — VR Cubo + Mano ✓",
            status + "\n\n" +
            "CONTROLES:\n\n" +
            "WASD     = mover el personaje\n" +
            "Mouse    = rotar la vista\n" +
            "G        = agarrar objeto (cuando el rayo verde lo toca)\n" +
            "G (otra vez) = soltar el objeto\n\n" +
            "La mano (esfera blanca) siempre está a tu derecha\n" +
            "y apunta a donde mira la cámara.\n" +
            "Acércate al cubo naranja y presiona G.\n\n" +
            "Ctrl+S para guardar.",
            "OK");
    }

    // ── Crear cubo interactuable ──────────────────────────────────────────────
    static bool CrearCubo()
    {
        // Eliminar cubo anterior
        foreach (var n in new[] { "VR_Cubo_Interactuable", "VR_Pelota_Interactuable" })
        {
            var old = GameObject.Find(n);
            if (old != null) Undo.DestroyObjectImmediate(old);
        }

        // Crear cubo primitivo
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "VR_Cubo_Interactuable";
        Undo.RegisterCreatedObjectUndo(cubo, "VR Cubo");

        cubo.transform.position   = new Vector3(9f, 0.8f, 2.5f); // Sala de Juegos, en el suelo
        cubo.transform.localScale = Vector3.one * 0.3f;            // 30 cm

        // Rigidbody
        var rb = cubo.AddComponent<Rigidbody>();
        rb.mass                   = 0.3f;
        rb.useGravity             = true;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Script interactuable (detectado por VRMano)
        cubo.AddComponent<VRCuboInteractuable>();

        // XRGrabInteractable — para que aparezca en la jerarquía como Building Block
        // (aunque el agarre real lo hace VRMano)
        var grabType = FindType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable")
                    ?? FindType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable");
        if (grabType != null)
            cubo.AddComponent(grabType);

        // Material naranja
        var mr     = cubo.GetComponent<MeshRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (mr != null && shader != null)
        {
            var mat = new Material(shader) { name = "Mat_CuboVR", color = new Color(1f, 0.45f, 0.1f) };
            mr.sharedMaterial = mat;
        }

        EditorUtility.SetDirty(cubo);
        Debug.Log("[CuboFix] ✓ VR_Cubo_Interactuable creado en Sala de Juegos.");
        return true;
    }

    // ── Instalar VRMano en la cámara del XR Rig ──────────────────────────────
    static bool InstalarVRMano()
    {
        // Eliminar VRMano existente
        foreach (var old in UnityEngine.Object.FindObjectsByType<VRMano>(FindObjectsSortMode.None))
            Undo.DestroyObjectImmediate(old.gameObject);

        // Buscar Main Camera dentro del XR Origin
        Camera cam = null;

        var xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin != null)
        {
            var camTr = xrOrigin.transform.Find("Camera Offset/Main Camera");
            if (camTr != null) cam = camTr.GetComponent<Camera>();
        }

        // Fallback: Camera.main
        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[CuboFix] No se encontró la cámara principal. Añade VRMano manualmente.");
            return false;
        }

        // Crear GO "VR_Mano_Derecha" hijo de la cámara
        var manoGO = new GameObject("VR_Mano_Derecha");
        Undo.RegisterCreatedObjectUndo(manoGO, "VR Mano");
        manoGO.transform.SetParent(cam.transform);
        manoGO.transform.localPosition = Vector3.zero; // VRMano.Start() lo posiciona

        var mano = manoGO.AddComponent<VRMano>();
        mano.posicionLocal = new Vector3(0.35f, -0.25f, 0.6f);
        mano.alcanceRayo   = 8f;
        mano.teclaAgarrar  = KeyCode.G;

        EditorUtility.SetDirty(manoGO);
        Debug.Log($"[CuboFix] ✓ VRMano instalada en: {cam.gameObject.name}");
        return true;
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    static Type FindType(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(name);
            if (t != null) return t;
        }
        return null;
    }
}
