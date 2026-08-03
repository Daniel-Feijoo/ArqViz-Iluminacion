using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// ArqViz / Configurar TV con Video
///
/// Le da a "TV_Pantalla" (creado por ArqVizInteriorBuilder, en Sala) un
/// material propio — antes compartía Int_Acento con lámparas, espejos,
/// dardos, etc., así que asignarle un video ahí lo hubiera repetido en
/// todos esos objetos. Importa el clip, arma un RenderTexture y lo
/// reproduce en loop vía VideoPlayer + AudioSource en el propio objeto.
///
/// No reconstruye Interior/Casa — actúa solo sobre lo que ya existe en la
/// escena abierta, para no pisar cambios pendientes.
///
/// Menú: ArqViz > Configurar TV con Video
/// </summary>
public static class ArqVizTV
{
    const string VIDEO_SRC     = @"C:\Users\Usuario\Downloads\Video clip tv sin señal.mp4";
    const string VIDEO_DIR     = "Assets/Videos/";
    const string VIDEO_REL     = VIDEO_DIR + "TV_SinSenal.mp4";
    const string RT_REL        = VIDEO_DIR + "RT_TVPantalla.renderTexture";
    const string MAT_PATH      = "Assets/Materials/Interior/Int_TVPantalla.mat";

    [MenuItem("ArqViz/Configurar TV con Video")]
    public static void Configurar()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

        var tv = GameObject.Find("TV_Pantalla");
        if (tv == null)
        {
            EditorUtility.DisplayDialog("ArqViz",
                "No se encontró 'TV_Pantalla' en la escena abierta.\n" +
                "Abre SampleScene.unity primero.", "OK");
            return;
        }

        if (!File.Exists(VIDEO_SRC))
        {
            EditorUtility.DisplayDialog("ArqViz", "No se encontró el video en:\n" + VIDEO_SRC, "OK");
            return;
        }

        // ── 1. Copiar el video al proyecto ────────────────────────────────
        Directory.CreateDirectory(Application.dataPath + "/Videos");
        string absDst = Application.dataPath + "/Videos/TV_SinSenal.mp4";
        File.Copy(VIDEO_SRC, absDst, true);
        AssetDatabase.ImportAsset(VIDEO_REL, ImportAssetOptions.ForceUpdate);
        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VIDEO_REL);
        if (clip == null)
        {
            Debug.LogError("[ArqVizTV] No se pudo importar el video como VideoClip.");
            return;
        }

        // ── 2. Material propio para la pantalla (deja de compartir Int_Acento) ─
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_PATH);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Int_TVPantalla" };
            AssetDatabase.CreateAsset(mat, MAT_PATH);
        }
        mat.SetColor("_BaseColor", Color.black);
        mat.SetFloat("_Metallic", 0.1f);
        mat.SetFloat("_Smoothness", 0.55f);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetColor("_EmissionColor", Color.white);

        // ── 3. RenderTexture donde el VideoPlayer dibuja cada cuadro ──────
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RT_REL);
        if (rt == null)
        {
            rt = new RenderTexture(640, 360, 0) { name = "RT_TVPantalla" };
            AssetDatabase.CreateAsset(rt, RT_REL);
        }
        mat.SetTexture("_EmissionMap", rt);
        EditorUtility.SetDirty(mat);

        tv.GetComponent<Renderer>().sharedMaterial = mat;

        // ── 4. VideoPlayer + AudioSource en el propio TV_Pantalla ─────────
        var vp = tv.GetComponent<VideoPlayer>();
        if (vp == null) vp = tv.AddComponent<VideoPlayer>();
        vp.source        = VideoSource.VideoClip;
        vp.clip          = clip;
        vp.renderMode    = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;
        vp.isLooping     = true;
        vp.playOnAwake   = true;
        vp.waitForFirstFrame = true;

        var audio = tv.GetComponent<AudioSource>();
        if (audio == null) audio = tv.AddComponent<AudioSource>();
        audio.playOnAwake  = false; // el VideoPlayer controla la reproducción
        audio.spatialBlend = 1f;    // sonido 3D posicional — es una TV en la sala
        audio.volume       = 0.4f;
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.SetTargetAudioSource(0, audio);

        EditorUtility.SetDirty(tv);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[ArqVizTV] ✓ TV_Pantalla: material propio + VideoPlayer en loop + audio 3D.");
        EditorUtility.DisplayDialog("ArqViz — TV ✓",
            "TV_Pantalla ahora tiene su propio material (ya no comparte Int_Acento) " +
            "y reproduce el video en loop con sonido posicional.\n\n" +
            "Se guardó la escena automáticamente.", "OK");
    }
}
