using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Construye el interior de la casa + player FPS.
/// Requiere haber ejecutado primero "ArqViz > Construir Casa Moderna".
/// Menú: ArqViz > Construir Interior y Player
/// </summary>
public static class ArqVizInteriorBuilder
{
    const string CASA_ROOT  = "Casa_Moderna_ArqViz";
    const string MAT_DIR    = "Assets/Materials/Interior/";

    // ─── Paleta minimalista interior ────────────────────────────────────────
    static readonly Color C_PISO     = new Color(0.73f, 0.56f, 0.33f); // madera roble
    static readonly Color C_PARED_IN = new Color(0.97f, 0.97f, 0.96f); // blanco roto
    static readonly Color C_TECHO_IN = new Color(1.00f, 1.00f, 1.00f); // blanco puro
    static readonly Color C_MADERA   = new Color(0.22f, 0.15f, 0.09f); // nogal oscuro
    static readonly Color C_TAPIZ    = new Color(0.80f, 0.78f, 0.74f); // gris cálido
    static readonly Color C_ACENTO   = new Color(0.12f, 0.12f, 0.12f); // negro mate
    static readonly Color C_COCINA   = new Color(0.88f, 0.87f, 0.85f); // gris perla
    static readonly Color C_COLCHA   = new Color(0.95f, 0.93f, 0.88f); // crema
    static readonly Color C_VIDRIO_I = new Color(0.50f, 0.70f, 0.90f, 0.25f); // vidrio
    static readonly Color C_LIENZO_1 = new Color(0.60f, 0.40f, 0.25f); // cuadro cálido
    static readonly Color C_LIENZO_2 = new Color(0.25f, 0.35f, 0.55f); // cuadro frío
    static readonly Color C_PLANTA_I = new Color(0.18f, 0.42f, 0.16f); // follaje
    static readonly Color C_MACETA_I = new Color(0.60f, 0.55f, 0.48f); // terracota

    static Material mPiso, mParedIn, mTechoIn, mMadera, mTapiz, mAcento,
                    mCocina, mColcha, mVidrioI, mLienzo1, mLienzo2,
                    mPlantaI, mMacetaI;

    static Transform casaT; // transform raíz de la casa
    static int muebleIdx = 0;

    // ═══════════════════════════════════════════════════════════════════════
    [MenuItem("ArqViz/Construir Interior y Player")]
    public static void ConstruirInterior()
    {
        var casaGO = GameObject.Find(CASA_ROOT);
        if (casaGO == null)
        {
            EditorUtility.DisplayDialog("ArqViz", "Primero ejecuta 'Construir Casa Moderna'.", "OK");
            return;
        }
        casaT = casaGO.transform;
        muebleIdx = 0;

        // Limpiar interior anterior si existe
        var intAnterior = casaT.Find("Interior");
        if (intAnterior != null) Object.DestroyImmediate(intAnterior.gameObject);
        var playerAnterior = GameObject.FindGameObjectWithTag("Player");
        if (playerAnterior != null) Object.DestroyImmediate(playerAnterior);

        CrearMateriales();

        var interiorRoot = new GameObject("Interior");
        interiorRoot.transform.SetParent(casaT, false);
        Transform ir = interiorRoot.transform;

        AgregarColisionadoresEstructurales(ir);
        ConstruirPisosYTechos(ir);
        ConstruirParedes(ir);
        ConfigurarPuerta();
        ConstruirSala(ir);
        ConstruirDormitorio(ir);
        ConstruirCocina(ir);
        AgregarFocosInteriores(ir);
        CrearPlayer();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("ArqViz",
            "¡Interior listo!\n\n" +
            "• WASD para moverse\n" +
            "• Mouse para mirar\n" +
            "• Shift para correr\n" +
            "• Escape para desbloquear cursor\n\n" +
            "Presiona Play para explorar.",
            "OK");
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

        mPiso     = Mat("Piso",     C_PISO,     0.00f, 0.45f);
        mParedIn  = Mat("ParedInt", C_PARED_IN, 0.00f, 0.15f);
        mTechoIn  = Mat("TechoInt", C_TECHO_IN, 0.00f, 0.10f);
        mMadera   = Mat("Madera",   C_MADERA,   0.00f, 0.30f);
        mTapiz    = Mat("Tapiz",    C_TAPIZ,    0.00f, 0.10f);
        mAcento   = Mat("Acento",   C_ACENTO,   0.10f, 0.60f);
        mCocina   = Mat("Cocina",   C_COCINA,   0.05f, 0.50f);
        mColcha   = Mat("Colcha",   C_COLCHA,   0.00f, 0.05f);
        mVidrioI  = Mat("VidrioI",  C_VIDRIO_I, 0.05f, 0.90f, transp: true);
        mLienzo1  = Mat("Lienzo1",  C_LIENZO_1, 0.00f, 0.05f);
        mLienzo2  = Mat("Lienzo2",  C_LIENZO_2, 0.00f, 0.05f);
        mPlantaI  = Mat("PlantaI",  C_PLANTA_I, 0.00f, 0.05f);
        mMacetaI  = Mat("MacetaI",  C_MACETA_I, 0.00f, 0.20f);

        AssetDatabase.SaveAssets();
    }

