using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Coloca las 3 mecánicas de interacción en la escena.
/// Requiere haber ejecutado "Construir Casa Grande" + "Construir Interior Grande".
/// Menú: ArqViz > Agregar Mecánicas
/// </summary>
public static class ArqVizMecanicasBuilder
{
    const string ROOT_MECANICAS = "Mecanicas_ArqViz";
    const string MAT_DIR        = "Assets/Materials/Mecanicas/";

    // Materiales pad teletransporte
    static readonly Color C_PAD_A = new Color(0.20f, 0.65f, 1.00f, 0.75f);
    static readonly Color C_PAD_B = new Color(1.00f, 0.55f, 0.10f, 0.75f);
    // Colores cajas físicas
    static readonly Color[] C_CAJAS = {
        new Color(0.85f,0.15f,0.15f),
        new Color(0.15f,0.55f,0.85f),
        new Color(0.15f,0.75f,0.25f),
        new Color(0.85f,0.75f,0.10f),
        new Color(0.70f,0.20f,0.80f)
    };

    [MenuItem("ArqViz/Agregar Mecánicas")]
    public static void AgregarMecanicas()
    {
        // Limpiar instancia anterior si existe
        var viejo = GameObject.Find(ROOT_MECANICAS);
        if (viejo != null) Object.DestroyImmediate(viejo);

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Mecanicas"))
            AssetDatabase.CreateFolder("Assets/Materials", "Mecanicas");

        var raiz = new GameObject(ROOT_MECANICAS);
        Transform r = raiz.transform;

        CrearMecanica1_Teleporte(r);
        CrearMecanica2_Fisica(r);
        CrearMecanica3_InfoPaneles(r);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("ArqViz — Mecánicas",
            "¡3 mecánicas instaladas!\n\n" +
            "1) TRIGGER — Pads de teletransporte (azul = entrada → juegos, naranja = regreso)\n\n" +
            "2) FÍSICA — Cajas con Rigidbody en la Sala de Juegos. Acércate y caen. [R] resetea.\n\n" +
            "3) GÉNERO — Paneles de info arquitectónica en cada habitación.\n\n" +
            "Presiona Play para probar. Ctrl+S para guardar.",
            "OK");
    }

