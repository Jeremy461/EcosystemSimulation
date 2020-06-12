using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class MapGenerator : MonoBehaviour
{
    public Transform tilePrefab;
    public Transform obstaclePrefab;
    public Transform foodPrefab;
    public Transform bunnyPrefab;
    public Vector2 mapSize;
    public float tileSize;

    public Material waterMaterial;

    [Range(0,1)]
    public float outlinePercent;
    [Range(0, 1)]
    public float obstaclePercent;
    [Range(0, 1)]
    public float foodPercent;
    [Range(0, 1)]
    public float waterPercent;
    public int lakeAmount;
    public int bunnyAmount;

    List<Coord> allTileCoords;
    Queue<Coord> shuffledTileCoords;
    Queue<Coord> shuffledOpenTileCoords;
    Coord mapCentre;
    Transform[,] tileMap;
    List<Coord> allOpenCoords;
    bool[,] obstacleMap;
    private int currentObstacleCount;

    private Transform mapHolder;

    public int seed = 10;

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
        mapCentre = new Coord((int)mapSize.x / 2, (int)mapSize.y / 2);

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
                Transform newTile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(Vector3.right * 90)) as Transform;
                newTile.localScale = Vector3.one * (1 - outlinePercent) * tileSize;
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

        shuffledOpenTileCoords = new Queue<Coord>(Utility.ShuffleArray(allOpenCoords.ToArray(), seed));
    }

    bool MapIsFullyAccessible(bool[,] obstacleMap)
    {
        bool[,] mapFlags = new bool[obstacleMap.GetLength(0),obstacleMap.GetLength(1)];
        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(mapCentre);
        mapFlags[mapCentre.x, mapCentre.y] = true;

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

    Vector3 CoordToPosition(int x, int y)
    {
        return new Vector3(-mapSize.x / 2 + 0.5f + x, 0, -mapSize.y / 2 + 0.5f + y) * tileSize;
    }

    public Transform GetTileFromPosition(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x / tileSize + (mapSize.x - 1) / 2f);
        int y = Mathf.RoundToInt(position.z / tileSize + (mapSize.y - 1) / 2f);
        x = Mathf.Clamp(x, 0, tileMap.GetLength(0));
        y = Mathf.Clamp(y, 0, tileMap.GetLength(1));
        return tileMap[x, y];
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
                    newObject.localScale = Vector3.one * (1 - outlinePercent) * tileSize;

                    tileMap[randomCoord.x, randomCoord.y].GetComponent<Tile>().isBlocked = true;
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
                    newObject.localScale = Vector3.one * (1 - outlinePercent) * tileSize;

                    tileMap[randomCoord.x, randomCoord.y].GetComponent<Tile>().isBlocked = true;
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
                    List<Transform> surroundingTiles = GetSurroundingTiles(currentTile.GetComponent<Tile>().x, currentTile.GetComponent<Tile>().y);

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
                            } else
                            {
                                obstacleMap[surroundingTiles[y].GetComponent<Tile>().x, surroundingTiles[y].GetComponent<Tile>().y] = false;
                                currentObstacleCount--;
                            }

                        }

                        int random = Random.Range(0, surroundingTiles.Count); 
                        surroundingTiles = GetSurroundingTiles(surroundingTiles[random].GetComponent<Tile>().x, surroundingTiles[random].GetComponent<Tile>().y);
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
        tile.GetComponent<Tile>().isWater = true;
        tile.GetComponent<Tile>().isBlocked = true;
        Utility.SetStaticEditorFlag(tile.gameObject, StaticEditorFlags.NavigationStatic, false);
    }

    private List<Transform> GetSurroundingTiles(int x, int y)
    {
        List<Transform> surroundingTiles = new List<Transform>();
        //Top Left
        //if (x - 1 >= 0 && y + 1 <= mapSize.y - 1) surroundingTiles.Add(tileMap[x - 1, y + 1]);
        //Top
        if (y + 1 <= mapSize.y - 1 && !tileMap[x, y + 1].GetComponent<Tile>().isWater) surroundingTiles.Add(tileMap[x, y + 1]);
        //Top Right
        //if (x + 1 <= mapSize.x - 1 && y + 1 <= mapSize.y - 1) surroundingTiles.Add(tileMap[x + 1, y + 1]);

        //Left
        if (x - 1 >= 0 && !tileMap[x - 1, y].GetComponent<Tile>().isWater) surroundingTiles.Add(tileMap[x - 1, y]);
        //Right
        if (x + 1 <= mapSize.x - 1 && !tileMap[x + 1, y].GetComponent<Tile>().isWater) surroundingTiles.Add(tileMap[x + 1, y]);

        //Bottom Left
        //if (x - 1 >= 0 && y - 1 >= 0) surroundingTiles.Add(tileMap[x - 1, y - 1]);
        //Bottom
        if (y - 1 >= 0 && !tileMap[x, y - 1].GetComponent<Tile>().isWater) surroundingTiles.Add(tileMap[x, y - 1]);
        //Bottom Right
        //if (x + 1 <= mapSize.x - 1 && y - 1 >= 0) surroundingTiles.Add(tileMap[x + 1, y - 1]);

        return surroundingTiles;
    }

    public struct Coord
    {
        public int x;
        public int y;

        public Coord(int _x, int _y)
        {
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
