# Super Mega Villa Builder

## Descripción breve
Generador procedural de pueblos/villas en Unity. A partir de un terreno cuadrado, el sistema divide el espacio en lotes, decide qué tipo de zona va en cada uno (casas, edificio, plaza, inicio, zona verde), construye cada zona con primitivas/prefabs, arma un "mapa de alcantarillado" que conecta todas las zonas, y genera árboles proceduralmente con L-Systems. Todo se puede regenerar en tiempo real y ajustar desde una UI en pantalla.

## Integrantes
- Vicente Farías
- Martín Vera
- Pablo Gutiérrez

## Técnicas PCG utilizadas
- **BSP (Binary Space Partitioning):** divide el terreno en lotes rectangulares cortando recursivamente el espacio en dos (`BSP_Script.cs`).
- **Gramática de reemplazo de símbolos (tipo L-System):** decide qué tipo de zona corresponde a cada lote, expandiendo un símbolo inicial paso a paso con una semilla reproducible (`SecuencialZoneGeneration.cs`).
- **L-System con turtle graphics (2D/3D):** genera la geometría de los árboles a partir de una gramática y reglas de producción (`LSystemTreeGeneratorP1.cs`).
- **Ruido de Perlin:** decide la densidad/ubicación de árboles dentro de una zona arbolada (`ConstruirZonaArbolada` en `Script_GenBase.cs`).
- **Random Walk:** recorre los lotes en orden aleatorio para trazar el "alcantarillado" que conecta todas las zonas (`RandomWalk.cs`).

## Instrucciones básicas de ejecución
1. Abrir el proyecto en Unity y cargar la escena principal (la que contiene `Script_GenBase`).
2. Presionar **Play**: la ciudad se genera automáticamente al iniciar (`Start()` llama a `GenerarEscenario()`).
3. Presionar **Espacio** en cualquier momento para regenerar la ciudad (nuevo layout).
4. Presionar **Tab** para mostrar/ocultar el panel de parámetros (`RuntimeParameterUI`).
5. Desde ese panel se pueden ajustar valores y volver a generar sin salir del ejecutable; también incluye control de cámara (clic derecho para rotar, clic medio o WASD para desplazar, rueda del mouse para zoom).
6. Botón "GENERAR CIUDAD DE NUEVO", "Centrar Vista" y "Volver a lo Original" (restaura los valores por defecto) están disponibles en el panel.

## Principales parámetros configurables
Desde el Inspector o desde el panel `RuntimeParameterUI` en tiempo real:
- **Terreno / BSP:** `tamanoTerreno`, `profundidadDivision`, `tamanoMinimoLote`, `margenCalles`.
- **Casas:** `ResolucionCasas` (casas por fila/columna del lote), `separacionCasas`.
- **Edificios:** `ResolucionEdificio`, `separacionEdificio`, `umbralEdificio`.
- **Zona arbolada:** `ResolucionZona`, `umbral` (umbral del ruido de Perlin para decidir si va un árbol).
- **Árboles (L-System):** `iteracionesArbol`, `anguloArbol`, `largoSegmentoArbol`, `grosorRamaArbol` (además del modo 2D/3D, axioma y reglas propias de `LSystemTreeGeneratorP1`).
- **Gramática de zonas:** `semilla` (reproducibilidad), `produccionInicial`, `produccionZonas` (lista de reglas), `simboloInicial`, `simboloZona`, `simboloFinal`.
- **Visibilidad de capas:** alternar entre "Mostrar Ciudad" y "Mostrar Alcantarillado".

## Colores utilizados

**Ciudad (construcción real de las zonas):**

| Elemento | Color | Valor en código |
|---|---|---|
| Calles / base del terreno | Gris oscuro | `Color(0.2, 0.2, 0.2)` |
| Plataforma de Inicio | Verde claro | `Color32(150, 255, 133, 255)` |
| Base de las Casas | Café / marrón | `Color32(106, 48, 5, 255)` |
| Base de los Edificios | Gris claro | `Color32(200, 200, 200, 255)` |
| Explanada de la Plaza | Amarillo | `Color.yellow` |
| Edificio público, pilares y techo de la Plaza | Blanco | `Color.white` |
| Ventanas de la Plaza | Negro | `Color.black` |
| Explanada de la Zona Arbolada | Verde oscuro | `Color(0.15, 0.4, 0.15)` |
| Cubo de emergencia (si falta el prefab de árbol) | Verde | `Color.green` |

**Árboles (L-System, color según altura de la rama):**

| Elemento | Color | Valor en código |
|---|---|---|
| Tronco (parte baja) | Café oscuro | `Color(0.35, 0.17, 0.06)` |


