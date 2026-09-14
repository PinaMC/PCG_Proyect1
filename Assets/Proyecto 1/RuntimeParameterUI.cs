using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interfaz visual (UI Runtime) para modificar en tiempo de ejecución los parámetros 
/// de los scripts de la escena "Proyecto1" (Script_GenBase, BSP_Script, SecuencialZoneGeneration y LSystemTreeGeneratorP1).
/// Funciona tanto en el Ejecutable (.exe) como en el Editor de Unity.
/// Etiquetas simplificadas para que cualquier persona (sin conocimientos técnicos) entienda qué hace cada control.
/// </summary>
public class RuntimeParameterUI : MonoBehaviour
{
    [Header("Referencias a Scripts del Escenario")]
    [SerializeField] private Script_GenBase scriptGenBase;
    [SerializeField] private BSP_Script bspScript;
    [SerializeField] private SecuencialZoneGeneration secuencialScript;

    [Header("Configuración de la Interfaz")]
    [SerializeField] private bool mostrarUI = true;
    [SerializeField] private KeyCode teclaOcultarUI = KeyCode.Tab;
    [SerializeField] private KeyCode teclaRegenerar = KeyCode.Space;

    [Header("Control de Cámara Integrado")]
    [SerializeField] private bool habilitarControlCamara = true;
    [SerializeField] private float velocidadRotacionCamara = 3f;
    [SerializeField] private float velocidadDesplazamientoCamara = 10f;
    [SerializeField] private float velocidadZoom = 15f;

    // Estado interno y ventanas GUI
    private Rect rectVentana = new Rect(20, 20, 420, 700);
    private Vector2 scrollPosition = Vector2.zero;

    // Pestañas / Secciones plegables
    private bool seccionBSP = true;
    private bool seccionCasas = true;
    private bool seccionArboles = true;
    private bool seccionGramatica = true;
    private bool seccionCapas = true;

    // Campos de texto temporales para entradas de la UI
    private string strSemilla = "12345";
    private string strProducciones = "EZ, VZ, PZ";
    private string strSimboloInicial = "A";
    private string strProduccionInicial = "SZG";
    private string strSimboloZona = "Z";
    private string strSimboloFinal = "V";

    // Valores por defecto para restablecer
    private float defTamanoTerreno = 100f;
    private int defProfundidadDivision = 4;
    private float defTamanoMinimoLote = 10f;
    private float defMargenCalles = 2f;
    private int defResolucionCasas = 3;
    private float defSeparacionCasas = 0.5f;
    private int defResolucionZona = 5;
    private float defUmbral = 0.5f;
    private int defIteracionesArbol = 2;
    private float defAnguloArbol = 40f;
    private float defLargoSegmentoArbol = 1f;
    private float defGrosorRamaArbol = 0.06f;

    // Control de cámara
    private Camera camaraPrincipal;
    private Vector3 pivoteCamara = Vector3.zero;
    private float distanciaCamara = 120f;
    private float rotacionX = 45f;
    private float rotacionY = 45f;

    // Estilos GUI personalizados
    private GUIStyle estiloTitulo;
    private GUIStyle estiloSeccion;
    private GUIStyle estiloSubtitulo;
    private GUIStyle estiloBotonGrande;
    private GUIStyle estiloEtiquetaVal;
    private GUIStyle estiloAyuda;
    private bool estilosInicializados = false;

    private void Awake()
    {
        BuscarReferencias();
        GuardarValoresPorDefecto();
    }

    private void Start()
    {
        camaraPrincipal = Camera.main;
        if (camaraPrincipal != null && habilitarControlCamara)
        {
            AjustarCamaraAlTerreno();
        }
    }

    private void BuscarReferencias()
    {
        if (scriptGenBase == null)
            scriptGenBase = FindObjectOfType<Script_GenBase>();

        if (scriptGenBase != null)
        {
            if (bspScript == null) bspScript = scriptGenBase.BSP;
            if (secuencialScript == null) secuencialScript = scriptGenBase.Secuencial;
        }

        if (bspScript == null) bspScript = FindObjectOfType<BSP_Script>();
        if (secuencialScript == null) secuencialScript = FindObjectOfType<SecuencialZoneGeneration>();

        // Sincronizar campos de texto con el estado inicial de los scripts
        if (secuencialScript != null)
        {
            strSemilla = secuencialScript.Semilla.ToString();
            strProducciones = secuencialScript.ProduccionZonasComoTexto;
            strSimboloInicial = secuencialScript.SimboloInicial;
            strProduccionInicial = secuencialScript.ProduccionInicial;
            strSimboloZona = secuencialScript.SimboloZona;
            strSimboloFinal = secuencialScript.SimboloFinal;
        }
    }

