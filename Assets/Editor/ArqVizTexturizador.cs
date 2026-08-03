using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// ArqViz / Texturizador Realista
///
/// Genera texturas procedurales tileables (albedo + normal) y las aplica
/// SOLO al slot de textura de los materiales existentes (Assets/Materials/Casa
/// y Assets/Materials/Interior). No modifica _BaseColor, _Metallic ni
/// _Smoothness — esos valores los siguen controlando ArqVizCasaBuilder /
/// ArqVizInteriorBuilder. No toca objetos de la escena, colliders, tags ni
/// scripts, por lo que la navegación y las mecánicas VR no se ven afectadas.
///
/// Menú: ArqViz > Aplicar Texturas Realistas
///       ArqViz > Quitar Texturas (volver a color plano)
/// </summary>
public static class ArqVizTexturizador
{
    const string TEX_DIR = "Assets/Textures/Generadas/";

    // ══════════════════════════════════════════════════════════════════════
    // JOB — describe una textura a generar y el material al que se aplica
    // ══════════════════════════════════════════════════════════════════════
    class Job
    {
        public string matPath;
        public int size;
        public Func<float, float, float> valueFn;
        public float lo, hi;
        public Vector2 tileScale;
        public bool bump;
        public float bumpScale;
    }

    // ══════════════════════════════════════════════════════════════════════
    // MENÚ
    // ══════════════════════════════════════════════════════════════════════
    [MenuItem("ArqViz/Aplicar Texturas Realistas")]
    public static void AplicarTexturas()
    {
        var jobs = GetJobs();
        try
        {
            Directory.CreateDirectory(Application.dataPath + "/Textures/Generadas");

            for (int i = 0; i < jobs.Count; i++)
            {
                var j = jobs[i];
                EditorUtility.DisplayProgressBar("ArqViz — Generando texturas",
                    Path.GetFileNameWithoutExtension(j.matPath), (float)i / jobs.Count);
                ProcesarJob(j);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog("ArqViz — Texturas ✓",
            $"{jobs.Count} materiales texturizados (paredes, pisos, techo, " +
            "concreto, césped, sendero, madera, tela, mármol, azulejo, etc.)\n\n" +
            "Solo se tocó el slot de textura de cada material — colores, " +
            "iluminación, colliders y scripts VR quedaron intactos.\n\n" +
            "Ctrl+S para guardar la escena (los materiales ya se guardaron solos).",
            "OK");
    }

    // Utilidad de verificación — NO forma parte del flujo normal del proyecto.
    // Renderiza la cámara principal a un PNG fuera del proyecto, para poder
    // revisar el resultado visual sin abrir el editor de Unity.
    [MenuItem("ArqViz/Debug - Renderizar Preview PNG")]
    public static void RenderizarPreview()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

        var camGO = GameObject.Find("Main Camera");
        Camera cam = camGO != null ? camGO.GetComponent<Camera>() : Camera.main;
        if (cam == null) { Debug.LogError("[Preview] No se encontró ninguna cámara."); return; }
        cam.gameObject.SetActive(true);
        cam.enabled = true;

        string vista = System.Environment.GetEnvironmentVariable("ARQVIZ_PREVIEW_VISTA");
        if (vista == "fachada")
        {
            cam.transform.position = new Vector3(-24f, 12f, -14f);
            cam.transform.LookAt(new Vector3(6f, 2f, 8f));
        }
        else if (vista == "sala")
        {
            cam.transform.position = new Vector3(-3f, 1.6f, 2.5f);
            cam.transform.rotation = Quaternion.Euler(10f, 200f, 0f);
        }
        else if (vista == "juegos")
        {
            cam.transform.position = new Vector3(15.5f, 1.7f, 3f);
            cam.transform.rotation = Quaternion.Euler(8f, 60f, 0f);
        }

        int w = 1280, h = 720;
        var rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        cam.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        cam.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(rt);

        byte[] png = tex.EncodeToPNG();
        string outPath = System.Environment.GetEnvironmentVariable("ARQVIZ_PREVIEW_PATH");
        if (string.IsNullOrEmpty(outPath))
            outPath = Application.dataPath + "/../arqviz_preview.png";
        File.WriteAllBytes(outPath, png);
        Debug.Log("[Preview] Guardado en: " + outPath);
    }

    [MenuItem("ArqViz/Quitar Texturas (volver a color plano)")]
    public static void QuitarTexturas()
    {
        var jobs = GetJobs();
        foreach (var j in jobs)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(j.matPath);
            if (mat == null) continue;
            mat.SetTexture("_BaseMap", null);
            mat.SetTexture("_BumpMap", null);
            mat.DisableKeyword("_NORMALMAP");
            mat.SetTextureScale("_BaseMap", Vector2.one);
            mat.SetTextureScale("_BumpMap", Vector2.one);
            EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("ArqViz", "Texturas removidas. Materiales vuelven a color plano.", "OK");
    }

    // ══════════════════════════════════════════════════════════════════════
    // PROCESAMIENTO DE UN JOB
    // ══════════════════════════════════════════════════════════════════════
    static void ProcesarJob(Job j)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(j.matPath);
        if (mat == null)
        {
            Debug.LogWarning("[ArqVizTexturas] Material no encontrado: " + j.matPath);
            return;
        }

        string nombre = Path.GetFileNameWithoutExtension(j.matPath);

        var albedoTex = BuildAlbedo(j.size, j.valueFn, j.lo, j.hi);
        var albedo = GuardarTextura(albedoTex, nombre + "_Albedo", false);
        mat.SetTexture("_BaseMap", albedo);
        mat.SetTextureScale("_BaseMap", j.tileScale);

        if (j.bump)
        {
            var normalTex = BuildNormal(j.size, j.valueFn, 3.5f);
            var normal = GuardarTextura(normalTex, nombre + "_Normal", true);
            mat.SetTexture("_BumpMap", normal);
            mat.SetTextureScale("_BumpMap", j.tileScale);
            mat.SetFloat("_BumpScale", j.bumpScale);
            mat.EnableKeyword("_NORMALMAP");
        }

        EditorUtility.SetDirty(mat);
    }

