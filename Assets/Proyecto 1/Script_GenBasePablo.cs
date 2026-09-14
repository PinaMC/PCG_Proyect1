using System.Collections.Generic;
using UnityEngine;

/* -------------------------------------------------------------------------
// NOTA IMPORTANTE
// -------------------------------------------------------------------------
//
// Este script es una RECONSTRUCCIÓN según lo que fuiste describiendo del
// Script_GenBase original (no me compartiste el archivo real, solo el
// resumen de su comportamiento). Los nombres de método de BSP_Script y
// SecuencialZoneGeneration son una suposición razonable según esa
// descripción -- ajústalos para que calcen con tus firmas reales; lo
// importante que sí puedes copiar tal cual es la parte de
// ConstruirZonaArbolada con Diamond-Square.
//
public class Script_GenBasePablo : MonoBehaviour
{
    [Header("Referencias")]

    [SerializeField]
    private BSP_Script bsp;

    [SerializeField]
    private SecuencialZoneGeneration gramaticaZonas;

    [SerializeField]
    private GameObject prefabEdificio;

    [SerializeField]
    private GameObject prefabPlaza;

    [SerializeField]
    private GameObject prefabArbol; // debe tener LSystemTreeGenerator (modo ThreeD)


    [Header("Vegetación / Diamond-Square")]

    [Tooltip("Tamaño de la grilla de muestreo dentro de cada lote arbolado (cantidad de puntos por eje).")]
    [SerializeField]
    private int puntosPorEje = 6;

    [Tooltip("Tamaño del heightmap Diamond-Square. Debe ser 2^n + 1.")]
    [SerializeField]
    private int tamanoHeightmap = 17;

    [Range(0.1f, 3f)]
    [SerializeField]
    private float roughness = 1.2f;

    [SerializeField]
    private int semillaHeightmap = 0;

    [Range(0f, 1f)]
    [SerializeField]
    private float umbralArbol = 0.55f;

    [Range(0f, 1f)]
    [SerializeField]
    private float rangoMaximoDiferencia = 0.45f; // (1 - umbral) aprox., para normalizar

    [SerializeField]
    private int minIteraciones = 1;

    [SerializeField]
    private int maxIteraciones = 4;


    // -------------------------------------------------------------------------
    // ORQUESTACIÓN PRINCIPAL
    // -------------------------------------------------------------------------
    public void GenerarTodo()
    {
        List<Lote> lotes = bsp.GenerarLotes();

        int pasos = Mathf.Max(0, lotes.Count - 2);
        string cadena = gramaticaZonas.Generar(pasos);

        if (cadena.Length != lotes.Count)
        {
            Debug.LogWarning(
                "Script_GenBase: el largo de la cadena (" + cadena.Length +
                ") no calza con la cantidad de lotes (" + lotes.Count + ")."
            );
        }

        int total = Mathf.Min(cadena.Length, lotes.Count);

        for (int i = 0; i < total; i++)
        {
            ConstruirZona(cadena[i], lotes[i]);
        }
    }

    private void ConstruirZona(char tipo, Lote lote)
    {
        switch (tipo)
        {
            case 'S':
                // Punto de inicio: marcador, sin construcción pesada.
                break;

            case 'E':
                ConstruirEdificio(lote);
                break;

            case 'V':
                ConstruirZonaArbolada(lote);
                break;

            case 'P':
                ConstruirPlaza(lote);
                break;

            case 'G':
                // Meta: marcador, sin construcción pesada.
                break;
        }
    }

    private void ConstruirEdificio(Lote lote)
    {
        if (prefabEdificio == null) return;

        Vector3 centro = new Vector3(
            lote.x + lote.ancho * 0.5f,
            0f,
            lote.z + lote.profundidad * 0.5f
        );

        Instantiate(prefabEdificio, centro, Quaternion.identity, transform);
    }

    private void ConstruirPlaza(Lote lote)
    {
        if (prefabPlaza == null) return;

        Vector3 centro = new Vector3(
            lote.x + lote.ancho * 0.5f,
            0f,
            lote.z + lote.profundidad * 0.5f
        );

        Instantiate(prefabPlaza, centro, Quaternion.identity, transform);
    }


    // -------------------------------------------------------------------------
    // ZONA ARBOLADA: DIAMOND-SQUARE + DIFERENCIA ALTURA-UMBRAL -> ITERACIONES
    // -------------------------------------------------------------------------
    private void ConstruirZonaArbolada(Lote lote)
    {
        if (prefabArbol == null) return;

        // Un heightmap propio por lote (misma semilla + offset del lote para
        // que lotes distintos no queden idénticos, pero sí reproducibles).
        int semillaLote = semillaHeightmap + lote.x * 73856093 ^ lote.z * 19349663;
        float[,] heightmap = DiamondSquareGenerator.Generate(
            tamanoHeightmap,
            roughness,
            semillaLote
        );

        for (int fila = 0; fila < puntosPorEje; fila++)
        {
            for (int columna = 0; columna < puntosPorEje; columna++)
            {
                // u, v en [0,1] dentro del lote -> coordenadas del punto en el mundo.
                float u = (columna + 0.5f) / puntosPorEje;
                float v = (fila + 0.5f) / puntosPorEje;

                float puntoX = lote.x + u * lote.ancho;
                float puntoZ = lote.z + v * lote.profundidad; // fila -> v, columna -> u (ya corregido)

                float altura = DiamondSquareGenerator.Sample(
                    heightmap,
                    tamanoHeightmap,
                    u,
                    v
                );

                float diferencia = altura - umbralArbol;

                if (diferencia <= 0f)
                {
                    continue; // no nace árbol en este punto
                }

                float diferenciaNormalizada = Mathf.InverseLerp(
                    0f,
                    rangoMaximoDiferencia,
                    diferencia
                );

                int iteraciones = Mathf.RoundToInt(
                    Mathf.Lerp(minIteraciones, maxIteraciones, diferenciaNormalizada)
                );

                Vector3 posicion = new Vector3(puntoX, 0f, puntoZ);

                GameObject instancia = Instantiate(
                    prefabArbol,
                    posicion,
                    Quaternion.identity,
                    transform
                );

                LSystemTreeGenerator generadorArbol =
                    instancia.GetComponent<LSystemTreeGenerator>();

                if (generadorArbol != null)
                {
                    generadorArbol.SetGenerationMode(LSystemTreeGenerator.GenerationMode.ThreeD);
                    generadorArbol.SetIterations(iteraciones);
                    generadorArbol.GenerateTree();
                }
            }
        }
    }
}


// -------------------------------------------------------------------------
// Lote: reemplaza esto por el tipo real que devuelve tu BSP_Script si ya
// tienes uno (Rect, Bounds, o una clase propia) -- lo dejo explícito acá
// solo para que el ejemplo compile de forma autocontenida.
// -------------------------------------------------------------------------
public struct Lote
{
    public float x;
    public float z;
    public float ancho;
    public float profundidad;
}
*/