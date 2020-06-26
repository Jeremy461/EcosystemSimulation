using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator instance = null;

    [Header("Prefabs")]
    public Transform tilePrefab;
    public Transform obstaclePrefab;
    public Transform foodPrefab;
    public Transform bunnyPrefab;
    public Material waterMaterial;

    [Header("Map Attributes")]
    public Vector2 mapSize;
    public float tileSize;
    [Range(0, 1)]
    public float obstaclePercent;
    [Range(0, 1)]
    public float foodPercent;
    [Range(0, 1)]
    public float waterPercent;
    public int lakeAmount;
    public int bunnyAmount;
    public int seed = 10;

    public Node[,] graph;
    public Transform[,] tileMap;

    private List<Coord> allTileCoords;
    private Queue<Coord> shuffledTileCoords;
    private Queue<Coord> shuffledOpenTileCoords;
    private List<Coord> allOpenCoords;
    private bool[,] obstacleMap;
    private int currentObstacleCount;
    private Transform mapHolder;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }

        instance = this;
        DontDestroyOnLoad(this.gameObject);

        GenerateMap();
    }

    public void GenerateMap()
    {
        currentObstacleCount = 0;
        Random.seed = seed;
        tileMap = new Transform[(int) mapSize.x, (int) mapSize.y];
        obstacleMap = new bool[(int)mapSize.x, (int)mapSize.y];

        //Generate Coords
        allTileCoords = new List<Coord>();
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                allTileCoords.Add(new Coord(x, y));
            }
        }

        shuffledTileCoords = new Queue<Coord>(Utility.ShuffleArray(allTileCoords.ToArray(), seed));

        //Apply map to holder object
        string holderName = "Generated Map";
        if (transform.Find(holderName))
        {
            DestroyImmediate(transform.Find(holderName).gameObject);
        }

        mapHolder = new GameObject(holderName).transform;
        mapHolder.parent = transform;

        //Spawn Tiles
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                Vector3 tilePosition = CoordToPosition(x, y);
                Transform newTile = Instantiate(tilePrefab, tilePosition, Quaternion.identity) as Transform;
                newTile.localScale = Vector3.one * tileSize;
                newTile.parent = mapHolder;
                newTile.GetComponent<Tile>().x = x;
                newTile.GetComponent<Tile>().y = y;
                tileMap[x, y] = newTile;
            }
        }
        allOpenCoords = new List<Coord>(allTileCoords);

        //Spawning Objects
        GenerateLakes();

        GenerateObstaclesByPercentage(obstaclePrefab, obstaclePercent);
        GenerateObstaclesByPercentage(foodPrefab, foodPercent);
        GenerateObstaclesByInteger(bunnyPrefab, bunnyAmount);
        GeneratePathfindingGraph();

        shuffledOpenTileCoords = new Queue<Coord>(Utility.ShuffleArray(allOpenCoords.ToArray(), seed));
    }

    bool MapIsFullyAccessible(bool[,] obstacleMap)
    {
        bool[,] mapFlags = new bool[obstacleMap.GetLength(0),obstacleMap.GetLength(1)];
        Queue<Coord> queue = new Queue<Coord>();
        Coord startCoord = new Coord(0, 0);
        queue.Enqueue(startCoord);
        mapFlags[startCoord.x, startCoord.y] = true;

        int accessibleTileCount = 1;

        while(queue.Count > 0)
        {
            Coord tile = queue.Dequeue();

            for(int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int neighbourX = tile.x + x;
                    int neighbourY = tile.y + y;
                    if (x == 0 || y == 0)
                    {
                        if (neighbourX >= 0 && neighbourX < obstacleMap.GetLength(0) && neighbourY >= 0 && neighbourY < obstacleMap.GetLength(1))
                        {
                            if (!mapFlags[neighbourX, neighbourY] && !obstacleMap[neighbourX, neighbourY])
                            {
                                mapFlags[neighbourX, neighbourY] = true;
                                queue.Enqueue(new Coord(neighbourX, neighbourY));
                                accessibleTileCount++;
                            }
                        }
                    }
                }
            }
        }

        int targetAccessibleTileCount = (int)(mapSize.x * mapSize.y - currentObstacleCount);
        return targetAccessibleTileCount == accessibleTileCount;
    }

    public Coord GetRandomCoord()
    {
        Coord randomCoord = shuffledTileCoords.Dequeue();
        shuffledTileCoords.Enqueue(randomCoord);
        return randomCoord;
    }

    public Transform GetRandomOpenTile()
    {
        Coord randomCoord = shuffledOpenTileCoords.Dequeue();
        shuffledOpenTileCoords.Enqueue(randomCoord);

        return tileMap[randomCoord.x, randomCoord.y];
    }

    public Vector3 CoordToPosition(int x, int y)
    {
        return new Vector3(0.5f + x, 0, 0.5f + y) * tileSize;
    }

    private void GenerateObstaclesByInteger(Transform objectToSpawn, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Coord randomCoord = GetRandomCoord();
            if (obstacleMap[randomCoord.x, randomCoord.y] != true)
            {
                obstacleMap[randomCoord.x, randomCoord.y] = true;
                currentObstacleCount++;

                if (MapIsFullyAccessible(obstacleMap))
                {
                    Vector3 objectPosition = CoordToPosition(randomCoord.x, randomCoord.y);
                    Transform newObject = Instantiate(objectToSpawn, objectPosition, Quaternion.identity) as Transform;
                    newObject.parent = mapHolder;
                    newObject.localScale = Vector3.one * tileSize;

                    tileMap[randomCoord.x, randomCoord.y].GetComponent<Tile>().isWalkable = false;
                    allOpenCoords.Remove(randomCoord);
                } else
                {
                    obstacleMap[randomCoord.x, randomCoord.y] = false;
                    currentObstacleCount--;
                }
            }
        }
    }

    private void GenerateObstaclesByPercentage(Transform objectToSpawn, float percentage)
    {
        int obstacleCount = (int)(mapSize.x * mapSize.y * percentage);
        for (int i = 0; i < obstacleCount; i++)
        {
            Coord randomCoord = GetRandomCoord();
            if (obstacleMap[randomCoord.x, randomCoord.y] != true)
            {
                obstacleMap[randomCoord.x, randomCoord.y] = true;
                currentObstacleCount++;

                if (MapIsFullyAccessible(obstacleMap))
                {
                    Vector3 objectPosition = CoordToPosition(randomCoord.x, randomCoord.y);
                    Transform newObject = Instantiate(objectToSpawn, objectPosition, Quaternion.identity) as Transform;
                    newObject.parent = mapHolder;
                    newObject.localScale = Vector3.one * tileSize;

                    tileMap[randomCoord.x, randomCoord.y].GetComponent<Tile>().isWalkable = false;
                    allOpenCoords.Remove(randomCoord);
                } else
                {
                    obstacleMap[randomCoord.x, randomCoord.y] = false;
                    currentObstacleCount--;
                }
            }          
        }
    }

    private void GenerateLakes()
    {
        int maxLakeSize = (int) (mapSize.x * mapSize.y * waterPercent) / lakeAmount;
        for (int i = 0; i < lakeAmount; i++)
        {
            Coord randomCoord = GetRandomCoord();

            if (obstacleMap[randomCoord.x, randomCoord.y] != true)
            {
                obstacleMap[randomCoord.x, randomCoord.y] = true;
                currentObstacleCount++;
                int currentLakeSize = 1;

                if (MapIsFullyAccessible(obstacleMap))
                {
                    Transform waterTile = tileMap[randomCoord.x, randomCoord.y];
                    TransformToWatertile(waterTile);
                    allOpenCoords.Remove(randomCoord);

                    Transform currentTile = tileMap[randomCoord.x, randomCoord.y];
                    List<Transform> surroundingTiles = GetSurroundingTiles(currentTile.GetComponent<Tile>().x, currentTile.GetComponent<Tile>().y, false);

                    while (currentLakeSize < maxLakeSize)
                    {
                        for(int y = 0; y < surroundingTiles.Count; y++)
                        {
                            obstacleMap[surroundingTiles[y].GetComponent<Tile>().x, surroundingTiles[y].GetComponent<Tile>().y] = true;
                            currentObstacleCount++;

                            if (MapIsFullyAccessible(obstacleMap))
                            {
                                TransformToWatertile(surroundingTiles[y]);
                                allOpenCoords.Remove(allOpenCoords.Find(coord => coord.x == surroundingTiles[y].GetComponent<Tile>().x && coord.y == surroundingTiles[y].GetComponent<Tile>().y));

                                currentLakeSize++;

                                surroundingTiles[y].GetComponent<Tile>().isWater = true;
                                surroundingTiles[y].GetComponent<BoxCollider>().enabled = false;

                                List<Transform> waterEdges = GetSurroundingTiles(surroundingTiles[y].GetComponent<Tile>().x, surroundingTiles[y].GetComponent<Tile>().y, false);
                                for (int z = 0; z < waterEdges.Count; z++)
                                {
                                    if (waterEdges[z].GetComponent<Tile>().isWalkable && !waterEdges[z].GetComponent<Tile>().isWater)
                                    {
                                        waterEdges[z].GetComponent<BoxCollider>().enabled = true;
                                        waterEdges[z].gameObject.layer = 10;
                                    }
                                }
                            } else
                            {
                                obstacleMap[surroundingTiles[y].GetComponent<Tile>().x, surroundingTiles[y].GetComponent<Tile>().y] = false;
                                currentObstacleCount--;
                            }
                        }

                        int random = Random.Range(0, surroundingTiles.Count); 
                        surroundingTiles = GetSurroundingTiles(surroundingTiles[random].GetComponent<Tile>().x, surroundingTiles[random].GetComponent<Tile>().y, false);
                    }
                }
                else
                {
                    obstacleMap[randomCoord.x, randomCoord.y] = false;
                    currentObstacleCount--;
                }
            }
        }
    }

    private void TransformToWatertile(Transform tile)
    {
        tile.GetComponent<Renderer>().sharedMaterial = waterMaterial;
        tile.GetComponent<Tile>().isWalkable = false;
        tile.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y - 0.3f, tile.transform.position.z);
    }

    public List<Transform> GetSurroundingTiles(int x, int y, bool getDiagionals)
    {
        List<Transform> surroundingTiles = new List<Transform>();

        if (getDiagionals)
        {
            //Top Left
            if (x - 1 >= 0 && y + 1 <= mapSize.y - 1 && tileMap[x - 1, y + 1].GetComponent<Tile>().isWalkable) surroundingTiles.Add(tileMap[x - 1, y + 1]);
            //Top Right
            if (x + 1 <= mapSize.x - 1 && y + 1 <= mapSize.y - 1 && tileMap[x + 1, y + 1].GetComponent<Tile>().isWalkable) surroundingTiles.Add(tileMap[x + 1, y + 1]);
            //Bottom Left
            if (x - 1 >= 0 && y - 1 >= 0 && tileMap[x - 1, y - 1].GetComponent<Tile>().isWalkable) surroundingTiles.Add(tileMap[x - 1, y - 1]);
            //Bottom Right
            if (x + 1 <= mapSize.x - 1 && y - 1 >= 0 && tileMap[x + 1, y - 1].GetComponent<Tile>().isWalkable) surroundingTiles.Add(tileMap[x + 1, y - 1]);
        }

        //Top
        if (y + 1 <= mapSize.y - 1 && tileMap[x, y + 1].GetComponent<Tile>().isWalkable) surroundingTiles.Add(tileMap[x, y + 1]);

        //Left
        if (x - 1 >= 0 && tileMap[x - 1, y].GetComponent<Tile>().isWalkable) surroundingTiles.Add(tileMap[x - 1, y]);
        //Right
        if (x + 1 <= mapSize.x - 1 && tileMap[x + 1, y].GetComponent<Tile>().isWalkable) surroundingTiles.Add(tileMap[x + 1, y]);

        //Bottom
        if (y - 1 >= 0 && tileMap[x, y - 1].GetComponent<Tile>().isWalkable) surroundingTiles.Add(tileMap[x, y - 1]);

        return surroundingTiles;
    }

    private void GeneratePathfindingGraph() {
        //Init array and all nodes
        graph = new Node[(int)mapSize.x, (int)mapSize.y];

        for (int x = 0; x < (int)mapSize.x; x++) {
            for (int y = 0; y < (int)mapSize.y; y++) {
                graph[x, y] = new Node {
                    x = x,
                    y = y
                };
            }
        }

        //Calculate neighbours
        for (int x = 0; x < (int)mapSize.x; x++) {
            for (int y = 0; y < (int)mapSize.y; y++) {
                if (tileMap[x, y].GetComponent<Tile>().isWalkable) {
                    //Top Left
                    if (x > 0 && y < (int)mapSize.y - 1) {
                        graph[x, y].neighbours.Add(graph[x - 1, y + 1]);
                    }
                    //Top
                    if (y < (int)mapSize.y - 1) {
                        graph[x, y].neighbours.Add(graph[x, y + 1]);
                    }
                    //Top Right
                    if (x < (int)mapSize.x - 1 && y < (int)mapSize.y - 1) {
                        graph[x, y].neighbours.Add(graph[x + 1, y + 1]);
                    }

                    //Left
                    if (x > 0) {
                        graph[x, y].neighbours.Add(graph[x - 1, y]);
                    }
                    //Right
                    if (x < (int)mapSize.x - 1) {
                        graph[x, y].neighbours.Add(graph[x + 1, y]);
                    }

                    //Bottom Left
                    if (x > 0 && y > 0) {
                        graph[x, y].neighbours.Add(graph[x - 1, y - 1]);
                    }
                    //Bottom
                    if (y > 0) {
                        graph[x, y].neighbours.Add(graph[x, y - 1]);
                    }
                    //Bottom Right
                    if (x < (int)mapSize.x - 1 && y > 0) {
                        graph[x, y].neighbours.Add(graph[x + 1, y - 1]);
                    }
                }
            }
        }
    }

    public struct Coord
    {
        public int x;
        public int y;

        public Coord(int _x, int _y) {
            x = _x;
            y = _y;
        }

        public static bool operator ==(Coord c1, Coord c2)
        {
            return c1.x == c2.x && c1.y == c2.y;
        }

        public static bool operator !=(Coord c1, Coord c2)
        {
            return !(c1 == c2);
        }
    }
}
