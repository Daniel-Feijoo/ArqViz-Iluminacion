```markdown
# ArqViz-Iluminacion

Proyecto de visualización arquitectónica desarrollado en **Unity 6000.4.5f1** con **Universal Render Pipeline (URP 17.4.0)**. Incluye una casa moderna construida con primitivos de Unity, controlador FPS, puerta animada e iluminación arquitectónica completa.

---

## Dirección de Arte y Concepto

### ¿Qué sensación queremos generar con la iluminación?
Buscamos transmitir calidez y habitabilidad. La combinación de luz solar en hora dorada con focos interiores cálidos crea una atmósfera acogedora que invita a recorrer el espacio, reforzando la idea de que la arquitectura no solo se ve, sino que se siente. El contraste entre el exterior luminoso y los focos interiores suaves genera profundidad y jerarquía espacial.

### ¿Qué referencias visuales usamos?
Nos basamos en fotografías de arquitectura residencial minimalista al atardecer, donde la luz natural rasante resalta los volúmenes y proyecta sombras alargadas que definen la forma del edificio. También tomamos como referencia renders arquitectónicos de casas modernas con iluminación interior tipo *recessed lighting*, donde los focos de techo puntual definen zonas funcionales sin sobrecargar el espacio.

### ¿Qué decisión fue la más difícil y por qué?
La más difícil fue balancear la intensidad de los focos interiores frente a la luz direccional exterior. Si los focos eran muy intensos, el interior se veía sobreexpuesto y artificial. Si eran muy tenues, desaparecían ante la luz solar. La solución fue diferenciar por zona: focos de sala y entrada con mayor rango (5–6 uds.), focos de dormitorio más cálidos y suaves (4 uds.), y focos de mesita casi decorativos (2 uds.), logrando una iluminación estratificada que respeta la escala de cada ambiente.

---

## Configuración de Iluminación

### 1. Directional Light

| Parámetro | Valor |
|---|---|
| Color | `#FFDF95` — R:1.0 G:0.878 B:0.588 (dorado cálido) |
| Intensidad | `1.2` |
| Rotación (Euler) | X: ~35° Y: ~30° Z: 0° — Sol bajo en horizonte (hora dorada) |
| Sombras | Soft Shadows — Strength: 0.75, Bias: 0.05 |

---

### 2. Luces Adicionales

#### Spot Light — Acento Arquitectónico

| Parámetro | Valor |
|---|---|
| Tipo | Spot |
| Color | `#FFF2CC` — R:1.0 G:0.95 B:0.8 (blanco cálido) |
| Intensidad | `25` |
| Rango | `30` |
| SpotAngle | `40°` |
| Posición | (-8, 7, -6) — exterior, fachada izquierda |

#### Fill Light — Cielo Ambiente

| Parámetro | Valor |
|---|---|
| Tipo | Point |
| Color | `#7CA3FF` — R:0.49 G:0.64 B:1.0 (azul cielo) |
| Intensidad | `1.8` |
| Rango | `50` |

#### Focos Interiores (Point Lights)

| Nombre | Intensidad | Rango | Color | Posición |
|---|---|---|---|---|
| Foco_Entrada | 5 | 7 | `#FFE0AD` | (0, 3.65, 0.6) |
| Foco_Sala_1 | 4 | 5 | `#FFEDC6` | (-1.5, 3.65, 1.5) |
| Foco_Sala_2 | 6 | 9 | `#FFEAB7` | (0.5, 3.65, 3.2) |
| Foco_Dormitorio | 4 | 7 | `#FFE5B2` | (-2, 3.65, 6.3) |
| Foco_Cocina | 5 | 6 | `#F2F7FF` (blanco frío) | (3.2, 2.75, 5.5) |
| Foco_Mesita_Izq | 2 | 2.5 | `#FFCC7F` | (-2.65, 1.2, 6.6) |
| Foco_Mesita_Der | 2 | 2.5 | `#FFCC7F` | (-0.35, 1.2, 6.6) |

---

### 3. Skybox

| Parámetro | Valor |
|---|---|
| Material | `Skybox_HoraDorada` |
| Shader | Unity Procedural Skybox (built-in) |
| Sky Tint | `#87BFFF` — R:0.53 G:0.75 B:1.0 (azul celeste) |
| Ground Color | `#665E57` — R:0.4 G:0.37 B:0.34 (tierra cálida) |
| Sun Size | `0.04` |
| Atmosphere Thickness | `1.2` |
| Exposure | `1.1` |
| Sun Disk | High Quality |
| Ambient Mode | Skybox |

---

### 4. Niebla (Fog)

| Parámetro | Valor |
|---|---|
| Estado | Activada |
| Color | `#C6BCAD` — R:0.78 G:0.74 B:0.68 (beige cálido) |
| Modo | Exponential |
| Densidad | `0.008` |

---

### 5. Post-Processing — Global Volume

#### Bloom

| Parámetro | Valor |
|---|---|
| Estado | Activo |
| Threshold | `0.85` |
| Intensity | `0.45` |
| Scatter | `0.5` |
| High Quality Filtering | ON |

#### Vignette

| Parámetro | Valor |
|---|---|
| Estado | Activo |
| Intensity | `0.35` |
| Smoothness | `0.4` |
| Rounded | ON |

#### Tonemapping

| Parámetro | Valor |
|---|---|
| Estado | Activo |
| Mode | ACES |
| Paper White | `234 nits` |
| Max Nits | `647` |

#### Color Adjustments
Sin overrides activos (neutro: exposure 0, contrast 0, saturation 0).

#### Motion Blur
Desactivado.

