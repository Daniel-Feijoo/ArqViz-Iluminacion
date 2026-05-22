using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Interior grande: sala abierta, cocina, dormitorio, baño, estudio + sala de juegos.
/// Requiere "ArqViz > Construir Casa Grande" primero.
/// Menú: ArqViz > Construir Interior Grande
/// </summary>
public static class ArqVizInteriorBuilder
{
    const string CASA_ROOT = "Casa_Moderna_ArqViz";
    const string MAT_DIR   = "Assets/Materials/Interior/";

    // ─── Paleta interior ────────────────────────────────────────────────
    static readonly Color C_PISO      = new Color(0.73f, 0.56f, 0.33f);
    static readonly Color C_PISO_JUEGOS = new Color(0.35f,0.30f,0.25f); // oscuro sala juegos
    static readonly Color C_PARED_IN  = new Color(0.97f, 0.97f, 0.96f);
    static readonly Color C_TECHO_IN  = new Color(1.00f, 1.00f, 1.00f);
    static readonly Color C_MADERA    = new Color(0.22f, 0.15f, 0.09f);
    static readonly Color C_TAPIZ     = new Color(0.80f, 0.78f, 0.74f);
    static readonly Color C_ACENTO    = new Color(0.12f, 0.12f, 0.12f);
    static readonly Color C_COCINA    = new Color(0.88f, 0.87f, 0.85f);
    static readonly Color C_COLCHA    = new Color(0.95f, 0.93f, 0.88f);
    static readonly Color C_LIENZO_1  = new Color(0.60f, 0.40f, 0.25f);
    static readonly Color C_LIENZO_2  = new Color(0.25f, 0.35f, 0.55f);
    static readonly Color C_PLANTA_I  = new Color(0.18f, 0.42f, 0.16f);
    static readonly Color C_MACETA_I  = new Color(0.60f, 0.55f, 0.48f);
    static readonly Color C_VERDE_B   = new Color(0.10f, 0.38f, 0.16f); // paño billar
    static readonly Color C_ROJO_J    = new Color(0.75f, 0.10f, 0.10f); // jugadores equipo A
    static readonly Color C_AZUL_J    = new Color(0.10f, 0.20f, 0.75f); // jugadores equipo B
    static readonly Color C_BAR       = new Color(0.14f, 0.08f, 0.05f); // barra bar oscura
    static readonly Color C_BANO      = new Color(0.92f, 0.92f, 0.95f); // blanco azulado baño

    static Material mPiso, mPisoJ, mParedIn, mTechoIn, mMadera, mTapiz, mAcento,
                    mCocina, mColcha, mLienzo1, mLienzo2, mPlantaI, mMacetaI,
                    mVerdeB, mRojoJ, mAzulJ, mBar, mBano;

    static Transform casaT;
    static int muebleIdx = 0;

    // ═══════════════════════════════════════════════════════════════════════
    [MenuItem("ArqViz/Construir Interior Grande")]
    public static void ConstruirInterior()
    {
        var casaGO = GameObject.Find(CASA_ROOT);
        if (casaGO == null)
        {
            EditorUtility.DisplayDialog("ArqViz", "Primero ejecuta 'Construir Casa Grande'.", "OK");
            return;
        }
        casaT = casaGO.transform;
        muebleIdx = 0;

        var intAnterior = casaT.Find("Interior");
        if (intAnterior != null) Object.DestroyImmediate(intAnterior.gameObject);
        var playerAnterior = GameObject.FindGameObjectWithTag("Player");
        if (playerAnterior != null) Object.DestroyImmediate(playerAnterior);

        CrearMateriales();

        var ir = new GameObject("Interior");
        ir.transform.SetParent(casaT, false);
        Transform iT = ir.transform;

        AgregarColisionadoresEstructurales(iT);
        ConstruirPisosYTechos(iT);
        ConstruirParedesInteriores(iT);
        ConfigurarPuerta();
        ConstruirSala(iT);
        ConstruirCocina(iT);
        ConstruirDormitorio(iT);
        ConstruirBano(iT);
        ConstruirEstudio(iT);
        ConstruirSalaJuegos(iT);
        AgregarFocosInteriores(iT);
        CrearPlayer();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("ArqViz",
            "¡Interior listo!\n\n" +
            "• WASD = moverse  |  Shift = correr\n" +
            "• Mouse = mirar   |  Escape = cursor\n\n" +
            "Habitaciones: Sala, Cocina, Dormitorio,\n" +
            "Baño, Estudio y Sala de Juegos.\n\nCtrl+S para guardar.", "OK");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MATERIALES
    // ═══════════════════════════════════════════════════════════════════════
    static void CrearMateriales()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Interior"))
            AssetDatabase.CreateFolder("Assets/Materials", "Interior");

        mPiso    = Mat("Piso",     C_PISO,      0.00f, 0.45f);
        mPisoJ   = Mat("PisoJ",    C_PISO_JUEGOS,0.00f,0.20f);
        mParedIn = Mat("ParedInt", C_PARED_IN,  0.00f, 0.15f);
        mTechoIn = Mat("TechoInt", C_TECHO_IN,  0.00f, 0.10f);
        mMadera  = Mat("Madera",   C_MADERA,    0.00f, 0.30f);
        mTapiz   = Mat("Tapiz",    C_TAPIZ,     0.00f, 0.10f);
        mAcento  = Mat("Acento",   C_ACENTO,    0.10f, 0.60f);
        mCocina  = Mat("Cocina",   C_COCINA,    0.05f, 0.50f);
        mColcha  = Mat("Colcha",   C_COLCHA,    0.00f, 0.05f);
        mLienzo1 = Mat("Lienzo1",  C_LIENZO_1,  0.00f, 0.05f);
        mLienzo2 = Mat("Lienzo2",  C_LIENZO_2,  0.00f, 0.05f);
        mPlantaI = Mat("PlantaI",  C_PLANTA_I,  0.00f, 0.05f);
        mMacetaI = Mat("MacetaI",  C_MACETA_I,  0.00f, 0.20f);
        mVerdeB  = Mat("VerdeB",   C_VERDE_B,   0.00f, 0.08f);
        mRojoJ   = Mat("RojoJ",    C_ROJO_J,    0.00f, 0.10f);
        mAzulJ   = Mat("AzulJ",    C_AZUL_J,    0.00f, 0.10f);
        mBar     = Mat("Bar",      C_BAR,       0.05f, 0.40f);
        mBano    = Mat("Bano",     C_BANO,      0.02f, 0.55f);
        AssetDatabase.SaveAssets();
    }

