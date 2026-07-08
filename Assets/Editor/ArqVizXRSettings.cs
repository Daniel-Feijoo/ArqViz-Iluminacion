using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ArqViz / Configurar OpenXR Settings
///
/// Activa OpenXR como XR loader en Project Settings programáticamente.
/// Corre DESPUÉS de que Unity termine de instalar los paquetes XR.
///
/// Equivalente a: Edit > Project Settings > XR Plugin Management > OpenXR (tick)
/// </summary>
public class ArqVizXRSettings
{
    [MenuItem("ArqViz/Activar OpenXR en Project Settings")]
    static void ConfigurarOpenXR()
    {
        // Verificar que XR Management está instalado
        var xrmSettingsType = FindType("UnityEditor.XR.Management.XRManagerSettings")
                           ?? FindType("UnityEngine.XR.Management.XRManagerSettings");

        if (xrmSettingsType == null)
        {
            EditorUtility.DisplayDialog("ArqViz XR",
                "XR Plugin Management no está instalado aún.\n\n" +
                "Espera que Unity descargue los paquetes del manifest.json y vuelve a intentarlo.",
                "OK");
            return;
        }

        bool usedEditor = ConfigurarViaEditorAPI();

        if (!usedEditor)
        {
            // Fallback: instrucciones manuales
            EditorUtility.DisplayDialog("ArqViz XR — Configuración manual requerida",
                "No se pudo configurar automáticamente. Haz esto manualmente:\n\n" +
                "1. Edit > Project Settings > XR Plugin Management\n" +
                "2. En la pestaña PC (escritorio): marca ✓ OpenXR\n" +
                "3. En la pestaña Android: marca ✓ OpenXR\n" +
                "4. Haz clic en el ícono ⚙ de OpenXR\n" +
                "5. En 'Interaction Profiles': haz clic en +\n" +
                "   Agrega 'Meta Quest Touch Pro Controller Profile'\n" +
                "6. En 'OpenXR Feature Groups': activa\n" +
                "   'Meta Quest Support'\n\n" +
                "Luego corre: ArqViz > Configurar VR — Building Blocks",
                "OK");
            return;
        }

        Debug.Log("[ArqVizXR] ✓ OpenXR configurado correctamente.");
        EditorUtility.DisplayDialog("ArqViz XR ✓",
            "OpenXR activado en Project Settings.\n\n" +
            "PASOS FINALES:\n" +
            "1. Edit > Project Settings > XR Plugin Management > OpenXR\n" +
            "   → En 'Interaction Profiles': + > Meta Quest Touch Pro\n" +
            "   → En 'OpenXR Feature Groups': activa 'Meta Quest Support'\n\n" +
            "2. Guarda (Ctrl+S)\n" +
            "3. Corre: ArqViz > Configurar VR — Building Blocks",
            "OK");
    }

    static bool ConfigurarViaEditorAPI()
    {
        try
        {
            // XR Plugin Management Editor API
            var xrmBuildType = FindType("UnityEditor.XR.Management.XRPackageMetadataStore");
            if (xrmBuildType == null) return false;

            // Intentar activar OpenXR via la API pública
            // UnityEditor.XR.Management.XRPackageMetadataStore.AssignLoader(...)
            var assignMethod = xrmBuildType.GetMethod("AssignLoader",
                BindingFlags.Static | BindingFlags.Public);
            if (assignMethod == null) return false;

            // Obtener XRGeneralSettings para BuildTargetGroup Standalone y Android
            var generalSettingsType = FindType("UnityEngine.XR.Management.XRGeneralSettings");
            var generalSettingsMgrType = FindType("UnityEngine.XR.Management.XRGeneralSettingsPerBuildTarget");

            if (generalSettingsType == null) return false;

            // Tipo del loader OpenXR
            var openXRLoaderType = FindType("UnityEngine.XR.OpenXR.OpenXRLoader");
            if (openXRLoaderType == null) return false;

            // Activar para Standalone
            assignMethod.Invoke(null, new object[]
            {
                FindType("UnityEngine.XR.Management.XRManagerSettings"),
                openXRLoaderType.FullName,
                BuildTargetGroup.Standalone.ToString()
            });

            // Activar para Android (Quest)
            assignMethod.Invoke(null, new object[]
            {
                FindType("UnityEngine.XR.Management.XRManagerSettings"),
                openXRLoaderType.FullName,
                BuildTargetGroup.Android.ToString()
            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ArqVizXR] No se pudo configurar via API: {ex.Message}");
            return false;
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────────
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

    /// <summary>
    /// Abre la ventana de XR Plugin Management directamente desde menú.
    /// </summary>
    [MenuItem("ArqViz/Abrir XR Plugin Management")]
    static void AbrirXRPluginManagement()
    {
        SettingsService.OpenProjectSettings("Project/XR Plugin Management");
    }
}
