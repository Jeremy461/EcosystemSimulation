using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public Transform tilePrefab;
    public Transform obstaclePrefab;
    public Transform foodPrefab;
    public Transform bunnyPrefab;
    public Transform navMeshFloor;
    public Transform navMeshMaskPrefab;
    public Vector2 mapSize;
    public Vector2 maximumMapSize;
    public float tileSize;

    [Range(0,1)]
    public float outlinePercent;
    [Range(0, 1)]
    public float obstaclePercent;
    [Range(0, 1)]
    public float foodPercent;
    public int bunnyAmount;

    List<Coord> allTileCoords;
    Queue<Coord> shuffledTileCoords;
    Queue<Coord> shuffledOpenTileCoords;
    Coord mapCentre;
    Transform[,] tileMap;
    List<Coord> allOpenCoords;

    private Transform mapHolder;

    public int seed = 10;

    public void GenerateMap()
    {
        tileMap = new Transform[(int) mapSize.x, (int) mapSize.y];

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
                tileMap[x, y] = newTile;
            }
        }
        allOpenCoords = new List<Coord>(allTileCoords);

        //Spawning Objects
        GenerateObstaclesByPercentage(obstaclePrefab, obstaclePercent);
        GenerateObstaclesByPercentage(foodPrefab, foodPercent);
        GenerateObstaclesByInteger(bunnyPrefab, bunnyAmount);

        navMeshFloor.localScale = new Vector3(maximumMapSize.x, maximumMapSize.y) * tileSize;

        shuffledOpenTileCoords = new Queue<Coord>(Utility.ShuffleArray(allOpenCoords.ToArray(), seed));

        GenerateNavMeshBoundaries();
    }

    bool MapIsFullyAccessible(bool[,] obstacleMap, int currentObstacleCount)
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

    private void GenerateObstaclesByInteger(Transform objectToSpawn, int amount)
    {
        bool[,] obstacleMap = new bool[(int)mapSize.x, (int)mapSize.y];

        int currentObstacleCount = 0;
        for (int i = 0; i < amount; i++)
        {
            Coord randomCoord = GetRandomCoord();
            obstacleMap[randomCoord.x, randomCoord.y] = true;
            currentObstacleCount++;

            if (randomCoord != mapCentre && MapIsFullyAccessible(obstacleMap, currentObstacleCount))
            {
                Vector3 foodPosition = CoordToPosition(randomCoord.x, randomCoord.y);
                Transform newFood = Instantiate(objectToSpawn, foodPosition, Quaternion.identity) as Transform;
                newFood.parent = mapHolder;
                newFood.localScale = Vector3.one * (1 - outlinePercent) * tileSize;

                allOpenCoords.Remove(randomCoord);
            } else
            {
                obstacleMap[randomCoord.x, randomCoord.y] = false;
                currentObstacleCount--;
            }
        }
    }

    private void GenerateObstaclesByPercentage(Transform objectToSpawn, float percentage)
    {
        bool[,] obstacleMap = new bool[(int)mapSize.x, (int)mapSize.y];

        int obstacleCount = (int)(mapSize.x * mapSize.y * percentage);
        int currentObstacleCount = 0;
        for (int i = 0; i < obstacleCount; i++)
        {
            Coord randomCoord = GetRandomCoord();
            obstacleMap[randomCoord.x, randomCoord.y] = true;
            currentObstacleCount++;

            if (randomCoord != mapCentre && MapIsFullyAccessible(obstacleMap, currentObstacleCount))
            {
                Vector3 foodPosition = CoordToPosition(randomCoord.x, randomCoord.y);
                Transform newFood = Instantiate(objectToSpawn, foodPosition, Quaternion.identity) as Transform;
                newFood.parent = mapHolder;
                newFood.localScale = Vector3.one * (1 - outlinePercent) * tileSize;

                allOpenCoords.Remove(randomCoord);
            } else
            {
                obstacleMap[randomCoord.x, randomCoord.y] = false;
                currentObstacleCount--;
            }
        }
    }

    private void GenerateNavMeshBoundaries()
    {
        Transform maskLeft = Instantiate(navMeshMaskPrefab, Vector3.left * (mapSize.x + maximumMapSize.x) / 4 * tileSize, Quaternion.identity) as Transform;
        maskLeft.parent = mapHolder;
        maskLeft.localScale = new Vector3((maximumMapSize.x - mapSize.x) / 2, 1, mapSize.y) * tileSize;

        Transform maskRight = Instantiate(navMeshMaskPrefab, Vector3.right * (mapSize.x + maximumMapSize.x) / 4 * tileSize, Quaternion.identity) as Transform;
        maskRight.parent = mapHolder;
        maskRight.localScale = new Vector3((maximumMapSize.x - mapSize.x) / 2, 1, mapSize.y) * tileSize;

        Transform maskTop = Instantiate(navMeshMaskPrefab, Vector3.forward * (mapSize.y + maximumMapSize.y) / 4 * tileSize, Quaternion.identity) as Transform;
        maskTop.parent = mapHolder;
        maskTop.localScale = new Vector3(maximumMapSize.x, 1, (maximumMapSize.y - mapSize.y) / 2) * tileSize;

        Transform maskBottom = Instantiate(navMeshMaskPrefab, Vector3.back * (mapSize.y + maximumMapSize.y) / 4 * tileSize, Quaternion.identity) as Transform;
        maskBottom.parent = mapHolder;
        maskBottom.localScale = new Vector3(maximumMapSize.x, 1, (maximumMapSize.y - mapSize.y) / 2) * tileSize;
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
