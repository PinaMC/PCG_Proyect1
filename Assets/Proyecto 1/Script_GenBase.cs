using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class Script_GenBase : MonoBehaviour
{
    [Header("Configuración del Terreno")]
    public float tamanoTerreno = 100f;
    public int profundidadDivision = 4;
    public float margenCalles = 2f; // NUEVO: Espacio entre los lotes

    public float umbralEdificio = 40f;

    public int ResolucionZona = 5; // Cuantas celdas en la zona

    public float umbral = 0.5f; // Cuantas celdas en la zona

    [Header("Prefab del arbol por Lsystm")]
    public GameObject prefabArbol; // Prefab del árbol que se instanciará en la zona arbolada
    private GameObject arbolGenerado; // Referencia al árbol generado 


    // Header que incluira configuracion de las casas
    [Header("Configuración de Casas")]
    public int ResolucionCasas = 3; // Cuántas casas por fila/columna en la zona residencial
    public float separacionCasas = 0.5f; // Margen entre cada casa del lote

    [SerializeField] GameObject CasaPrefab;

    [SerializeField] GameObject InicioPrefab;

    [Header("Configuración de Árboles L-System")]
    public int iteracionesArbol = 2;
    public float anguloArbol = 40f;
    public float largoSegmentoArbol = 1f;
    public float grosorRamaArbol = 0.06f;

    [Header("Configuración de Edificios")]
    public int ResolucionEdificio = 3; // Cuántas casas por fila/columna en la zona residencial
    public float separacionEdificio = 0.5f; // Margen entre cada casa del lote

    [SerializeField] GameObject EdificioPrefab;

    [SerializeField] private SecuencialZoneGeneration SecuencialScript;

    [SerializeField] private BSP_Script scriptBSP;

    [Header("Mapa de Recorrido (Random Walk)")]
    [SerializeField] private RandomWalk randomWalkScript;

    [Header("Visibilidad de Capas")]
    [SerializeField] private bool mostrarCiudad = true;
    [SerializeField] private bool mostrarAlcantarillado = false;

    // Lectura del estado actual, para que RuntimeParameterUI pueda
    // reflejarlo en sus botones sin duplicar el estado acá y allá.
    public bool MostrandoCiudad => mostrarCiudad;
    public bool MostrandoAlcantarillado => mostrarAlcantarillado;

    public SecuencialZoneGeneration Secuencial => SecuencialScript;
    public BSP_Script BSP => scriptBSP;

    // Lista para guardar los objetos y poder borrarlos
    private List<GameObject> lotesInstanciados = new List<GameObject>(); //esto almacena los cubos o lotes creados en el script

    // Contenedor de todo lo que es "ciudad" (calles, edificios, casas, plaza, inicio, árboles)
    private Transform contenedorCiudad;

    void Start()
    {
        GenerarEscenario(); //el metodo start se ejecutal al iniciar 
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerarEscenario(); //cada que presiones el espacio se generara un nuevo escenario
        }
    }

    // -------------------------------------------------------------------
    // API pública de visibilidad de capas: no dibuja ninguna UI acá.
    // El control visual (botones/toggle) vive en RuntimeParameterUI, que
    // llama a estos métodos.
    // -------------------------------------------------------------------
    public void MostrarCiudad()
    {
        mostrarCiudad = true;
        mostrarAlcantarillado = false;
        AplicarVisibilidadCapas();
    }

    public void MostrarAlcantarillado()
    {
        mostrarCiudad = false;
        mostrarAlcantarillado = true;
        AplicarVisibilidadCapas();
    }

    private void AplicarVisibilidadCapas()
    {
        if (contenedorCiudad != null)
        {
            contenedorCiudad.gameObject.SetActive(mostrarCiudad);
        }

        if (randomWalkScript != null)
        {
            randomWalkScript.SetVisible(mostrarAlcantarillado);
        }
    }

    // Centraliza el "Add a la lista + parentar al contenedor Ciudad"
    // para no repetir las dos líneas en cada método de construcción.
    private void RegistrarObjetoCiudad(GameObject obj)
    {
        lotesInstanciados.Add(obj);
        obj.transform.SetParent(contenedorCiudad, true); // true = mantiene su posición en el mundo
    }

    public void GenerarEscenario()
    {
        foreach (GameObject lote in lotesInstanciados) //recorre la lista de lotes instanciados y los destruye
        {
            Destroy(lote);
        }
        lotesInstanciados.Clear();

        // Contenedor "Ciudad": se crea una sola vez y se reutiliza entre regeneraciones
        if (contenedorCiudad == null)
        {
            GameObject ciudadObj = new GameObject("Ciudad");
            contenedorCiudad = ciudadObj.transform;
        }

        //zona de aerboles
        if (arbolGenerado != null) Destroy(arbolGenerado);

        // Validar e instanciar usando prefabArbol, no arbolGenerado
        if (prefabArbol != null)
        {
            arbolGenerado = Instantiate(prefabArbol);
            arbolGenerado.name = "Template_Arbol_Oculto";

            LSystemTreeGeneratorP1 scriptArbol = arbolGenerado.GetComponent<LSystemTreeGeneratorP1>();
            if (scriptArbol != null)
            {
                scriptArbol.SetIterations(iteracionesArbol);
                scriptArbol.SetAngle(anguloArbol);
                scriptArbol.SetSegmentLength(largoSegmentoArbol);
                scriptArbol.SetBranchRadius(grosorRamaArbol);
                scriptArbol.GenerateTree();
            }

            arbolGenerado.SetActive(false);
        }

        //crear calles
        GameObject calles = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(calles); // Para que se borre al presionar Espacio
        calles.transform.position = new Vector3(0f, -0.05f, 0f);

        //escalar a tamaño total del terreno
        calles.transform.localScale = new Vector3(tamanoTerreno, 0.1f, tamanoTerreno);
        calles.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.2f);

        // se define el rectangulo del terreno completo centrado en el origen
        Rect terrenoCompleto = new Rect(-tamanoTerreno / 2, -tamanoTerreno / 2, tamanoTerreno, tamanoTerreno);
        List<Rect> lotesGenerados = scriptBSP.EjecutarBSP(terrenoCompleto, profundidadDivision);

        string mapa = SecuencialScript.GenerarZona(lotesGenerados.Count);
        if (string.IsNullOrEmpty(mapa))
        {
            Debug.LogError("Cadena no Valida");
            return;
        }

        int limite = Mathf.Min(lotesGenerados.Count, mapa.Length); // limite asegurado 

        for (int i = 0; i < limite; i++)
        {
            Rect lote = lotesGenerados[i];
            char zona = mapa[i];

            switch (zona)
            {
                case 'E':
                    ConstruirEdificio(lote);
                    break;
                case 'P':
                    ConstruirCasas(lote);
                    break;
                case 'G':
                    ConstruirPlaza(lote);
                    break;
                case 'S':
                    ConstruirInicio(lote);
                    break;
                default:
                    ConstruirZonaArbolada(lote);
                    break;
            }
        }

        // Genera (o regenera) el recorrido random walk sobre "Alcantarillado"
        if (randomWalkScript != null)
        {
            randomWalkScript.GenerarMapa(lotesGenerados, mapa);
        }

        // Aplica el estado de visibilidad actual a ambas capas recién creadas
        AplicarVisibilidadCapas();
    }

    void ConstruirEdificio(Rect lote) //funcion para construir el bloque en la posciion del lote
    {
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(cubo);

        // Posición central
        cubo.transform.position = new Vector3(lote.center.x, 0.5f, lote.center.y);

        // Al tamaño le restamos el margen para que queden espacios (calles) entre ellos
        float anchoConMargen = Mathf.Max(1f, lote.width - margenCalles);
        float largoConMargen = Mathf.Max(1f, lote.height - margenCalles);

        cubo.transform.position = new Vector3(lote.center.x, 0.05f, lote.center.y);
        cubo.transform.localScale = new Vector3(anchoConMargen, 1f, largoConMargen);
        Color color = new Color32(200, 200, 200, 255);
        cubo.GetComponent<Renderer>().material.color = color;

        // Calcular el tamaño que tendran las casas dependiendo del lote
        float mitadMargen = margenCalles / 2f;
        float anchoDisponible = lote.width - margenCalles;
        float largoDisponible = lote.height - margenCalles;

        float pasoX = anchoDisponible / ResolucionCasas;
        float pasoZ = largoDisponible / ResolucionCasas;

        // Tamaño real de la casa (restándole el margen entre casas)
        float anchoEdificio = pasoX - separacionEdificio;
        float largoEdificio = pasoZ - separacionEdificio;
        if (anchoEdificio <= 0 || largoEdificio <= 0)
        {
            Debug.LogWarning("El tamaño de las casas es demasiado pequeño para el lote. Ajusta la ResoluciónCasas o separacionCasas.");
            return;
        }

        for (int fila = 0; fila < ResolucionCasas; fila++)
        {
            for (int columna = 0; columna < ResolucionCasas; columna++)
            {
                // 1. CORRECCIÓN: Calcular el centro exacto de la parcela (se añade pasoX / 2f)
                float centroX = lote.xMin + mitadMargen + (columna * pasoX) + (pasoX / 2f);
                float centroZ = lote.yMin + mitadMargen + (fila * pasoZ) + (pasoZ / 2f);

                GameObject edificio = Instantiate(EdificioPrefab);
                RegistrarObjetoCiudad(edificio);

                // 2. CORRECCIÓN: Aumentamos la altura para que no parezcan placas

                Renderer rendCasa = edificio.GetComponentInChildren<Renderer>();

                // Tamaño real del modelo tal como vino, antes de tocar su escala
                Vector3 tamanoOriginal = rendCasa.bounds.size;

                // Escala UNIFORME (mismo factor en los 3 ejes) para que quepa en el lote
                // sin deformar las proporciones del modelo
                float factor = Mathf.Min(anchoEdificio * tamanoOriginal.x, largoEdificio * tamanoOriginal.z);
                edificio.transform.localScale = Vector3.one * (factor);
                edificio.transform.localScale = new Vector3(edificio.transform.localScale.x, edificio.transform.localScale.z, tamanoOriginal.y);

                // Ya escalado, medimos de nuevo cuánto hay entre el pivote y la base real
                // del modelo, para posicionarlo apoyado en el piso (y = 0.1f) en vez de
                // asumir que el pivote está al centro

                edificio.transform.position = new Vector3(centroX, cubo.transform.localScale.y, centroZ);
            }
        }
    }

    private float Perlinbase(float x, float z)
    {
        return Mathf.PerlinNoise(x * 0.1f, z * 0.1f) * 2f - 1f;
    }

    void ConstruirPlaza(Rect lote)
    {
        // 1. GENERAR EXPLANADA AMARILLA (Base)
        GameObject explanada = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(explanada);

        float anchoConMargen = Mathf.Max(1f, lote.width - margenCalles);
        float largoConMargen = Mathf.Max(1f, lote.height - margenCalles);

        explanada.transform.position = new Vector3(lote.center.x, 0f, lote.center.y);
        explanada.transform.localScale = new Vector3(anchoConMargen, 0.1f, largoConMargen);
        explanada.GetComponent<Renderer>().material.color = Color.yellow;


        // 2. DIMENSIONES DEL EDIFICIO PÚBLICO
        float anchoEdificio = anchoConMargen * 0.7f;
        float largoEdificio = largoConMargen * 0.7f;
        float altoEdificio = 5f;

        // 3. GENERAR BLOQUE PRINCIPAL (Blanco)
        GameObject edificio = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(edificio);

        edificio.transform.position = new Vector3(lote.center.x, 0.1f + (altoEdificio / 2f), lote.center.y);
        edificio.transform.localScale = new Vector3(anchoEdificio, altoEdificio, largoEdificio);
        edificio.GetComponent<Renderer>().material.color = Color.white;


        // 4. GENERAR VENTANAS NEGRAS (Fachada Principal)
        float altoVentanas = altoEdificio * 0.4f;
        float alturaY = 0.1f + (altoEdificio / 2f) + 0.5f; // Altura común para todas las ventanas

        // --- Ventanas del Frontis (Izquierda y Derecha del pórtico) ---
        float anchoVentanaFrontal = anchoEdificio * 0.25f;

        GameObject ventFrontIzq = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(ventFrontIzq);
        ventFrontIzq.transform.position = new Vector3(lote.center.x - (anchoEdificio * 0.25f), alturaY, lote.center.y - (largoEdificio / 2f));
        ventFrontIzq.transform.localScale = new Vector3(anchoVentanaFrontal, altoVentanas, 0.15f);
        ventFrontIzq.GetComponent<Renderer>().material.color = Color.black;

        GameObject ventFrontDer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(ventFrontDer);
        ventFrontDer.transform.position = new Vector3(lote.center.x + (anchoEdificio * 0.25f), alturaY, lote.center.y - (largoEdificio / 2f));
        ventFrontDer.transform.localScale = new Vector3(anchoVentanaFrontal, altoVentanas, 0.15f);
        ventFrontDer.GetComponent<Renderer>().material.color = Color.black;

        // --- Ventanas Laterales (Muro Izquierdo y Derecho del edificio) ---
        float largoVentanaLateral = largoEdificio * 0.6f;

        GameObject ventLatIzq = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(ventLatIzq);
        ventLatIzq.transform.position = new Vector3(lote.center.x - (anchoEdificio / 2f), alturaY, lote.center.y);
        ventLatIzq.transform.localScale = new Vector3(0.15f, altoVentanas, largoVentanaLateral);
        ventLatIzq.GetComponent<Renderer>().material.color = Color.black;

        GameObject ventLatDer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(ventLatDer);
        ventLatDer.transform.position = new Vector3(lote.center.x + (anchoEdificio / 2f), alturaY, lote.center.y);
        ventLatDer.transform.localScale = new Vector3(0.15f, altoVentanas, largoVentanaLateral);
        ventLatDer.GetComponent<Renderer>().material.color = Color.black;

        // 5. GENERAR PÓRTICO (Pilares y Techo)
        float grosorPilar = 0.4f;
        float altoPilar = 2.5f;
        float offsetPilaresZ = (largoEdificio / 2f) + 0.6f; // Ligeramente despegado de la fachada

        // Pilar Izquierdo
        GameObject pilarIzq = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(pilarIzq);
        pilarIzq.transform.position = new Vector3(lote.center.x - (anchoEdificio / 3f), 0.1f + (altoPilar / 2f), lote.center.y - offsetPilaresZ);
        pilarIzq.transform.localScale = new Vector3(grosorPilar, altoPilar, grosorPilar);
        pilarIzq.GetComponent<Renderer>().material.color = Color.white;

        // Pilar Derecho
        GameObject pilarDer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(pilarDer);
        pilarDer.transform.position = new Vector3(lote.center.x + (anchoEdificio / 3f), 0.1f + (altoPilar / 2f), lote.center.y - offsetPilaresZ);
        pilarDer.transform.localScale = new Vector3(grosorPilar, altoPilar, grosorPilar);
        pilarDer.GetComponent<Renderer>().material.color = Color.white;

        // Techo del Pórtico
        GameObject techoPortico = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(techoPortico);
        float anchoTecho = anchoEdificio * 0.9f;

        techoPortico.transform.position = new Vector3(lote.center.x, 0.1f + altoPilar + 0.2f, lote.center.y - offsetPilaresZ + 0.3f);
        techoPortico.transform.localScale = new Vector3(anchoTecho, 0.4f, 1.5f);
        techoPortico.GetComponent<Renderer>().material.color = Color.white;
    }

    void ConstruirInicio(Rect lote)
    {
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(cubo);

        float anchoConMargen = Mathf.Max(1f, lote.width - margenCalles);
        float largoConMargen = Mathf.Max(1f, lote.height - margenCalles);

        cubo.transform.position = new Vector3(lote.center.x, 0.05f, lote.center.y);
        cubo.transform.localScale = new Vector3(anchoConMargen, 1f, largoConMargen);
        cubo.GetComponent<Renderer>().material.color = new Color32(150, 255, 133, 255);

        GameObject inicio = Instantiate(InicioPrefab);
        RegistrarObjetoCiudad(inicio);

        Renderer rendInicio = inicio.GetComponentInChildren<Renderer>();
        if (rendInicio != null)
        {
            // Tamaño real del modelo antes de tocar su escala
            Vector3 tamanoOriginal = rendInicio.bounds.size;

            // Factor uniforme para que la huella (X, Z) quepa en el lote,
            // sin deformar el modelo
            float factor = Mathf.Min(
                tamanoOriginal.x / anchoConMargen,
                tamanoOriginal.z / largoConMargen
            );
            inicio.transform.localScale = Vector3.one * factor;

            // Tope real de la plataforma (centro + mitad del alto)
            float topeCubo = cubo.transform.position.y + (cubo.transform.localScale.y / 2f);

            // Ya escalado, medimos cuánto hay entre el pivote y la base real
            // del modelo para que quede apoyado, no flotando ni enterrado
            float alturaPivoteSobreBase = inicio.transform.position.y - rendInicio.bounds.min.y;

            inicio.transform.position = new Vector3(
                cubo.transform.position.x,
                topeCubo + alturaPivoteSobreBase,
                cubo.transform.position.z
            );
        }
        else
        {
            Debug.LogWarning("InicioPrefab no tiene Renderer en sí mismo ni en sus hijos.");
        }
    }

    void ConstruirCasas(Rect lote) //funcion para construir el bloque en la posciion del lote
    {
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(cubo);

        // Posición central
        cubo.transform.position = new Vector3(lote.center.x, 0.5f, lote.center.y);

        // Al tamaño le restamos el margen para que queden espacios (calles) entre ellos
        float anchoConMargen = Mathf.Max(1f, lote.width - margenCalles);
        float largoConMargen = Mathf.Max(1f, lote.height - margenCalles);

        cubo.transform.position = new Vector3(lote.center.x, 0.05f, lote.center.y);
        cubo.transform.localScale = new Vector3(anchoConMargen, 1f, largoConMargen);
        Color color = new Color32(106, 48, 5, 255);
        cubo.GetComponent<Renderer>().material.color = color;

        // Calcular el tamaño que tendran las casas dependiendo del lote
        float mitadMargen = margenCalles / 2f;
        float anchoDisponible = lote.width - margenCalles;
        float largoDisponible = lote.height - margenCalles;

        float pasoX = anchoDisponible / ResolucionCasas;
        float pasoZ = largoDisponible / ResolucionCasas;

        // Tamaño real de la casa (restándole el margen entre casas)
        float anchoCasa = pasoX - separacionCasas;
        float largoCasa = pasoZ - separacionCasas;
        if (anchoCasa <= 0 || largoCasa <= 0)
        {
            Debug.LogWarning("El tamaño de las casas es demasiado pequeño para el lote. Ajusta la ResoluciónCasas o separacionCasas.");
            return;
        }

        for (int fila = 0; fila < ResolucionCasas; fila++)
        {
            for (int columna = 0; columna < ResolucionCasas; columna++)
            {
                // 1. CORRECCIÓN: Calcular el centro exacto de la parcela (se añade pasoX / 2f)
                float centroX = lote.xMin + mitadMargen + (columna * pasoX) + (pasoX / 2f);
                float centroZ = lote.yMin + mitadMargen + (fila * pasoZ) + (pasoZ / 2f);

                GameObject casa = Instantiate(CasaPrefab);
                RegistrarObjetoCiudad(casa);

                // 2. CORRECCIÓN: Aumentamos la altura para que no parezcan placas

                Renderer rendCasa = casa.GetComponentInChildren<Renderer>();

                // Tamaño real del modelo tal como vino, antes de tocar su escala
                Vector3 tamanoOriginal = rendCasa.bounds.size;

                // Escala UNIFORME (mismo factor en los 3 ejes) para que quepa en el lote
                // sin deformar las proporciones del modelo
                float factor = Mathf.Min(anchoCasa * tamanoOriginal.x, largoCasa * tamanoOriginal.z);
                casa.transform.localScale = Vector3.one * (factor);
                casa.transform.localScale = new Vector3(casa.transform.localScale.x, casa.transform.localScale.z, tamanoOriginal.y * 2);

                // Ya escalado, medimos de nuevo cuánto hay entre el pivote y la base real
                // del modelo, para posicionarlo apoyado en el piso (y = 0.1f) en vez de
                // asumir que el pivote está al centro

                casa.transform.position = new Vector3(centroX, cubo.transform.localScale.y, centroZ);
            }
        }
    }

    public void ConstruirZonaArbolada(Rect lote)
    {
        // Instanciar y configurar la explanada base del lote
        GameObject explanada = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RegistrarObjetoCiudad(explanada);

        float anchoConMargen = Mathf.Max(1f, lote.width - margenCalles); // el ancho margen se encarga de que el ancho del lote no genere problemas de espacio
        float largoConMargen = Mathf.Max(1f, lote.height - margenCalles);

        // Centrar la explanada en el lote a ras de suelo
        explanada.transform.position = new Vector3(lote.center.x, 0f, lote.center.y);
        explanada.transform.localScale = new Vector3(anchoConMargen, 0.1f, largoConMargen);

        // Aplicar un color base (ej. verde oscuro) para diferenciar de los árboles
        explanada.GetComponent<Renderer>().material.color = new Color(0.15f, 0.4f, 0.15f);

        // Generar los elementos decorativos (árboles) con ruido de Perlin
        float mitadMargen = margenCalles / 2f; //despues aca se debera calcular los puntos o esquinas del cuadrado y luego asignar arboles con Lsystem
        float pasoX = (lote.width - margenCalles) / ResolucionZona;
        float pasoZ = (lote.height - margenCalles) / ResolucionZona;

        for (int fila = 0; fila < ResolucionZona; fila++)
        {
            for (int columna = 0; columna < ResolucionZona; columna++)
            {
                float puntoX = lote.xMin + mitadMargen + columna * pasoX;
                float puntoZ = lote.yMin + mitadMargen + fila * pasoZ;

                float altura = Perlinbase(puntoX, puntoZ);
                if (altura >= umbral)
                {
                    // Comprobar el molde (arbolGenerado), no el prefab
                    if (arbolGenerado != null)
                    {
                        GameObject arbolClon = Instantiate(arbolGenerado);
                        arbolClon.SetActive(true);
                        RegistrarObjetoCiudad(arbolClon);

                        arbolClon.transform.position = new Vector3(puntoX, 0.1f, puntoZ);
                        arbolClon.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                        arbolClon.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    }
                    else
                    {
                        // Sistema de emergencia (Cubo verde)
                        GameObject zoneA = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        RegistrarObjetoCiudad(zoneA);
                        zoneA.transform.position = new Vector3(puntoX, 0.15f, puntoZ);
                        zoneA.transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);
                        zoneA.GetComponent<Renderer>().material.color = Color.green;
                    }
                }
            }
        }
    }
}