    static Material Mat(string n, Color c, float metal, float smooth, bool transp = false)
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
        if (transp)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend",   0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COLISIONADORES ESTRUCTURALES (invisibles, perimetro exterior)
    // ═══════════════════════════════════════════════════════════════════════
    static void AgregarColisionadoresEstructurales(Transform ir)
    {
        var col = new GameObject("Colisionadores_Perim");
        col.transform.SetParent(ir, false);
        Transform c = col.transform;

        // Pared izquierda exterior
        Colis(c, "Col_PIzq",    new Vector3(-5.10f, 2f, 4f),   new Vector3(0.6f, 5f, 8.5f));
        // Pared derecha exterior (bloque principal)
        Colis(c, "Col_PDer",    new Vector3( 5.10f, 2f, 4f),   new Vector3(0.6f, 5f, 8.5f));
        // Pared trasera exterior
        Colis(c, "Col_PTras",   new Vector3( 0f, 2f, 8.30f),   new Vector3(11f,  5f, 0.6f));
        // Pared frontal izquierda (a la izquierda de las ventanas)
        Colis(c, "Col_PF_Izq",  new Vector3(-3.80f, 2f, -0.20f), new Vector3(2.4f, 5f, 0.6f));
        // Pared frontal derecha
        Colis(c, "Col_PF_Der",  new Vector3( 3.80f, 2f, -0.20f), new Vector3(2.4f, 5f, 0.6f));
        // Viga sobre puerta y ventanas (bloquea saltar por arriba)
        Colis(c, "Col_Viga_F",  new Vector3( 0f, 3.60f, -0.20f), new Vector3(5f, 1f, 0.6f));
        // Bajo ventana izquierda
        Colis(c, "Col_BVizq",   new Vector3(-2.80f, 0.50f, -0.20f), new Vector3(2.5f, 1f, 0.6f));
        // Bajo ventana derecha
        Colis(c, "Col_BVder",   new Vector3( 2.80f, 0.50f, -0.20f), new Vector3(2.5f, 1f, 0.6f));
        // Ala lateral derecha - pared derecha
        Colis(c, "Col_Ala_D",   new Vector3(11.25f, 1.5f, 3.5f), new Vector3(0.6f, 4f, 7.5f));
        // Ala lateral - frente
        Colis(c, "Col_Ala_F",   new Vector3(8f, 1.5f, -0.20f),   new Vector3(6f, 4f, 0.6f));
        // Ala lateral - trasera
        Colis(c, "Col_Ala_T",   new Vector3(8f, 1.5f, 7.10f),    new Vector3(6f, 4f, 0.6f));

        // Escalones exteriores (agregar colisionador a los existentes)
        AgregarColisionadorExistente("Escalon_1");
        AgregarColisionadorExistente("Escalon_2");
        AgregarColisionadorExistente("Plataforma");
        AgregarColisionadorExistente("Suelo_Frente");
        AgregarColisionadorExistente("Suelo_Cesped");
    }

