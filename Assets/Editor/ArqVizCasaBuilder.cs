using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Construye una casa moderna minimalista con materiales URP.
/// Menú: ArqViz > Construir Casa Moderna
/// </summary>
public static class ArqVizCasaBuilder
{
    const string ROOT_NAME = "Casa_Moderna_ArqViz";
    const string MAT_DIR   = "Assets/Materials/Casa/";

    // ─── Paleta ─────────────────────────────────────────────────────────────
    static readonly Color C_PARED    = new Color(0.94f, 0.92f, 0.87f);        // crema blanco
    static readonly Color C_TECHO    = new Color(0.18f, 0.18f, 0.18f);        // gris carbón
    static readonly Color C_VIDRIO   = new Color(0.55f, 0.76f, 0.90f, 0.30f); // azul glass
    static readonly Color C_PUERTA   = new Color(0.20f, 0.14f, 0.09f);        // madera oscura
    static readonly Color C_CONCRETO = new Color(0.68f, 0.67f, 0.64f);        // concreto
    static readonly Color C_CESPED   = new Color(0.18f, 0.40f, 0.16f);        // pasto
    static readonly Color C_SENDERO  = new Color(0.80f, 0.78f, 0.74f);        // baldosa
    static readonly Color C_TRONCO   = new Color(0.33f, 0.22f, 0.11f);        // tronco
    static readonly Color C_COPA     = new Color(0.14f, 0.36f, 0.14f);        // follaje
    static readonly Color C_PILAR    = new Color(0.90f, 0.89f, 0.86f);        // mármol claro
    static readonly Color C_MARCO    = new Color(0.25f, 0.25f, 0.25f);        // marco ventana

    static Material mPared, mTecho, mVidrio, mPuerta, mConcreto, mCesped,
                    mSendero, mTronco, mCopa, mPilar, mMarco;
    static int treeIdx;

    [MenuItem("ArqViz/Construir Casa Moderna")]
    public static void Construir()
    {
        // Limpiar instancia anterior
        var old = GameObject.Find(ROOT_NAME);
        if (old != null) Object.DestroyImmediate(old);

        treeIdx = 0;
        CrearMateriales();

        var root = new GameObject(ROOT_NAME);

        ConstruirTerreno(root.transform);
        ConstruirCuerpoMayor(root.transform);
        ConstruirAlaLateral(root.transform);
        ConstruirTechos(root.transform);
        ConstruirEntrada(root.transform);
        ConstruirJardin(root.transform);
        ReposicionarLuces();
        ReposicionarCamara();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[ArqViz] Casa moderna construida. Ctrl+S para guardar.");
        EditorUtility.DisplayDialog("ArqViz", "¡Casa moderna lista!\nAjusta la vista y presiona Ctrl+S.", "OK");
    }

    // ══════════════════════════════════════════════════════════════════════
    // MATERIALES
    // ══════════════════════════════════════════════════════════════════════
    static void CrearMateriales()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Casa"))
            AssetDatabase.CreateFolder("Assets/Materials", "Casa");

        mPared    = Mat("MatPared",    C_PARED,    0.00f, 0.30f, doubleSided: true);  // doble cara: visible desde dentro
        mTecho    = Mat("MatTecho",    C_TECHO,    0.00f, 0.60f, doubleSided: true);
        mVidrio   = Mat("MatVidrio",   C_VIDRIO,   0.05f, 0.95f, transp: true);
        mPuerta   = Mat("MatPuerta",   C_PUERTA,   0.00f, 0.35f, doubleSided: true);
        mConcreto = Mat("MatConcreto", C_CONCRETO, 0.00f, 0.15f, doubleSided: true);
        mCesped   = Mat("MatCesped",   C_CESPED,   0.00f, 0.05f);
        mSendero  = Mat("MatSendero",  C_SENDERO,  0.00f, 0.10f);
        mTronco   = Mat("MatTronco",   C_TRONCO,   0.00f, 0.20f);
        mCopa     = Mat("MatCopa",     C_COPA,     0.00f, 0.05f);
        mPilar    = Mat("MatPilar",    C_PILAR,    0.00f, 0.55f);
        mMarco    = Mat("MatMarco",    C_MARCO,    0.20f, 0.70f);

