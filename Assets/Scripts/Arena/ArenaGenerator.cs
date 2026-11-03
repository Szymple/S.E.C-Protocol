using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

// Arena con obstáculos al azar
//   - Casillas reservadas: spawns del WaveManager y alrededor del jugador
//   - El flood fill empieza desde el jugador
//   - Cambia la arena cada 5 oleadas

[RequireComponent(typeof(NavMeshSurface))]
public class ArenaGenerator : MonoBehaviour
{
    [Header("Cuadrícula")]
    [Tooltip("Casillas a lo ancho (X) y a lo largo (Z). 20 x 5 m = 100 m")]
    public Vector2Int mapSize = new Vector2Int(20, 20);

    [Tooltip("Tamaño de cada casilla en metros")]
    public float tileSize = 5f;

    [Header("Obstáculos")]
    [Tooltip("Prefabs de obstáculos (se elige uno al azar por casilla)")]
    public GameObject[] obstaclePrefabs;

    [Tooltip("Dónde se guardan los obstáculos creados (Arena/Obstaculos)")]
    public Transform obstacleHolder;

    [Tooltip("Porcentaje de casillas con obstáculo (0.15 = 15%)")]
    [Range(0f, 0.5f)]
    public float obstaclePercent = 0.15f;

    [Header("Casillas libres")]
    [Tooltip("Casillas libres alrededor del jugador (1 = un cuadro de 3x3)")]
    public int playerClearRadius = 1;

    [Header("Cambio de arena")]
    [Tooltip("Cada cuántas oleadas cambia la arena")]
    public int changeEveryWaves = 5;

    [Header("Debug (solo para ver)")]
    [SerializeField] private int obstaclesPlaced;

    private NavMeshSurface surface;
    private WaveManager waveManager;
    private Transform player;

    private bool[,] reserved;

    // Una casilla de la cuadrícula 
    private struct Coord
    {
        public int x;
        public int y;
        public Coord(int x, int y) { this.x = x; this.y = y; }
    }

    private void Awake()
    {
        surface = GetComponent<NavMeshSurface>();
    }

    private void Start()
    {
        waveManager = FindObjectOfType<WaveManager>();
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        if (waveManager != null)
            waveManager.OnWaveCompleted.AddListener(OnWaveCompleted);

        Generate();
    }

    // Cambiar la arena cada X oleadas, en muestro caso cads 5
    private void OnWaveCompleted(int wave)
    {
        if (changeEveryWaves > 0 && wave % changeEveryWaves == 0)
            Generate();
    }

    // Generar la arena
    public void Generate()
    {
        ClearObstacles();
        BuildReservedTiles();

        // Todas las casillas que no están reservadas, mezcladas
        List<Coord> candidates = new List<Coord>();
        for (int x = 0; x < mapSize.x; x++)
            for (int y = 0; y < mapSize.y; y++)
                if (!reserved[x, y]) candidates.Add(new Coord(x, y));
        Shuffle(candidates);

        bool[,] obstacleMap = new bool[mapSize.x, mapSize.y];
        int target = (int)(mapSize.x * mapSize.y * obstaclePercent);
        int count = 0;

        foreach (Coord c in candidates)
        {
            if (count >= target) break;

            // Probar el obstáculo, si encierra una zona, se quita para evitar problemas
            obstacleMap[c.x, c.y] = true;
            count++;

            if (MapIsFullyAccessible(obstacleMap, count))
            {
                PlaceObstacle(c);
            }
            else
            {
                obstacleMap[c.x, c.y] = false;
                count--;
            }
        }

        obstaclesPlaced = count;

        // NavMesh nuevo con los obstáculos puestos
        surface.BuildNavMesh();

        Debug.Log($"Arena generada: {count} obstáculos");
    }