    static void AgregarColisionadorExistente(string nombre)
    {
        // Buscar en toda la jerarquía de la casa
        var tr = casaT.Find(nombre);
        if (tr == null)
        {
            // Búsqueda profunda
            var all = casaT.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
                if (t.name == nombre) { tr = t; break; }
        }
        if (tr != null && tr.GetComponent<Collider>() == null)
            tr.gameObject.AddComponent<BoxCollider>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PISOS Y TECHOS INTERIORES
    // ═══════════════════════════════════════════════════════════════════════
    static void ConstruirPisosYTechos(Transform ir)
    {
        // Piso interior (elevado a y=0.10 → sin Z-fighting con el cimiento en y=-0.05)
        CuboCol(ir, "Piso_Interior",   new Vector3(0f,   0.10f, 4f),   new Vector3(9.4f, 0.20f, 7.7f), mPiso);
        // Piso ala derecha
        CuboCol(ir, "Piso_Ala",        new Vector3(8f,   0.10f, 3.5f), new Vector3(5.5f, 0.20f, 7f),   mPiso);
        // El techo interior lo provee el CasaBuilder (Techo_Interior) — no duplicar
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PAREDES INTERIORES (divisiones entre habitaciones)
    // ═══════════════════════════════════════════════════════════════════════
    static void ConstruirParedes(Transform ir)
    {
        // Pared divisoria sala / zona trasera (z=4)
        // Tiene un vano de puerta al centro: x=-0.7 a 0.7, y=0 a 2.2
        // Pieza izquierda
        CuboCol(ir, "Pared_Div_Izq", new Vector3(-2.85f, 2f, 4f), new Vector3(3.7f, 4f, 0.18f), mParedIn);
        // Pieza derecha
        CuboCol(ir, "Pared_Div_Der", new Vector3( 2.65f, 2f, 4f), new Vector3(3.7f, 4f, 0.18f), mParedIn);
        // Dintel sobre el vano (arriba de la puerta interior)
        CuboCol(ir, "Pared_Div_Top", new Vector3( 0f, 3.30f, 4f), new Vector3(1.4f, 1.4f, 0.18f), mParedIn);

        // Pared divisoria dormitorio / cocina (x=1.8, de z=4 a z=7.85)
        CuboCol(ir, "Pared_DormCoc", new Vector3(1.8f, 2f, 5.9f), new Vector3(0.18f, 4f, 3.85f), mParedIn);

        // Marco de la puerta interior (decorativo, sin colisionador)
        CuboVis(ir, "Marco_PInt_L", new Vector3(-0.75f, 1.1f, 4f), new Vector3(0.10f, 2.2f, 0.22f), mMadera);
        CuboVis(ir, "Marco_PInt_R", new Vector3( 0.75f, 1.1f, 4f), new Vector3(0.10f, 2.2f, 0.22f), mMadera);
        CuboVis(ir, "Marco_PInt_T", new Vector3( 0f, 2.30f, 4f),   new Vector3(1.60f, 0.10f, 0.22f), mMadera);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PUERTA EXTERIOR — configurar pivote + DoorController
    // ═══════════════════════════════════════════════════════════════════════
    static void ConfigurarPuerta()
    {
        // Buscar hoja existente
        Transform hoja = null;
        var all = casaT.GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
            if (t.name == "Puerta_Hoja") { hoja = t; break; }

        if (hoja == null) { Debug.LogWarning("[ArqViz] Puerta_Hoja no encontrada."); return; }

        // Eliminar pivot anterior si existe
        var pivotViejo = casaT.Find("Puerta_Pivot");
        if (pivotViejo != null) Object.DestroyImmediate(pivotViejo.gameObject);

        // Crear pivot en el borde bisagra (lado izquierdo de la puerta)
        // Puerta está en localPos (0, 1.1, -0.05), ancho 1.2 → bisagra en x=-0.6
        var pivotGO = new GameObject("Puerta_Pivot");
        pivotGO.transform.SetParent(casaT, false);
        pivotGO.transform.localPosition = new Vector3(-0.60f, 0f, -0.05f);

        // Reparentar hoja al pivot (mantener posición mundial)
        hoja.SetParent(pivotGO.transform, true);
        // Ajustar localPos relativa al pivot
        hoja.localPosition = new Vector3(0.60f, 1.10f, 0f);
        hoja.localRotation = Quaternion.identity;
        hoja.localScale    = new Vector3(1.2f, 2.2f, 0.1f);

        // Agregar BoxCollider a la hoja (para bloquear al player)
        if (hoja.GetComponent<BoxCollider>() == null)
            hoja.gameObject.AddComponent<BoxCollider>();

        // Trigger de detección (hijo del pivot, frente a la puerta)
        var trigGO = new GameObject("Puerta_Trigger");
        trigGO.transform.SetParent(pivotGO.transform, false);
        trigGO.transform.localPosition = new Vector3(0.60f, 1.50f, -1.80f);
        var bc = trigGO.AddComponent<BoxCollider>();
        bc.size      = new Vector3(3.5f, 3.5f, 3.0f);
        bc.isTrigger = true;

        // Agregar DoorController al pivot (via reflexión para evitar dependencia de assembly)
        var dc = AddCompReflection(pivotGO, "DoorController");
        if (dc != null)
        {
            var so = new SerializedObject(dc);
            so.FindProperty("anguloApertura").floatValue    = 90f;
            so.FindProperty("velocidadApertura").floatValue = 2.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(pivotGO);
        Debug.Log("[ArqViz] Puerta configurada con pivote y DoorController.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SALA / LIVING ROOM   (z: 0.15 – 4,  x: -4.55 – 4.55)
    // ═══════════════════════════════════════════════════════════════════════
    static void ConstruirSala(Transform ir)
    {
        var sala = Sub(ir, "Sala");

        // ── Sofá de 3 piezas (frente a las ventanas, mirando Z-)  ─────────
        // Asiento
        CuboCol(sala, "Sofa_Asiento",   new Vector3(-0.5f, 0.42f, 2.60f), new Vector3(2.60f, 0.44f, 0.95f), mTapiz);
        // Respaldo
        CuboVis(sala, "Sofa_Respaldo",  new Vector3(-0.5f, 0.82f, 3.04f), new Vector3(2.60f, 0.50f, 0.16f), mTapiz);
        // Brazo izquierdo
        CuboVis(sala, "Sofa_Brazo_Izq", new Vector3(-1.85f, 0.55f, 2.60f), new Vector3(0.16f, 0.28f, 0.95f), mTapiz);
        // Brazo derecho
        CuboVis(sala, "Sofa_Brazo_Der", new Vector3( 0.85f, 0.55f, 2.60f), new Vector3(0.16f, 0.28f, 0.95f), mTapiz);
        // Patas (4 cilindros)
        PatasCilindro(sala, new Vector3(-0.5f, 0f, 2.60f), 2.40f, 0.80f, 0.10f, 0.20f, mMadera);
        // Cojines decorativos
        CuboVis(sala, "Sofa_Cojin_1", new Vector3(-1.1f, 0.68f, 2.72f), new Vector3(0.40f, 0.28f, 0.40f), mColcha);
        CuboVis(sala, "Sofa_Cojin_2", new Vector3(-0.5f, 0.68f, 2.72f), new Vector3(0.40f, 0.28f, 0.40f), mColcha);
        CuboVis(sala, "Sofa_Cojin_3", new Vector3( 0.1f, 0.68f, 2.72f), new Vector3(0.40f, 0.28f, 0.40f), mColcha);

        // ── Mesa de centro ────────────────────────────────────────────────
        CuboCol(sala, "Mesa_Centro_Top",  new Vector3(-0.5f, 0.46f, 1.70f), new Vector3(1.10f, 0.05f, 0.60f), mMadera);
        PatasCilindro(sala, new Vector3(-0.5f, 0f, 1.70f), 1.00f, 0.50f, 0.06f, 0.45f, mAcento);

        // ── Mueble TV + televisor ─────────────────────────────────────────
        CuboCol(sala, "MuebleTV_Base",    new Vector3(-0.5f, 0.28f, 3.80f), new Vector3(2.20f, 0.55f, 0.50f), mMadera);
        CuboVis(sala, "TV_Pantalla",      new Vector3(-0.5f, 0.90f, 3.87f), new Vector3(1.40f, 0.80f, 0.06f), mAcento);
        CuboVis(sala, "TV_Marco",         new Vector3(-0.5f, 0.90f, 3.83f), new Vector3(1.50f, 0.88f, 0.04f), mMadera);

        // ── Cuadro pared izquierda ────────────────────────────────────────
        CuboVis(sala, "Cuadro_Marco_1",   new Vector3(-4.65f, 2.20f, 2.20f), new Vector3(0.10f, 0.90f, 1.30f), mMadera);
        CuboVis(sala, "Cuadro_Lienzo_1",  new Vector3(-4.62f, 2.20f, 2.20f), new Vector3(0.05f, 0.78f, 1.18f), mLienzo1);

        // ── Cuadro pequeño pared divisoria ────────────────────────────────
        CuboVis(sala, "Cuadro_Marco_2",   new Vector3(-3.50f, 2.40f, 4.06f), new Vector3(0.06f, 0.65f, 0.50f), mMadera);
        CuboVis(sala, "Cuadro_Lienzo_2",  new Vector3(-3.50f, 2.40f, 4.09f), new Vector3(0.04f, 0.55f, 0.40f), mLienzo2);

        // ── Planta de piso (esquina izquierda frontal) ───────────────────
        CuboVis(sala, "Planta_Maceta",    new Vector3(-4.10f, 0.20f, 0.60f), new Vector3(0.35f, 0.40f, 0.35f), mMacetaI);
        EsfVis(sala,  "Planta_Hoja_1",    new Vector3(-4.10f, 0.72f, 0.60f), new Vector3(0.65f, 0.75f, 0.65f), mPlantaI);
        EsfVis(sala,  "Planta_Hoja_2",    new Vector3(-3.88f, 0.60f, 0.50f), new Vector3(0.35f, 0.40f, 0.35f), mPlantaI);

        // ── Lámpara de piso ───────────────────────────────────────────────
        CuboVis(sala, "Lamp_Base",        new Vector3( 1.30f, 0.07f, 2.90f), new Vector3(0.22f, 0.14f, 0.22f), mAcento);
        CilVis(sala,  "Lamp_Tallo",       new Vector3( 1.30f, 0.95f, 2.90f), new Vector3(0.04f, 1.70f, 0.04f), mAcento);
        CuboVis(sala, "Lamp_Pantalla",    new Vector3( 1.30f, 1.90f, 2.90f), new Vector3(0.30f, 0.38f, 0.30f), mColcha);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DORMITORIO   (z: 4 – 7.85,  x: -4.55 – 1.8)
    // ═══════════════════════════════════════════════════════════════════════
    static void ConstruirDormitorio(Transform ir)
    {
        var dorm = Sub(ir, "Dormitorio");

        // ── Cama doble ────────────────────────────────────────────────────
        // Base / somier
        CuboCol(dorm, "Cama_Base",    new Vector3(-1.50f, 0.22f, 6.60f), new Vector3(1.90f, 0.44f, 2.20f), mMadera);
        // Colchon
        CuboVis(dorm, "Cama_Colch",   new Vector3(-1.50f, 0.47f, 6.60f), new Vector3(1.80f, 0.14f, 2.10f), mColcha);
        // Cabecero
        CuboVis(dorm, "Cama_Cab",     new Vector3(-1.50f, 0.85f, 7.60f), new Vector3(1.90f, 0.80f, 0.12f), mMadera);
        // Almohadas
        CuboVis(dorm, "Almoh_1",      new Vector3(-2.05f, 0.56f, 7.18f), new Vector3(0.65f, 0.12f, 0.45f), mColcha);
        CuboVis(dorm, "Almoh_2",      new Vector3(-0.95f, 0.56f, 7.18f), new Vector3(0.65f, 0.12f, 0.45f), mColcha);
        // Pie de cama
        CuboVis(dorm, "Pie_Cama",     new Vector3(-1.50f, 0.35f, 5.55f), new Vector3(1.90f, 0.15f, 0.14f), mMadera);

        // ── Mesitas de noche ──────────────────────────────────────────────
        CuboCol(dorm, "Mesita_Izq",   new Vector3(-2.65f, 0.34f, 6.60f), new Vector3(0.48f, 0.68f, 0.42f), mMadera);
        CuboCol(dorm, "Mesita_Der",   new Vector3(-0.35f, 0.34f, 6.60f), new Vector3(0.48f, 0.68f, 0.42f), mMadera);
        // Lámparas de mesita
        CilVis(dorm,  "Lamp_M_Izq",   new Vector3(-2.65f, 0.84f, 6.60f), new Vector3(0.05f, 0.42f, 0.05f), mAcento);
        CuboVis(dorm, "Lamp_M_Izq_P", new Vector3(-2.65f, 1.07f, 6.60f), new Vector3(0.20f, 0.22f, 0.20f), mColcha);
        CilVis(dorm,  "Lamp_M_Der",   new Vector3(-0.35f, 0.84f, 6.60f), new Vector3(0.05f, 0.42f, 0.05f), mAcento);
        CuboVis(dorm, "Lamp_M_Der_P", new Vector3(-0.35f, 1.07f, 6.60f), new Vector3(0.20f, 0.22f, 0.20f), mColcha);

        // ── Armario / Closet ──────────────────────────────────────────────
        CuboCol(dorm, "Armario",      new Vector3(-3.95f, 1.30f, 5.30f), new Vector3(1.20f, 2.60f, 0.58f), mMadera);
        // Puertas del armario (decorativas)
        CuboVis(dorm, "Arm_Puerta_1", new Vector3(-4.22f, 1.30f, 5.08f), new Vector3(0.04f, 2.35f, 0.55f), mColcha);
        CuboVis(dorm, "Arm_Puerta_2", new Vector3(-3.68f, 1.30f, 5.08f), new Vector3(0.04f, 2.35f, 0.55f), mColcha);
        // Tirador
        CuboVis(dorm, "Arm_Tiradores",new Vector3(-3.95f, 1.30f, 4.79f), new Vector3(0.45f, 0.04f, 0.04f), mAcento);

        // ── Cuadro pared trasera dormitorio ───────────────────────────────
        CuboVis(dorm, "Cuadro_Dorm_M", new Vector3(-1.50f, 2.30f, 7.90f), new Vector3(0.08f, 0.75f, 1.00f), mMadera);
        CuboVis(dorm, "Cuadro_Dorm_L", new Vector3(-1.50f, 2.30f, 7.93f), new Vector3(0.05f, 0.63f, 0.88f), mLienzo2);

        // ── Planta en esquina ─────────────────────────────────────────────
        CuboVis(dorm, "Dorm_Mac",     new Vector3(-4.10f, 0.22f, 7.50f), new Vector3(0.30f, 0.44f, 0.30f), mMacetaI);
        EsfVis(dorm,  "Dorm_Planta",  new Vector3(-4.10f, 0.72f, 7.50f), new Vector3(0.60f, 0.70f, 0.60f), mPlantaI);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COCINA / COMEDOR   (z: 4 – 7.85,  x: 1.8 – 4.55)
    // ═══════════════════════════════════════════════════════════════════════
    static void ConstruirCocina(Transform ir)
    {
        var coc = Sub(ir, "Cocina");

        // ── Mueble bajo + encimera ────────────────────────────────────────
        CuboCol(coc, "Mueble_Bajo",   new Vector3(3.80f, 0.47f, 6.20f), new Vector3(1.30f, 0.94f, 0.58f), mMadera);
        CuboVis(coc, "Encimera",      new Vector3(3.80f, 0.96f, 6.20f), new Vector3(1.35f, 0.05f, 0.62f), mCocina);
        // Mueble bajo trasero
        CuboCol(coc, "Mueble_Bajo_T", new Vector3(3.40f, 0.47f, 7.60f), new Vector3(2.10f, 0.94f, 0.50f), mMadera);
        CuboVis(coc, "Encimera_T",    new Vector3(3.40f, 0.96f, 7.60f), new Vector3(2.15f, 0.05f, 0.54f), mCocina);

        // ── Mueble alto de cocina (pared derecha) ─────────────────────────
        CuboCol(coc, "Mueble_Alto",   new Vector3(4.40f, 2.10f, 5.80f), new Vector3(0.25f, 2.60f, 1.40f), mMadera);
        CuboVis(coc, "Mueb_Alto_P1",  new Vector3(4.28f, 2.40f, 5.30f), new Vector3(0.04f, 1.10f, 0.62f), mCocina);
        CuboVis(coc, "Mueb_Alto_P2",  new Vector3(4.28f, 2.40f, 6.30f), new Vector3(0.04f, 1.10f, 0.62f), mCocina);

        // ── Mesa de comedor + sillas ──────────────────────────────────────
        // Mesa
        CuboCol(coc, "Mesa_Com_Top",  new Vector3(3.00f, 0.78f, 5.00f), new Vector3(1.00f, 0.05f, 0.65f), mMadera);
        PatasCilindro(coc, new Vector3(3.00f, 0f, 5.00f), 0.85f, 0.55f, 0.05f, 0.77f, mMadera);

        // Silla 1 (frente, z-)
        Silla(coc, "Silla_1", new Vector3(3.00f, 0f, 4.30f), 0f);
        // Silla 2 (atrás, z+)
        Silla(coc, "Silla_2", new Vector3(3.00f, 0f, 5.70f), 180f);

        // ── Repisa decorativa en pared divisoria ──────────────────────────
        CuboVis(coc, "Repisa_Tabla",  new Vector3(1.95f, 1.60f, 4.90f), new Vector3(0.06f, 0.06f, 0.80f), mMadera);
        // Objetos en repisa (cajas decorativas)
        CuboVis(coc, "Repisa_Obj_1",  new Vector3(1.95f, 1.68f, 4.70f), new Vector3(0.05f, 0.16f, 0.16f), mCocina);
        CuboVis(coc, "Repisa_Obj_2",  new Vector3(1.95f, 1.68f, 5.00f), new Vector3(0.05f, 0.22f, 0.14f), mMadera);
        CuboVis(coc, "Repisa_Obj_3",  new Vector3(1.95f, 1.68f, 5.20f), new Vector3(0.05f, 0.12f, 0.12f), mLienzo1);

        // ── Planta sobre encimera ─────────────────────────────────────────
        CuboVis(coc, "Coc_Mac",       new Vector3(3.80f, 1.02f, 5.90f), new Vector3(0.18f, 0.22f, 0.18f), mMacetaI);
        EsfVis(coc,  "Coc_Planta",    new Vector3(3.80f, 1.28f, 5.90f), new Vector3(0.32f, 0.36f, 0.32f), mPlantaI);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PLAYER FPS
    // ═══════════════════════════════════════════════════════════════════════
    static void CrearPlayer()
    {
        var playerGO = new GameObject("Player");
        playerGO.transform.position = new Vector3(0f, 0.9f, -3.5f);
        playerGO.tag = "Player";

        // CharacterController
        var cc = playerGO.AddComponent<CharacterController>();
        cc.height     = 1.80f;
        cc.radius     = 0.35f;
        cc.center     = new Vector3(0f, 0.90f, 0f);
        cc.stepOffset = 0.40f;
        cc.slopeLimit = 50f;

        // Rigidbody kinematic (necesario para triggers)
        var rb = playerGO.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // ── Reusar la Main Camera existente en lugar de crear una nueva ──
        // Evita el problema de "No cameras rendering"
        GameObject camGO;
        var camExistente = Camera.main;

        if (camExistente != null)
        {
            // Adoptar la cámara existente: hacerla hija del player
            camGO = camExistente.gameObject;
            camGO.transform.SetParent(playerGO.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camGO.transform.localRotation = Quaternion.identity;
            camExistente.enabled          = true;
            camExistente.fieldOfView      = 75f;
            camExistente.nearClipPlane    = 0.08f;
            Debug.Log("[ArqViz] Main Camera reusada como cámara FPS.");
        }
        else
        {
            // Si no hay cámara en la escena, crear una
            camGO = new GameObject("PlayerCamera");
            camGO.transform.SetParent(playerGO.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var cam          = camGO.AddComponent<Camera>();
            cam.tag          = "MainCamera";
            cam.fieldOfView  = 75f;
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane  = 200f;
            Debug.Log("[ArqViz] Nueva PlayerCamera creada.");
        }

        // Script de control (via reflexión para evitar CS0246)
        var ctrl = AddCompReflection(playerGO, "PlayerController");
        if (ctrl != null)
        {
            var so = new SerializedObject(ctrl);
            var camProp = so.FindProperty("camara");
            if (camProp != null) camProp.objectReferenceValue = camGO.transform;
            so.FindProperty("velocidad").floatValue       = 4.0f;
            so.FindProperty("velocidadCorrer").floatValue = 7.0f;
            so.FindProperty("sensibilidad").floatValue    = 0.15f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(playerGO);
        Debug.Log("[ArqViz] Player listo en " + playerGO.transform.position);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    static Transform Sub(Transform parent, string nombre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    // Cubo CON BoxCollider
    static void CuboCol(Transform p, string n, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = n;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        // BoxCollider se mantiene (CreatePrimitive lo agrega por defecto)
    }

    // Cubo SIN BoxCollider (decorativo)
    static void CuboVis(Transform p, string n, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = n;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
    }

    static void CilVis(Transform p, string n, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = n;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
    }

    static void EsfVis(Transform p, string n, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = n;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<SphereCollider>());
    }

    // Colisionador invisible (sin renderer)
    static void Colis(Transform p, string n, Vector3 pos, Vector3 scale)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.AddComponent<BoxCollider>();
    }

    // Crea 4 patas cilíndricas para muebles
    static void PatasCilindro(Transform p, Vector3 centro, float anchoX, float profZ,
                               float radio, float altura, Material m)
    {
        muebleIdx++;
        string pre = "Pata_" + muebleIdx;
        float hx = anchoX / 2f - 0.08f;
        float hz = profZ  / 2f - 0.08f;

        Vector3[] offsets = {
            new Vector3(-hx, altura / 2f,  hz),
            new Vector3( hx, altura / 2f,  hz),
            new Vector3(-hx, altura / 2f, -hz),
            new Vector3( hx, altura / 2f, -hz)
        };
        for (int i = 0; i < 4; i++)
            CilVis(p, pre + "_" + i, centro + offsets[i],
                   new Vector3(radio * 2f, altura, radio * 2f), m);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FOCOS INTERIORES
    // ═══════════════════════════════════════════════════════════════════════
    static void AgregarFocosInteriores(Transform ir)
    {
        var focos = Sub(ir, "Focos_Interiores");

        // SALA — foco cálido central de techo
        Foco(focos, "Foco_Sala_1",       new Vector3(-1.5f, 3.65f, 1.5f),
             new Color(1.00f, 0.92f, 0.72f), intensity: 6f,  range: 9f);
        // SALA — foco secundario lado TV
        Foco(focos, "Foco_Sala_2",       new Vector3( 0.5f, 3.65f, 3.2f),
             new Color(1.00f, 0.90f, 0.70f), intensity: 4f,  range: 7f);

        // DORMITORIO — foco central suave
        Foco(focos, "Foco_Dormitorio",   new Vector3(-2.0f, 3.65f, 6.3f),
             new Color(1.00f, 0.88f, 0.68f), intensity: 5f,  range: 7f);
        // DORMITORIO — apliques de mesita (bajo, cálido)
        Foco(focos, "Foco_Mesita_Izq",   new Vector3(-2.65f, 1.20f, 6.60f),
             new Color(1.00f, 0.80f, 0.50f), intensity: 2f,  range: 2.5f);
        Foco(focos, "Foco_Mesita_Der",   new Vector3(-0.35f, 1.20f, 6.60f),
             new Color(1.00f, 0.80f, 0.50f), intensity: 2f,  range: 2.5f);

        // COCINA — luz blanca neutra de trabajo
        Foco(focos, "Foco_Cocina",       new Vector3( 3.2f, 2.75f, 5.5f),
             new Color(0.95f, 0.97f, 1.00f), intensity: 5f,  range: 6f);

        // ENTRADA — foco sobre la puerta interior
        Foco(focos, "Foco_Entrada",      new Vector3( 0f,   3.65f, 0.6f),
             new Color(1.00f, 0.93f, 0.78f), intensity: 4f,  range: 5f);
    }

    static void Foco(Transform parent, string nombre, Vector3 pos, Color color,
                     float intensity, float range)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        // Luz puntual
        var luz = go.AddComponent<Light>();
        luz.type           = LightType.Point;
        luz.color          = color;
        luz.intensity      = intensity;
        luz.range          = range;
        luz.shadows        = LightShadows.Soft;
        luz.shadowStrength = 0.55f;
        luz.shadowNormalBias = 0.4f;

        // Bombilla visible — esfera pequeña con material emisivo
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

        // Soporte del foco (cilindro pequeño, como accesorio de techo)
        CilVis(go.transform, nombre + "_Base",
               new Vector3(0f, 0.07f, 0f),
               new Vector3(0.06f, 0.14f, 0.06f), mAcento);

        EditorUtility.SetDirty(go);
    }

    // Agrega un componente por nombre usando reflexión (evita dependencia directa de assembly)
    static Component AddCompReflection(GameObject go, string typeName)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return go.AddComponent(t);
        }
        Debug.LogWarning("[ArqViz] Tipo no encontrado en ningún assembly: " + typeName);
        return null;
    }

    // Crea una silla completa
    static void Silla(Transform p, string nombre, Vector3 base_, float rotY)
    {
        var s = Sub(p, nombre);
        s.localPosition = Vector3.zero;

        // Asiento
        var asiento = GameObject.CreatePrimitive(PrimitiveType.Cube);
        asiento.name = "Asiento";
        asiento.transform.SetParent(s, false);
        asiento.transform.localPosition = base_ + new Vector3(0f, 0.46f, 0f);
        asiento.transform.localScale    = new Vector3(0.44f, 0.05f, 0.44f);
        asiento.transform.localEulerAngles = new Vector3(0f, rotY, 0f);
        asiento.GetComponent<Renderer>().sharedMaterial = mTapiz;
        // mantiene collider

        float bz = rotY == 0 ? 0.22f : -0.22f;

        // Respaldo
        CuboVis(s, "Respaldo", base_ + new Vector3(0f, 0.75f, bz),
                new Vector3(0.44f, 0.55f, 0.05f), mMadera);

        // Patas (sin collider para no molestar)
        float hx = 0.17f, hz = 0.17f;
        Vector3[] pts = {
            base_ + new Vector3(-hx, 0.22f,  hz),
            base_ + new Vector3( hx, 0.22f,  hz),
            base_ + new Vector3(-hx, 0.22f, -hz),
            base_ + new Vector3( hx, 0.22f, -hz)
        };
        for (int i = 0; i < 4; i++)
            CilVis(s, "Pata_" + i, pts[i], new Vector3(0.04f, 0.44f, 0.04f), mMadera);
    }
}