**Mapa de recorrido / Alcantarillado (`RandomWalk`):**

| Zona | Color | Valor en código |
|---|---|---|
| Inicio (S) | Verde claro | `Color(0.59, 1, 0.52)` |
| Edificio (E) | Naranjo | `Color(1, 0.55, 0.2)` |
| Casas (P) | Cyan | `Color.cyan` |
| Plaza (G) | Amarillo | `Color.yellow` |
| Verde (cualquier otra zona) | Verde | `Color(0.15, 0.7, 0.15)` |
| Tuberías de conexión | Gris | `Color.gray` |
| Etiquetas de texto | Negro | `Color.black` |

## Idea general

El flujo completo se dispara al iniciar la escena (o al presionar **Espacio**) desde `Script_GenBase`, que actúa como orquestador:

1. **División del terreno (BSP):** `BSP_Script` parte el terreno cuadrado en lotes rectangulares usando un árbol binario (BSP — Binary Space Partitioning).
2. **Asignación de zonas (gramática secuencial):** `SecuencialZoneGeneration` decide, con una gramática tipo L-System de una sola letra por paso, qué tipo de zona (casa, edificio, plaza, etc.) le corresponde a cada lote.
3. **Construcción de la ciudad:** `Script_GenBase` recorre los lotes y, según la letra asignada, instancia casas, edificios, la plaza o el punto de inicio, además de rellenar con árboles las zonas no asignadas.
4. **Árboles (L-System 3D/2D):** `LSystemTreeGeneratorP1` genera la geometría de cada árbol con turtle graphics a partir de una gramática L-System.
5. **Mapa de recorrido / alcantarillado:** `RandomWalk` conecta los centros de los lotes con "tuberías" en un orden aleatorio, y marca cada zona con una esfera de color y una etiqueta de texto.
6. **Interfaz en tiempo real:** `RuntimeParameterUI` dibuja una ventana (IMGUI) para tocar todos los parámetros anteriores sin salir del ejecutable, además de manejar una cámara orbital simple.

## Scripts del proyecto

### `BSP_Script.cs`
Implementa la partición binaria del espacio (BSP).
- `NodoBSP`: nodo del árbol binario, guarda un `Rect` y referencias a sus dos hijos.
- `EjecutarBSP(areaInicial, profundidadMaxima)`: crea el nodo raíz con el terreno completo y dispara la recursión. Devuelve la lista de rectángulos hoja (los lotes finales).
- `DividirNodo(...)`: corta el rectángulo del nodo en dos, alternando entre corte vertical u horizontal (si el rectángulo es muy alargado, fuerza el corte en el sentido correcto para evitar lotes muy delgados). El punto de corte es aleatorio entre 30% y 70% del ancho/alto. Se detiene por profundidad máxima o por tamaño mínimo de lote (`tamanoMinimoLote`).
- `ObtenerHojas(...)`: recorre el árbol y junta solo los nodos sin hijos (los lotes utilizables).

### `SecuencialZoneGeneration.cs`
Es la "gramática" que decide qué va en cada lote, mediante reemplazo de símbolos (similar a un L-System de una sola derivación por paso).
- Empieza con una `produccionInicial` (por defecto `"SZG"`: Start, Zona por definir, Goal/plaza).
- En cada paso busca el símbolo de zona (`Z`) y lo reemplaza por una producción aleatoria de la lista `produccionZonas` (por defecto `EZ`, `VZ`, `PZ`: Edificio, Verde, Casas — cada una deja un nuevo `Z` pendiente para seguir expandiendo).
- Usa un `System.Random` con semilla (`semilla`) para que el resultado sea reproducible.
- Al terminar las iteraciones (o si ya no queda `Z`), reemplaza los `Z` restantes por el símbolo final (`V`, Verde) para no dejar zonas sin definir.
- `ValidateGrammar()` valida que la configuración tenga sentido (símbolos no vacíos, al menos una producción, que el símbolo final no contenga al símbolo de zona, etc.) antes de generar.
- El resultado es un string donde cada carácter le indica a `Script_GenBase` qué construir en el lote correspondiente (mismo orden que la lista de lotes del BSP).