    // ══════════════════════════════════════════════════════════════════════
    // CONSTRUCCIÓN DE TEXTURAS
    // ══════════════════════════════════════════════════════════════════════
    static Texture2D BuildAlbedo(int size, Func<float, float, float> valueFn, float lo, float hi)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float v = y / (float)size;
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float val = Mathf.Clamp01(Mathf.Lerp(lo, hi, valueFn(u, v)));
                px[y * size + x] = new Color(val, val, val, 1f);
            }
        }
        tex.SetPixels(px);
        tex.Apply(false);
        return tex;
    }

    static Texture2D BuildNormal(int size, Func<float, float, float> valueFn, float strength)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        float step = 1f / size;
        for (int y = 0; y < size; y++)
        {
            float v = y / (float)size;
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float hL = valueFn(u - step, v);
                float hR = valueFn(u + step, v);
                float hD = valueFn(u, v - step);
                float hU = valueFn(u, v + step);
                float dx = (hR - hL) * strength;
                float dy = (hU - hD) * strength;
                Vector3 n = new Vector3(-dx, -dy, 1f).normalized;
                px[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
        }
        tex.SetPixels(px);
        tex.Apply(false);
        return tex;
    }

    static Texture2D GuardarTextura(Texture2D tex, string nombre, bool normalMap)
    {
        int size = tex.width;
        byte[] png = tex.EncodeToPNG();
        string relPath = TEX_DIR + nombre + ".png";
        string absPath = Application.dataPath + relPath.Substring("Assets".Length);
        File.WriteAllBytes(absPath, png);
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(relPath, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(relPath);
        imp.wrapMode = TextureWrapMode.Repeat;
        imp.filterMode = FilterMode.Trilinear;
        imp.mipmapEnabled = true;
        imp.maxTextureSize = Mathf.Max(size, 32);
        imp.textureCompression = TextureImporterCompression.Compressed;
        if (normalMap)
        {
            imp.textureType = TextureImporterType.NormalMap;
            imp.convertToNormalmap = false;
            imp.sRGBTexture = false;
        }
        else
        {
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
        }
        imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(relPath);
    }

    // ══════════════════════════════════════════════════════════════════════
    // RUIDO BASE — value-noise con lattice modular (tileable exacto)
    // ══════════════════════════════════════════════════════════════════════
    static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 2147483647;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / 2147483647f;
        }
    }

    static float ValueNoise(float u, float v, int cells, int seed)
    {
        float gx = u * cells, gy = v * cells;
        int x0 = Mathf.FloorToInt(gx), y0 = Mathf.FloorToInt(gy);
        float fx = gx - x0, fy = gy - y0;
        int x1 = x0 + 1, y1 = y0 + 1;
        int X0 = ((x0 % cells) + cells) % cells, X1 = ((x1 % cells) + cells) % cells;
        int Y0 = ((y0 % cells) + cells) % cells, Y1 = ((y1 % cells) + cells) % cells;
        float v00 = Hash(X0, Y0, seed), v10 = Hash(X1, Y0, seed);
        float v01 = Hash(X0, Y1, seed), v11 = Hash(X1, Y1, seed);
        float sx = fx * fx * (3f - 2f * fx);
        float sy = fy * fy * (3f - 2f * fy);
        return Mathf.Lerp(Mathf.Lerp(v00, v10, sx), Mathf.Lerp(v01, v11, sx), sy);
    }

    // Nota de diseño: todos los generadores de abajo solo escalan u/v por
    // ENTEROS antes de pasarlos a Fbm — así el patrón resultante siempre
    // repite exacto en el borde (0↔1), sin costuras visibles al tilear.
    static float Fbm(float u, float v, int octaves, int baseCells, int seed)
    {
        float sum = 0f, amp = 0.5f, ampSum = 0f; int cells = baseCells;
        for (int i = 0; i < octaves; i++)
        {
            sum += ValueNoise(u, v, cells, seed + i * 101) * amp;
            ampSum += amp;
            amp *= 0.5f;
            cells *= 2;
        }
        return sum / ampSum;
    }

    static float SmoothMask(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    // ══════════════════════════════════════════════════════════════════════
    // PATRONES
    // ══════════════════════════════════════════════════════════════════════
    static float Textura(float u, float v, int seed, int broadCells, int fineCells, float broadWeight)
    {
        float broad = Fbm(u, v, 2, broadCells, seed);
        float fine  = Fbm(u, v, 2, fineCells, seed + 71);
        return Mathf.Clamp01(broadWeight * broad + (1f - broadWeight) * fine);
    }

    static float Grass(float u, float v, int seed)
    {
        float patches = Fbm(u, v, 3, 6, seed);
        float blades  = Fbm(u, v, 3, 90, seed + 31);
        return Mathf.Clamp01(0.30f + 0.35f * patches + 0.55f * blades);
    }

    static float Bark(float u, float v, int seed)
    {
        float warp  = Fbm(u * 2f, v * 2f, 2, 6, seed) * 0.10f;
        float ridge = Fbm(u * 3f + warp, v, 3, 12, seed + 7);
        float fine  = Fbm(u, v, 2, 44, seed + 19);
        return Mathf.Clamp01(0.25f + 0.45f * ridge + 0.30f * fine);
    }

    static float WoodPlank(float u, float v, int seed, int numPlanks)
    {
        float vp = v * numPlanks;
        int plankIdx = ((Mathf.FloorToInt(vp) % numPlanks) + numPlanks) % numPlanks;
        float vLocal = vp - Mathf.Floor(vp);
        float plankTone = Hash(plankIdx, 0, seed) * 0.30f;
        float grain = Fbm(u * 4f + plankIdx * 0.37f, vLocal, 2, 16, seed + plankIdx * 13 + 5);
        float seamFade = SmoothMask(0f, 0.07f, vLocal) * SmoothMask(0f, 0.07f, 1f - vLocal);
        float val = 0.5f + plankTone + 0.28f * grain;
        val *= Mathf.Lerp(0.65f, 1f, seamFade);
        return Mathf.Clamp01(val);
    }

    static float GridGrout(float u, float v, int seed, int tilesU, int tilesV, float groutWidth)
    {
        float tu = u * tilesU, tv = v * tilesV;
        float fu = tu - Mathf.Floor(tu), fv = tv - Mathf.Floor(tv);
        int idxU = Mathf.FloorToInt(tu), idxV = Mathf.FloorToInt(tv);
        bool grout = fu < groutWidth || fu > 1f - groutWidth || fv < groutWidth || fv > 1f - groutWidth;
        if (grout) return 0.22f;
        float tileTone = Hash(idxU, idxV, seed) * 0.18f;
        float noise = Fbm(u, v, 2, 48, seed + idxU * 7 + idxV * 13) * 0.15f;
        return Mathf.Clamp01(0.65f + tileTone + noise);
    }

    static float Fabric(float u, float v, int seed, int threadsPerUnit)
    {
        float wx = Mathf.Sin(u * threadsPerUnit * Mathf.PI * 2f);
        float wy = Mathf.Sin(v * threadsPerUnit * Mathf.PI * 2f);
        float weave = (wx * wy) * 0.15f;
        float noise = Fbm(u, v, 2, threadsPerUnit * 2, seed) * 0.20f;
        return Mathf.Clamp01(0.70f + weave + noise);
    }

    static float Marble(float u, float v, int seed)
    {
        float warp = Fbm(u, v, 3, 5, seed) * 2.2f;
        float vein = Mathf.Sin((u + warp) * Mathf.PI * 4f);
        float sharp = Mathf.Pow(Mathf.Abs(vein), 0.3f);
        float grain = Fbm(u, v, 2, 20, seed + 3) * 0.15f;
        return Mathf.Clamp01(0.90f - sharp * 0.5f + grain - 0.10f);
    }

    static float Brushed(float u, float v, int seed)
    {
        float streaks = Fbm(0.5f, v, 3, 16, seed);
        float subtleU = Fbm(u, 0.5f, 1, 4, seed + 9) * 0.08f;
        return Mathf.Clamp01(0.70f + 0.30f * (streaks - 0.5f) + subtleU - 0.04f);
    }

    // ══════════════════════════════════════════════════════════════════════
    // LISTA DE MATERIALES A TEXTURIZAR
    //
    // Se dejan sin texturizar (a propósito): vidrios, focos/luces, bolas y
    // botellas de bar, piezas de futbolín, y TODO lo de Mecanicas_ArqViz
    // (pads de teletransporte, cajas físicas) — son elementos de juego que
    // deben verse limpios y legibles, no "realistas".
    // ══════════════════════════════════════════════════════════════════════
    static List<Job> GetJobs()
    {
        const string CASA = "Assets/Materials/Casa/";
        const string INT  = "Assets/Materials/Interior/";

        return new List<Job>
        {
            // ── Casa (exterior) ─────────────────────────────────────────
            new Job { matPath = CASA+"MatPared.mat",    size=512, lo=0.72f, hi=1.05f, tileScale=new Vector2(8f,1.6f),   bump=true,  bumpScale=0.30f, valueFn=(u,v)=>Textura(u,v,1,4,64,0.55f) },
            new Job { matPath = CASA+"MatTecho.mat",    size=512, lo=0.60f, hi=1.00f, tileScale=new Vector2(10f,8f),    bump=true,  bumpScale=0.50f, valueFn=(u,v)=>GridGrout(u,v,2,1,7,0.035f) },
            new Job { matPath = CASA+"MatPuerta.mat",   size=256, lo=0.65f, hi=1.05f, tileScale=new Vector2(1f,1.9f),   bump=true,  bumpScale=0.40f, valueFn=(u,v)=>WoodPlank(v,u,3,5) },
            new Job { matPath = CASA+"MatConcreto.mat", size=512, lo=0.55f, hi=1.00f, tileScale=new Vector2(10f,8f),    bump=true,  bumpScale=0.30f, valueFn=(u,v)=>Textura(u,v,4,6,48,0.5f) },
            new Job { matPath = CASA+"MatCesped.mat",   size=512, lo=0.50f, hi=1.05f, tileScale=new Vector2(67f,44f),   bump=true,  bumpScale=0.22f, valueFn=(u,v)=>Grass(u,v,5) },
            new Job { matPath = CASA+"MatSendero.mat",  size=512, lo=0.60f, hi=1.00f, tileScale=new Vector2(2.5f,5.7f), bump=true,  bumpScale=0.35f, valueFn=(u,v)=>GridGrout(u,v,6,4,4,0.05f) },
            new Job { matPath = CASA+"MatTronco.mat",   size=256, lo=0.55f, hi=1.00f, tileScale=new Vector2(2f,5f),     bump=true,  bumpScale=0.45f, valueFn=(u,v)=>Bark(u,v,7) },
            new Job { matPath = CASA+"MatCopa.mat",     size=256, lo=0.60f, hi=1.05f, tileScale=new Vector2(1.5f,1.5f), bump=false, bumpScale=0f,    valueFn=(u,v)=>Textura(u,v,8,5,30,0.5f) },
            new Job { matPath = CASA+"MatPilar.mat",    size=256, lo=0.70f, hi=1.05f, tileScale=new Vector2(1f,4.5f),   bump=true,  bumpScale=0.28f, valueFn=(u,v)=>Textura(u,v,9,5,40,0.55f) },
            new Job { matPath = CASA+"MatMarco.mat",    size=256, lo=0.75f, hi=1.05f, tileScale=new Vector2(4f,1f),     bump=false, bumpScale=0f,    valueFn=(u,v)=>Brushed(u,v,10) },

            // ── Interior ─────────────────────────────────────────────────
            new Job { matPath = INT+"Int_Piso.mat",     size=512, lo=0.65f, hi=1.05f, tileScale=new Vector2(6.25f,5f),   bump=true,  bumpScale=0.30f, valueFn=(u,v)=>WoodPlank(u,v,11,10) },
            new Job { matPath = INT+"Int_PisoJ.mat",    size=512, lo=0.55f, hi=0.95f, tileScale=new Vector2(7.5f,8.75f), bump=true,  bumpScale=0.30f, valueFn=(u,v)=>WoodPlank(u,v,12,10) },
            new Job { matPath = INT+"Int_ParedInt.mat", size=512, lo=0.85f, hi=1.05f, tileScale=new Vector2(4f,1.6f),    bump=true,  bumpScale=0.18f, valueFn=(u,v)=>Textura(u,v,13,4,64,0.4f) },
            new Job { matPath = INT+"Int_TechoInt.mat", size=256, lo=0.92f, hi=1.00f, tileScale=new Vector2(6.7f,5.3f),  bump=false, bumpScale=0f,    valueFn=(u,v)=>Textura(u,v,14,3,48,0.3f) },
            new Job { matPath = INT+"Int_Madera.mat",   size=256, lo=0.60f, hi=1.05f, tileScale=new Vector2(2f,1f),      bump=true,  bumpScale=0.35f, valueFn=(u,v)=>WoodPlank(v,u,15,6) },
            new Job { matPath = INT+"Int_Tapiz.mat",    size=256, lo=0.80f, hi=1.05f, tileScale=new Vector2(3.75f,3.1f), bump=true,  bumpScale=0.22f, valueFn=(u,v)=>Fabric(u,v,16,24) },
            new Job { matPath = INT+"Int_Colcha.mat",   size=256, lo=0.85f, hi=1.05f, tileScale=new Vector2(2.2f,1.7f),  bump=false, bumpScale=0f,    valueFn=(u,v)=>Fabric(u,v,17,20) },
            new Job { matPath = INT+"Int_Cocina.mat",   size=256, lo=0.75f, hi=1.05f, tileScale=new Vector2(3f,1f),      bump=false, bumpScale=0f,    valueFn=(u,v)=>Marble(u,v,18) },
            new Job { matPath = INT+"Int_Bano.mat",     size=256, lo=0.80f, hi=1.05f, tileScale=new Vector2(5f,5f),      bump=true,  bumpScale=0.30f, valueFn=(u,v)=>GridGrout(u,v,19,6,6,0.06f) },
            new Job { matPath = INT+"Int_Bar.mat",      size=256, lo=0.50f, hi=0.95f, tileScale=new Vector2(2f,1f),      bump=false, bumpScale=0f,    valueFn=(u,v)=>WoodPlank(u,v,20,8) },
            new Job { matPath = INT+"Int_Acento.mat",   size=256, lo=0.80f, hi=1.05f, tileScale=new Vector2(2f,1f),      bump=false, bumpScale=0f,    valueFn=(u,v)=>Brushed(u,v,21) },
            new Job { matPath = INT+"Int_PlantaI.mat",  size=256, lo=0.65f, hi=1.05f, tileScale=new Vector2(1f,1f),      bump=false, bumpScale=0f,    valueFn=(u,v)=>Textura(u,v,22,5,30,0.5f) },
            new Job { matPath = INT+"Int_MacetaI.mat",  size=256, lo=0.75f, hi=1.05f, tileScale=new Vector2(1f,1f),      bump=false, bumpScale=0f,    valueFn=(u,v)=>Textura(u,v,23,6,40,0.45f) },
            new Job { matPath = INT+"Int_VerdeB.mat",   size=256, lo=0.80f, hi=1.02f, tileScale=new Vector2(5f,2.6f),    bump=false, bumpScale=0f,    valueFn=(u,v)=>Fabric(u,v,24,40) },
        };
    }
}