    static Material Mat(string n, Color c, float metal, float smooth)
    {
        string path = MAT_DIR + "Int_" + n + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = n };
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Metallic",  metal);
        m.SetFloat("_Smoothness", smooth);
        EditorUtility.SetDirty(m);
        return m;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COLISIONADORES ESTRUCTURALES
    //
    // Cuerpo principal: X[-10,10], Z[0,16]
    // Ala (juegos):     X[10,22],  Z[0,14]
    // Conexión (gap):   X=10,      Z[1,5]
    // ═══════════════════════════════════════════════════════════════════════
    static void AgregarColisionadoresEstructurales(Transform ir)
    {
        var col = new GameObject("Colisionadores_Perim");
        col.transform.SetParent(ir, false);
        Transform c = col.transform;

        // ── Cuerpo principal exterior ─────────────────────────────────────
        Colis(c, "Col_PIzq",      new Vector3(-10.15f,2f,  8f),   new Vector3(0.6f,5f,17f));
        Colis(c, "Col_PDer_F",    new Vector3( 10.15f,2f,  0.5f), new Vector3(0.6f,5f, 1f));  // Z[0,1]
        Colis(c, "Col_PDer_T",    new Vector3( 10.15f,2f, 10.5f), new Vector3(0.6f,5f,11f));  // Z[5,16]
        Colis(c, "Col_PTras",     new Vector3(  0f,   2f, 16.15f),new Vector3(21f,5f, 0.6f));
        // Fachada frontal
        Colis(c, "Col_PF_Izq",   new Vector3( -8.5f, 2f, -0.2f), new Vector3(3.2f,5f,0.6f));
        Colis(c, "Col_PF_Der",   new Vector3(  8.5f, 2f, -0.2f), new Vector3(3.2f,5f,0.6f));
        Colis(c, "Col_PF_EntL",  new Vector3( -1.1f, 2f, -0.2f), new Vector3(0.85f,5f,0.6f));
        Colis(c, "Col_PF_EntR",  new Vector3(  1.1f, 2f, -0.2f), new Vector3(0.85f,5f,0.6f));
        Colis(c, "Col_Viga_F",   new Vector3(  0f, 3.6f, -0.2f), new Vector3(14f, 1f,0.6f));
        Colis(c, "Col_BVizq",    new Vector3(-4.25f,0.45f,-0.2f), new Vector3(5.5f,1f,0.6f));
        Colis(c, "Col_BVder",    new Vector3( 4.25f,0.45f,-0.2f), new Vector3(5.5f,1f,0.6f));

        // ── Ala exterior ─────────────────────────────────────────────────
        Colis(c, "Col_Ala_PIzq_F", new Vector3(10.15f,2f,  0.5f), new Vector3(0.6f,5f, 1f));  // Z[0,1]
        Colis(c, "Col_Ala_PIzq_T", new Vector3(10.15f,2f,  9.5f), new Vector3(0.6f,5f, 9f));  // Z[5,14]
        Colis(c, "Col_Ala_PDer",   new Vector3(22.15f,2f,  7f),   new Vector3(0.6f,5f,14f));
        Colis(c, "Col_Ala_PTras",  new Vector3(16f,   2f, 14.15f),new Vector3(12f, 5f,0.6f));
        Colis(c, "Col_Ala_PF_L",   new Vector3(11.5f, 2f, -0.2f), new Vector3(3.2f,5f,0.6f));
        Colis(c, "Col_Ala_PF_R",   new Vector3(20.5f, 2f, -0.2f), new Vector3(3.2f,5f,0.6f));
        Colis(c, "Col_Ala_PF_T",   new Vector3(16f, 3.6f, -0.2f), new Vector3(12.5f,1f,0.6f));
        Colis(c, "Col_Ala_PF_B",   new Vector3(16f,0.45f, -0.2f), new Vector3(6.5f, 1f,0.6f));

        // ── Paredes interiores Z=8 (col sin vanos de puerta) ─────────────
        // Puerta dorm en X=-6 (±0.9 → X[-6.9,-5.1])
        // Puerta estudio en X=5 (±0.9 → X[4.1,5.9])
        Colis(c, "Col_Div_Z8_A", new Vector3(-8.45f,2f,8f), new Vector3(3.1f,4.5f,0.28f));
        Colis(c, "Col_Div_Z8_B", new Vector3(-0.5f, 2f,8f), new Vector3(9.2f,4.5f,0.28f));
        Colis(c, "Col_Div_Z8_C", new Vector3( 7.95f,2f,8f), new Vector3(4.1f,4.5f,0.28f));
        // Dinteles sobre puertas Z=8
        Colis(c, "Col_Div_Z8_D1T",new Vector3(-6f, 3.3f,8f), new Vector3(1.8f,1.4f,0.28f));
        Colis(c, "Col_Div_Z8_D2T",new Vector3( 5f, 3.3f,8f), new Vector3(1.8f,1.4f,0.28f));

        // ── Pared X=-2 (dormitorio/baño): puerta en Z=10 (±0.75) ─────────
        Colis(c, "Col_Xm2_A",  new Vector3(-2f,2f,  8.625f), new Vector3(0.28f,4.5f,1.25f));
        Colis(c, "Col_Xm2_B",  new Vector3(-2f,2f, 13.375f), new Vector3(0.28f,4.5f,5.25f));
        Colis(c, "Col_Xm2_DT", new Vector3(-2f,3.3f,  10f),  new Vector3(0.28f,1.4f,1.5f));

        // ── Pared X=2 (baño/estudio): puerta en Z=10 (±0.75) ────────────
        Colis(c, "Col_X2_A",   new Vector3(2f,2f,   8.625f), new Vector3(0.28f,4.5f,1.25f));
        Colis(c, "Col_X2_B",   new Vector3(2f,2f,  13.375f), new Vector3(0.28f,4.5f,5.25f));
        Colis(c, "Col_X2_DT",  new Vector3(2f,3.3f,   10f),  new Vector3(0.28f,1.4f,1.5f));

        // ── Elementos exteriores con colisionador ─────────────────────────
        AgregarColisionadorExistente("Escalon_1");
        AgregarColisionadorExistente("Escalon_2");
        AgregarColisionadorExistente("Plataforma");
        AgregarColisionadorExistente("Suelo_Frente");
        AgregarColisionadorExistente("Suelo_Cesped");
        AgregarColisionadorExistente("Suelo_Lateral_D");
    }

