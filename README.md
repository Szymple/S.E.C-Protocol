# S.E.C. Protocol

Shooter en tercera persona por oleadas, hecho en **Unity 2022.3.62f3 (URP)** para el curso de Diseño de Videojuegos.

El jugador sobrevive oleadas de soldados en una arena que se genera al azar. Gana al completar la **oleada 10** y puede seguir jugando en modo infinito.

---

## Cómo abrir y jugar

1. Abrir el proyecto con **Unity 2022.3.62f3** desde Unity Hub.
2. Abrir la escena **`Assets/Scenes/MainMenu`**.
3. Presionar **Play** → **Jugar**.

### Controles

| Acción | Tecla |
|---|---|
| Moverse | W A S D |
| Apuntar / cámara | Mouse |
| Correr | Shift (usa stamina) |
| Saltar / doble salto | Espacio |
| Agacharse | Ctrl o C |
| Disparar | Clic izquierdo (mantener con el rifle) |
| Recargar | R |
| Cambiar de arma | 1 / 2 o rueda del mouse |
| Pausa | ESC |

---

## Segundo avance: qué se hizo

Se construyó el **ciclo de juego completo**: menú → partida → pausa → derrota o victoria → reiniciar o volver al menú.

### Jugador
- Movimiento en tercera persona con la cámara al hombro (estilo Fortnite / Crab Champions). El personaje mira siempre hacia donde apunta la cámara.
- **Correr** con stamina (5 s de duración y 5 s de recarga; si se agota, no corre hasta llenarse).
- **Doble salto** (caer de un borde cuenta como el primero).
- **Agacharse**: menos altura y velocidad; no se puede parar si hay un techo encima.
- **Vida**: 150 HP.

### Armas
- **Rifle**: 20 de daño, cargador de 25, se puede mantener el clic, usa la reserva compartida.
- **Pistola**: 10 de daño, cargador de 12, reserva infinita.
- Disparo en tercera persona: apunta desde el centro de la cámara y el láser sale desde la punta del arma.
- Recarga con R o automática al vaciar el cargador. Si el rifle se queda sin balas, cambia solo a la pistola.
- Reserva compartida de munición (75 iniciales, máximo 150).

### Enemigo: Soldado
- IA con estados **perseguir / atacar / muerto** usando NavMesh.
- Se detiene a distancia, dispara un láser rojo con imprecisión, tiene cargador y recarga.
- **Barra de vida** flotante sobre la cabeza.

### Oleadas
- Cantidad de enemigos por oleada: **E(n) = 10 + (n−1) + ⌊(n−1)/2⌋ + R**, con R al azar entre 0 y 2.
- Máximo **10 enemigos vivos** a la vez; el resto espera en cola.
- **5 s** de descanso entre oleadas.
- Los enemigos ganan **+2 HP cada 2 oleadas**.
- **Victoria al completar la oleada 10**, con opción de seguir jugando (oleadas infinitas).

### Arena aleatoria
- Arena de **100 × 100 m** dividida en una cuadrícula de 20 × 20 casillas de 5 m.
- Los obstáculos (caja, columna, muro) se colocan al azar con altura y rotación variables.
- Un algoritmo de **flood fill** asegura que el mapa siempre quede conectado (ningún obstáculo encierra una zona).
- Nunca se ponen obstáculos sobre los 8 puntos de aparición ni alrededor del jugador.
- El NavMesh se genera en tiempo real y **la arena cambia cada 5 oleadas**.

### Menús y estado del juego
- **Menú principal**: Jugar y Salir.
- **Pausa** (ESC): Reanudar, Reiniciar, Menú. El juego y la cámara se congelan y se libera el mouse.
- **Game Over** al morir y **Victoria** al ganar, ambos con estadísticas de la partida.
- **Estadísticas**: oleada alcanzada, enemigos eliminados, tiempo, disparos, aciertos (precisión) y daño recibido.

> Los menús y paneles actuales son provisionales; se reemplazarán por versiones finales en el tercer avance.

---

## Cómo está armado

El proyecto usa **eventos** (patrón observador) para que cada sistema avise lo que pasa sin depender de los demás. Por ejemplo, `Health` avisa cuando alguien muere y el `GameManager`, el `WaveManager` y `GameStats` escuchan ese aviso.

| Script | Carpeta | Qué hace |
|---|---|---|
| `PlayerController` | Player | Movimiento, stamina, doble salto, agacharse |
| `PlayerInputs` | Player | Lee el teclado y el mouse |
| `Health` | Core | Vida de jugador y enemigos; avisa daño, cambios y muerte |
| `Gun` | Weapons | Disparo, láser, cargador y recarga |
| `PlayerAmmo` | Weapons | Reserva de munición compartida |
| `WeaponSwitching` | Weapons | Cambio de arma |
| `EnemySoldado` | Enemies | IA del soldado |
| `HealthBar` | UI | Barra de vida (flotante o de HUD) |
| `WaveManager` | Waves | Oleadas, dificultad y victoria |
| `ArenaGenerator` | Arena | Arena aleatoria y NavMesh en tiempo real |
| `ObstacleSettings` | Arena | Ajustes de cada obstáculo (altura y rotación al azar) |
| `GameManager` | Core | Pausa, Game Over, Victoria, reiniciar, ir al menú |
| `GameStats` | Core | Estadísticas de la partida |
| `MainMenu` | UI | Menú principal |
| `DebugHUD` | Core | HUD temporal de prueba (se reemplazará) |

### Escenas
| Escena | Uso |
|---|---|
| `Scenes/MainMenu` | Menú principal (primera escena del juego) |
| `Scenes/Pruebas/Test_Player` | Escena de juego |

