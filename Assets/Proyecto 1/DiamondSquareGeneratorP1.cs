using UnityEngine;
using System;

// -------------------------------------------------------------------------
// DIAMOND-SQUARE
// -------------------------------------------------------------------------
//
// Se genera un heightmap fractal sobre una grilla cuadrada de tamaño
// (2^n + 1) x (2^n + 1). 
// Esta compienza desde las 4 esquinas con un valor semilla
// aleatorio y se subdivide recursivamente en dos "pasadas" por nivel:
//
//      DIAMOND: para cada celda, el punto central se calcula como el
//               promedio de las 4 esquinas + un desplazamiento aleatorio.
//
//      SQUARE:  para cada punto medio de arista, se calcula como el
//               promedio de sus vecinos en cruz (2 a 4 según si está
//               en el borde) + un desplazamiento aleatorio.
//
// La magnitud del desplazamiento aleatorio ("scale") decrece en cada
// nivel según "roughness": valores altos de roughness generan un
// decaimiento más rápido -> terreno más suave; valores bajos generan
// un terreno más quebrado/accidentado.
//
public static class DiamondSquareGeneratorP1
{
    // size DEBE ser 2^n + 1 (ej: 9, 17, 33, 65...).
    public static float[,] Generate(int size, float roughness, int seed)
    {
        if (!IsValidSize(size))
        {
            throw new ArgumentException(
                "DiamondSquareGenerator: size debe ser 2^n + 1 (9, 17, 33, 65...)."
            );
        }

        System.Random rng = new System.Random(seed);

        float[,] map = new float[size, size];
        int max = size - 1;

        // Semillas iniciales en las 4 esquinas.
        map[0, 0] = (float)rng.NextDouble();
        map[0, max] = (float)rng.NextDouble();
        map[max, 0] = (float)rng.NextDouble();
        map[max, max] = (float)rng.NextDouble();

        float scale = 1f;
        int step = max;

        while (step > 1)
        {
            int half = step / 2;

            // ---- Paso Diamond ----
            for (int x = 0; x < max; x += step)
            {
                for (int y = 0; y < max; y += step)
                {
                    float avg =
                        (map[x, y] +
                         map[x + step, y] +
                         map[x, y + step] +
                         map[x + step, y + step]) * 0.25f;

                    map[x + half, y + half] =
                        avg + RandomOffset(rng, scale);
                }
            }

            // ---- Paso Square ----
            for (int x = 0; x < size; x += half)
            {
                int startY = (x / half) % 2 == 0 ? half : 0;

                for (int y = startY; y < size; y += step)
                {
                    float sum = 0f;
                    int count = 0;

                    if (x - half >= 0) { sum += map[x - half, y]; count++; }
                    if (x + half <= max) { sum += map[x + half, y]; count++; }
                    if (y - half >= 0) { sum += map[x, y - half]; count++; }
                    if (y + half <= max) { sum += map[x, y + half]; count++; }

                    map[x, y] = (sum / count) + RandomOffset(rng, scale);
                }
            }

            step = half;
            scale *= Mathf.Pow(2f, -roughness);
        }

        NormalizeInPlace(map, size);

        return map;
    }

    // Muestreo con interpolación bilineal para un punto (u, v) en [0,1] x [0,1],
    // útil para consultar la altura de un punto arbitrario del lote sin
    // depender de que caiga exactamente sobre una celda de la grilla.
    public static float Sample(float[,] map, int size, float u, float v)
    {
        float fx = Mathf.Clamp01(u) * (size - 1);
        float fy = Mathf.Clamp01(v) * (size - 1);

        int x0 = Mathf.FloorToInt(fx);
        int y0 = Mathf.FloorToInt(fy);
        int x1 = Mathf.Min(x0 + 1, size - 1);
        int y1 = Mathf.Min(y0 + 1, size - 1);

        float tx = fx - x0;
        float ty = fy - y0;

        float a = Mathf.Lerp(map[x0, y0], map[x1, y0], tx);
        float b = Mathf.Lerp(map[x0, y1], map[x1, y1], tx);

        return Mathf.Lerp(a, b, ty);
    }

    private static float RandomOffset(System.Random rng, float scale)
    {
        return ((float)rng.NextDouble() * 2f - 1f) * scale;
    }

    private static bool IsValidSize(int size)
    {
        int n = size - 1;

        if (n <= 0 || (n & (n - 1)) != 0)
        {
            return false;
        }

        return true;
    }

    // Reescala todos los valores a [0,1], porque los offsets aleatorios
    // acumulados pueden sacar el heightmap fuera de ese rango.
    private static void NormalizeInPlace(float[,] map, int size)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (map[x, y] < min) min = map[x, y];
                if (map[x, y] > max) max = map[x, y];
            }
        }

        float range = Mathf.Max(0.0001f, max - min);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                map[x, y] = (map[x, y] - min) / range;
            }
        }
    }
}
