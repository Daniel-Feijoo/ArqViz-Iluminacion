using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Casa moderna ampliada: cuerpo 20x4x16m + ala sala de juegos 12x4x14m.
/// Menú: ArqViz > Construir Casa Grande
/// </summary>
public static class ArqVizCasaBuilder
{
    const string ROOT_NAME = "Casa_Moderna_ArqViz";
    const string MAT_DIR   = "Assets/Materials/Casa/";

    static readonly Color C_PARED    = new Color(0.94f, 0.92f, 0.87f);
    static readonly Color C_TECHO    = new Color(0.18f, 0.18f, 0.18f);
    static readonly Color C_VIDRIO   = new Color(0.55f, 0.76f, 0.90f, 0.30f);
    static readonly Color C_PUERTA   = new Color(0.20f, 0.14f, 0.09f);
    static readonly Color C_CONCRETO = new Color(0.68f, 0.67f, 0.64f);
    static readonly Color C_CESPED   = new Color(0.18f, 0.40f, 0.16f);
    static readonly Color C_SENDERO  = new Color(0.80f, 0.78f, 0.74f);
    static readonly Color C_TRONCO   = new Color(0.33f, 0.22f, 0.11f);
    static readonly Color C_COPA     = new Color(0.14f, 0.36f, 0.14f);
    static readonly Color C_PILAR    = new Color(0.90f, 0.89f, 0.86f);
    static readonly Color C_MARCO    = new Color(0.25f, 0.25f, 0.25f);

    static Material mPared, mTecho, mVidrio, mPuerta, mConcreto, mCesped,
                    mSendero, mTronco, mCopa, mPilar, mMarco;
    static int treeIdx;