    // ══════════════════════════════════════════════════════════════════════
    // MECÁNICA 1 — TRIGGER: TELETRANSPORTE
    // Pad A (azul): entrada → sala de juegos
    // Pad B (naranja): sala de juegos → entrada
    // ══════════════════════════════════════════════════════════════════════
    static void CrearMecanica1_Teleporte(Transform raiz)
    {
        var grp = Sub(raiz, "M1_Teletransporte");

        // ── PAD A: frente a la puerta principal → sala de juegos ──────────
        var padA = Sub(grp, "TeleportPad_Entrada");
        padA.localPosition = Vector3.zero;

        // Visual: disco brillante en el suelo
        var discA = CrearDiscoVisual(padA, "Disc_A", new Vector3(0f, 0.22f, -2.0f),
                                     new Vector3(1.8f, 0.04f, 1.8f), C_PAD_A);
        // Anillo exterior
        var anilloA = CrearDiscoVisual(padA, "Anillo_A", new Vector3(0f, 0.21f, -2.0f),
                                       new Vector3(2.2f, 0.03f, 2.2f),
                                       new Color(C_PAD_A.r, C_PAD_A.g, C_PAD_A.b, 0.4f));

        // Collider trigger
        var padAGO = new GameObject("ZonaTrigger_A");
        padAGO.transform.SetParent(padA, false);
        padAGO.transform.localPosition = new Vector3(0f, 0.5f, -2.0f);
        var bcA = padAGO.AddComponent<BoxCollider>();
        bcA.size      = new Vector3(1.8f, 1.0f, 1.8f);
        bcA.isTrigger = true;

        // Script ZonaTeleporte
        var ztA = AddComp(padAGO, "ZonaTeleporte");
        if (ztA != null)
        {
            var so = new SerializedObject(ztA);
            so.FindProperty("posicionDestino").vector3Value = new Vector3(14f, 1.5f, 7f);
            so.FindProperty("nombreDestino").stringValue    = "Sala de Juegos";
            so.FindProperty("tiempoActivacion").floatValue  = 1.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Flecha / símbolo sobre el pad
        CrearFlechaVisual(padA, new Vector3(0f, 0.55f, -2.0f), C_PAD_A);

        // ── PAD B: sala de juegos → entrada de casa ────────────────────────
        var padB = Sub(grp, "TeleportPad_SalaJuegos");
        padB.localPosition = Vector3.zero;

        CrearDiscoVisual(padB, "Disc_B",   new Vector3(14f, 0.22f, 7f),
                         new Vector3(1.8f, 0.04f, 1.8f), C_PAD_B);
        CrearDiscoVisual(padB, "Anillo_B", new Vector3(14f, 0.21f, 7f),
                         new Vector3(2.2f, 0.03f, 2.2f),
                         new Color(C_PAD_B.r, C_PAD_B.g, C_PAD_B.b, 0.4f));

        var padBGO = new GameObject("ZonaTrigger_B");
        padBGO.transform.SetParent(padB, false);
        padBGO.transform.localPosition = new Vector3(14f, 0.5f, 7f);
        var bcB = padBGO.AddComponent<BoxCollider>();
        bcB.size      = new Vector3(1.8f, 1.0f, 1.8f);
        bcB.isTrigger = true;

        var ztB = AddComp(padBGO, "ZonaTeleporte");
        if (ztB != null)
        {
            var so = new SerializedObject(ztB);
            so.FindProperty("posicionDestino").vector3Value = new Vector3(0f, 1.5f, 2.0f);
            so.FindProperty("nombreDestino").stringValue    = "Entrada Principal";
            so.FindProperty("tiempoActivacion").floatValue  = 1.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        CrearFlechaVisual(padB, new Vector3(14f, 0.55f, 7f), C_PAD_B);

        Debug.Log("[ArqViz] Mecánica 1 (Teletransporte) creada.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // MECÁNICA 2 — FÍSICA: CAJAS CON RIGIDBODY
    // Stack de 5 cajas en la sala de juegos.
    // Al acercarse → caen y se dispersan. [R] resetea.
    // ══════════════════════════════════════════════════════════════════════
    static void CrearMecanica2_Fisica(Transform raiz)
    {
        var grp = Sub(raiz, "M2_Fisica");
        grp.localPosition = Vector3.zero;

        // Posición base del stack (sala de juegos, esquina lounge)
        var stackGO = new GameObject("CajaStack_Fisica");
        stackGO.transform.SetParent(grp, false);
        stackGO.transform.localPosition = Vector3.zero;

        // BoxCollider TRIGGER en el stack (detecta al jugador desde ~3m)
        var trigCol = stackGO.AddComponent<BoxCollider>();
        trigCol.center    = new Vector3(19.5f, 1.0f, 8.0f);
        trigCol.size      = new Vector3(4.5f, 2.5f, 4.5f);
        trigCol.isTrigger = true;

        // Script ObjetoFisico
        var of = AddComp(stackGO, "ObjetoFisico");
        if (of != null)
        {
            var so = new SerializedObject(of);
            so.FindProperty("fuerzaImpulso").floatValue = 3.0f;
            so.FindProperty("permitirReset").boolValue  = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Pirámide de 5 cajas (3 base, 1 centro, 1 cima) ────────────────
        // Base (3 cajas)
        float sz = 0.38f;
        CrearCajaFisica(stackGO.transform, "Caja_0", new Vector3(18.90f, sz/2f+0.21f, 7.80f), sz, C_CAJAS[0]);
        CrearCajaFisica(stackGO.transform, "Caja_1", new Vector3(19.50f, sz/2f+0.21f, 8.30f), sz, C_CAJAS[1]);
        CrearCajaFisica(stackGO.transform, "Caja_2", new Vector3(20.10f, sz/2f+0.21f, 7.80f), sz, C_CAJAS[2]);
        // Fila media (2 cajas)
        CrearCajaFisica(stackGO.transform, "Caja_3", new Vector3(19.20f, sz*1.5f+0.21f, 8.05f), sz, C_CAJAS[3]);
        CrearCajaFisica(stackGO.transform, "Caja_4", new Vector3(19.80f, sz*1.5f+0.21f, 8.05f), sz, C_CAJAS[4]);
        // Cima
        CrearCajaFisica(stackGO.transform, "Caja_5", new Vector3(19.50f, sz*2.5f+0.21f, 8.05f), sz, C_CAJAS[0]);

        // Cartel indicativo (cubo plano sobre la pared)
        var cartel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cartel.name = "Cartel_Fisica";
        cartel.transform.SetParent(grp, false);
        cartel.transform.localPosition = new Vector3(21.8f, 2.0f, 7.8f);
        cartel.transform.localScale    = new Vector3(0.08f, 0.45f, 1.20f);
        var mCartel = MatSimple("CartelFisica", new Color(0.9f,0.75f,0.2f));
        cartel.GetComponent<Renderer>().sharedMaterial = mCartel;
        Object.DestroyImmediate(cartel.GetComponent<BoxCollider>());

        Debug.Log("[ArqViz] Mecánica 2 (Física/Rigidbody) creada.");
    }

    static void CrearCajaFisica(Transform parent, string nombre, Vector3 worldPos,
                                  float size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(parent, false);
        go.transform.position   = worldPos;
        go.transform.localScale = new Vector3(size, size, size);

        // Material
        var m = MatSimple(nombre + "_Mat", color);
        m.SetFloat("_Smoothness", 0.4f);
        go.GetComponent<Renderer>().sharedMaterial = m;

        // Rigidbody — empieza kinematic, ObjetoFisico lo libera
        var rb            = go.AddComponent<Rigidbody>();
        rb.isKinematic    = true;
        rb.mass           = 1.0f;
        rb.linearDamping  = 0.3f;
        rb.angularDamping = 0.3f;
    }

    // ══════════════════════════════════════════════════════════════════════
    // MECÁNICA 3 — GÉNERO: PANELES DE INFORMACIÓN ARQUITECTÓNICA
    // Aparece un panel en pantalla al entrar a cada habitación.
    // ══════════════════════════════════════════════════════════════════════
    static void CrearMecanica3_InfoPaneles(Transform raiz)
    {
        var grp = Sub(raiz, "M3_PanelesInfo");
        grp.localPosition = Vector3.zero;

        // (nombre, area, estilo, descripcion, colorAcento, posición, tamaño collider)
        var rooms = new (string tit, string area, string est, string desc, Color col,
                         Vector3 pos, Vector3 size)[]
        {
            (
                "Sala Principal",
                "104 m²",
                "Moderno Minimalista",
                "Zona de estar con luz natural cenital y ventanas panorámicas.",
                new Color(0.90f,0.75f,0.30f),
                new Vector3(-3.5f, 1.5f, 3.5f),
                new Vector3(12f, 3f, 7f)
            ),
            (
                "Cocina — Comedor",
                "56 m²",
                "Open Plan Contemporáneo",
                "Integración funcional entre cocina, isla y zona de comedor.",
                new Color(0.60f,0.85f,0.95f),
                new Vector3(6.5f, 1.5f, 3.5f),
                new Vector3(6f, 3f, 7f)
            ),
            (
                "Dormitorio Principal",
                "64 m²",
                "Confort Nórdico",
                "Ambiente íntimo con iluminación cálida estratificada por zonas.",
                new Color(0.80f,0.70f,0.95f),
                new Vector3(-6f, 1.5f, 12f),
                new Vector3(7.5f, 3f, 7f)
            ),
            (
                "Baño Completo",
                "32 m²",
                "Spa Minimalista",
                "Bañera, ducha independiente y acabados en blanco azulado.",
                new Color(0.40f,0.88f,0.82f),
                new Vector3(0f, 1.5f, 12f),
                new Vector3(3.8f, 3f, 6.5f)
            ),
            (
                "Estudio / Habitación 2",
                "64 m²",
                "Productividad Moderna",
                "Escritorio, biblioteca y zona de descanso integrados.",
                new Color(0.45f,0.65f,0.95f),
                new Vector3(6f, 1.5f, 12f),
                new Vector3(7f, 3f, 7f)
            ),
            (
                "Sala de Juegos",
                "168 m²",
                "Entretenimiento Total",
                "Billar, futbolín, diana, mini-bar y zona lounge en un solo espacio.",
                new Color(0.30f,0.95f,0.40f),
                new Vector3(16f, 1.5f, 7.5f),
                new Vector3(11f, 3f, 12f)
            ),
        };

        foreach (var rm in rooms)
        {
            var panelGO = new GameObject("Panel_" + rm.tit.Replace(" ", "_").Replace("/","_").Replace("—",""));
            panelGO.transform.SetParent(grp, false);
            panelGO.transform.localPosition = rm.pos;
            panelGO.transform.localScale    = Vector3.one;

            var bc = panelGO.AddComponent<BoxCollider>();
            bc.size      = rm.size;
            bc.isTrigger = true;

            var pi = AddComp(panelGO, "PanelInfo");
            if (pi != null)
            {
                var so = new SerializedObject(pi);
                so.FindProperty("titulo").stringValue      = rm.tit;
                so.FindProperty("area").stringValue        = rm.area;
                so.FindProperty("estilo").stringValue      = rm.est;
                so.FindProperty("descripcion").stringValue = rm.desc;
                so.FindProperty("colorAcento").colorValue  = rm.col;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(panelGO);
        }

        Debug.Log("[ArqViz] Mecánica 3 (Paneles de Información) creada.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS VISUALES
    // ══════════════════════════════════════════════════════════════════════
    static GameObject CrearDiscoVisual(Transform parent, string nombre,
                                        Vector3 worldPos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = nombre;
        go.transform.SetParent(parent, false);
        go.transform.position   = worldPos;
        go.transform.localScale = scale;

        var m = MatSimple(nombre + "_Mat", color);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend",   0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;

        // Emisión para que brille
        Color emCol = new Color(color.r * 2f, color.g * 2f, color.b * 2f);
        m.SetColor("_EmissionColor", emCol);
        m.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(m);

        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        return go;
    }

    static void CrearFlechaVisual(Transform parent, Vector3 worldPos, Color color)
    {
        // Símbolo de flecha hecho con un cubo vertical delgado
        var eje = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eje.name = "FlechaEje";
        eje.transform.SetParent(parent, false);
        eje.transform.position   = worldPos + new Vector3(0f, 0.25f, 0f);
        eje.transform.localScale = new Vector3(0.06f, 0.40f, 0.06f);
        var m = MatSimple("FlechaEje_Mat", color);
        eje.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(eje.GetComponent<BoxCollider>());

        var punta = GameObject.CreatePrimitive(PrimitiveType.Cube);
        punta.name = "FlechaPunta";
        punta.transform.SetParent(parent, false);
        punta.transform.position   = worldPos + new Vector3(0f, 0.52f, 0f);
        punta.transform.localScale = new Vector3(0.18f, 0.14f, 0.18f);
        punta.transform.eulerAngles = new Vector3(45f, 0f, 0f);
        punta.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(punta.GetComponent<BoxCollider>());
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS GENERALES
    // ══════════════════════════════════════════════════════════════════════
    static Transform Sub(Transform parent, string nombre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Material MatSimple(string nombre, Color color)
    {
        string path = MAT_DIR + nombre + ".mat";
        Material m  = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = nombre };
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Metallic",  0f);
        m.SetFloat("_Smoothness",0.3f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Agrega componente por nombre via reflexión (evita dependencia de assembly)
    static Component AddComp(GameObject go, string typeName)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return go.AddComponent(t);
        }
        Debug.LogWarning("[ArqViz] Tipo no encontrado: " + typeName +
                         ". Asegúrate de compilar primero.");
        return null;
    }
}