### Prefabs principales
`Player`, `Soldado`, `Arena`, `Obs_Caja`, `Obs_Columna`, `Obs_Muro`, `BarraVida`.

---

## Pendiente para el tercer avance

**Contenido**
- Orbes de munición y vida que sueltan los enemigos.
- Nuevos enemigos: **Mago** y **Payaso**.
- Tercera arma: **SMG**.
- Movimientos: **slide** y **dash**.

**Visual**
- HUD final: vida, stamina, balas, arma actual y oleada.
- Menú principal y paneles de pausa, Game Over y Victoria definitivos.
- Modelos 3D de jugador, enemigos, armas y obstáculos.
- Animaciones (caminar, correr, disparar, agacharse, morir).

**Pulido**
- Balance de velocidades, vida y daño en la arena de 100 m.
- Quitar las herramientas de prueba (`DebugHUD`, `DebugShooter`) y objetos sin usar.
- Ordenar la escena final del juego.
- Prueba completa del ciclo.

---

## Referencias

| Sección | Referencia | Link |
|---|---|---|
| Jugador | First Person + Third Person Character Controllers (v2.0.2) – Unity Technologies (Unity Companion License) | https://assetstore.unity.com/packages/3d/characters/first-person-third-person-character-controllers-196526 |
| Vida (Health) | Events & Delegates in Unity – John French, Game Dev Beginner | https://gamedevbeginner.com/events-and-delegates-in-unity/ |
| Tags y layers | Tags and Layers (Tag Manager) – Unity Manual | https://docs.unity.com/en-us/engine/6000.6/manual/unity-editor/editor-settings-reference/comp-manager-group/class-tag-manager |
| Soldado (NavMesh) | Unity NavMesh (videos de Brackeys) – Unity Learn | https://learn.unity.com/tutorial/unity-navmesh |
| Soldado (IA) | Enemy AI in Unity – John French, Game Dev Beginner | https://gamedevbeginner.com/enemy-ai-in-unity/ |
| Armas (disparo) | Raycasts in Unity, made easy – John French, Game Dev Beginner | https://gamedevbeginner.com/raycasts-in-unity-made-easy/ |
| Armas (disparo) | Shooting with Raycasts – Brackeys | https://youtu.be/THnivyG0Mvo |
| Armas (munición y recarga) | Ammo & Reloading – Brackeys | https://youtu.be/kAx5g9V5bcM |
| Armas (cambio de arma) | Weapon Switching – Brackeys | https://youtu.be/Dn_BUIVdAPg |
| Armas (cadencia y láser) | Let's Try: Shooting with Raycasts – Unity Learn | https://learn.unity.com/tutorial/let-s-try-shooting-with-raycasts |
| Oleadas | How to make a Wave Spawner in Unity 5 – Parte 1 – Brackeys | https://youtu.be/Vrld13ypX_I |
| Oleadas | How to make a Wave Spawner in Unity 5 – Parte 2 – Brackeys | https://youtu.be/q0SBfDFn2Bs |
| GameManager (pausa) | PAUSE MENU in Unity – Brackeys | https://youtu.be/JivuXdrIHK0 |
| GameManager (Game Over) | GAME OVER – How to make a Video Game in Unity (E08) – Brackeys | https://youtu.be/VbZ9_C4-Qbo |
| GameManager (victoria) | WINNING LEVELS – How to make a Video Game in Unity (E09) – Brackeys | https://youtu.be/Iv7A8TzreY4 |
| GameStats | Events & Delegates in Unity (eventos estáticos y Actions) – John French, Game Dev Beginner | https://gamedevbeginner.com/events-and-delegates-in-unity/ |
| Barra de vida (HealthBar) | Health Bar Unity Tutorial in 7 Minutes, DevLog 5 – BlackCitadel Studios | https://youtu.be/3JjBJfoWDCM |
| Arena aleatoria | Unity Create a Game Series (E08. tile map) – Sebastian Lague | https://youtu.be/gIUVRYViG_g |
| Arena aleatoria | Unity Create a Game Series (E09. obstacle placement) – Sebastian Lague | https://youtu.be/q7BL-lboRXo |
| Arena aleatoria | Unity Create a Game Series (E10. map connectivity) – Sebastian Lague | https://youtu.be/2ycN6ZkWgOo |
| Arena aleatoria | Unity Create a Game Series (E11. map navigation) – Sebastian Lague | https://youtu.be/vQgLdFNrCN8 |
| Arena aleatoria | Unity Create a Game Series (E12. finishing the map generator) – Sebastian Lague | https://youtu.be/HuQITd3epIU |
| Arena aleatoria | Create-a-Game-Source (MapGenerator.cs) – Sebastian Lague (GitHub) | https://github.com/SebLague/Create-a-Game-Source |
| Arena aleatoria | NavMeshSurface (AI Navigation 1.0) – Unity Technologies | https://docs.unity3d.com/Packages/com.unity.ai.navigation@1.0/api/Unity.AI.Navigation.NavMeshSurface.html |
| Arena aleatoria | Introduction to A* (Breadth First Search) – Amit Patel, Red Blob Games | https://www.redblobgames.com/pathfinding/a-star/introduction.html |
| Arena aleatoria | Grids and Graphs – Amit Patel, Red Blob Games | https://www.redblobgames.com/pathfinding/grids/graphs.html |
| Menú principal | START MENU in Unity – Brackeys | https://youtu.be/zc8ac_qUXQY |
| General (todo el código) | Unity Scripting API (2022.3) – Unity Technologies | https://docs.unity3d.com/2022.3/Documentation/ScriptReference/ |
| General (todo el código) | Documentación de C# – Microsoft Learn | https://learn.microsoft.com/en-us/dotnet/csharp/ |