    [MenuItem("ArqViz/Construir Casa Grande")]
    public static void Construir()
    {
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
        Debug.Log("[ArqViz] Casa grande construida. Ctrl+S para guardar.");
        EditorUtility.DisplayDialog("ArqViz",
            "¡Casa grande lista!\nEjecuta 'Construir Interior Grande' y luego Ctrl+S.", "OK");
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

        mPared    = Mat("MatPared",    C_PARED,    0.00f, 0.30f, doubleSided: true);
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
        m.SetFloat("_Cull", doubleSided ? 0f : 2f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // ══════════════════════════════════════════════════════════════════════
    // TERRENO
    // ══════════════════════════════════════════════════════════════════════
    static void ConstruirTerreno(Transform p)
    {
        Cubo(p, "Suelo_Cesped",    new Vector3(6f,  -0.50f,  8f),  new Vector3(120f, 0.5f, 80f),   mCesped);
        Cubo(p, "Suelo_Frente",    new Vector3(0f,  -0.35f, -4f),  new Vector3(35f,  0.3f,  8f),   mConcreto);
        Cubo(p, "Suelo_Lateral_D", new Vector3(16f, -0.35f,  7f),  new Vector3(14f,  0.3f, 16f),   mConcreto);
        Cubo(p, "Cimiento",        new Vector3(0f,  -0.20f,  8f),  new Vector3(20.6f,0.3f,16.6f),  mConcreto);
        Cubo(p, "Cimiento_Ala",    new Vector3(16f, -0.20f,  7f),  new Vector3(12.6f,0.3f,14.6f),  mConcreto);
    }

    // ══════════════════════════════════════════════════════════════════════
    // CUERPO PRINCIPAL  W=20, H=4, D=16
    // Pared derecha tiene gap Z[1,5] para conectar con ala (sala de juegos)
    // ══════════════════════════════════════════════════════════════════════
    static void ConstruirCuerpoMayor(Transform p)
    {
        float W=20f, H=4f, D=16f;

        // Paredes perimetrales
        Cubo(p, "Pared_Izquierda",  new Vector3(-W/2f, H/2f, D/2f),  new Vector3(0.30f, H, D),    mPared);
        // Pared derecha: gap Z[1,5] para paso al ala
        Cubo(p, "Pared_Derecha_F",  new Vector3(W/2f,  H/2f, 0.5f),  new Vector3(0.30f, H, 1f),   mPared);
        Cubo(p, "Pared_Derecha_T",  new Vector3(W/2f,  H/2f,10.5f),  new Vector3(0.30f, H,11f),   mPared);
        Cubo(p, "Pared_Trasera",    new Vector3(0f,    H/2f, D),      new Vector3(W,     H, 0.30f), mPared);

        // Fachada frontal — sólidos extremos
        Cubo(p, "Pared_F_Izq",  new Vector3(-8.5f,  H/2f,  0f), new Vector3(3.0f, H,    0.30f), mPared);
        Cubo(p, "Pared_F_Der",  new Vector3( 8.5f,  H/2f,  0f), new Vector3(3.0f, H,    0.30f), mPared);
        // Tiras verticales entre ventanas y puerta
        Cubo(p, "Pared_F_EntL", new Vector3(-1.10f, H/2f,  0f), new Vector3(0.70f,H,    0.30f), mPared);
        Cubo(p, "Pared_F_EntR", new Vector3( 1.10f, H/2f,  0f), new Vector3(0.70f,H,    0.30f), mPared);
        // Banda superior
        Cubo(p, "Pared_F_Top",  new Vector3(0f,  3.80f,    0f), new Vector3(W,    0.40f, 0.30f), mPared);
        // Alféizares bajo ventanas
        Cubo(p, "Pared_F_BajL", new Vector3(-4.25f, 0.45f, 0f), new Vector3(5.5f, 0.90f, 0.30f), mPared);
        Cubo(p, "Pared_F_BajR", new Vector3( 4.25f, 0.45f, 0f), new Vector3(5.5f, 0.90f, 0.30f), mPared);
        // Techo interior (doble cara, visible desde adentro)
        Cubo(p, "Techo_Interior", new Vector3(0f, H-0.02f, D/2f), new Vector3(W-0.30f,0.05f,D-0.30f), mPared);

        // Ventanas panorámicas fachada
        Cubo(p, "Marco_V_Izq",  new Vector3(-4.25f, 2.25f, 0f), new Vector3(5.5f, 2.70f, 0.25f), mMarco);
        Cubo(p, "Vidrio_Izq",   new Vector3(-4.25f, 2.20f, 0f), new Vector3(5.2f, 2.40f, 0.30f), mVidrio);
        Cubo(p, "Marco_V_Der",  new Vector3( 4.25f, 2.25f, 0f), new Vector3(5.5f, 2.70f, 0.25f), mMarco);
        Cubo(p, "Vidrio_Der",   new Vector3( 4.25f, 2.20f, 0f), new Vector3(5.2f, 2.40f, 0.30f), mVidrio);
        // Puerta principal
        Cubo(p, "Marco_Puerta", new Vector3(0f, 1.2f, 0f), new Vector3(1.5f, 2.6f, 0.25f), mMarco);
        Cubo(p, "Puerta_Hoja",  new Vector3(0f, 1.2f, 0f), new Vector3(1.2f, 2.3f, 0.30f), mPuerta);
        Cubo(p, "Zocalo_Frente", new Vector3(0f, 0.15f, 0f), new Vector3(W, 0.30f, 0.28f), mConcreto);

        // Ventanas pared trasera
        Cubo(p, "Vidrio_Trasero", new Vector3(-3f, 2.5f, D), new Vector3(3f, 2f, 0.28f), mVidrio);
        Cubo(p, "Vidrio_Trasero2",new Vector3( 4f, 2.5f, D), new Vector3(2f, 2f, 0.28f), mVidrio);
        // Ventana corrida pared izquierda
        Cubo(p, "Marco_V_Lat_Izq", new Vector3(-W/2f, 2.5f, 8f), new Vector3(0.28f,1.6f,10f), mMarco);
        Cubo(p, "Vidrio_Lat_Izq",  new Vector3(-W/2f, 2.5f, 8f), new Vector3(0.32f,1.3f, 9.7f), mVidrio);
        // Ventanas pared derecha (fuera del gap de conexión)
        Cubo(p, "Vidrio_Lat_D1",  new Vector3(W/2f, 2.4f, 7f),   new Vector3(0.32f,1.4f,1.5f), mVidrio);
        Cubo(p, "Vidrio_Lat_D2",  new Vector3(W/2f, 2.4f,12.5f), new Vector3(0.32f,1.4f,3.5f), mVidrio);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ALA LATERAL — SALA DE JUEGOS  W=12, H=4, D=14  (cx=16, cz=7)
    // Pared izquierda tiene gap Z[1,5] igual que la pared derecha del cuerpo
    // ══════════════════════════════════════════════════════════════════════
    static void ConstruirAlaLateral(Transform p)
    {
        float W=12f, H=4f, D=14f;
        float cx=16f, cz=D/2f; // cz=7

        // Pared izquierda con gap Z[1,5]
        Cubo(p, "Ala_PIzq_F",    new Vector3(cx-W/2f, H/2f, 0.5f),  new Vector3(0.30f,H, 1f), mPared);
        Cubo(p, "Ala_PIzq_T",    new Vector3(cx-W/2f, H/2f, 9.5f),  new Vector3(0.30f,H, 9f), mPared);
        Cubo(p, "Ala_PDer",      new Vector3(cx+W/2f, H/2f, cz),    new Vector3(0.30f,H,  D), mPared);
        Cubo(p, "Ala_PTras",     new Vector3(cx,       H/2f, D),     new Vector3(W,    H,0.30f), mPared);
        Cubo(p, "Ala_Techo_Int", new Vector3(cx,    H-0.02f, cz),   new Vector3(W-0.30f,0.05f,D-0.30f), mPared);

        // Fachada frontal ala
        Cubo(p, "Ala_PFrente_L", new Vector3(11.5f, H/2f,  0f),    new Vector3(3f,  H,    0.30f), mPared);
        Cubo(p, "Ala_PFrente_R", new Vector3(20.5f, H/2f,  0f),    new Vector3(3f,  H,    0.30f), mPared);
        Cubo(p, "Ala_PFrente_T", new Vector3(cx,  H-0.20f, 0f),    new Vector3(W,   0.40f,0.30f), mPared);
        Cubo(p, "Ala_PFrente_B", new Vector3(cx,    0.45f, 0f),    new Vector3(6f,  0.90f,0.30f), mPared);
        // Ventana panorámica ala frontal
        Cubo(p, "Ala_Marco_F",   new Vector3(cx,    2.20f, 0f),    new Vector3(6f,  2.5f, 0.25f), mMarco);
        Cubo(p, "Ala_Vidrio_F",  new Vector3(cx,    2.20f, 0f),    new Vector3(5.7f,2.2f, 0.30f), mVidrio);
        // Ventanas laterales ala
        Cubo(p, "Ala_Vidrio_R",  new Vector3(cx+W/2f,2.0f, 5f),   new Vector3(0.30f,1.8f,6f),   mVidrio);
        Cubo(p, "Ala_Vidrio_Tr", new Vector3(cx,    2.3f,  D),     new Vector3(5f,  1.8f,0.28f), mVidrio);
        Cubo(p, "Ala_Zocalo",    new Vector3(cx,    0.15f, 0f),    new Vector3(W,   0.30f,0.28f), mConcreto);
    }

    // ══════════════════════════════════════════════════════════════════════
    // TECHOS
    // ══════════════════════════════════════════════════════════════════════
    static void ConstruirTechos(Transform p)
    {
        Cubo(p, "Techo_Principal",  new Vector3(0f,  4.22f,  8f),   new Vector3(22f,  0.45f,17.5f), mTecho);
        Cubo(p, "Techo_Fascia_F",   new Vector3(0f,  3.98f, -0.45f),new Vector3(22f,  0.18f, 0.6f), mMarco);
        Cubo(p, "Techo_Ala",        new Vector3(16f, 4.22f,  7f),   new Vector3(13f,  0.45f,15f),   mTecho);
        Cubo(p, "Parapeto_Izq",     new Vector3(-10.5f,4.6f, 8f),   new Vector3(0.25f,0.75f,17f),   mPared);
        Cubo(p, "Parapeto_Tras",    new Vector3(0f,  4.6f,  16.8f), new Vector3(21f,  0.75f, 0.25f),mPared);
        Cubo(p, "Parapeto_Ala_D",   new Vector3(22.8f,4.6f, 7f),   new Vector3(0.25f,0.75f,15f),   mPared);
        Cubo(p, "Parapeto_Ala_T",   new Vector3(16f, 4.6f, 14.8f), new Vector3(13f,  0.75f, 0.25f),mPared);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ENTRADA
    // ══════════════════════════════════════════════════════════════════════
    static void ConstruirEntrada(Transform p)
    {
        Cubo(p, "Plataforma",        new Vector3(0f,   0.03f, -0.6f), new Vector3(7f,  0.06f, 1.2f), mConcreto);
        Cubo(p, "Escalon_1",         new Vector3(0f,   0.18f, -1.2f), new Vector3(6f,  0.35f, 0.7f), mConcreto);
        Cubo(p, "Escalon_2",         new Vector3(0f,   0.08f, -1.8f), new Vector3(6f,  0.15f,0.55f), mConcreto);
        Cilindro(p, "Pilar_L", new Vector3(-3f, 2.1f, -0.4f), new Vector3(0.22f, 4.45f, 0.22f), mPilar);
        Cilindro(p, "Pilar_R", new Vector3( 3f, 2.1f, -0.4f), new Vector3(0.22f, 4.45f, 0.22f), mPilar);
        Cubo(p, "Sendero_Principal", new Vector3(0f,  -0.03f, -5.5f), new Vector3(3.5f,0.07f,  8f), mSendero);
        Cubo(p, "Maceta_L_Base",     new Vector3(-5f,  0.25f,  -1f),  new Vector3(0.7f,0.5f, 0.7f), mConcreto);
        Cubo(p, "Maceta_R_Base",     new Vector3( 5f,  0.25f,  -1f),  new Vector3(0.7f,0.5f, 0.7f), mConcreto);
        Esfera(p, "Maceta_L_Planta", new Vector3(-5f,  0.90f,  -1f),  new Vector3(0.9f,0.9f, 0.9f), mCopa);
        Esfera(p, "Maceta_R_Planta", new Vector3( 5f,  0.90f,  -1f),  new Vector3(0.9f,0.9f, 0.9f), mCopa);
    }

    // ══════════════════════════════════════════════════════════════════════
    // JARDÍN
    // ══════════════════════════════════════════════════════════════════════
    static void ConstruirJardin(Transform p)
    {
        Arbol(p, new Vector3(-15f, 0f,  1f), 3.8f, 2.2f);
        Arbol(p, new Vector3(-14f, 0f,  9f), 4.5f, 2.6f);
        Arbol(p, new Vector3(-12f, 0f, 17f), 3.5f, 2.0f);
        Arbol(p, new Vector3( 27f, 0f,  0f), 4.0f, 2.3f);
        Arbol(p, new Vector3( 27f, 0f,  8f), 5.0f, 2.8f);
        Arbol(p, new Vector3( 11f, 0f, 19f), 3.2f, 1.9f);
        Arbol(p, new Vector3( -3f, 0f, 20f), 4.2f, 2.4f);
        Arbol(p, new Vector3( -7f, 0f, 21f), 3.6f, 2.1f);
        Arbol(p, new Vector3( 17f, 0f, 18f), 4.0f, 2.3f);

        Cubo(p, "Seto_1",  new Vector3(-13f,0.55f,  0f), new Vector3(0.9f,1.1f,1.8f), mCopa);
        Cubo(p, "Seto_2",  new Vector3(-13f,0.55f,  5f), new Vector3(0.9f,1.1f,1.8f), mCopa);
        Cubo(p, "Seto_3",  new Vector3(-13f,0.55f, 10f), new Vector3(0.9f,1.1f,1.8f), mCopa);
        Cubo(p, "Seto_4",  new Vector3(-13f,0.55f, 15f), new Vector3(0.9f,1.1f,1.8f), mCopa);
        Cubo(p, "Seto_D1", new Vector3( 25f, 0.4f,  2f), new Vector3(0.7f,0.8f,2.0f), mCopa);
        Cubo(p, "Seto_D2", new Vector3( 25f, 0.4f,  7f), new Vector3(0.7f,0.8f,2.0f), mCopa);
        Cubo(p, "Seto_D3", new Vector3( 25f, 0.4f, 12f), new Vector3(0.7f,0.8f,2.0f), mCopa);
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
    // LUCES
    // ══════════════════════════════════════════════════════════════════════
    static void ReposicionarLuces()
    {
        var fill = GameObject.Find("Fill Light — Cielo Ambiente");
        if (fill != null)
        {
            fill.transform.position = new Vector3(6f, 18f, 8f);
            var l = fill.GetComponent<Light>();
            if (l != null) { l.range = 90f; l.intensity = 1.8f; }
            EditorUtility.SetDirty(fill);
        }
        var spot = GameObject.Find("Spot Light — Acento Arquitectónico");
        if (spot != null)
        {
            spot.transform.position = new Vector3(-16f, 11f, -10f);
            spot.transform.LookAt(new Vector3(0f, 2f, 0f));
            var l = spot.GetComponent<Light>();
            if (l != null) { l.range = 45f; l.intensity = 30f; l.spotAngle = 40f; }
            EditorUtility.SetDirty(spot);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // CÁMARA
    // ══════════════════════════════════════════════════════════════════════
    static void ReposicionarCamara()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(-24f, 12f, -14f);
            cam.transform.LookAt(new Vector3(6f, 2f, 8f));
            EditorUtility.SetDirty(cam.gameObject);
        }
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null)
            sv.LookAt(new Vector3(6f, 3f, 8f), Quaternion.Euler(22f, 215f, 0f), 35f);
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
