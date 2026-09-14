LABORATORIO 2 - GENERACIÓN PROCEDURAL DE TERRENOS
====================================================

CONTENIDO DEL PAQUETE

Scripts/
    TerrainGenerator.cs
    HeightmapGenerator.cs
    PerlinNoiseGenerator.cs
    DiamondSquareGenerator.cs

Editor/
    TerrainGeneratorEditor.cs


CONFIGURACIÓN EN UNITY

1. Copie la carpeta completa dentro de Assets/Laboratorios/Lab2/TerrainGeneration/
   o en una ubicación equivalente dentro de Assets.

2. IMPORTANTE:
   TerrainGeneratorEditor.cs debe permanecer dentro de una carpeta llamada "Editor".

3. Cree una nueva escena.

4. Cree un GameObject vacío llamado:
       TerrainGenerator

5. Añada el componente:
       TerrainGenerator.cs

6. El terreno será creado automáticamente por la herramienta.

7. Use el Inspector para seleccionar el método de generación:
       - Random Noise
       - Value Noise
       - Perlin Noise
       - Diamond-Square

8. Los métodos incompletos están marcados con:
       TODO

9. La infraestructura de Unity, la creación del Terrain, la visualización por
   alturas y la selección de parámetros ya se encuentran implementadas.

10. El código entregado compila aunque los métodos evaluados todavía no estén
    implementados. Los resultados incompletos pueden aparecer planos o parciales
    hasta completar cada TODO.


NOTA SOBRE AUTO UPDATE

Mientras se programa puede desactivar "Auto Update" para evitar regenerar el
terreno cada vez que se modifica un parámetro. Puede utilizar el botón
"Generate Terrain" para probar manualmente.


OBJETIVO

Completar las partes indicadas en los scripts para implementar:
    - Random Noise
    - Value Noise
    - Interpolación bilineal
    - Interpolación bicúbica
    - Perlin / Gradient Noise 2D
    - Diamond-Square