        AssetDatabase.SaveAssets();
    }

    static Material Mat(string nombre, Color color, float metallic, float smooth,
                        bool transp = false, bool doubleSided = false)
    {
        string path = MAT_DIR + nombre + ".mat";
        Material m  = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = nombre };
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Metallic",  metallic);
        m.SetFloat("_Smoothness", smooth);
        if (transp)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend",   0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
        }
        // _Cull: 0=Off(doble cara), 2=Back(normal, solo exterior)
        m.SetFloat("_Cull", doubleSided ? 0f : 2f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // ══════════════════════════════════════════════════════════════════════
    // TERRENO
    // ══════════════════════════════════════════════════════════════════════
    static void ConstruirTerreno(Transform p)
    {
        // Pasto bajado para no traspasar el piso de la casa (top en y=-0.25)
        Cubo(p, "Suelo_Cesped",    new Vector3(3f,  -0.50f, 10f),  new Vector3(70f, 0.5f, 60f),  mCesped);
        // Concreto frontal (top en y=-0.20, bien bajo el umbral de la puerta)
        Cubo(p, "Suelo_Frente",    new Vector3(0f,  -0.35f, -4f),  new Vector3(20f, 0.3f,  8f),  mConcreto);
        // Concreto lateral derecho
        Cubo(p, "Suelo_Lateral_D", new Vector3(10f, -0.35f,  4f),  new Vector3(8f,  0.3f, 12f),  mConcreto);
        // Cimiento visible debajo de la casa (rellena el hueco entre suelo y pared)
        Cubo(p, "Cimiento",        new Vector3(0f,  -0.20f,  4f),  new Vector3(10.6f, 0.3f, 8.6f), mConcreto);
    }

    // ══════════════════════════════════════════════════════════════════════
    // CUERPO PRINCIPAL  (10 × 4 × 8 m, frente en Z=0)
    // ══════════════════════════════════════════════════════════════════════
    static void ConstruirCuerpoMayor(Transform p)
    {
        float W=10f, H=4f, D=8f;

        // ── Casco hueco: paredes individuales (doble cara → visible desde dentro) ──
        Cubo(p, "Pared_Izquierda", new Vector3(-W/2f,      H/2f, D/2f), new Vector3(0.30f, H,    D   ), mPared);
        Cubo(p, "Pared_Derecha",   new Vector3( W/2f,      H/2f, D/2f), new Vector3(0.30f, H,    D   ), mPared);
        Cubo(p, "Pared_Trasera",   new Vector3( 0f,        H/2f, D   ), new Vector3(W,     H,    0.30f), mPared);
        // Frente sólido: paneles izq/der de las ventanas y banda superior
        Cubo(p, "Pared_F_Izq",    new Vector3(-4.60f,     H/2f, 0f  ), new Vector3(0.80f, H,    0.30f), mPared);
        Cubo(p, "Pared_F_Der",    new Vector3( 4.60f,     H/2f, 0f  ), new Vector3(0.80f, H,    0.30f), mPared);
        Cubo(p, "Pared_F_Top",    new Vector3( 0f,        3.80f, 0f  ), new Vector3(W,    0.40f, 0.30f), mPared);
        // Bandas bajas bajo ventanas (entre zócalo y vidrio)
        Cubo(p, "Pared_F_BajL",   new Vector3(-2.80f,     0.55f, 0f  ), new Vector3(2.80f, 0.70f, 0.30f), mPared);
        Cubo(p, "Pared_F_BajR",   new Vector3( 2.80f,     0.55f, 0f  ), new Vector3(2.80f, 0.70f, 0.30f), mPared);
        // Tiras verticales entre ventanas y puerta (las 2 paredes faltantes)
        Cubo(p, "Pared_F_EntL",   new Vector3(-1.075f,    H/2f,  0f  ), new Vector3(0.65f, H,    0.30f), mPared);
        Cubo(p, "Pared_F_EntR",   new Vector3( 1.075f,    H/2f,  0f  ), new Vector3(0.65f, H,    0.30f), mPared);
        // Techo interior visible desde dentro
        Cubo(p, "Techo_Interior",  new Vector3( 0f,        H-0.02f, D/2f), new Vector3(W-0.30f, 0.05f, D-0.30f), mPared);

        // ── Fachada frontal (Z=0): ventanas + puerta ──────────────────────

        // Ventana grande izquierda  (marco + vidrio)
        Cubo(p, "Marco_V_Izq",   new Vector3(-2.8f, 2.2f, 0f),  new Vector3(2.8f, 2.8f, 0.25f), mMarco);
        Cubo(p, "Vidrio_Izq",    new Vector3(-2.8f, 2.2f, 0f),  new Vector3(2.5f, 2.5f, 0.30f), mVidrio);

        // Ventana grande derecha
        Cubo(p, "Marco_V_Der",   new Vector3( 2.8f, 2.2f, 0f),  new Vector3(2.8f, 2.8f, 0.25f), mMarco);
        Cubo(p, "Vidrio_Der",    new Vector3( 2.8f, 2.2f, 0f),  new Vector3(2.5f, 2.5f, 0.30f), mVidrio);

        // Puerta principal
        Cubo(p, "Marco_Puerta",  new Vector3(0f, 1.2f, 0f),     new Vector3(1.5f, 2.6f, 0.25f), mMarco);
        Cubo(p, "Puerta_Hoja",   new Vector3(0f, 1.2f, 0f),     new Vector3(1.2f, 2.3f, 0.30f), mPuerta);

        // Viga decorativa frontal baja (zócalo)
        Cubo(p, "Zocalo_Frente", new Vector3(0f, 0.15f, 0f),    new Vector3(W, 0.30f, 0.28f), mConcreto);

        // ── Pared trasera: ventana estrecha alta ──────────────────────────
        Cubo(p, "Vidrio_Trasero", new Vector3(2f, 2.5f, D),      new Vector3(1.2f, 2f, 0.28f), mVidrio);

        // ── Pared izquierda: ventana corrida ──────────────────────────────
        Cubo(p, "Marco_V_Lat_Izq", new Vector3(-W/2f, 2.5f, 3f), new Vector3(0.28f, 1.6f, 3.5f), mMarco);
        Cubo(p, "Vidrio_Lat_Izq",  new Vector3(-W/2f, 2.5f, 3f), new Vector3(0.32f, 1.3f, 3.2f), mVidrio);

        // ── Pared derecha: dos ventanas  ─────────────────────────────────
        Cubo(p, "Vidrio_Lat_D1", new Vector3(W/2f, 2.4f, 2f),   new Vector3(0.32f, 1.4f, 1.8f), mVidrio);
        Cubo(p, "Vidrio_Lat_D2", new Vector3(W/2f, 2.4f, 6f),   new Vector3(0.32f, 1.4f, 1.8f), mVidrio);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ALA LATERAL DERECHA  (6 × 3 × 7 m, más baja)
    // ══════════════════════════════════════════════════════════════════════
    static void ConstruirAlaLateral(Transform p)
    {
        float W=6f, H=3f, D=7f;
        float cx = 8f; // centro X
        float cz = D/2f;

        // Ala hueca: paredes individuales doble cara
        Cubo(p, "Ala_PIzq",      new Vector3(cx-W/2f,  H/2f,  cz),  new Vector3(0.30f, H,    D    ), mPared);
        Cubo(p, "Ala_PDer",      new Vector3(cx+W/2f,  H/2f,  cz),  new Vector3(0.30f, H,    D    ), mPared);
        Cubo(p, "Ala_PTras",     new Vector3(cx,        H/2f,  D  ), new Vector3(W,     H,    0.30f), mPared);
        Cubo(p, "Ala_Techo_Int", new Vector3(cx,     H-0.02f,  cz), new Vector3(W-0.30f, 0.05f, D-0.30f), mPared);
        // Pared frontal del ala: paneles sólidos flanqueando la ventana
        Cubo(p, "Ala_PFrente_L", new Vector3(5.625f,   H/2f,  0f),  new Vector3(1.25f, H,    0.30f), mPared);
        Cubo(p, "Ala_PFrente_R", new Vector3(10.375f,  H/2f,  0f),  new Vector3(1.25f, H,    0.30f), mPared);
        Cubo(p, "Ala_PFrente_T", new Vector3(cx,       H-0.20f, 0f), new Vector3(W,    0.40f, 0.30f), mPared);

        // Ventana frontal ala
        Cubo(p, "Ala_Marco_F", new Vector3(cx, 1.8f, 0f),   new Vector3(3.5f, 2.2f, 0.25f), mMarco);
        Cubo(p, "Ala_Vidrio_F", new Vector3(cx, 1.8f, 0f),  new Vector3(3.2f, 1.9f, 0.30f), mVidrio);

        // Ventana lateral derecha ala
        Cubo(p, "Ala_Vidrio_R", new Vector3(cx+W/2f, 1.8f, 2.5f), new Vector3(0.30f, 1.5f, 2.8f), mVidrio);

        // Zócalo ala
        Cubo(p, "Ala_Zocalo", new Vector3(cx, 0.15f, 0f), new Vector3(W, 0.30f, 0.28f), mConcreto);
    }

    // ══════════════════════════════════════════════════════════════════════
    // TECHOS
    // ══════════════════════════════════════════════════════════════════════
    static void ConstruirTechos(Transform p)
    {
        // Techo principal con voladizo generoso
        Cubo(p, "Techo_Principal",
            new Vector3(0f, 4.22f, 4f),
            new Vector3(11.5f, 0.45f, 9.5f), mTecho);

        // Fascia frontal del voladizo (detalle visual)
        Cubo(p, "Techo_Fascia_F",
            new Vector3(0f, 3.98f, -0.45f),
            new Vector3(11.5f, 0.18f, 0.6f), mMarco);

        // Techo del ala (más bajo, sin voladizo lateral)
        Cubo(p, "Techo_Ala",
            new Vector3(8f, 3.17f, 3.5f),
            new Vector3(7f, 0.35f, 8f), mTecho);

        // Detalle: parapeto (muro perimetral del techo plano)
        Cubo(p, "Parapeto_Izq",  new Vector3(-5.5f, 4.6f, 4f),  new Vector3(0.25f, 0.75f, 9f), mPared);
        Cubo(p, "Parapeto_Tras", new Vector3(0f, 4.6f, 8.7f),   new Vector3(11f, 0.75f, 0.25f), mPared);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ENTRADA
    // ══════════════════════════════════════════════════════════════════════
    static void ConstruirEntrada(Transform p)
    {
        // Plataforma de entrada
        Cubo(p, "Plataforma", new Vector3(0f, 0.03f, -0.6f), new Vector3(5f, 0.06f, 1.2f), mConcreto);

        // Escalones (bajan hacia el sendero)
        Cubo(p, "Escalon_1", new Vector3(0f, 0.18f, -1.2f), new Vector3(4f,  0.35f, 0.7f), mConcreto);
        Cubo(p, "Escalon_2", new Vector3(0f, 0.08f, -1.8f), new Vector3(4f,  0.15f, 0.55f), mConcreto);

        // Pilares esbeltos bajo el voladizo del techo
        Cilindro(p, "Pilar_L", new Vector3(-2.2f, 2.1f, -0.4f), new Vector3(0.22f, 4.45f, 0.22f), mPilar);
        Cilindro(p, "Pilar_R", new Vector3( 2.2f, 2.1f, -0.4f), new Vector3(0.22f, 4.45f, 0.22f), mPilar);

        // Sendero de acceso
        Cubo(p, "Sendero_Principal", new Vector3(0f, -0.03f, -5.5f), new Vector3(2.8f, 0.07f, 8f), mSendero);

        // Macetas de entrada
        Cubo(p, "Maceta_L_Base", new Vector3(-3.3f, 0.25f, -1f), new Vector3(0.7f, 0.5f, 0.7f), mConcreto);
        Cubo(p, "Maceta_R_Base", new Vector3( 3.3f, 0.25f, -1f), new Vector3(0.7f, 0.5f, 0.7f), mConcreto);
        Esfera(p, "Maceta_L_Planta", new Vector3(-3.3f, 0.90f, -1f), new Vector3(0.9f, 0.9f, 0.9f), mCopa);
        Esfera(p, "Maceta_R_Planta", new Vector3( 3.3f, 0.90f, -1f), new Vector3(0.9f, 0.9f, 0.9f), mCopa);
    }

    // ══════════════════════════════════════════════════════════════════════
    // JARDÍN
    // ══════════════════════════════════════════════════════════════════════
    static void ConstruirJardin(Transform p)
    {
        // Árboles alrededor
        Arbol(p, new Vector3(-9f, 0f, 1f),  3.8f, 2.2f);
        Arbol(p, new Vector3(-8f, 0f, 7f),  4.5f, 2.6f);
        Arbol(p, new Vector3(-6f, 0f, 12f), 3.5f, 2.0f);
        Arbol(p, new Vector3(15f, 0f, 0f),  4.0f, 2.3f);
        Arbol(p, new Vector3(15f, 0f, 6f),  5.0f, 2.8f);
        Arbol(p, new Vector3(4f,  0f, 14f), 3.2f, 1.9f);
        Arbol(p, new Vector3(-2f, 0f, 13f), 4.2f, 2.4f);

        // Setos recortados (izquierda)
        Cubo(p, "Seto_1", new Vector3(-7f, 0.55f, 0f),  new Vector3(0.9f, 1.1f, 1.8f), mCopa);
        Cubo(p, "Seto_2", new Vector3(-7f, 0.55f, 2.5f), new Vector3(0.9f, 1.1f, 1.8f), mCopa);
        Cubo(p, "Seto_3", new Vector3(-7f, 0.55f, 5f),   new Vector3(0.9f, 1.1f, 1.8f), mCopa);

        // Seto lateral derecho
        Cubo(p, "Seto_D1", new Vector3(13f, 0.4f, 2f),  new Vector3(0.7f, 0.8f, 2f), mCopa);
        Cubo(p, "Seto_D2", new Vector3(13f, 0.4f, 5f),  new Vector3(0.7f, 0.8f, 2f), mCopa);
    }

    static void Arbol(Transform p, Vector3 base_, float altTronco, float radCopa)
    {
        treeIdx++;
        string id = "Arbol_" + treeIdx;
        Cilindro(p, id + "_Tronco",
            base_ + new Vector3(0f, altTronco / 2f, 0f),
            new Vector3(0.25f, altTronco, 0.25f), mTronco);
        Esfera(p, id + "_Copa",
            base_ + new Vector3(0f, altTronco + radCopa * 0.35f, 0f),
            new Vector3(radCopa, radCopa * 1.25f, radCopa), mCopa);
    }

    // ══════════════════════════════════════════════════════════════════════
    // LUCES — reposiciona las existentes para enmarcar la casa
    // ══════════════════════════════════════════════════════════════════════
    static void ReposicionarLuces()
    {
        // Fill Light: sube y centra sobre la casa
        var fill = GameObject.Find("Fill Light — Cielo Ambiente");
        if (fill != null)
        {
            fill.transform.position = new Vector3(3f, 12f, 5f);
            var l = fill.GetComponent<Light>();
            if (l != null) { l.range = 50f; l.intensity = 1.8f; }
            EditorUtility.SetDirty(fill);
        }

        // Spot: apunta a la fachada desde el frente-izquierda
        var spot = GameObject.Find("Spot Light — Acento Arquitectónico");
        if (spot != null)
        {
            spot.transform.position = new Vector3(-8f, 7f, -6f);
            spot.transform.LookAt(new Vector3(0f, 2f, 0f));
            var l = spot.GetComponent<Light>();
            if (l != null) { l.range = 30f; l.intensity = 25f; l.spotAngle = 40f; }
            EditorUtility.SetDirty(spot);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // CÁMARA — posición de render arquitectónico clásico (45° frontal)
    // ══════════════════════════════════════════════════════════════════════
    static void ReposicionarCamara()
    {
        // Main Camera del juego
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(-14f, 7f, -9f);
            cam.transform.LookAt(new Vector3(3f, 1.8f, 4f));
            EditorUtility.SetDirty(cam.gameObject);
        }

        // Scene View — para ver la casa al abrir Unity
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null)
            sv.LookAt(new Vector3(3f, 2.5f, 4f), Quaternion.Euler(22f, 215f, 0f), 22f);
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════
    static GameObject Cubo(Transform p, string n, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = n;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        return go;
    }

    static GameObject Cilindro(Transform p, string n, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = n;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        return go;
    }

    static GameObject Esfera(Transform p, string n, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = n;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        return go;
    }
}
