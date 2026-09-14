using UnityEngine;
using System.Collections.Generic;

public class RandomWalk : MonoBehaviour
{
    [Header("Mapa de Recorrido (Random Walk)")]
    public float radioEsferaZona = 1.5f;
    public float alturaMapaRecorrido = 8f; // Y donde flota el mapa, para no chocar con lo demás
    public float radioTuberia = 0.3f;

    private static readonly Dictionary<char, (string nombre, Color color)> InfoZonas =
        new Dictionary<char, (string, Color)>()
    {
        { 'S', ("Inicio",   new Color(0.59f, 1f, 0.52f)) },
        { 'E', ("Edificio", new Color(1f, 0.55f, 0.2f))  },
        { 'P', ("Casas",    Color.cyan)                   },
        { 'G', ("Plaza",    Color.yellow)                 },
    };
    private static readonly (string nombre, Color color) InfoZonaVerde =
        ("Verde", new Color(0.15f, 0.7f, 0.15f));

    private List<GameObject> objetosMapa = new List<GameObject>();

    // Contenedor "Alcantarillado": se crea una sola vez y se reutiliza entre regeneraciones
    private Transform contenedorAlcantarillado;

    public void GenerarMapa(List<Rect> lotes, string mapa)
    {
        LimpiarMapa();

        if (contenedorAlcantarillado == null)
        {
            GameObject alcantObj = new GameObject("Alcantarillado");
            contenedorAlcantarillado = alcantObj.transform;
        }

        if (lotes == null || lotes.Count == 0 || string.IsNullOrEmpty(mapa)) return;

        // Random walk: orden aleatorio de los índices de lote
        List<int> orden = new List<int>();
        for (int i = 0; i < lotes.Count; i++) orden.Add(i);
        for (int i = orden.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (orden[i], orden[j]) = (orden[j], orden[i]);
        }

        // Tuberías entre centros consecutivos del recorrido
        for (int i = 0; i < orden.Count - 1; i++)
        {
            Vector3 a = CentroFlotante(lotes[orden[i]]);
            Vector3 b = CentroFlotante(lotes[orden[i + 1]]);
            CrearTuberia(a, b);
        }

        // Esfera + nombre por cada zona
        int limite = Mathf.Min(lotes.Count, mapa.Length);
        for (int i = 0; i < limite; i++)
        {
            char zona = mapa[i];
            var info = InfoZonas.ContainsKey(zona) ? InfoZonas[zona] : InfoZonaVerde;
            CrearEsferaZona(CentroFlotante(lotes[i]), info.color, info.nombre);
        }
    }

    public void LimpiarMapa()
    {
        foreach (GameObject obj in objetosMapa)
        {
            if (obj != null) Destroy(obj);
        }
        objetosMapa.Clear();
    }

    // Llamado desde Script_GenBase para mostrar/ocultar toda la capa
    // "Alcantarillado" según el estado del toggle de UI.
    public void SetVisible(bool visible)
    {
        if (contenedorAlcantarillado != null)
        {
            contenedorAlcantarillado.gameObject.SetActive(visible);
        }
    }

    private Vector3 CentroFlotante(Rect lote)
    {
        return new Vector3(lote.center.x, alturaMapaRecorrido, lote.center.y);
    }

    private void CrearTuberia(Vector3 a, Vector3 b)
    {
        GameObject tuberia = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        objetosMapa.Add(tuberia);
        tuberia.transform.SetParent(contenedorAlcantarillado, true);

        tuberia.transform.position = (a + b) / 2f;
        tuberia.transform.up = (b - a).normalized; // el cilindro crece en su eje "up"
        tuberia.transform.localScale = new Vector3(radioTuberia, Vector3.Distance(a, b) / 2f, radioTuberia);
        tuberia.GetComponent<Renderer>().material.color = Color.gray;
    }

    private void CrearEsferaZona(Vector3 centro, Color color, string nombre)
    {
        GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = "Zona_" + nombre;
        objetosMapa.Add(esfera);
        esfera.transform.SetParent(contenedorAlcantarillado, true);

        esfera.transform.position = centro;
        esfera.transform.localScale = Vector3.one * radioEsferaZona;
        esfera.GetComponent<Renderer>().material.color = color;

        GameObject textoObj = new GameObject("Etiqueta_" + nombre);
        objetosMapa.Add(textoObj);
        textoObj.transform.SetParent(contenedorAlcantarillado, true);
        textoObj.transform.position = centro + Vector3.up * (radioEsferaZona * 0.7f);

        TextMesh texto = textoObj.AddComponent<TextMesh>();
        texto.text = nombre;
        texto.characterSize = 0.3f;
        texto.fontSize = 48;
        texto.anchor = TextAnchor.MiddleCenter;
        texto.alignment = TextAlignment.Center;
        texto.color = Color.black;
    }
}