    private void GuardarValoresPorDefecto()
    {
        if (scriptGenBase != null)
        {
            defTamanoTerreno = scriptGenBase.tamanoTerreno;
            defProfundidadDivision = scriptGenBase.profundidadDivision;
            defMargenCalles = scriptGenBase.margenCalles;
            defResolucionCasas = scriptGenBase.ResolucionCasas;
            defSeparacionCasas = scriptGenBase.separacionCasas;
            defResolucionZona = scriptGenBase.ResolucionZona;
            defUmbral = scriptGenBase.umbral;
            defIteracionesArbol = scriptGenBase.iteracionesArbol;
            defAnguloArbol = scriptGenBase.anguloArbol;
            defLargoSegmentoArbol = scriptGenBase.largoSegmentoArbol;
            defGrosorRamaArbol = scriptGenBase.grosorRamaArbol;
        }

        if (bspScript != null)
        {
            defTamanoMinimoLote = bspScript.tamanoMinimoLote;
        }
    }

    private void Update()
    {
        // Atajo de teclado para alternar visibilidad de la UI
        if (Input.GetKeyDown(teclaOcultarUI))
        {
            mostrarUI = !mostrarUI;
        }

        // Atajo de teclado para regenerar
        if (Input.GetKeyDown(teclaRegenerar) && !strSemillaFocus())
        {
            RegenerarEscenario();
        }

        // Control interactivo de cámara
        if (habilitarControlCamara && camaraPrincipal != null)
        {
            ManejarControlCamara();
        }
    }

    private bool strSemillaFocus()
    {
        // Evitar que presionar barra espaciadora active la generación si se está escribiendo texto
        return GUI.GetNameOfFocusedControl() == "CampoTextoGUI";
    }

    private void ManejarControlCamara()
    {
        // Solo manipular cámara si el cursor no está sobre la ventana de la UI
        Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (mostrarUI && rectVentana.Contains(mousePos))
            return;

        // Rotar cámara manteniendo clic derecho
        if (Input.GetMouseButton(1))
        {
            rotacionY += Input.GetAxis("Mouse X") * velocidadRotacionCamara;
            rotacionX -= Input.GetAxis("Mouse Y") * velocidadRotacionCamara;
            rotacionX = Mathf.Clamp(rotacionX, 10f, 85f);
        }

        // Desplazar pivote de cámara manteniendo clic medio o WASD
        if (Input.GetMouseButton(2))
        {
            Vector3 derecho = camaraPrincipal.transform.right;
            Vector3 adelante = Vector3.Cross(derecho, Vector3.up).normalized;
            pivoteCamara -= (derecho * Input.GetAxis("Mouse X") + adelante * Input.GetAxis("Mouse Y")) * (velocidadDesplazamientoCamara * 0.5f);
        }

        float inputH = Input.GetAxis("Horizontal");
        float inputV = Input.GetAxis("Vertical");
        if (Mathf.Abs(inputH) > 0.01f || Mathf.Abs(inputV) > 0.01f)
        {
            Vector3 derecho = camaraPrincipal.transform.right;
            Vector3 adelante = Vector3.Cross(derecho, Vector3.up).normalized;
            pivoteCamara += (derecho * inputH + adelante * inputV) * velocidadDesplazamientoCamara * Time.deltaTime;
        }

        // Zoom con la rueda del ratón
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            distanciaCamara -= scroll * velocidadZoom * 10f;
            distanciaCamara = Mathf.Clamp(distanciaCamara, 15f, 400f);
        }

        // Actualizar posición y rotación de la cámara
        Quaternion rot = Quaternion.Euler(rotacionX, rotacionY, 0f);
        Vector3 pos = pivoteCamara - (rot * Vector3.forward * distanciaCamara);

