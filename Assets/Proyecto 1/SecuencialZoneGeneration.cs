using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class SecuencialZoneGeneration : MonoBehaviour
{
    [SerializeField] private int semilla = 12345;

    [SerializeField] private string simboloInicial = "A"; // "A" = Zona no Asignada
    [SerializeField] private string produccionInicial = "SZG"; // S = Entrada (Start)
                                                               // Z = Zona por Definir
                                                               // G = Plaza (Goal)
    [SerializeField] private string simboloZona = "Z";
    [SerializeField] private List<string> produccionZonas =
        new List<string>()
        {
            "EZ", //E = Edificio
            "VZ", //V  = Zona Verde
            "PZ", //P = Casas
   //       "BZT" // Puente a Torre
        };
    [SerializeField] private string simboloFinal = "V";

    public int Semilla { get => semilla; set => semilla = value; }
    public string SimboloInicial { get => simboloInicial; set => simboloInicial = value; }
    public string ProduccionInicial { get => produccionInicial; set => produccionInicial = value; }
    public string SimboloZona { get => simboloZona; set => simboloZona = value; }
    public string SimboloFinal { get => simboloFinal; set => simboloFinal = value; }
    public List<string> ProduccionZonas { get => produccionZonas; set => produccionZonas = value; }

    public string ProduccionZonasComoTexto
    {
        get => produccionZonas != null ? string.Join(", ", produccionZonas) : "";
        set
        {
            if (produccionZonas == null) produccionZonas = new List<string>();
            else produccionZonas.Clear();
            if (!string.IsNullOrEmpty(value))
            {
                string[] partes = value.Split(new char[] { ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (string p in partes)
                {
                    string trimmed = p.Trim();
                    if (!string.IsNullOrEmpty(trimmed)) produccionZonas.Add(trimmed);
                }
            }
        }
    }

    public string GenerarZona(int expansionSteps)
    {
        if (!ValidateGrammar())
        {
            return null;
        }
        if (expansionSteps < 3)
        {
            Debug.LogWarning("la cantidad de lotes debe ser mayor a 3",this);
            return null;
        }
        System.Random Randomizador = new System.Random(semilla);
        string mapa = produccionInicial;
        List<string> derivaciones = new List<string>(); // Historial de cambios
        derivaciones.Add(mapa);// se agrega el mapa como esta

        // expandir el mapa cantidad de veces asignadas
        for (int paso = 0; paso < expansionSteps-3; paso++)
        {
            int posicion = mapa.IndexOf(simboloZona);// buscar en el string "Z"
            if (posicion == -1) // Si no se encuentra simboloZona termina las iteraciones 
            { Debug.LogWarning(simboloZona + " No encontrado.");
                break;
            }
            int indice = Randomizador.Next(produccionZonas.Count);// Seleccionar zonas al azar
            string produccion = produccionZonas[indice];
            //se remplaza el produccion Zonas por la tarea obtenida aleatoriamente
            string fpart = mapa.Substring(0, posicion); //recortar hasta antes del symbolo
            string spart = mapa.Substring(posicion + simboloZona.Length); // la parte despues del symbolo
            mapa = fpart + produccion + spart;

            derivaciones.Add(produccion);
        }
        mapa = mapa.Replace(simboloZona, simboloFinal);
        derivaciones.Add(mapa);
        return mapa;
    }


    private bool ValidateGrammar()
    {
        // Si el simbolo inicial esta vacio no inicia
        if (string.IsNullOrEmpty(simboloInicial)) {
            Debug.LogError("el simboloInicial no puede estar vacio", this);
            return false;
        }
        // Si la produccion inicial esta vacia no inicia
        if (string.IsNullOrEmpty(produccionInicial))
        {
            Debug.LogError("el produccionInicial no puede estar vacio", this);
            return false;
        }
        // Si el simbolo de zona esta vacio no inicia
        if (string.IsNullOrEmpty(simboloZona))
        {
            Debug.LogError("el simboloZona no puede estar vacio", this);
            return false;
        }
        // Exiten zonas para seguir generando mas
        if (produccionZonas == null || produccionZonas.Count == 0)
        {
            Debug.LogError("Debe existir al menos una zona para generar", this);
            return false;
        }
        // Si el simbolo de zona esta vacio no inicia
        if (string.IsNullOrEmpty(simboloFinal))
        {
            Debug.LogError("el SimboloFinal no puede estar vacio", this);
            return false;
        }
        // Si el simbolo de zona esta vacio no inicia
        if (simboloFinal.Contains(simboloZona))
        {
            Debug.LogError("simboloFinal no puede contener '"+
                simboloZona +"'", this);
            return false;
        }

        return true;
    }
}
