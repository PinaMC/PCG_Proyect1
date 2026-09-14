using System.Collections.Generic;
using UnityEngine;


// Esta clase almacena la informacion matematica de cada pedazo de terreno.
public class NodoBSP
{
    // Parte creando un rectangulo, despues se dividen en dos y se crean hijos, hasta que se cumpla la condicion de detencion
    public Rect espacio; // Las dimensiones x, y, ancho y alto de este nodo
    public NodoBSP hijoIzquierdo;
    public NodoBSP hijoDerecho;

    // Constructor se encarga de inicializar el rectangulo del nodo
    public NodoBSP(Rect espacioInicial)
    {
        this.espacio = espacioInicial;
    }

    // Metodo para saber si es un lote final (no tiene hijos)
    public bool EsHoja()
    {
        return hijoIzquierdo == null && hijoDerecho == null; // Si no tiene hijos, es una hoja
    }
}

public class BSP_Script : MonoBehaviour
{
    [Header("Configuracion BSP")]
    public float tamanoMinimoLote = 10f; // condicion de parada por tamaño, si el lote es menor a este tamaño no se divide mas

    // Esta es la funcion q se llamará desde "GeneradorBase".
    public List<Rect> EjecutarBSP(Rect areaInicial, int profundidadMaxima)
    {
        List<Rect> lotesFinales = new List<Rect>(); //Almacena el lote final que se devolvera en el genbase
        
        // Paso 1: Crear el nodo raíz con el área inicial
        NodoBSP raiz = new NodoBSP(areaInicial);

        // Paso 2 y 3: Iniciar la recursividad
        DividirNodo(raiz, profundidadMaxima);

        // Paso 4: Recolectar solo las hojas (los lotes donde construiremos)
        ObtenerHojas(raiz, lotesFinales);

        return lotesFinales; // Le devolvemos la lista de Rectángulos al GeneradorBase
    }

    // La funcion recursiva que hace los cortes
    private void DividirNodo(NodoBSP nodo, int profundidadMaxima)
    {
        // CONDICION DE PARADA: Detenemos la recursividad si la profundidad llega a 0
        if (profundidadMaxima <= 0) return;

        // Si el nodo ya es muy pequeño, nos detenemos (Condicion de parada por tamaño)
        if (nodo.espacio.width <= tamanoMinimoLote * 2 || nodo.espacio.height <= tamanoMinimoLote * 2)
        {
            return;
        }

        // Lógica del corte: Vertical u Horizontal?
        bool cortarVertical = Random.value > 0.5f;

        // Si el terreno es mucho mas ancho que alto, forzamos un corte vertical, y viceversa.
        if (nodo.espacio.width > nodo.espacio.height * 1.5f) cortarVertical = true;
        else if (nodo.espacio.height > nodo.espacio.width * 1.5f) cortarVertical = false;

        if (cortarVertical)
        {
            // Cortamos a lo ancho. Calculamos un punto aleatorio entre el 30% y el 70%
            float puntoCorte = Random.Range(nodo.espacio.width * 0.3f, nodo.espacio.width * 0.7f);

            // Creamos los dos Rect hijos
            Rect RectIzquierdo = new Rect(nodo.espacio.x, nodo.espacio.y, puntoCorte, nodo.espacio.height);
            Rect RectDerecho = new Rect(nodo.espacio.x + puntoCorte, nodo.espacio.y, nodo.espacio.width - puntoCorte, nodo.espacio.height);

            // Asignamos los hijos al nodo actual
            nodo.hijoIzquierdo = new NodoBSP(RectIzquierdo);
            nodo.hijoDerecho = new NodoBSP(RectDerecho);
        }
        else // Corte Horizontal
        {
            float puntoCorte = Random.Range(nodo.espacio.height * 0.3f, nodo.espacio.height * 0.7f);

            Rect RectArriba = new Rect(nodo.espacio.x, nodo.espacio.y, nodo.espacio.width, puntoCorte);
            Rect RectAbajo = new Rect(nodo.espacio.x, nodo.espacio.y + puntoCorte, nodo.espacio.width, nodo.espacio.height - puntoCorte);

            nodo.hijoIzquierdo = new NodoBSP(RectArriba);
            nodo.hijoDerecho = new NodoBSP(RectAbajo);
        }

        // *RECURSIVIDAD: Mandamos a dividir a los hijos restando 1 a la profundidad
        DividirNodo(nodo.hijoIzquierdo, profundidadMaxima - 1);
        DividirNodo(nodo.hijoDerecho, profundidadMaxima - 1);
    }

    // Funcion auxiliar para extraer solo los lotes finales
    private void ObtenerHojas(NodoBSP nodo, List<Rect> listaHojas)
    {
        if (nodo == null) return;

        if (nodo.EsHoja())
        {
            listaHojas.Add(nodo.espacio);
        }
        else
        {
            ObtenerHojas(nodo.hijoIzquierdo, listaHojas);
            ObtenerHojas(nodo.hijoDerecho, listaHojas);
        }
    }
}