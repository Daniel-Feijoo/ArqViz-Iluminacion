using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ArqViz / Agregar UI y Game Manager
/// Coloca en escena:
///   - GameManager (GameObject vacío con script)
///   - Canvas WorldSpace con panel de estado (UI en world space)
///   - Cartel 3D de bienvenida en la entrada
///   - Carteles de nombre sobre puertas de habitaciones
/// </summary>
public class ArqVizUIBuilder
{
    const string ROOT_GM  = "GameManager_ArqViz";
    const string ROOT_UI  = "UI_WorldSpace_ArqViz";
    const string ROOT_CAR = "Carteles_ArqViz";

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("ArqViz/Agregar UI y Game Manager")]
    static void Build()
    {
        // Limpiar raíces previas
        foreach (var n in new[] { ROOT_GM, ROOT_UI, ROOT_CAR })
        {
            var old = GameObject.Find(n);
            if (old != null) { Undo.DestroyObjectImmediate(old); }
        }

        CrearGameManager();
        var canvas = CrearCanvasWorldSpace();
        CrearCartelBienvenida();
        CrearCartelesSalas();

        // Conectar GameManager → CartelWorldSpace
        var gm  = GameObject.Find(ROOT_GM)?.GetComponent<GameManager>();
        var cws = canvas?.GetComponent<CartelWorldSpace>();
        if (gm != null && cws != null)
        {
            var so = new SerializedObject(gm);
            so.FindProperty("cartelEstado").objectReferenceValue = cws;
            so.ApplyModifiedProperties();
        }

        // Conectar ZonaTeleporte A → onTeleport → GameManager.RegistrarZonaVisitada
        ConectarTeleporteAGameManager("ZonaTeleporte_A", gm);
        ConectarTeleporteAGameManager("ZonaTeleporte_B", gm);

        // Conectar ObjetoFisico → GameManager.RegistrarObjetoFisico (si existe)
        var of = GameObject.Find("Mecanica_Fisica")?.GetComponentInChildren<ObjetoFisico>();
        if (of != null && gm != null)
            ConectarObjetoFisicoAGameManager(of, gm);

        Debug.Log("[ArqVizUIBuilder] UI y GameManager creados correctamente.");
        EditorUtility.DisplayDialog("ArqViz", "UI World Space y Game Manager listos.\n\nRecuerda:\n• Conecta las acciones extra del UnityEvent desde el Inspector.\n• Ejecuta primero 'Construir Casa Grande' y 'Construir Interior Grande'.", "OK");
    }

    // ── Game Manager ─────────────────────────────────────────────────────────
    static void CrearGameManager()
    {
        var go = new GameObject(ROOT_GM);
        Undo.RegisterCreatedObjectUndo(go, "Crear GameManager");
        go.AddComponent<GameManager>();
    }

    // ── Canvas World Space ────────────────────────────────────────────────────
    static GameObject CrearCanvasWorldSpace()
    {
        var root = new GameObject(ROOT_UI);
        Undo.RegisterCreatedObjectUndo(root, "Crear UI WorldSpace");

        // Posición: pared izquierda de la sala (X=-10), visible al caminar por la sala.
        // Rotación (0,90,0): canvas local -Z apunta hacia -X (hacia el interior), texto correcto.
        root.transform.position = new Vector3(-9.6f, 2.1f, 3.5f);
        root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        root.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);

        // Canvas
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400f, 260f);

        // Fondo
        var fondo = CrearUIRect("Fondo", root.transform, new Vector2(400, 260), Vector2.zero);
        var imgFondo = fondo.AddComponent<Image>();
        imgFondo.color = new Color(0.06f, 0.06f, 0.08f, 0.88f);

        // Borde izquierdo acento dorado
        var borde = CrearUIRect("Borde", root.transform, new Vector2(6, 260), new Vector2(-197f, 0f));
        var imgBorde = borde.AddComponent<Image>();
        imgBorde.color = new Color(0.9f, 0.75f, 0.3f, 1f);

        // Título
        var goTit = CrearUIRect("Titulo", root.transform, new Vector2(370, 36), new Vector2(15f, 100f));
        var txtTit = goTit.AddComponent<Text>();
        txtTit.text      = "▪ Estado de la Visita";
        txtTit.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtTit.fontSize  = 22;
        txtTit.fontStyle = FontStyle.Bold;
        txtTit.color     = new Color(0.9f, 0.75f, 0.3f, 1f);
        txtTit.alignment = TextAnchor.MiddleLeft;

        // Separador
        var sep = CrearUIRect("Separador", root.transform, new Vector2(360, 2), new Vector2(10f, 76f));
        sep.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Zonas
        var txtZ = CrearTexto("TxtZonas",  root.transform, "Zonas exploradas: 0",  new Vector2(15f, 42f),  16);
        var txtO = CrearTexto("TxtObjetos",root.transform, "Objetos activados: 0", new Vector2(15f, 14f),  16);
        var txtT = CrearTexto("TxtTiempo", root.transform, "Tiempo: 00:00",         new Vector2(15f, -14f), 16);

        // Panel logro (oculto al inicio)
        var goLogro = CrearUIRect("PanelLogro", root.transform, new Vector2(380, 48), new Vector2(10f, -80f));
        var imgLogro = goLogro.AddComponent<Image>();
        imgLogro.color = new Color(0.1f, 0.45f, 0.1f, 0.9f);
        goLogro.SetActive(false);

        var txtLogroGo = CrearUIRect("TxtLogro", goLogro.transform, new Vector2(360, 44), Vector2.zero);
        var txtLogro   = txtLogroGo.AddComponent<Text>();
        txtLogro.text      = "✓  ¡Exploración completa!";
        txtLogro.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtLogro.fontSize  = 20;
        txtLogro.fontStyle = FontStyle.Bold;
        txtLogro.color     = new Color(0.6f, 1f, 0.6f, 1f);
        txtLogro.alignment = TextAnchor.MiddleCenter;

        // Conectar CartelWorldSpace
        var cws = root.AddComponent<CartelWorldSpace>();
        var so = new SerializedObject(cws);
        so.FindProperty("txtZonas").objectReferenceValue   = txtZ;
        so.FindProperty("txtObjetos").objectReferenceValue = txtO;
        so.FindProperty("txtTiempo").objectReferenceValue  = txtT;
        so.FindProperty("txtLogro").objectReferenceValue   = txtLogro;
        so.FindProperty("fondoLogro").objectReferenceValue = imgLogro;
        so.ApplyModifiedProperties();

        return root;
    }

    // ── Cartel 3D de bienvenida ───────────────────────────────────────────────
    static void CrearCartelBienvenida()
    {
        var root = new GameObject(ROOT_CAR);
        Undo.RegisterCreatedObjectUndo(root, "Crear Carteles");

        // Poste izquierdo
        Poste(root.transform, new Vector3(-1.2f, 0f, -4.8f));
        // Poste derecho
        Poste(root.transform, new Vector3( 1.2f, 0f, -4.8f));

        // Tablón horizontal
        var tablon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tablon.name = "Cartel_Tablon";
        tablon.transform.SetParent(root.transform);
        tablon.transform.localPosition = new Vector3(0f, 2.6f, -4.8f);
        tablon.transform.localScale    = new Vector3(3f, 0.55f, 0.12f);
        SetColor(tablon, new Color(0.25f, 0.15f, 0.05f));

        // Canvas sobre el tablón
        var canvasGo = new GameObject("Cartel_Canvas");
        canvasGo.transform.SetParent(root.transform);
        canvasGo.transform.position   = new Vector3(0f, 2.62f, -4.73f);
        canvasGo.transform.rotation   = Quaternion.identity;
        canvasGo.transform.localScale = new Vector3(0.006f, 0.006f, 0.006f);

        var cv = canvasGo.AddComponent<Canvas>();
        cv.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();

        var rtCv = canvasGo.GetComponent<RectTransform>();
        rtCv.sizeDelta = new Vector2(480f, 80f);

        var txtGo = CrearUIRect("TxtBienvenida", canvasGo.transform, new Vector2(460, 70), Vector2.zero);
        var txt   = txtGo.AddComponent<Text>();
        txt.text      = "Casa Moderna ArqViz";
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = 36;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = new Color(0.95f, 0.85f, 0.5f, 1f);
        txt.alignment = TextAnchor.MiddleCenter;
    }

    // ── Carteles de salas sobre puertas ──────────────────────────────────────
    static void CrearCartelesSalas()
    {
        var root = GameObject.Find(ROOT_CAR)?.transform
                   ?? new GameObject(ROOT_CAR).transform;

        // (nombre, posición, rotación Y)
        // Regla: Canvas local -Z apunta hacia el jugador.
        //   rotY=0   → front hacia -Z (jugador viene de Z+)
        //   rotY=180 → front hacia +Z (jugador viene de Z-)  ← ESPEJA texto, NO usar
        //   rotY=90  → front hacia -X (jugador viene de X+)
        //   rotY=270 → front hacia +X (jugador viene de X-)
        //
        // Desplazados 0.5 u desde la pared para evitar clipping.
        var salas = new (string nombre, Vector3 pos, float rotY)[]
        {
            // Pared Z=0 interior, cartel visible desde la sala mirando hacia la entrada
            ("Sala Principal",  new Vector3( 0f,  2.6f,  0.6f),   0f),
            // Pared Z=8, carteles visibles desde la sala (jugador viene de Z-)
            // rotY=0 → front hacia -Z (hacia sala, donde está el jugador)
            ("Dormitorio",      new Vector3(-6f,  2.6f,  7.5f),   0f),
            ("Bano",            new Vector3( 0f,  2.6f,  7.5f),   0f),
            ("Estudio",         new Vector3( 6f,  2.6f,  7.5f),   0f),
            // Apertura hacia sala de juegos, pared X=10, jugador viene de X-
            // rotY=270 → front hacia +X... wait: local -Z → world +X cuando rotY=270
            // Mejor: rotY=90 → local -Z → world -X (hacia sala principal, jugador en X<10)
            ("Sala de Juegos",  new Vector3( 9.5f, 2.6f, 2.5f),  90f),
        };

        foreach (var s in salas)
        {
            var go = new GameObject($"Cartel_{s.nombre.Replace(" ", "_")}");
            go.transform.SetParent(root);
            go.transform.position = s.pos;
            go.transform.rotation = Quaternion.Euler(0f, s.rotY, 0f);
            go.transform.localScale = new Vector3(0.007f, 0.007f, 0.007f);

            var cv = go.AddComponent<Canvas>();
            cv.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 60f);

            // Fondo semitransparente con borde
            var fondo = CrearUIRect("Fondo", go.transform, new Vector2(300, 60), Vector2.zero);
            fondo.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.82f);

            // Borde inferior acento
            var borde = CrearUIRect("Borde", go.transform, new Vector2(300, 4), new Vector2(0, -28));
            borde.AddComponent<Image>().color = new Color(0.9f, 0.75f, 0.3f, 1f);

            var txtGo = CrearUIRect("Texto", go.transform, new Vector2(280, 52), Vector2.zero);
            var txt   = txtGo.AddComponent<Text>();
            txt.text      = s.nombre;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 26;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
        }
    }

    // ── Conexión UnityEvents ──────────────────────────────────────────────────
    static void ConectarTeleporteAGameManager(string nombreGO, GameManager gm)
    {
        if (gm == null) return;
        var go = GameObject.Find(nombreGO);
        if (go == null) return;

        var zt = go.GetComponent<ZonaTeleporte>();
        if (zt == null) return;

        var so = new SerializedObject(zt);
        var evProp = so.FindProperty("onTeleport");
        if (evProp == null) return;

        // Añadir listener persistente
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            zt.onTeleport,
            gm.RegistrarZonaVisitada
        );
        EditorUtility.SetDirty(zt);
    }

    static void ConectarObjetoFisicoAGameManager(ObjetoFisico of, GameManager gm)
    {
        // ObjetoFisico no tiene UnityEvent propio — el GameManager.RegistrarObjetoFisico
        // se puede conectar desde el Inspector al trigger del ObjetoFisico si se desea.
        // Aquí solo marcamos dirty para que quede serializado.
        EditorUtility.SetDirty(of);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static void Poste(Transform parent, Vector3 pos)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        p.name = "Poste";
        p.transform.SetParent(parent);
        p.transform.localPosition = pos;
        p.transform.localScale    = new Vector3(0.12f, 1.4f, 0.12f);
        SetColor(p, new Color(0.2f, 0.12f, 0.04f));
    }

    static void SetColor(GameObject go, Color color)
    {
        var mr = go.GetComponent<MeshRenderer>();
        if (mr == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mr.sharedMaterial = mat;
    }

    static GameObject CrearUIRect(string name, Transform parent, Vector2 size, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta       = size;
        rt.anchoredPosition = anchoredPos;
        return go;
    }

    static Text CrearTexto(string name, Transform parent, string contenido, Vector2 pos, int size)
    {
        var go  = CrearUIRect(name, parent, new Vector2(370, 28), pos);
        var txt = go.AddComponent<Text>();
        txt.text      = contenido;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = size;
        txt.color     = new Color(0.85f, 0.85f, 0.85f, 1f);
        txt.alignment = TextAnchor.MiddleLeft;
        return txt;
    }
}