    // Spawns y alrededor del jugador nunca llevan obstáculo
    private void BuildReservedTiles()
    {
        reserved = new bool[mapSize.x, mapSize.y];

        if (waveManager != null && waveManager.spawnPoints != null)
        {
            foreach (Transform sp in waveManager.spawnPoints)
            {
                if (sp == null) continue;
                Coord c = PositionToCoord(sp.position);
                reserved[c.x, c.y] = true;
            }
        }

        Coord p = PlayerCoord();
        for (int dx = -playerClearRadius; dx <= playerClearRadius; dx++)
        {
            for (int dy = -playerClearRadius; dy <= playerClearRadius; dy++)
            {
                int nx = p.x + dx;
                int ny = p.y + dy;
                if (IsInsideMap(nx, ny)) reserved[nx, ny] = true;
            }
        }
    }

    // Flood fill desde domde esta el jugador
    private bool MapIsFullyAccessible(bool[,] obstacleMap, int obstacleCount)
    {
        bool[,] visited = new bool[mapSize.x, mapSize.y];
        Queue<Coord> queue = new Queue<Coord>();

        Coord start = PlayerCoord();
        queue.Enqueue(start);
        visited[start.x, start.y] = true;
        int accessibleCount = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();

            // Solo los 4 del alrededor (arriba, abajo, izquierda, derecha)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx != 0 && dy != 0) continue;

                    int nx = tile.x + dx;
                    int ny = tile.y + dy;
                    if (!IsInsideMap(nx, ny)) continue;

                    if (!visited[nx, ny] && !obstacleMap[nx, ny])
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue(new Coord(nx, ny));
                        accessibleCount++;
                    }
                }
            }
        }

        int targetAccessible = mapSize.x * mapSize.y - obstacleCount;
        return accessibleCount == targetAccessible;
    }

    // Poner un obstáculo al azar en la casilla
    private void PlaceObstacle(Coord c)
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        ObstacleSettings settings = prefab.GetComponent<ObstacleSettings>();

        Quaternion rotation = Quaternion.identity;
        if (settings == null || settings.randomRotation)
            rotation = Quaternion.Euler(0f, 90f * Random.Range(0, 4), 0f);

        GameObject obs = Instantiate(prefab, CoordToPosition(c.x, c.y), rotation, obstacleHolder);

        // Altura al azar, si el prefab lo permite
        if (settings != null && settings.randomHeight)
        {
            Vector3 scale = obs.transform.localScale;
            scale.y *= Random.Range(settings.minHeight, settings.maxHeight);
            obs.transform.localScale = scale;
        }
    }

    // Borrar la arena anterior
    private void ClearObstacles()
    {
        if (obstacleHolder == null) return;

        foreach (Transform child in obstacleHolder)
        {
            // Se debe de APAGAR PRIMERO! Destroy espera al final y el NavMesh nuevo no debe ver los obstáculos viejos
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    // Utilidades 
    // Casilla -> posición en el mundo 
    private Vector3 CoordToPosition(int x, int y)
    {
        return transform.position + new Vector3(
            (-mapSize.x / 2f + 0.5f + x) * tileSize,
            0f,
            (-mapSize.y / 2f + 0.5f + y) * tileSize);
    }

    // Posición en el mundo -> casilla (al revés de CoordToPosition)
    private Coord PositionToCoord(Vector3 worldPos)
    {
        Vector3 local = worldPos - transform.position;
        int x = Mathf.RoundToInt(local.x / tileSize + mapSize.x / 2f - 0.5f);
        int y = Mathf.RoundToInt(local.z / tileSize + mapSize.y / 2f - 0.5f);
        x = Mathf.Clamp(x, 0, mapSize.x - 1);
        y = Mathf.Clamp(y, 0, mapSize.y - 1);
        return new Coord(x, y);
    }

    private Coord PlayerCoord()
    {
        if (player != null) return PositionToCoord(player.position);
        return new Coord(mapSize.x / 2, mapSize.y / 2);
    }

    private bool IsInsideMap(int x, int y)
    {
        return x >= 0 && x < mapSize.x && y >= 0 && y < mapSize.y;
    }

    // Mezclar la lista fisher-Yates, se usa en el video de Lague, por ende la use aca
    private void Shuffle(List<Coord> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            int r = Random.Range(i, list.Count);
            Coord temp = list[r];
            list[r] = list[i];
            list[i] = temp;
        }
    }

    [ContextMenu("Prueba: generar arena nueva")]
    private void DebugGenerate()
    {
        Generate();
    }
}