### `Script_GenBase.cs`
Es el director de orquesta: conecta el BSP, la gramática, los árboles y el random walk, y construye la escena.
- `GenerarEscenario()`: limpia lo generado antes, crea el terreno/calles, pide los lotes al BSP, pide el mapa de zonas a la gramática secuencial y, para cada lote, llama al método de construcción según la letra que le tocó:
  - `S` → `ConstruirInicio` (plataforma de inicio + prefab).
  - `E` → `ConstruirEdificio` (cuadrícula de edificios dentro del lote).
  - `P` → `ConstruirCasas` (cuadrícula de casas dentro del lote).
  - `G` → `ConstruirPlaza` (explanada + edificio público con ventanas y pórtico, todo generado con primitivas).
  - cualquier otra letra → `ConstruirZonaArbolada` (rellena el lote con árboles usando ruido de Perlin para decidir dónde poner cada uno).
- Antes de construir, genera **una** copia "molde" del árbol L-System (`arbolGenerado`) y la deja desactivada; luego la clona en cada punto de la zona arbolada en vez de regenerar el L-System por cada árbol (más barato).
- Al final llama a `RandomWalk.GenerarMapa(...)` para armar el "alcantarillado" y aplica la visibilidad de capas (Ciudad vs. Alcantarillado) mediante `MostrarCiudad()` / `MostrarAlcantarillado()`.
- Guarda todos los objetos instanciados en una lista para poder destruirlos y regenerar limpio cada vez (con la tecla Espacio).

### `LSystemTreeGeneratorP1.cs`
Genera árboles con turtle graphics a partir de una gramática L-System, en modo 2D o 3D.
- Tiene un axioma y una lista de reglas de reemplazo (`LSystemRule`) por separado para 2D y 3D, más número de iteraciones y ángulo de giro.
- `GenerateTree()`: expande la gramática (`ParallelGrammarGenerator.Generate`) y luego interpreta el string resultante con una tortuga:
  - `F` avanza y dibuja una rama (cilindro).
  - `f` avanza sin dibujar.
  - `+` / `-` giran en el plano (yaw).
  - `&` / `^` inclinan la rama (pitch) usando un eje perpendicular al rumbo actual.
  - `\` / `/` rotan el propio "frame" de la tortuga sobre su eje (roll), usado para separar simétricamente las ramas de un verticilo (por ejemplo, tipo pino).
  - `[` / `]` guardan y restauran el estado (posición + rotación) en una pila, para poder ramificar.
- Expone getters/setters (`SetIterations`, `SetAngle`, etc.) para que `Script_GenBase` y `RuntimeParameterUI` puedan configurarlo sin tocar el Inspector.

### `RandomWalk.cs`
Genera visualmente una capa alternativa ("Alcantarillado") que conecta todos los lotes.
- Baraja el orden de los lotes al azar (Fisher-Yates) y dibuja un cilindro ("tubería") entre los centros de cada par consecutivo, flotando a una altura fija (`alturaMapaRecorrido`) para no chocar con la ciudad.
- Sobre cada lote pone una esfera de color y una etiqueta de texto (`TextMesh`) según el tipo de zona (Inicio, Edificio, Casas, Plaza o Verde).
- `SetVisible(bool)` permite mostrar/ocultar toda esta capa sin destruirla, lo que usa `Script_GenBase` para alternar entre la vista "Ciudad" y la vista "Alcantarillado".

### `RuntimeParameterUI.cs`
UI en tiempo de ejecución (IMGUI) pensada para que cualquiera, sin tocar código, pueda ajustar la generación.
- Busca automáticamente las referencias a `Script_GenBase`, `BSP_Script` y `SecuencialZoneGeneration` en la escena si no están asignadas.
- Dibuja una ventana arrastrable con secciones plegables para: configuración del BSP, casas, árboles, la gramática de zonas (semilla, reglas, símbolos) y qué capa mostrar (Ciudad/Alcantarillado).
- Botón para regenerar semilla al azar, botón para regenerar la ciudad, botón para volver a los valores por defecto guardados al iniciar, y atajos de teclado (**Tab** para ocultar la UI, **Espacio** para regenerar).
- Incluye un control de cámara orbital simple (clic derecho para rotar, clic medio o WASD para desplazar el pivote, rueda del mouse para hacer zoom), que se desactiva mientras el mouse está sobre la ventana de la UI.

## Controles
- Click derecho para mover la rotación de la cámara 
- WASD para mover la posicion de la cámara (acercar o alejarse)
- Espacio para rehacer la generación 

## Notas
- Todo se regenera destruyendo y volviendo a crear los objetos (no hay pooling), lo cual es simple pero puede notarse al presionar Espacio muy seguido con terrenos grandes.
- La generación de zonas usa una semilla (`semilla` en `SecuencialZoneGeneration`) para ser reproducible, pero el corte del BSP y el orden del random walk usan `UnityEngine.Random` sin sembrar explícitamente, por lo que el resultado geométrico varía entre corridas aunque la semilla de zonas sea la misma.