        camaraPrincipal.transform.rotation = rot;
        camaraPrincipal.transform.position = pos;
    }

    public void AjustarCamaraAlTerreno()
    {
        if (camaraPrincipal == null) camaraPrincipal = Camera.main;
        if (camaraPrincipal == null) return;

        float tamano = (scriptGenBase != null) ? scriptGenBase.tamanoTerreno : 100f;
        pivoteCamara = Vector3.zero;
        distanciaCamara = tamano * 1.3f;
        rotacionX = 50f;
        rotacionY = 45f;
    }

    private void OnGUI()
    {
        // Botón flotante para mostrar UI si está oculta
        if (!mostrarUI)
        {
            if (GUI.Button(new Rect(15, 15, 200, 35), "⚙️ Abrir Controles (TAB)"))
            {
                mostrarUI = true;
            }
            return;
        }

        InicializarEstilos();

        rectVentana = GUI.Window(0, rectVentana, DibujarVentanaParametros, "🛠️ CONTROLES DE LA CIUDAD");
    }

    private void InicializarEstilos()
    {
        if (estilosInicializados) return;

        estiloTitulo = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        estiloTitulo.normal.textColor = Color.yellow;

        estiloSeccion = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        estiloSubtitulo = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold
        };
        estiloSubtitulo.normal.textColor = new Color(0.4f, 0.8f, 1f);

        estiloBotonGrande = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        estiloEtiquetaVal = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleRight
        };
        estiloEtiquetaVal.normal.textColor = Color.white;

        estiloAyuda = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Italic,
            wordWrap = true
        };
        estiloAyuda.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

        estilosInicializados = true;
    }

    private void DibujarVentanaParametros(int windowID)
    {
        GUILayout.BeginVertical();

        // Banner Superior
        GUILayout.Space(5);
        GUILayout.Label("Mueve los controles y presiona el botón verde para ver los cambios", estiloTitulo);
        GUILayout.Space(5);

        // Scroll View Principal
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(395), GUILayout.Height(540));

        // ---------------------------------------------------------------------
        // SECCIÓN 1: FORMA DEL TERRENO Y BARRIOS (antes: TERRENO Y LOTES BSP)
        // ---------------------------------------------------------------------
        if (GUILayout.Button((seccionBSP ? "▼" : "►") + "  1. FORMA DEL TERRENO Y BARRIOS", estiloSeccion))
        {
            seccionBSP = !seccionBSP;
        }

        if (seccionBSP && scriptGenBase != null && bspScript != null)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // Tamaño del terreno
            GUILayout.BeginHorizontal();
            GUILayout.Label("Tamaño de la ciudad:", GUILayout.Width(170));
            scriptGenBase.tamanoTerreno = Mathf.Round(GUILayout.HorizontalSlider(scriptGenBase.tamanoTerreno, 30f, 300f));
            GUILayout.Label(scriptGenBase.tamanoTerreno.ToString("F0"), estiloEtiquetaVal, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            GUILayout.Label("Qué tan grande es el terreno donde se construye todo.", estiloAyuda);

            // Profundidad división BSP
            GUILayout.BeginHorizontal();
            GUILayout.Label("Cantidad de barrios:", GUILayout.Width(170));
            scriptGenBase.profundidadDivision = Mathf.RoundToInt(GUILayout.HorizontalSlider(scriptGenBase.profundidadDivision, 1f, 6f));
            GUILayout.Label(scriptGenBase.profundidadDivision.ToString(), estiloEtiquetaVal, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            GUILayout.Label("Más alto = el terreno se corta en más pedazos (más barrios, más chicos).", estiloAyuda);

            // Tamaño mínimo de lote BSP
            GUILayout.BeginHorizontal();
            GUILayout.Label("Tamaño mínimo del lote:", GUILayout.Width(170));
            bspScript.tamanoMinimoLote = Mathf.Round(GUILayout.HorizontalSlider(bspScript.tamanoMinimoLote, 2f, 40f));
            GUILayout.Label(bspScript.tamanoMinimoLote.ToString("F0"), estiloEtiquetaVal, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            GUILayout.Label("El terreno deja de dividirse una vez que los lotes llegan a este tamaño.", estiloAyuda);

            // Margen de calles
            GUILayout.BeginHorizontal();
            GUILayout.Label("Ancho de las calles:", GUILayout.Width(170));
            scriptGenBase.margenCalles = Mathf.Round(GUILayout.HorizontalSlider(scriptGenBase.margenCalles, 0f, 10f) * 10f) / 10f;
            GUILayout.Label(scriptGenBase.margenCalles.ToString("F1"), estiloEtiquetaVal, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            GUILayout.Label("Qué tan separados quedan los barrios entre sí.", estiloAyuda);

            GUILayout.EndVertical();
        }
        GUILayout.Space(6);

        // ---------------------------------------------------------------------
        // SECCIÓN 2: CASAS (sin cambio de nombre, ya era claro)
        // ---------------------------------------------------------------------
        if (GUILayout.Button((seccionCasas ? "▼" : "►") + "  2. CASAS", estiloSeccion))
        {
            seccionCasas = !seccionCasas;
        }

        if (seccionCasas && scriptGenBase != null)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // Resolución de casas por lote
            GUILayout.BeginHorizontal();
            GUILayout.Label("Casas por barrio:", GUILayout.Width(170));
            scriptGenBase.ResolucionCasas = Mathf.RoundToInt(GUILayout.HorizontalSlider(scriptGenBase.ResolucionCasas, 1f, 8f));
            GUILayout.Label(scriptGenBase.ResolucionCasas.ToString(), estiloEtiquetaVal, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            GUILayout.Label("Cuántas casas caben en cada barrio (filas y columnas).", estiloAyuda);

            // Separación entre casas
            GUILayout.BeginHorizontal();
            GUILayout.Label("Espacio entre casas:", GUILayout.Width(170));
            scriptGenBase.separacionCasas = Mathf.Round(GUILayout.HorizontalSlider(scriptGenBase.separacionCasas, 0f, 2f) * 10f) / 10f;
            GUILayout.Label(scriptGenBase.separacionCasas.ToString("F1"), estiloEtiquetaVal, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            GUILayout.Label("Qué tan pegadas o separadas están las casas una de otra.", estiloAyuda);

            GUILayout.EndVertical();
        }
        GUILayout.Space(6);

        // ---------------------------------------------------------------------
        // SECCIÓN 3: BOSQUE Y ÁRBOLES (antes: ZONA ARBOLADA Y L-SYSTEM)
        // ---------------------------------------------------------------------
        if (GUILayout.Button((seccionArboles ? "▼" : "►") + "  3. BOSQUE Y ÁRBOLES", estiloSeccion))
        {
            seccionArboles = !seccionArboles;
        }

        if (seccionArboles && scriptGenBase != null)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // Grilla de zona arbolada
            GUILayout.BeginHorizontal();
            GUILayout.Label("Tamaño del bosque:", GUILayout.Width(170));
            scriptGenBase.ResolucionZona = Mathf.RoundToInt(GUILayout.HorizontalSlider(scriptGenBase.ResolucionZona, 1f, 15f));
            GUILayout.Label(scriptGenBase.ResolucionZona.ToString(), estiloEtiquetaVal, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            GUILayout.Label("Cuántos espacios posibles para árboles hay en la zona verde.", estiloAyuda);

            // Umbral de vegetación Perlin
            GUILayout.BeginHorizontal();
            GUILayout.Label("Densidad de árboles:", GUILayout.Width(170));
            scriptGenBase.umbral = Mathf.Round(GUILayout.HorizontalSlider(scriptGenBase.umbral, -0.9f, 0.9f) * 100f) / 100f;
            GUILayout.Label(scriptGenBase.umbral.ToString("F2"), estiloEtiquetaVal, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            GUILayout.Label("Más bajo = más árboles. Más alto = bosque más despejado.", estiloAyuda);

            GUILayout.Label("--- Cómo se ve cada árbol ---", estiloSubtitulo);

            // Iteraciones L-System
            GUILayout.BeginHorizontal();
            GUILayout.Label("Nivel de ramificación:", GUILayout.Width(170));
            scriptGenBase.iteracionesArbol = Mathf.RoundToInt(GUILayout.HorizontalSlider(scriptGenBase.iteracionesArbol, 0f, 4f));
            GUILayout.Label(scriptGenBase.iteracionesArbol.ToString(), estiloEtiquetaVal, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            GUILayout.Label("Más alto = árboles más frondosos y complejos (pero más lento de generar).", estiloAyuda);

            // Ángulo del árbol
            GUILayout.BeginHorizontal();
            GUILayout.Label("Apertura de las ramas:", GUILayout.Width(170));
            scriptGenBase.anguloArbol = Mathf.Round(GUILayout.HorizontalSlider(scriptGenBase.anguloArbol, 10f, 80f));
            GUILayout.Label(scriptGenBase.anguloArbol.ToString("F0") + "°", estiloEtiquetaVal, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            GUILayout.Label("Bajo = árbol delgado y derecho. Alto = árbol más abierto, tipo arbusto.", estiloAyuda);

            // Largo segmento
            GUILayout.BeginHorizontal();
            GUILayout.Label("Largo de las ramas:", GUILayout.Width(170));
            scriptGenBase.largoSegmentoArbol = Mathf.Round(GUILayout.HorizontalSlider(scriptGenBase.largoSegmentoArbol, 0.2f, 2.5f) * 10f) / 10f;
            GUILayout.Label(scriptGenBase.largoSegmentoArbol.ToString("F1"), estiloEtiquetaVal, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            GUILayout.Label("Qué tan largo es cada tramo de rama. Afecta la altura del árbol.", estiloAyuda);

            // Grosor rama
            GUILayout.BeginHorizontal();
            GUILayout.Label("Grosor de las ramas:", GUILayout.Width(170));
            scriptGenBase.grosorRamaArbol = Mathf.Round(GUILayout.HorizontalSlider(scriptGenBase.grosorRamaArbol, 0.01f, 0.15f) * 100f) / 100f;
            GUILayout.Label(scriptGenBase.grosorRamaArbol.ToString("F2"), estiloEtiquetaVal, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            GUILayout.Label("Qué tan gruesas se ven las ramas del árbol.", estiloAyuda);

            GUILayout.EndVertical();
        }
        GUILayout.Space(6);

        // ---------------------------------------------------------------------
        // SECCIÓN 4: ORDEN DE GENERACIÓN DE ZONAS (antes: GRAMÁTICA SECUENCIAL)
        // ---------------------------------------------------------------------
        if (GUILayout.Button((seccionGramatica ? "▼" : "►") + "  4. ORDEN DE GENERACIÓN DE ZONAS", estiloSeccion))
        {
            seccionGramatica = !seccionGramatica;
        }

        if (seccionGramatica && secuencialScript != null)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Estos controles definen en qué orden y con qué reglas se van creando las zonas de la ciudad. Son más técnicos: si no estás seguro, mejor no los toques.", estiloAyuda);
            GUILayout.Space(4);

            // Semilla aleatoria
            GUILayout.BeginHorizontal();
            GUILayout.Label("Código de variación:", GUILayout.Width(130));
            GUI.SetNextControlName("CampoTextoGUI");
            strSemilla = GUILayout.TextField(strSemilla, GUILayout.Width(140));
            if (int.TryParse(strSemilla, out int valSemilla))
            {
                secuencialScript.Semilla = valSemilla;
            }

            if (GUILayout.Button("🎲 Al azar", GUILayout.Width(75)))
            {
                int nuevaSemilla = Random.Range(10000, 99999);
                strSemilla = nuevaSemilla.ToString();
                secuencialScript.Semilla = nuevaSemilla;
                RegenerarEscenario();
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Cambia este número (o presiona 'Al azar') para obtener una ciudad distinta.", estiloAyuda);

            // Producciones de zonas (reglas)
            GUILayout.BeginHorizontal();
            GUILayout.Label("Reglas de las zonas:", GUILayout.Width(130));
            string nuevaProd = GUILayout.TextField(strProducciones, GUILayout.Width(210));
            if (nuevaProd != strProducciones)
            {
                strProducciones = nuevaProd;
                secuencialScript.ProduccionZonasComoTexto = strProducciones;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Lista de códigos que definen qué tipos de zona pueden aparecer.", estiloAyuda);

            // Producción inicial
            GUILayout.BeginHorizontal();
            GUILayout.Label("Punto de partida:", GUILayout.Width(130));
            strProduccionInicial = GUILayout.TextField(strProduccionInicial, GUILayout.Width(60));
            secuencialScript.ProduccionInicial = strProduccionInicial;

            GUILayout.Label("Letra de zona:", GUILayout.Width(90));
            strSimboloZona = GUILayout.TextField(strSimboloZona, GUILayout.Width(30));
            secuencialScript.SimboloZona = strSimboloZona;
            GUILayout.EndHorizontal();

            // Símbolo inicial y final
            GUILayout.BeginHorizontal();
            GUILayout.Label("Letra de inicio:", GUILayout.Width(130));
            strSimboloInicial = GUILayout.TextField(strSimboloInicial, GUILayout.Width(60));
            secuencialScript.SimboloInicial = strSimboloInicial;

            GUILayout.Label("Letra final:", GUILayout.Width(90));
            strSimboloFinal = GUILayout.TextField(strSimboloFinal, GUILayout.Width(30));
            secuencialScript.SimboloFinal = strSimboloFinal;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        GUILayout.Space(6);

        // ---------------------------------------------------------------------
        // SECCIÓN 5: QUÉ SE MUESTRA (antes: CAPAS VISIBLES)
        // ---------------------------------------------------------------------
        if (GUILayout.Button((seccionCapas ? "▼" : "►") + "  5. QUÉ SE MUESTRA", estiloSeccion))
        {
            seccionCapas = !seccionCapas;
        }

        if (seccionCapas && scriptGenBase != null)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Activa o desactiva partes de la escena.", estiloAyuda);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();

            GUI.backgroundColor = scriptGenBase.MostrandoCiudad ? new Color(0.3f, 0.85f, 0.4f) : Color.white;
            if (GUILayout.Button(scriptGenBase.MostrandoCiudad ? "● Mostrar Ciudad" : "○ Mostrar Ciudad"))
            {
                scriptGenBase.MostrarCiudad();
            }

            GUI.backgroundColor = scriptGenBase.MostrandoAlcantarillado ? new Color(0.3f, 0.85f, 0.4f) : Color.white;
            if (GUILayout.Button(scriptGenBase.MostrandoAlcantarillado ? "● Mostrar Alcantarillado" : "○ Mostrar Alcantarillado"))
            {
                scriptGenBase.MostrarAlcantarillado();
            }

            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        GUILayout.EndScrollView();

        // ---------------------------------------------------------------------
        // PANEL INFERIOR DE ACCIONES
        // ---------------------------------------------------------------------
        GUILayout.Space(5);

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
        if (GUILayout.Button("🔄 GENERAR CIUDAD DE NUEVO (ESPACIO)", estiloBotonGrande, GUILayout.Height(38)))
        {
            RegenerarEscenario();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("🎯 Centrar Vista"))
        {
            AjustarCamaraAlTerreno();
        }

        if (GUILayout.Button("↩️ Volver a lo Original"))
        {
            RestablecerValoresPorDefecto();
        }

        if (GUILayout.Button("🙈 Ocultar (TAB)"))
        {
            mostrarUI = false;
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        // Hacer la ventana arrastrable
        GUI.DragWindow();
    }

    private void RegenerarEscenario()
    {
        BuscarReferencias();

        if (secuencialScript != null && int.TryParse(strSemilla, out int sem))
        {
            secuencialScript.Semilla = sem;
        }

        if (scriptGenBase != null)
        {
            scriptGenBase.GenerarEscenario();
        }
        else
        {
            Debug.LogWarning("RuntimeParameterUI: No se encontró Script_GenBase en la escena.");
        }
    }

    private void RestablecerValoresPorDefecto()
    {
        if (scriptGenBase != null)
        {
            scriptGenBase.tamanoTerreno = defTamanoTerreno;
            scriptGenBase.profundidadDivision = defProfundidadDivision;
            scriptGenBase.margenCalles = defMargenCalles;
            scriptGenBase.ResolucionCasas = defResolucionCasas;
            scriptGenBase.separacionCasas = defSeparacionCasas;
            scriptGenBase.ResolucionZona = defResolucionZona;
            scriptGenBase.umbral = defUmbral;
            scriptGenBase.iteracionesArbol = defIteracionesArbol;
            scriptGenBase.anguloArbol = defAnguloArbol;
            scriptGenBase.largoSegmentoArbol = defLargoSegmentoArbol;
            scriptGenBase.grosorRamaArbol = defGrosorRamaArbol;
        }

        if (bspScript != null)
        {
            bspScript.tamanoMinimoLote = defTamanoMinimoLote;
        }

        if (secuencialScript != null)
        {
            secuencialScript.Semilla = 12345;
            strSemilla = "12345";
            secuencialScript.ProduccionZonasComoTexto = "EZ, VZ, PZ";
            strProducciones = "EZ, VZ, PZ";
            secuencialScript.ProduccionInicial = "SZG";
            strProduccionInicial = "SZG";
            secuencialScript.SimboloZona = "Z";
            strSimboloZona = "Z";
            secuencialScript.SimboloFinal = "V";
            strSimboloFinal = "V";
        }

        RegenerarEscenario();
    }
}