---

## Mecánicas del Proyecto

### 1. Mecánica con Trigger (ZonaTeleporte.cs)
Ubicada en la zona exterior (punto azul). Exige que el jugador permanezca dentro del área de colisión durante un tiempo determinado antes de efectuar la acción, mostrando una barra de progreso en pantalla. Al completarse el tiempo, desactiva temporalmente el componente de movimiento para reubicar al jugador en el interior de la estructura de forma limpia.

```csharp
void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player")) { jugadorDentro = true; tiempoAcumulado = 0f; }
}

void Update()
{
    if (!jugadorDentro) return;
    tiempoAcumulado += Time.deltaTime;
    if (tiempoAcumulado >= tiempoActivacion) Teleportar();
}

void Teleportar()
{
    jugadorDentro = false;
    var player = GameObject.FindGameObjectWithTag("Player");
    var cc = player.GetComponent<CharacterController>();
    if (cc != null) cc.enabled = false;
    player.transform.position = posicionDestino;
    if (cc != null) cc.enabled = true;
}

```

### 2. Mecánica con Física (ObjetoFisico.cs)

Ubicada en la sala de juegos. Mantiene un conjunto de bloques de colores inicialmente congelados (`isKinematic = true`) para conservar la estética inicial del espacio. Al entrar el jugador en el volumen de activación, los objetos se liberan y reciben un impulso de fuerza y torque aleatorios para simular una dispersión física realista. La mecánica incluye la posibilidad de restaurar el orden original presionando la tecla `[R]`.

```csharp
void Start()
{
    for (int i = 0; i < rbs.Length; i++) { rbs[i].isKinematic = true; }
}

void OnTriggerEnter(Collider other)
{
    if (activado || !other.CompareTag("Player")) return;
    activado = true;

    foreach (var rb in rbs)
    {
        rb.isKinematic = false;
        Vector3 dir = (rb.transform.position - transform.position).normalized;
        dir.y += 0.5f;
        rb.AddForce(dir.normalized * fuerzaImpulso + Random.insideUnitSphere * 2f, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);
    }
}

```

### 3. Mecánica Propia del Género — Visualización Arquitectónica (PanelInfo.cs)

Diseñada específicamente para entornos de recorrido virtual (punto anaranjado). Detecta la cercanía del usuario mediante áreas de interacción y realiza un fundido suave (*Fade-In / Fade-Out*) de una tarjeta informativa en pantalla que detalla las características técnicas de la habitación actual (tales como el área en metros cuadrados, el estilo arquitectónico y su descripción conceptual).

```csharp
void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player")) mostrar = true;
}

void OnTriggerExit(Collider other)
{
    if (other.CompareTag("Player")) mostrar = false;
}

void Update()
{
    float objetivo = mostrar ? 1f : 0f;
    alpha = Mathf.MoveTowards(alpha, objetivo, Time.deltaTime * 3f);
}

```

---

## Scripts del Núcleo Base

### PlayerController.cs

Controlador en primera persona (FPS) desarrollado sobre el **New Input System**.

* **Mapeo:** Movimiento con `WASD`, control de mirada con el mouse, aceleración con `Shift`.
* **Gestión de pantalla:** Bloqueo del cursor con clic izquierdo y liberación mediante la tecla `Escape`.

### DoorController.cs

Control de accesos automatizado por proximidad.

* Realiza una apertura y cierre suaves empleando interpolación esférica (`Quaternion.Slerp`) y suavizado numérico al detectar la cercanía del jugador con el tag correspondiente.
* Ángulo máximo de apertura: `90°`.

### ZonaTeleporte.cs

Gestiona la mecánica de trigger con retardo temporal, UI de progreso y reubicación espacial del CharacterController.

### ObjetoFisico.cs

Controla el estado Kinematic de múltiples Rigidbody, aplicando `AddForce` y `AddTorque` mediante un trigger y permitiendo el reseteo de posiciones.

### PanelInfo.cs

Sistema de UI contextual que interactúa con el jugador, usando matemáticas de interpolación (`Mathf.MoveTowards`) para transiciones de opacidad (Alpha).

---

## Estructura del Proyecto

```
Assets/
├── Editor/
│   ├── ArqVizIluminacionSetup.cs   # Configura la iluminación desde el menú de Unity
│   ├── ArqVizCasaBuilder.cs        # Construcción procedimental de la envolvente exterior
│   └── ArqVizInteriorBuilder.cs    # Distribución de estancias, mobiliario y jugador
├── Materials/
│   ├── Casa/                       # Acabados de fachada y exteriores
│   ├── Interior/                   # Revestimientos, texturas de salas y emisivos de focos
│   └── Skybox_HoraDorada.mat       # Cielo procedural configurado para el atardecer
├── Scenes/
│   └── SampleScene.unity           # Escena principal del recorrido
├── Scripts/
│   ├── PlayerController.cs
│   ├── DoorController.cs
│   ├── ZonaTeleporte.cs
│   ├── ObjetoFisico.cs
│   └── PanelInfo.cs
└── Settings/
    ├── SampleSceneProfile.asset    # Perfil activo de Postprocesado (Volumen Global)
    ├── PC_RPAsset.asset            # Configuración de renderizado para URP
    └── PC_Renderer.asset

```

---

## Tecnologías Utilizadas

* **Motor Base:** Unity 6000.4.5f1
* **Pipeline Gráfico:** Universal Render Pipeline (URP) 17.4.0
* **Entrada de Usuario:** New Input System 1.19.0
* **Lenguaje:** C# (.NET / Scripts procedurales sin dependencias de frameworks externos)

```

```