    static void AgregarColisionadorExistente(string nombre)
    {
        var all = casaT.GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
            if (t.name == nombre && t.GetComponent<Collider>() == null)
            { t.gameObject.AddComponent<BoxCollider>(); break; }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PISOS Y TECHOS
    // ═══════════════════════════════════════════════════════════════════════
    static void ConstruirPisosYTechos(Transform ir)
    {
        // Piso principal cuerpo (X[-10,10], Z[0,16])
        CuboCol(ir, "Piso_Principal", new Vector3(0f,  0.10f, 8f),  new Vector3(19.7f,0.20f,15.7f), mPiso);
        // Piso ala sala de juegos (X[10,22], Z[0,14]) — piso oscuro
        CuboCol(ir, "Piso_Juegos",    new Vector3(16f, 0.10f, 7f),  new Vector3(11.7f,0.20f,13.7f), mPisoJ);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PAREDES INTERIORES (geometría visible + colisionadores ya en Colis)
    // ═══════════════════════════════════════════════════════════════════════
    static void ConstruirParedesInteriores(Transform ir)
    {
        // ── Pared transversal Z=8 ─────────────────────────────────────────
        // Segmento A: X[-10,-6.9]
        CuboCol(ir,"PDiv_Z8_A", new Vector3(-8.45f,2f,8f), new Vector3(3.1f,4f,0.20f), mParedIn);
        // Segmento B: X[-5.1,4.1]
        CuboCol(ir,"PDiv_Z8_B", new Vector3(-0.5f, 2f,8f), new Vector3(9.2f,4f,0.20f), mParedIn);
        // Segmento C: X[5.9,10]
        CuboCol(ir,"PDiv_Z8_C", new Vector3( 7.95f,2f,8f), new Vector3(4.1f,4f,0.20f), mParedIn);
        // Dinteles
        CuboVis(ir,"PDiv_Z8_D1T",new Vector3(-6f, 3.3f,8f),new Vector3(1.8f,1.4f,0.20f),mParedIn);
        CuboVis(ir,"PDiv_Z8_D2T",new Vector3( 5f, 3.3f,8f),new Vector3(1.8f,1.4f,0.20f),mParedIn);
        // Marcos de puerta Z=8
        MarcosPuerta(ir, new Vector3(-6f,1.1f,8f), 1.8f, mMadera);
        MarcosPuerta(ir, new Vector3( 5f,1.1f,8f), 1.8f, mMadera);

        // ── Pared X=-2 (dormitorio/baño): puerta Z[9.25,10.75] ───────────
        CuboCol(ir,"PDiv_Xm2_A", new Vector3(-2f,2f, 8.625f),new Vector3(0.20f,4f,1.25f), mParedIn);
        CuboCol(ir,"PDiv_Xm2_B", new Vector3(-2f,2f,13.375f),new Vector3(0.20f,4f,5.25f), mParedIn);
        CuboVis(ir,"PDiv_Xm2_DT",new Vector3(-2f,3.3f,  10f), new Vector3(0.20f,1.4f,1.5f),mParedIn);
        MarcosPuertaX(ir, new Vector3(-2f,1.1f,10f), 1.5f, mMadera);

        // ── Pared X=2 (baño/estudio): puerta Z[9.25,10.75] ───────────────
        CuboCol(ir,"PDiv_X2_A",  new Vector3(2f, 2f, 8.625f),new Vector3(0.20f,4f,1.25f), mParedIn);
        CuboCol(ir,"PDiv_X2_B",  new Vector3(2f, 2f,13.375f),new Vector3(0.20f,4f,5.25f), mParedIn);
        CuboVis(ir,"PDiv_X2_DT", new Vector3(2f, 3.3f,  10f), new Vector3(0.20f,1.4f,1.5f),mParedIn);
        MarcosPuertaX(ir, new Vector3(2f,1.1f,10f), 1.5f, mMadera);
    }

    // Marco de puerta en pared Z (dintel + jambas)
    static void MarcosPuerta(Transform ir, Vector3 centro, float ancho, Material m)
    {
        CuboVis(ir, "MarcP_L_" + centro.x, centro + new Vector3(-ancho/2f-0.05f,0,0),
                new Vector3(0.08f,2.4f,0.24f), m);
        CuboVis(ir, "MarcP_R_" + centro.x, centro + new Vector3( ancho/2f+0.05f,0,0),
                new Vector3(0.08f,2.4f,0.24f), m);
        CuboVis(ir, "MarcP_T_" + centro.x, centro + new Vector3(0f,1.3f,0),
                new Vector3(ancho+0.2f,0.10f,0.24f), m);
    }

    // Marco de puerta en pared X (jambas a lo largo de Z)
    static void MarcosPuertaX(Transform ir, Vector3 centro, float ancho, Material m)
    {
        CuboVis(ir, "MarcPX_L_" + centro.z, centro + new Vector3(0,0,-ancho/2f-0.05f),
                new Vector3(0.24f,2.4f,0.08f), m);
        CuboVis(ir, "MarcPX_R_" + centro.z, centro + new Vector3(0,0, ancho/2f+0.05f),
                new Vector3(0.24f,2.4f,0.08f), m);
        CuboVis(ir, "MarcPX_T_" + centro.z, centro + new Vector3(0,1.3f,0),
                new Vector3(0.24f,0.10f,ancho+0.2f), m);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PUERTA EXTERIOR
    // ═══════════════════════════════════════════════════════════════════════
    static void ConfigurarPuerta()
    {
        Transform hoja = null;
        foreach (var t in casaT.GetComponentsInChildren<Transform>(true))
            if (t.name == "Puerta_Hoja") { hoja = t; break; }
        if (hoja == null) { Debug.LogWarning("[ArqViz] Puerta_Hoja no encontrada."); return; }

        var pivotViejo = casaT.Find("Puerta_Pivot");
        if (pivotViejo != null) Object.DestroyImmediate(pivotViejo.gameObject);

        var pivotGO = new GameObject("Puerta_Pivot");
        pivotGO.transform.SetParent(casaT, false);
        pivotGO.transform.localPosition = new Vector3(-0.60f, 0f, -0.05f);

        hoja.SetParent(pivotGO.transform, true);
        hoja.localPosition = new Vector3(0.60f, 1.10f, 0f);
        hoja.localRotation = Quaternion.identity;
        hoja.localScale    = new Vector3(1.2f, 2.2f, 0.1f);

        if (hoja.GetComponent<BoxCollider>() == null)
            hoja.gameObject.AddComponent<BoxCollider>();

        var trigGO = new GameObject("Puerta_Trigger");
        trigGO.transform.SetParent(pivotGO.transform, false);
        trigGO.transform.localPosition = new Vector3(0.60f, 1.50f, -1.80f);
        var bc = trigGO.AddComponent<BoxCollider>();
        bc.size      = new Vector3(3.5f, 3.5f, 3.0f);
        bc.isTrigger = true;

        var dc = AddCompReflection(pivotGO, "DoorController");
        if (dc != null)
        {
            var so = new SerializedObject(dc);
            so.FindProperty("anguloApertura").floatValue    = 90f;
            so.FindProperty("velocidadApertura").floatValue = 2.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorUtility.SetDirty(pivotGO);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SALA / LIVING ROOM   X[-10,10], Z[0,8]  — open plan con cocina
    // Zona de sala: X[-10,3], TV contra pared Z=8, sofá mirando hacia TV
    // ═══════════════════════════════════════════════════════════════════════
    static void ConstruirSala(Transform ir)
    {
        var s = Sub(ir, "Sala");

        // ── Sofá grande en L (mirando hacia TV en Z=8) ───────────────────
        // Cuerpo principal frente al TV
        CuboCol(s,"Sofa_Asiento",   new Vector3(-3f,0.42f,3.5f),  new Vector3(3.5f,0.44f,1.0f),  mTapiz);
        CuboVis(s,"Sofa_Respaldo",  new Vector3(-3f,0.82f,3.98f), new Vector3(3.5f,0.50f,0.16f), mTapiz);
        CuboVis(s,"Sofa_Brazo_Izq", new Vector3(-4.85f,0.55f,3.5f),new Vector3(0.18f,0.28f,1.0f),mTapiz);
        CuboVis(s,"Sofa_Brazo_Der", new Vector3(-1.15f,0.55f,3.5f),new Vector3(0.18f,0.28f,1.0f),mTapiz);
        PatasCilindro(s, new Vector3(-3f,0f,3.5f), 3.3f, 0.85f, 0.07f, 0.20f, mMadera);
        // Cojines
        CuboVis(s,"Sofa_Cojin_1",new Vector3(-4.2f,0.68f,3.62f),new Vector3(0.45f,0.28f,0.40f),mColcha);
        CuboVis(s,"Sofa_Cojin_2",new Vector3(-3.0f,0.68f,3.62f),new Vector3(0.45f,0.28f,0.40f),mColcha);
        CuboVis(s,"Sofa_Cojin_3",new Vector3(-1.8f,0.68f,3.62f),new Vector3(0.45f,0.28f,0.40f),mColcha);
        // Brazo lateral izquierdo del sofá en L (contra pared izquierda)
        CuboCol(s,"Sofa_L_Asiento",  new Vector3(-4.5f,0.42f,2.0f),  new Vector3(1.0f,0.44f,2.5f), mTapiz);
        CuboVis(s,"Sofa_L_Respaldo", new Vector3(-4.90f,0.82f,2.0f), new Vector3(0.16f,0.50f,2.5f),mTapiz);

        // ── Mesa de centro ────────────────────────────────────────────────
        CuboCol(s,"Mesa_Centro_Top", new Vector3(-3f,0.46f,2.20f), new Vector3(1.20f,0.06f,0.70f),mMadera);
        PatasCilindro(s, new Vector3(-3f,0f,2.20f), 1.10f, 0.60f, 0.05f, 0.44f, mAcento);

        // ── Mueble TV + televisor (contra pared Z=8) ──────────────────────
        CuboCol(s,"MuebleTV_Base",  new Vector3(-2f,0.30f,7.85f), new Vector3(3.0f,0.60f,0.52f), mMadera);
        CuboVis(s,"TV_Marco",       new Vector3(-2f,1.05f,7.90f), new Vector3(2.20f,1.10f,0.08f),mMadera);
        CuboVis(s,"TV_Pantalla",    new Vector3(-2f,1.05f,7.93f), new Vector3(2.00f,0.95f,0.06f),mAcento);

        // ── Armario TV / repisa librería (pared izquierda) ───────────────
        CuboCol(s,"Libreria",       new Vector3(-9.7f,1.50f,6.0f), new Vector3(0.50f,3.0f,3.5f), mMadera);
        CuboVis(s,"Lib_Tab_1",      new Vector3(-9.45f,0.80f,6.0f),new Vector3(0.08f,0.06f,3.3f),mMadera);
        CuboVis(s,"Lib_Tab_2",      new Vector3(-9.45f,1.60f,6.0f),new Vector3(0.08f,0.06f,3.3f),mMadera);
        CuboVis(s,"Lib_Tab_3",      new Vector3(-9.45f,2.40f,6.0f),new Vector3(0.08f,0.06f,3.3f),mMadera);

        // ── Cuadros en pared izquierda ────────────────────────────────────
        CuboVis(s,"Cuadro_M1",  new Vector3(-9.75f,2.3f,1.5f), new Vector3(0.08f,0.90f,1.30f),mMadera);
        CuboVis(s,"Cuadro_L1",  new Vector3(-9.72f,2.3f,1.5f), new Vector3(0.05f,0.78f,1.18f),mLienzo1);
        CuboVis(s,"Cuadro_M2",  new Vector3(-9.75f,2.3f,3.0f), new Vector3(0.08f,0.70f,0.90f),mMadera);
        CuboVis(s,"Cuadro_L2",  new Vector3(-9.72f,2.3f,3.0f), new Vector3(0.05f,0.58f,0.78f),mLienzo2);

        // ── Planta decorativa esquina izquierda delantera ─────────────────
        CuboVis(s,"Planta_Mac",  new Vector3(-9.5f,0.22f,0.7f), new Vector3(0.40f,0.44f,0.40f),mMacetaI);
        EsfVis(s, "Planta_H1",   new Vector3(-9.5f,0.80f,0.7f), new Vector3(0.75f,0.80f,0.75f),mPlantaI);
        EsfVis(s, "Planta_H2",   new Vector3(-9.1f,0.65f,0.5f), new Vector3(0.40f,0.45f,0.40f),mPlantaI);

        // ── Lámpara de piso junto al sofá ─────────────────────────────────
        CuboVis(s,"Lamp_Base",     new Vector3(-1f,0.07f,3.8f), new Vector3(0.22f,0.14f,0.22f),mAcento);
        CilVis(s, "Lamp_Tallo",    new Vector3(-1f,0.95f,3.8f), new Vector3(0.04f,1.70f,0.04f),mAcento);
        CuboVis(s,"Lamp_Pantalla", new Vector3(-1f,1.90f,3.8f), new Vector3(0.32f,0.40f,0.32f),mColcha);

        // ── Sillón individual ─────────────────────────────────────────────
        CuboCol(s,"Siljon_Asiento",  new Vector3(1.0f,0.42f,3.0f), new Vector3(0.90f,0.44f,0.90f),mTapiz);
        CuboVis(s,"Siljon_Respaldo", new Vector3(1.0f,0.82f,3.44f),new Vector3(0.90f,0.50f,0.16f),mTapiz);
        CuboVis(s,"Siljon_BrazoIzq", new Vector3(0.55f,0.55f,3.0f),new Vector3(0.16f,0.28f,0.90f),mTapiz);
        CuboVis(s,"Siljon_BrazoDer", new Vector3(1.45f,0.55f,3.0f),new Vector3(0.16f,0.28f,0.90f),mTapiz);
        PatasCilindro(s, new Vector3(1.0f,0f,3.0f), 0.80f, 0.80f, 0.06f, 0.20f, mMadera);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COCINA / COMEDOR   X[3,10], Z[0,8]  — integrada con sala (open plan)
    // ═══════════════════════════════════════════════════════════════════════
    static void ConstruirCocina(Transform ir)
    {
        var k = Sub(ir, "Cocina");

        // ── Mueble bajo contra pared derecha (X=9.7) ──────────────────────
        CuboCol(k,"Mueble_Bajo",   new Vector3(9.4f,0.47f,5.0f), new Vector3(1.0f,0.94f,5.5f),  mMadera);
        CuboVis(k,"Encimera",      new Vector3(9.4f,0.96f,5.0f), new Vector3(1.05f,0.06f,5.55f), mCocina);
        // Mueble alto (hasta techo)
        CuboCol(k,"Mueble_Alto",   new Vector3(9.7f,2.0f,2.0f),  new Vector3(0.50f,4.0f,2.5f),  mMadera);
        // Puertas armario cocina
        CuboVis(k,"Arm_P1",        new Vector3(9.45f,2.0f,1.5f), new Vector3(0.06f,2.8f,0.95f), mCocina);
        CuboVis(k,"Arm_P2",        new Vector3(9.45f,2.0f,2.5f), new Vector3(0.06f,2.8f,0.95f), mCocina);

        // ── Isla de cocina (divisor visual con sala) ───────────────────────
        CuboCol(k,"Isla_Base",     new Vector3(5.5f,0.47f,2.0f), new Vector3(2.0f,0.94f,1.0f),  mMadera);
        CuboVis(k,"Isla_Encimera", new Vector3(5.5f,0.96f,2.0f), new Vector3(2.1f,0.06f,1.05f), mCocina);
        // Taburetes isla
        Taburete(k,"Tab_1",new Vector3(5.0f,0f,1.1f));
        Taburete(k,"Tab_2",new Vector3(6.0f,0f,1.1f));

        // ── Mesa comedor ──────────────────────────────────────────────────
        CuboCol(k,"Mesa_Com",      new Vector3(6.0f,0.78f,5.8f), new Vector3(1.60f,0.06f,0.90f),mMadera);
        PatasCilindro(k, new Vector3(6.0f,0f,5.8f), 1.45f,0.75f, 0.06f, 0.77f, mMadera);
        // 4 sillas comedor
        Silla(k,"Silla_1",new Vector3(6.0f,0f,5.0f),   0f);
        Silla(k,"Silla_2",new Vector3(6.0f,0f,6.6f), 180f);
        Silla(k,"Silla_3",new Vector3(5.15f,0f,5.8f), 90f);
        Silla(k,"Silla_4",new Vector3(6.85f,0f,5.8f),-90f);

        // ── Repisa decorativa pared trasera Z=8 (zona cocina) ─────────────
        CuboVis(k,"Repisa",    new Vector3(7f,1.80f,7.90f), new Vector3(3.5f,0.08f,0.20f),mMadera);
        CuboVis(k,"Rep_Obj1",  new Vector3(6.0f,1.94f,7.85f),new Vector3(0.18f,0.28f,0.18f),mCocina);
        CuboVis(k,"Rep_Obj2",  new Vector3(7.0f,1.94f,7.85f),new Vector3(0.14f,0.35f,0.14f),mMadera);
        CuboVis(k,"Rep_Obj3",  new Vector3(8.0f,1.94f,7.85f),new Vector3(0.18f,0.22f,0.18f),mLienzo1);

        // ── Planta sobre encimera ──────────────────────────────────────────
        CuboVis(k,"Coc_Mac",   new Vector3(9.4f,1.02f,7.2f), new Vector3(0.20f,0.24f,0.20f),mMacetaI);
        EsfVis(k, "Coc_Planta",new Vector3(9.4f,1.30f,7.2f), new Vector3(0.36f,0.40f,0.36f),mPlantaI);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DORMITORIO PRINCIPAL   X[-10,-2], Z[8,16]
    // ═══════════════════════════════════════════════════════════════════════
    static void ConstruirDormitorio(Transform ir)
    {
        var d = Sub(ir, "Dormitorio");

        // ── Cama doble contra pared trasera (Z=16) ────────────────────────
        CuboCol(d,"Cama_Base",  new Vector3(-6f,0.22f,14.5f), new Vector3(2.0f,0.44f,2.20f), mMadera);
        CuboVis(d,"Cama_Colch", new Vector3(-6f,0.47f,14.5f), new Vector3(1.90f,0.14f,2.10f),mColcha);
        CuboVis(d,"Cama_Cab",   new Vector3(-6f,0.85f,15.60f),new Vector3(2.0f,0.85f,0.14f), mMadera);
        CuboVis(d,"Almoh_1",    new Vector3(-6.6f,0.56f,15.1f),new Vector3(0.65f,0.13f,0.45f),mColcha);
        CuboVis(d,"Almoh_2",    new Vector3(-5.4f,0.56f,15.1f),new Vector3(0.65f,0.13f,0.45f),mColcha);
        CuboVis(d,"Pie_Cama",   new Vector3(-6f,0.36f,13.5f), new Vector3(2.0f,0.16f,0.14f), mMadera);

        // ── Mesitas de noche ──────────────────────────────────────────────
        CuboCol(d,"Mesita_Izq",   new Vector3(-7.2f,0.34f,14.5f),new Vector3(0.50f,0.68f,0.45f),mMadera);
        CuboCol(d,"Mesita_Der",   new Vector3(-4.8f,0.34f,14.5f),new Vector3(0.50f,0.68f,0.45f),mMadera);
        CilVis(d, "Lamp_M_Izq",   new Vector3(-7.2f,0.84f,14.5f),new Vector3(0.05f,0.44f,0.05f),mAcento);
        CuboVis(d,"Lamp_M_Izq_P", new Vector3(-7.2f,1.08f,14.5f),new Vector3(0.22f,0.24f,0.22f),mColcha);
        CilVis(d, "Lamp_M_Der",   new Vector3(-4.8f,0.84f,14.5f),new Vector3(0.05f,0.44f,0.05f),mAcento);
        CuboVis(d,"Lamp_M_Der_P", new Vector3(-4.8f,1.08f,14.5f),new Vector3(0.22f,0.24f,0.22f),mColcha);

        // ── Armario / Closet contra pared izquierda (X=-10) ──────────────
        CuboCol(d,"Armario",      new Vector3(-9.5f,1.55f,11.0f),new Vector3(0.80f,3.0f,3.5f), mMadera);
        CuboVis(d,"Arm_P1",       new Vector3(-9.1f,1.55f,10.4f),new Vector3(0.06f,2.80f,1.3f),mColcha);
        CuboVis(d,"Arm_P2",       new Vector3(-9.1f,1.55f,11.6f),new Vector3(0.06f,2.80f,1.3f),mColcha);
        CuboVis(d,"Arm_Tirador",  new Vector3(-9.1f,1.55f,11.0f),new Vector3(0.05f,0.05f,0.55f),mAcento);

        // ── Escritorio/tocador contra pared derecha (X=-2.3) ─────────────
        CuboCol(d,"Escritorio",   new Vector3(-2.55f,0.40f,12.0f),new Vector3(0.50f,0.80f,1.60f),mMadera);
        CuboVis(d,"Escritorio_T", new Vector3(-2.42f,0.82f,12.0f),new Vector3(0.22f,0.05f,1.55f),mCocina);
        CuboVis(d,"Espejo",       new Vector3(-2.45f,1.80f,12.0f),new Vector3(0.06f,1.00f,0.80f),mAcento);
        // Silla escritorio
        CuboCol(d,"SillaEsc",     new Vector3(-3.2f,0.44f,12.0f),new Vector3(0.46f,0.06f,0.46f),mTapiz);
        PatasCilindro(d, new Vector3(-3.2f,0f,12.0f), 0.44f,0.44f, 0.04f, 0.44f, mAcento);

        // ── Cuadro pared trasera sobre la cama ────────────────────────────
        CuboVis(d,"Cuadro_Dorm_M",new Vector3(-6f,2.50f,15.95f),new Vector3(0.08f,0.80f,1.20f),mMadera);
        CuboVis(d,"Cuadro_Dorm_L",new Vector3(-6f,2.50f,15.98f),new Vector3(0.05f,0.68f,1.08f),mLienzo2);

        // ── Planta esquina ────────────────────────────────────────────────
        CuboVis(d,"Dorm_Mac",    new Vector3(-9.5f,0.22f,15.5f),new Vector3(0.32f,0.44f,0.32f),mMacetaI);
        EsfVis(d, "Dorm_Planta", new Vector3(-9.5f,0.72f,15.5f),new Vector3(0.65f,0.72f,0.65f),mPlantaI);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BAÑO   X[-2,2], Z[8,16]
    // ═══════════════════════════════════════════════════════════════════════
    static void ConstruirBano(Transform ir)
    {
        var b = Sub(ir, "Bano");

        // ── Bañera contra pared trasera izquierda ─────────────────────────
        CuboCol(b,"Banera",     new Vector3(-1.0f,0.22f,15.2f), new Vector3(1.4f,0.44f,2.0f), mBano);
        CuboVis(b,"Banera_Int", new Vector3(-1.0f,0.36f,15.2f), new Vector3(1.2f,0.20f,1.8f), mAcento);
        // Grifo
        CilVis(b, "Grifo",      new Vector3(-1.0f,0.65f,14.3f), new Vector3(0.05f,0.30f,0.05f),mAcento);

        // ── Inodoro contra pared trasera derecha ──────────────────────────
        CuboCol(b,"Inodoro_Base",new Vector3(1.3f,0.22f,15.5f), new Vector3(0.55f,0.44f,0.70f),mBano);
        CuboVis(b,"Inodoro_Cis", new Vector3(1.3f,0.60f,15.85f),new Vector3(0.50f,0.40f,0.18f),mBano);

        // ── Lavabo + mueble contra pared X=-2, cerca puerta ───────────────
        CuboCol(b,"Mueble_Lav",  new Vector3(-1.2f,0.40f,9.8f), new Vector3(1.1f,0.80f,0.55f),mMadera);
        CuboVis(b,"Encimera_Lav",new Vector3(-1.2f,0.82f,9.8f), new Vector3(1.15f,0.05f,0.58f),mBano);
        CuboVis(b,"Lavabo",      new Vector3(-1.2f,0.80f,9.8f), new Vector3(0.70f,0.12f,0.40f),mBano);
        CilVis(b, "Grifo_Lav",   new Vector3(-1.2f,0.97f,9.6f), new Vector3(0.04f,0.28f,0.04f),mAcento);
        // Espejo sobre lavabo
        CuboVis(b,"Espejo_Bano", new Vector3(-1.2f,1.60f,9.82f),new Vector3(0.06f,0.80f,0.65f),mAcento);

        // ── Ducha contra pared derecha (X=2) ─────────────────────────────
        CuboCol(b,"Ducha_Base",  new Vector3(1.2f,0.05f,13.5f), new Vector3(1.2f,0.10f,1.5f), mBano);
        // Paredes ducha (3 lados, vidrio)
        CuboVis(b,"Ducha_P1",   new Vector3(1.82f,1.1f,13.5f), new Vector3(0.06f,2.2f,1.5f), mBano);
        CuboVis(b,"Ducha_P2",   new Vector3(1.2f,1.1f,12.77f), new Vector3(1.2f,2.2f,0.06f), mBano);
        CuboVis(b,"Ducha_P3",   new Vector3(1.2f,1.1f,14.23f), new Vector3(1.2f,2.2f,0.06f), mBano);
        // Cabezal ducha
        CilVis(b, "Ducha_Tubo", new Vector3(1.6f,2.8f,12.9f), new Vector3(0.04f,0.80f,0.04f),mAcento);
        CuboVis(b,"Ducha_Cab",  new Vector3(1.6f,2.8f,13.1f), new Vector3(0.20f,0.05f,0.28f),mAcento);

        // ── Toallero ──────────────────────────────────────────────────────
        CilVis(b,"Toallero",    new Vector3(1.85f,1.1f,10.5f), new Vector3(0.04f,0.04f,0.55f),mAcento);
        CuboVis(b,"Toalla",     new Vector3(1.85f,1.1f,10.5f), new Vector3(0.08f,0.08f,0.50f),mColcha);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ESTUDIO / HABITACIÓN 2   X[2,10], Z[8,16]
    // ═══════════════════════════════════════════════════════════════════════
    static void ConstruirEstudio(Transform ir)
    {
        var e = Sub(ir, "Estudio");

        // ── Escritorio grande contra pared derecha (X=9.7) ────────────────
        CuboCol(e,"Escritorio",    new Vector3(9.4f,0.40f,12.0f), new Vector3(0.80f,0.80f,3.0f),  mMadera);
        CuboVis(e,"Escritorio_T",  new Vector3(9.22f,0.83f,12.0f),new Vector3(0.46f,0.06f,2.95f), mCocina);
        // Monitor
        CuboVis(e,"Monitor_Base",  new Vector3(9.0f,0.89f,12.0f), new Vector3(0.06f,0.06f,0.25f), mAcento);
        CuboVis(e,"Monitor_P",     new Vector3(8.9f,1.30f,12.0f), new Vector3(0.08f,0.70f,1.10f), mAcento);
        // Silla ergonómica
        CuboCol(e,"SillaErg",      new Vector3(8.5f,0.44f,12.0f), new Vector3(0.50f,0.06f,0.50f), mTapiz);
        CuboVis(e,"SillaErg_Res",  new Vector3(8.5f,0.82f,12.35f),new Vector3(0.50f,0.55f,0.12f), mTapiz);
        PatasCilindro(e, new Vector3(8.5f,0f,12.0f), 0.48f,0.48f, 0.04f, 0.44f, mAcento);

        // ── Estantería / librería contra pared trasera (Z=16) ────────────
        CuboCol(e,"Libreria",      new Vector3(6.5f,1.55f,15.85f),new Vector3(4.5f,3.0f,0.55f),  mMadera);
        CuboVis(e,"Lib_Tab_1",     new Vector3(6.5f,0.70f,15.60f),new Vector3(4.3f,0.06f,0.35f), mMadera);
        CuboVis(e,"Lib_Tab_2",     new Vector3(6.5f,1.40f,15.60f),new Vector3(4.3f,0.06f,0.35f), mMadera);
        CuboVis(e,"Lib_Tab_3",     new Vector3(6.5f,2.10f,15.60f),new Vector3(4.3f,0.06f,0.35f), mMadera);
        // Libros decorativos
        for (int i=0; i<6; i++)
            CuboVis(e,"Libro_"+i, new Vector3(4.5f+i*0.55f,0.82f,15.58f),
                    new Vector3(0.08f,0.50f,0.30f), i%2==0?mLienzo1:mLienzo2);

        // ── Sofá pequeño / loveseat ───────────────────────────────────────
        CuboCol(e,"Sofa2_Asiento", new Vector3(5.0f,0.42f,10.5f),new Vector3(1.60f,0.44f,0.85f),mTapiz);
        CuboVis(e,"Sofa2_Resp",    new Vector3(5.0f,0.82f,10.90f),new Vector3(1.60f,0.50f,0.14f),mTapiz);
        CuboVis(e,"Sofa2_BIzq",    new Vector3(4.2f,0.55f,10.5f), new Vector3(0.16f,0.28f,0.85f),mTapiz);
        CuboVis(e,"Sofa2_BDer",    new Vector3(5.8f,0.55f,10.5f), new Vector3(0.16f,0.28f,0.85f),mTapiz);
        PatasCilindro(e, new Vector3(5.0f,0f,10.5f), 1.44f,0.70f, 0.06f, 0.20f, mMadera);

        // ── Mesa auxiliar con planta ───────────────────────────────────────
        CuboCol(e,"Mesa_Aux",   new Vector3(3.5f,0.45f,10.5f),new Vector3(0.55f,0.06f,0.55f),mMadera);
        PatasCilindro(e, new Vector3(3.5f,0f,10.5f), 0.48f,0.48f, 0.05f, 0.44f, mMadera);
        CuboVis(e,"Aux_Mac",    new Vector3(3.5f,0.52f,10.5f),new Vector3(0.18f,0.22f,0.18f),mMacetaI);
        EsfVis(e, "Aux_Planta", new Vector3(3.5f,0.80f,10.5f),new Vector3(0.35f,0.40f,0.35f),mPlantaI);

        // ── Cuadro motivacional pared izquierda (X=2) ────────────────────
        CuboVis(e,"Cuadro_Est_M", new Vector3(2.08f,2.3f,13.0f),new Vector3(0.08f,0.80f,1.20f),mMadera);
        CuboVis(e,"Cuadro_Est_L", new Vector3(2.11f,2.3f,13.0f),new Vector3(0.05f,0.68f,1.08f),mLienzo1);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SALA DE JUEGOS   X[10,22], Z[0,14]  (ala lateral)
    // Incluye: mesa de billar, futbolín, diana de dardos, mini-bar, lounge
    // ═══════════════════════════════════════════════════════════════════════
    static void ConstruirSalaJuegos(Transform ir)
    {
        var j = Sub(ir, "SalaJuegos");

        // ── MESA DE BILLAR — centro (13.5, 0, 4.5) ───────────────────────
        // Cuerpo de madera
        CuboCol(j,"Billar_Cuerpo",  new Vector3(13.5f,0.40f,4.5f), new Vector3(3.0f,0.80f,1.60f),mMadera);
        // Paño verde (superficie de juego)
        CuboVis(j,"Billar_Pano",    new Vector3(13.5f,0.83f,4.5f), new Vector3(2.70f,0.05f,1.30f),mVerdeB);
        // Bandas/rails laterales
        CuboVis(j,"Billar_Rail_F",  new Vector3(13.5f,0.84f,3.82f),new Vector3(2.80f,0.10f,0.15f),mMadera);
        CuboVis(j,"Billar_Rail_T",  new Vector3(13.5f,0.84f,5.18f),new Vector3(2.80f,0.10f,0.15f),mMadera);
        CuboVis(j,"Billar_Rail_L",  new Vector3(12.08f,0.84f,4.5f),new Vector3(0.15f,0.10f,1.10f),mMadera);
        CuboVis(j,"Billar_Rail_R",  new Vector3(14.92f,0.84f,4.5f),new Vector3(0.15f,0.10f,1.10f),mMadera);
        // 6 troneras (pockets)
        EsfVis(j,"Pocket_1", new Vector3(12.1f,0.82f,3.85f), new Vector3(0.16f,0.16f,0.16f),mAcento);
        EsfVis(j,"Pocket_2", new Vector3(13.5f,0.82f,3.82f), new Vector3(0.16f,0.16f,0.16f),mAcento);
        EsfVis(j,"Pocket_3", new Vector3(14.9f,0.82f,3.85f), new Vector3(0.16f,0.16f,0.16f),mAcento);
        EsfVis(j,"Pocket_4", new Vector3(12.1f,0.82f,5.15f), new Vector3(0.16f,0.16f,0.16f),mAcento);
        EsfVis(j,"Pocket_5", new Vector3(13.5f,0.82f,5.18f), new Vector3(0.16f,0.16f,0.16f),mAcento);
        EsfVis(j,"Pocket_6", new Vector3(14.9f,0.82f,5.15f), new Vector3(0.16f,0.16f,0.16f),mAcento);
        // Bolas de billar
        float[] bx = {13.0f,13.3f,13.6f,12.8f,13.1f,13.4f};
        float[] bz = {4.20f,4.20f,4.20f,4.45f,4.45f,4.70f};
        Color[] bc2 = {
            Color.white, Color.yellow, new Color(0.1f,0.1f,0.8f),
            Color.red,   new Color(0.5f,0f,0.5f), Color.black
        };
        for (int i=0;i<6;i++)
        {
            string mbPath = MAT_DIR + "Int_Bola"+i+".mat";
            Material mb = AssetDatabase.LoadAssetAtPath<Material>(mbPath);
            if (mb==null){ mb=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mb,mbPath); }
            mb.SetColor("_BaseColor",bc2[i]); mb.SetFloat("_Smoothness",0.8f); EditorUtility.SetDirty(mb);
            EsfVis(j,"Bola_"+i, new Vector3(bx[i],0.91f,bz[i]), new Vector3(0.10f,0.10f,0.10f),mb);
        }
        // Taco de billar apoyado en la mesa (inclinado — aproximado con un cubo delgado)
        CuboVis(j,"Taco",           new Vector3(12.8f,0.95f,3.5f),new Vector3(0.05f,0.05f,1.40f),mMadera);
        // Rack de tacos en pared izquierda
        CuboVis(j,"Rack_Base",      new Vector3(10.18f,1.30f,6.5f),new Vector3(0.12f,0.10f,1.20f),mMadera);
        CilVis(j, "Taco_R1",        new Vector3(10.16f,1.0f,6.2f), new Vector3(0.04f,1.30f,0.04f),mMadera);
        CilVis(j, "Taco_R2",        new Vector3(10.16f,1.0f,6.5f), new Vector3(0.04f,1.30f,0.04f),mMadera);
        CilVis(j, "Taco_R3",        new Vector3(10.16f,1.0f,6.8f), new Vector3(0.04f,1.30f,0.04f),mMadera);

        // ── FUTBOLÍN — centro (17, 0, 9.5) ────────────────────────────────
        CuboCol(j,"Futbolin_Cuerpo", new Vector3(17f,0.44f,9.5f),  new Vector3(1.60f,0.88f,0.90f),mMadera);
        CuboVis(j,"Futbolin_Sup",    new Vector3(17f,0.90f,9.5f),  new Vector3(1.50f,0.05f,0.80f),mVerdeB);
        // Barras (8 cilindros horizontales a lo largo del eje Z)
        float[] bx2 = {16.3f,16.5f,16.7f,16.9f,17.1f,17.3f,17.5f,17.7f};
        for (int i=0;i<8;i++)
            CilVis(j,"FBarra_"+i, new Vector3(bx2[i],0.90f,9.5f), new Vector3(0.05f,0.05f,0.95f),mAcento);
        // Jugadores en barras
        Color[] tc = {mRojoJ.color, mAzulJ.color, mRojoJ.color, mAzulJ.color,
                      mRojoJ.color, mAzulJ.color, mRojoJ.color, mAzulJ.color};
        for (int i=0;i<8;i++)
        {
            string mp2 = MAT_DIR+"Int_FJ"+i+".mat";
            Material mj=AssetDatabase.LoadAssetAtPath<Material>(mp2);
            if(mj==null){mj=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mj,mp2);}
            mj.SetColor("_BaseColor",tc[i]); EditorUtility.SetDirty(mj);
            CuboVis(j,"FJug_"+i, new Vector3(bx2[i],0.90f,9.5f), new Vector3(0.12f,0.22f,0.12f),mj);
        }
        // Patas futbolín
        PatasCilindro(j, new Vector3(17f,0f,9.5f), 1.50f,0.80f, 0.06f,0.44f, mMadera);

        // ── DIANA DE DARDOS — en pared derecha X=22 ────────────────────────
        // Tablero
        CuboVis(j,"Diana_Back",   new Vector3(21.87f,1.73f,3.0f), new Vector3(0.08f,0.70f,0.70f),mMadera);
        // Anillos concéntricos (cilindros planos en X)
        CilVis(j, "Diana_R1",    new Vector3(21.85f,1.73f,3.0f), new Vector3(0.05f,0.60f,0.60f),mAcento);   // exterior negro
        CilVis(j, "Diana_R2",    new Vector3(21.85f,1.73f,3.0f), new Vector3(0.04f,0.48f,0.48f),mVerdeB);   // verde
        CilVis(j, "Diana_R3",    new Vector3(21.84f,1.73f,3.0f), new Vector3(0.04f,0.34f,0.34f),mAcento);   // negro
        CilVis(j, "Diana_R4",    new Vector3(21.84f,1.73f,3.0f), new Vector3(0.04f,0.20f,0.20f),mRojoJ);    // rojo
        EsfVis(j, "Diana_Bull",  new Vector3(21.83f,1.73f,3.0f), new Vector3(0.07f,0.07f,0.07f),mVerdeB);  // bullseye
        // Dardos en la diana
        CuboVis(j,"Dardo_1",     new Vector3(21.70f,1.73f,3.0f), new Vector3(0.22f,0.03f,0.03f),mAcento);
        CuboVis(j,"Dardo_2",     new Vector3(21.70f,1.80f,3.1f), new Vector3(0.20f,0.03f,0.03f),mAcento);
        // Línea de lanzamiento en el piso
        CuboVis(j,"Linea_Dardos",new Vector3(19.37f,0.11f,3.0f), new Vector3(0.04f,0.05f,0.60f),mAcento);

        // ── MINI-BAR — contra pared trasera Z=14 ──────────────────────────
        CuboCol(j,"Bar_Mostrador", new Vector3(16f,0.55f,13.65f), new Vector3(7.0f,1.10f,0.70f), mBar);
        CuboVis(j,"Bar_Enc",       new Vector3(16f,1.12f,13.65f), new Vector3(7.1f,0.06f,0.74f), mCocina);
        // Estantes detrás del bar
        CuboVis(j,"Bar_Est1",      new Vector3(16f,1.60f,13.95f), new Vector3(6.5f,0.06f,0.30f), mBar);
        CuboVis(j,"Bar_Est2",      new Vector3(16f,2.10f,13.95f), new Vector3(6.5f,0.06f,0.30f), mBar);
        CuboVis(j,"Bar_Est3",      new Vector3(16f,2.60f,13.95f), new Vector3(6.5f,0.06f,0.30f), mBar);
        // Botellas decorativas en estantes
        for (int i=0;i<7;i++)
        {
            Color bc3 = i%3==0?new Color(0.15f,0.35f,0.15f):i%3==1?new Color(0.55f,0.30f,0.10f):new Color(0.65f,0.65f,0.70f);
            string mbp=MAT_DIR+"Int_Bot"+i+".mat";
            Material mb=AssetDatabase.LoadAssetAtPath<Material>(mbp);
            if(mb==null){mb=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mb,mbp);}
            mb.SetColor("_BaseColor",bc3); mb.SetFloat("_Smoothness",0.7f); EditorUtility.SetDirty(mb);
            CilVis(j,"Botella_"+i, new Vector3(12.5f+i*0.80f,1.68f,13.95f), new Vector3(0.10f,0.28f,0.10f),mb);
        }
        // Taburetes de bar
        Taburete(j,"BTab_1",new Vector3(13.0f,0f,13.0f));
        Taburete(j,"BTab_2",new Vector3(14.2f,0f,13.0f));
        Taburete(j,"BTab_3",new Vector3(15.4f,0f,13.0f));
        Taburete(j,"BTab_4",new Vector3(16.6f,0f,13.0f));
        Taburete(j,"BTab_5",new Vector3(17.8f,0f,13.0f));

        // ── ZONA LOUNGE — esquina derecha frontal (X[19,22], Z[0,6]) ─────
        // 2 sillones
        CuboCol(j,"Lounge_S1",      new Vector3(20.5f,0.42f,2.0f), new Vector3(0.95f,0.44f,0.95f),mTapiz);
        CuboVis(j,"Lounge_S1_R",    new Vector3(20.5f,0.82f,2.45f),new Vector3(0.95f,0.50f,0.16f),mTapiz);
        CuboVis(j,"Lounge_S1_BL",   new Vector3(20.02f,0.55f,2.0f),new Vector3(0.16f,0.28f,0.90f),mTapiz);
        CuboVis(j,"Lounge_S1_BR",   new Vector3(20.98f,0.55f,2.0f),new Vector3(0.16f,0.28f,0.90f),mTapiz);
        PatasCilindro(j, new Vector3(20.5f,0f,2.0f), 0.80f,0.80f, 0.06f,0.20f, mMadera);

        CuboCol(j,"Lounge_S2",      new Vector3(20.5f,0.42f,4.5f), new Vector3(0.95f,0.44f,0.95f),mTapiz);
        CuboVis(j,"Lounge_S2_R",    new Vector3(20.5f,0.82f,4.95f),new Vector3(0.95f,0.50f,0.16f),mTapiz);
        CuboVis(j,"Lounge_S2_BL",   new Vector3(20.02f,0.55f,4.5f),new Vector3(0.16f,0.28f,0.90f),mTapiz);
        CuboVis(j,"Lounge_S2_BR",   new Vector3(20.98f,0.55f,4.5f),new Vector3(0.16f,0.28f,0.90f),mTapiz);
        PatasCilindro(j, new Vector3(20.5f,0f,4.5f), 0.80f,0.80f, 0.06f,0.20f, mMadera);

        // Mesa redonda lounge
        CuboCol(j,"Lounge_Mesa",    new Vector3(20.5f,0.46f,3.3f), new Vector3(0.80f,0.06f,0.80f),mMadera);
        PatasCilindro(j, new Vector3(20.5f,0f,3.3f), 0.70f,0.70f, 0.05f,0.44f, mAcento);
        // Planta lounge
        CuboVis(j,"Lounge_Mac",     new Vector3(20.5f,0.22f,0.8f), new Vector3(0.35f,0.44f,0.35f),mMacetaI);
        EsfVis(j, "Lounge_Planta",  new Vector3(20.5f,0.80f,0.8f), new Vector3(0.65f,0.72f,0.65f),mPlantaI);

        // ── PANEL DECORATIVO ENTRADA (pared X=10.3, Z[6,9]) ──────────────
        CuboVis(j,"Panel_Dec",  new Vector3(10.20f,2.0f,7.5f), new Vector3(0.12f,2.5f,2.5f),mBar);
        CuboVis(j,"Cuadro_J_M",new Vector3(10.19f,2.2f,7.5f), new Vector3(0.08f,1.2f,1.6f),mMadera);
        CuboVis(j,"Cuadro_J_L",new Vector3(10.18f,2.2f,7.5f), new Vector3(0.06f,1.0f,1.4f),mLienzo1);

        // ── LETRERO SALA DE JUEGOS (decorativo, pared frontal) ───────────
        CuboVis(j,"Letrero", new Vector3(16f,3.2f,0.22f), new Vector3(4.0f,0.50f,0.15f),mBar);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FOCOS INTERIORES
    // ═══════════════════════════════════════════════════════════════════════
    static void AgregarFocosInteriores(Transform ir)
    {
        var f = Sub(ir, "Focos_Interiores");

        // SALA
        Foco(f,"Foco_Sala_1",      new Vector3(-4.0f,3.65f,2.0f), new Color(1f,0.92f,0.72f),6f,10f);
        Foco(f,"Foco_Sala_2",      new Vector3(-1.5f,3.65f,5.0f), new Color(1f,0.90f,0.70f),5f, 9f);

        // COCINA
        Foco(f,"Foco_Cocina_1",    new Vector3(5.5f,3.65f,2.0f), new Color(0.95f,0.97f,1.0f),5f,8f);
        Foco(f,"Foco_Cocina_2",    new Vector3(7.5f,3.65f,6.0f), new Color(0.95f,0.97f,1.0f),4f,7f);

        // DORMITORIO
        Foco(f,"Foco_Dorm",        new Vector3(-6.0f,3.65f,12.5f),new Color(1f,0.88f,0.68f),5f,9f);
        Foco(f,"Foco_Mesita_Izq",  new Vector3(-7.2f,1.20f,14.5f),new Color(1f,0.80f,0.50f),2f,3f);
        Foco(f,"Foco_Mesita_Der",  new Vector3(-4.8f,1.20f,14.5f),new Color(1f,0.80f,0.50f),2f,3f);

        // BAÑO
        Foco(f,"Foco_Bano",        new Vector3(0f,3.65f,12.0f),   new Color(0.90f,0.95f,1.0f),4f,8f);

        // ESTUDIO
        Foco(f,"Foco_Estudio",     new Vector3(6.0f,3.65f,12.0f), new Color(0.95f,0.97f,1.0f),5f,9f);

        // PASILLO / ENTRADA (sala → dormitorios)
        Foco(f,"Foco_Pasillo",     new Vector3(0f,3.65f,8.5f),    new Color(1f,0.93f,0.78f),3f,5f);

        // SALA DE JUEGOS
        Foco(f,"Foco_Juegos_C",    new Vector3(16f,3.65f,7.0f),   new Color(1f,0.95f,0.85f),8f,16f);
        Foco(f,"Foco_Billar",      new Vector3(13.5f,3.0f,4.5f),  new Color(0.95f,0.97f,1.0f),5f,5f);
        Foco(f,"Foco_Futbolin",    new Vector3(17f,3.0f,9.5f),    new Color(0.95f,0.97f,1.0f),4f,5f);
        Foco(f,"Foco_Bar",         new Vector3(16f,3.0f,13.0f),   new Color(1f,0.85f,0.60f),5f,8f);
        Foco(f,"Foco_Lounge",      new Vector3(20.5f,3.0f,3.3f),  new Color(1f,0.88f,0.68f),3f,6f);
        Foco(f,"Foco_Dardos",      new Vector3(20f,3.0f,3.0f),    new Color(0.95f,0.97f,1.0f),3f,4f);
    }

    static void Foco(Transform p, string nombre, Vector3 pos, Color color, float intensity, float range)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;

        var luz = go.AddComponent<Light>();
        luz.type             = LightType.Point;
        luz.color            = color;
        luz.intensity        = intensity;
        luz.range            = range;
        luz.shadows          = LightShadows.Soft;
        luz.shadowStrength   = 0.55f;
        luz.shadowNormalBias = 0.4f;

        string matPath = MAT_DIR + "Int_Foco_" + nombre + ".mat";
        Material mBulb = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mBulb == null)
        {
            mBulb = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Foco" };
            AssetDatabase.CreateAsset(mBulb, matPath);
        }
        mBulb.SetColor("_BaseColor",     Color.white);
        mBulb.SetColor("_EmissionColor", color * intensity * 0.5f);
        mBulb.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(mBulb);

        var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.name = nombre + "_Bombilla";
        bulb.transform.SetParent(go.transform, false);
        bulb.transform.localPosition = Vector3.zero;
        bulb.transform.localScale    = new Vector3(0.10f, 0.10f, 0.10f);
        bulb.GetComponent<Renderer>().sharedMaterial = mBulb;
        Object.DestroyImmediate(bulb.GetComponent<SphereCollider>());

        CilVis(go.transform, nombre + "_Base",
               new Vector3(0f, 0.07f, 0f), new Vector3(0.06f, 0.14f, 0.06f), mAcento);
        EditorUtility.SetDirty(go);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PLAYER FPS
    // ═══════════════════════════════════════════════════════════════════════
    static void CrearPlayer()
    {
        var playerGO = new GameObject("Player");
        playerGO.transform.position = new Vector3(0f, 0.9f, -3.5f);
        playerGO.tag = "Player";

        var cc = playerGO.AddComponent<CharacterController>();
        cc.height     = 1.80f;
        cc.radius     = 0.35f;
        cc.center     = new Vector3(0f, 0.90f, 0f);
        cc.stepOffset = 0.40f;
        cc.slopeLimit = 50f;

        var rb = playerGO.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        GameObject camGO;
        var camExistente = Camera.main;
        if (camExistente != null)
        {
            camGO = camExistente.gameObject;
            camGO.transform.SetParent(playerGO.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camGO.transform.localRotation = Quaternion.identity;
            camExistente.enabled          = true;
            camExistente.fieldOfView      = 75f;
            camExistente.nearClipPlane    = 0.08f;
        }
        else
        {
            camGO = new GameObject("PlayerCamera");
            camGO.transform.SetParent(playerGO.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var cam           = camGO.AddComponent<Camera>();
            cam.tag           = "MainCamera";
            cam.fieldOfView   = 75f;
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane  = 300f;
        }

        var ctrl = AddCompReflection(playerGO, "PlayerController");
        if (ctrl != null)
        {
            var so = new SerializedObject(ctrl);
            var camProp = so.FindProperty("camara");
            if (camProp != null) camProp.objectReferenceValue = camGO.transform;
            so.FindProperty("velocidad").floatValue       = 4.5f;
            so.FindProperty("velocidadCorrer").floatValue = 8.0f;
            so.FindProperty("sensibilidad").floatValue    = 0.15f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorUtility.SetDirty(playerGO);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════
    static Transform Sub(Transform p, string n)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        return go.transform;
    }

    static void CuboCol(Transform p, string n, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = n; go.transform.SetParent(p, false);
        go.transform.localPosition = pos; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    static void CuboVis(Transform p, string n, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = n; go.transform.SetParent(p, false);
        go.transform.localPosition = pos; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
    }

    static void CilVis(Transform p, string n, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = n; go.transform.SetParent(p, false);
        go.transform.localPosition = pos; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
    }

    static void EsfVis(Transform p, string n, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = n; go.transform.SetParent(p, false);
        go.transform.localPosition = pos; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<SphereCollider>());
    }

    static void Colis(Transform p, string n, Vector3 pos, Vector3 scale)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.AddComponent<BoxCollider>();
    }

    static void PatasCilindro(Transform p, Vector3 centro, float anchoX, float profZ,
                               float radio, float altura, Material m)
    {
        muebleIdx++;
        string pre = "Pata_" + muebleIdx;
        float hx = anchoX / 2f - 0.08f, hz = profZ / 2f - 0.08f;
        Vector3[] off = {
            new Vector3(-hx,altura/2f, hz), new Vector3(hx,altura/2f, hz),
            new Vector3(-hx,altura/2f,-hz), new Vector3(hx,altura/2f,-hz)
        };
        for (int i=0; i<4; i++)
            CilVis(p, pre+"_"+i, centro+off[i], new Vector3(radio*2f,altura,radio*2f), m);
    }

    static void Taburete(Transform p, string n, Vector3 base_)
    {
        var t = Sub(p, n);
        CuboCol(t,"Asiento", base_+new Vector3(0f,0.72f,0f), new Vector3(0.38f,0.05f,0.38f),mTapiz);
        CilVis(t, "Ped",     base_+new Vector3(0f,0.35f,0f), new Vector3(0.06f,0.70f,0.06f),mAcento);
        CuboVis(t,"Base",    base_+new Vector3(0f,0.04f,0f), new Vector3(0.30f,0.08f,0.30f),mAcento);
    }

    static void Silla(Transform p, string n, Vector3 base_, float rotY)
    {
        var s = Sub(p, n);
        var asiento = GameObject.CreatePrimitive(PrimitiveType.Cube);
        asiento.name = "Asiento";
        asiento.transform.SetParent(s, false);
        asiento.transform.localPosition = base_ + new Vector3(0f,0.46f,0f);
        asiento.transform.localScale    = new Vector3(0.44f,0.05f,0.44f);
        asiento.transform.localEulerAngles = new Vector3(0f,rotY,0f);
        asiento.GetComponent<Renderer>().sharedMaterial = mTapiz;

        float bz = rotY == 0 ? 0.22f : -0.22f;
        CuboVis(s,"Respaldo", base_+new Vector3(0f,0.76f,bz), new Vector3(0.44f,0.55f,0.05f),mMadera);
        float hx=0.17f, hz=0.17f;
        Vector3[] pts = { base_+new Vector3(-hx,0.22f, hz), base_+new Vector3(hx,0.22f, hz),
                          base_+new Vector3(-hx,0.22f,-hz), base_+new Vector3(hx,0.22f,-hz) };
        for (int i=0; i<4; i++) CilVis(s,"Pata_"+i,pts[i],new Vector3(0.04f,0.44f,0.04f),mMadera);
    }

    static Component AddCompReflection(GameObject go, string typeName)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return go.AddComponent(t);
        }
        Debug.LogWarning("[ArqViz] Tipo no encontrado: " + typeName);
        return null;
    }
}
