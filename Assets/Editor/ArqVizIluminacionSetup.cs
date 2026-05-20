using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Setup de iluminación arquitectónica - Estilo "Hora Dorada" (Golden Hour)
/// Menú: ArqViz > Configurar Iluminación Arquitectónica
/// </summary>
public static class ArqVizIluminacionSetup
{
    // ─── Paleta de color - Hora Dorada Arquitectónica ───────────────────────
    static readonly Color SOL_DORADO        = new Color(1.00f, 0.878f, 0.588f); // #FFE096 sol tarde
    static readonly Color CIELO_FILL        = new Color(0.49f, 0.64f, 1.00f);   // #7DA3FF cielo azul
    static readonly Color ACENTO_CALIDO     = new Color(1.00f, 0.95f, 0.80f);   // #FFF2CC blanco calido
    static readonly Color NIEBLA_CALIDA     = new Color(0.78f, 0.74f, 0.68f);   // bruma dorada sutil

    [MenuItem("ArqViz/Configurar Iluminación Arquitectónica")]
    public static void SetupIluminacion()
    {
        SetupDirectionalLight();
        SetupLuzFillCielo();
        SetupSpotAcento();
        SetupSkybox();
        SetupNiebla();
        SetupPostProcesado();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "ArqViz — Setup Completo",
            "✓ Directional Light: Hora dorada (35°, SO)\n" +
            "✓ Fill Light: Cielo azul ambiental\n" +
            "✓ Spot Light: Acento arquitectónico\n" +
            "✓ Skybox: Procedural atardecer\n" +
            "✓ Niebla: Bruma atmosférica sutil\n" +
            "✓ Post-pro: Bloom + Vignette + Color Adjustments\n\n" +
            "Guarda la escena (Ctrl+S) para conservar los cambios.",
            "OK"
        );
    }

    // ─── 1. DIRECTIONAL LIGHT ────────────────────────────────────────────────
    static void SetupDirectionalLight()
    {
        Light dirLight = FindOrCreateLight("Directional Light", LightType.Directional);

        // Ángulo: sol de tarde-tarde (35° elevación, suroeste)
        dirLight.transform.rotation = Quaternion.Euler(35f, -150f, 0f);

        dirLight.color        = SOL_DORADO;
        dirLight.intensity    = 1.2f;
        dirLight.shadows      = LightShadows.Soft;
        dirLight.shadowStrength = 0.75f;
        dirLight.shadowBias   = 0.05f;
        dirLight.shadowNormalBias = 0.3f;

        EditorUtility.SetDirty(dirLight.gameObject);
        Debug.Log("[ArqViz] Directional Light → hora dorada, ángulo SO, sombras suaves");
    }

    // ─── 2. FILL LIGHT (Point) ───────────────────────────────────────────────
    // Razón: simula la luz difusa del cielo azul que llena las sombras.
    // Sin este fill, las sombras quedan negro puro (irreal en exteriores).
    static void SetupLuzFillCielo()
    {
        const string nombre = "Fill Light — Cielo Ambiente";
        Light fill = FindOrCreateLight(nombre, LightType.Point);

        fill.gameObject.transform.position = new Vector3(0f, 8f, 0f);
        fill.color     = CIELO_FILL;
        fill.intensity = 1.5f;
        fill.range     = 30f;
        fill.shadows   = LightShadows.None; // fill sin sombras = más performance

        EditorUtility.SetDirty(fill.gameObject);
        Debug.Log("[ArqViz] Fill Light (Point) → cielo azul, sin sombras, Y=8");
    }

    // ─── 3. SPOT LIGHT (Acento) ──────────────────────────────────────────────
    // Razón: dirige la atención a la fachada principal (punto de interés).
    // En arch-viz, un spot sobre la entrada crea jerarquía visual inmediata.
    static void SetupSpotAcento()
    {
        const string nombre = "Spot Light — Acento Arquitectónico";
        Light spot = FindOrCreateLight(nombre, LightType.Spot);

        spot.gameObject.transform.position = new Vector3(5f, 6f, -3f);
        spot.gameObject.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
        spot.color        = ACENTO_CALIDO;
        spot.intensity    = 15f;
        spot.range        = 20f;
        spot.spotAngle    = 45f;
        spot.shadows      = LightShadows.Soft;
        spot.shadowStrength = 0.5f;

        EditorUtility.SetDirty(spot.gameObject);
        Debug.Log("[ArqViz] Spot Light → acento fachada, cálido, sombras suaves");
    }

    // ─── 4. SKYBOX ───────────────────────────────────────────────────────────
    // Procedural shader incluido en Unity (no requiere import externo).
    // Configurado para cielo de atardecer claro con sol visible.
    static void SetupSkybox()
    {
        const string matPath = "Assets/Materials/Skybox_HoraDorada.mat";

        Material sky = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (sky == null)
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                Debug.LogError("[ArqViz] Shader 'Skybox/Procedural' no encontrado.");
                return;
            }
            sky = new Material(shader) { name = "Skybox_HoraDorada" };

            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");

            AssetDatabase.CreateAsset(sky, matPath);
        }

        // Parámetros de Skybox/Procedural
        sky.SetFloat("_SunSize",            0.04f);  // sol pequeño realista
        sky.SetFloat("_SunSizeConvergence", 5f);
        sky.SetFloat("_AtmosphereThickness",1.2f);   // atmósfera densa = calor
        sky.SetColor("_SkyTint",  new Color(0.53f, 0.75f, 1.00f)); // azul suave
        sky.SetColor("_GroundColor", new Color(0.40f, 0.37f, 0.34f)); // tierra cálida
        sky.SetFloat("_Exposure", 1.1f);

        RenderSettings.skybox = sky;
        DynamicGI.UpdateEnvironment();

        EditorUtility.SetDirty(sky);
        Debug.Log("[ArqViz] Skybox procedural → atardecer claro creado en " + matPath);
    }

    // ─── 5. NIEBLA ───────────────────────────────────────────────────────────
    // Decisión: niebla ACTIVADA con densidad muy baja.
    // Razón de diseño: en arch-viz exterior la niebla da profundidad
    // atmosférica (aerial perspective) haciendo los planos lejanos más
    // claros. Densidad 0.008 es casi invisible pero aporta realismo sutil.
    static void SetupNiebla()
    {
        RenderSettings.fog        = true;
        RenderSettings.fogMode    = FogMode.Exponential;
        RenderSettings.fogColor   = NIEBLA_CALIDA;
        RenderSettings.fogDensity = 0.008f;

        Debug.Log("[ArqViz] Niebla → exponencial, bruma dorada, densidad 0.008");
    }

    // ─── 6. POST-PROCESADO ───────────────────────────────────────────────────
    static void SetupPostProcesado()
    {
        const string profilePath = "Assets/Settings/SampleSceneProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);

        if (profile == null)
        {
            Debug.LogError("[ArqViz] No se encontró: " + profilePath);
            return;
        }

        // — Bloom ──────────────────────────────────────────────────────────
        // Threshold alto = solo brillan superficies muy iluminadas (ventanas, metal).
        // Intensidad baja = sutil, profesional, no "videojueguero".
        if (!profile.TryGet<Bloom>(out var bloom))
            bloom = profile.Add<Bloom>();

        bloom.active = true;
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(0.45f);
        bloom.scatter.Override(0.5f);
        bloom.highQualityFiltering.Override(true);

        // — Vignette ───────────────────────────────────────────────────────
        // Focaliza la mirada en el centro de la composición.
        // Muy usada en renders de arquitectura para dar "encuadre fotográfico".
        if (!profile.TryGet<Vignette>(out var vignette))
            vignette = profile.Add<Vignette>();

        vignette.active = true;
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.40f);
        vignette.rounded.Override(true);

        // — Color Adjustments ──────────────────────────────────────────────
        // Color filter cálido refuerza la hora dorada.
        // Contraste +12 da "punch" fotográfico realista.
        // Saturación +15 hace materiales más vívidos sin quemar.
        if (!profile.TryGet<ColorAdjustments>(out var colorAdj))
            colorAdj = profile.Add<ColorAdjustments>();

        colorAdj.active = true;
        colorAdj.postExposure.Override(0.10f);
        colorAdj.contrast.Override(12f);
        colorAdj.colorFilter.Override(new Color(1.00f, 0.97f, 0.90f)); // filtro ámbar suave
        colorAdj.hueShift.Override(0f);
        colorAdj.saturation.Override(15f);

        EditorUtility.SetDirty(profile);
        Debug.Log("[ArqViz] Post-procesado → Bloom + Vignette + Color Adjustments configurados");
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    static Light FindOrCreateLight(string goName, LightType type)
    {
        // Buscar por nombre exacto primero
        var go = GameObject.Find(goName);
        if (go != null)
        {
            var existing = go.GetComponent<Light>();
            if (existing != null) return existing;
        }

        // Si es Directional, reusar la existente de la escena
        if (type == LightType.Directional)
        {
#pragma warning disable CS0618
            var all = Object.FindObjectsOfType<Light>();
#pragma warning restore CS0618
            foreach (var l in all)
                if (l.type == LightType.Directional) return l;
        }

        // Crear nueva
        var newGo = new GameObject(goName);
        var light  = newGo.AddComponent<Light>();
        light.type = type;
        return light;
    }

}
