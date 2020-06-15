using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bunny : MonoBehaviour
{
    //Genes
    public float hopFrequency = 1;
    public float awarenessRadius = 4;

    //Stats
    public float hunger = 60;
    public float thirst = 0;
    private float walkingSpeed = 3;
    private float rotationSpeed = 3;

    //States
    private bool lookingForFood = false;
    private bool foundFood = false;
    private bool isEating = false;
    private Transform targetPlant;

    private bool lookingForWater = true;
    private bool foundWater = false;

    //Other
    private List<Node> currentPath = null;
    private bool isWalking = true;
    private Transform currentTile;
    private Animation animation;
    private Coroutine movementCoroutine;
    private MapGenerator mapGenerator;

    void Start()
    {
        mapGenerator = MapGenerator.instance;
        animation = GetComponentInChildren<Animation>();
        hopFrequency = hopFrequency * Random.Range(0.7f, 1.3f);

        StartCoroutine(Move());
    }

    private void Update() {
        if (!isEating) {
            hunger += Time.deltaTime;
        }
        thirst += Time.deltaTime;

        if (hunger > 60 && !foundFood) {
            lookingForFood = true;
        }

        if (thirst > 60) {
            lookingForWater = true;
        }
    }

    private void OnTriggerStay(Collider other) {
        if (other.gameObject.layer == 9 && lookingForFood) {
            GeneratePathTo(Mathf.FloorToInt(other.transform.position.x), Mathf.FloorToInt(other.transform.position.z), mapGenerator.graph);
            targetPlant = other.gameObject.transform;
            lookingForFood = false;
            foundFood = true;
        }
    }

    private IEnumerator Move()
    {
        yield return new WaitForSeconds(hopFrequency);

        while(true) {
            while (currentPath == null && foundFood) {
                isEating = true;
                while (hunger > 0) {
                    hunger -= Time.deltaTime * 20;
                    targetPlant.localScale = Vector3.Scale(targetPlant.localScale, new Vector3(0.997f, 0.997f, 0.997f));
                    yield return null;
                }

                Destroy(targetPlant.gameObject);
                isEating = false;
                foundFood = false;
            }

            while (currentPath == null) {
                currentTile = GetTileFromPosition(transform.position);

                List<Transform> surroundingTiles = mapGenerator.GetSurroundingTiles(currentTile.GetComponent<Tile>().x, currentTile.GetComponent<Tile>().y, true);
                movementCoroutine = StartCoroutine(HopToTile(surroundingTiles[Random.Range(0, surroundingTiles.Count)].position));

                yield return new WaitForSeconds(hopFrequency);
            }

            while (currentPath != null) {
                MoveToNextTile();

                yield return new WaitForSeconds(hopFrequency);
            }
        }
    }

    private void OnDrawGizmosSelected() {
        UnityEditor.Handles.color = Color.green;
        UnityEditor.Handles.DrawWireDisc(transform.position, transform.up, awarenessRadius);
    }

    public Transform GetTileFromPosition(Vector3 position) {
        return mapGenerator.tileMap[Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.z)];
    }

    private void SetTarget(int x, int y) {
        GeneratePathTo(x, y, mapGenerator.graph);
    }

    private void MoveToNextTile() {
        currentPath.RemoveAt(0);

        movementCoroutine = StartCoroutine(HopToTile(mapGenerator.CoordToPosition(currentPath[0].x, currentPath[0].y)));

        if (currentPath.Count == 1) {
            currentPath = null;
        }
    }

    private IEnumerator HopToTile(Vector3 targetTile) {
        Vector3 direction = (targetTile - transform.position).normalized;
        transform.LookAt(targetTile);

        while (Vector3.Distance(transform.position, targetTile) > 0f) {
            transform.position = Vector3.MoveTowards(transform.position, targetTile, walkingSpeed * Time.deltaTime);
            if (!animation.isPlaying) {
                animation.Play();
            }
            yield return null;
        }

        yield return null;
    }

    public void GeneratePathTo(int x, int y, Node[,] graph) {
        currentPath = null;
        currentTile = GetTileFromPosition(transform.position);

        Dictionary<Node, float> dist = new Dictionary<Node, float>();
        Dictionary<Node, Node> prev = new Dictionary<Node, Node>();

        List<Node> unvisited = new List<Node>();

        Node source = graph[currentTile.GetComponent<Tile>().x, currentTile.GetComponent<Tile>().y];
        Node target = graph[x, y];

        dist[source] = 0;
        prev[source] = null;

        //Init everything to have infinity distance
        foreach (Node v in graph) {
            if (v != source) {
                dist[v] = Mathf.Infinity;
                prev[v] = null;
            }

            unvisited.Add(v);
        }

        while (unvisited.Count > 0) {
            //U is going to be the unvisited node with the smallest distance
            Node u = null;

            foreach (Node possibleU in unvisited) {
                if (u == null || dist[possibleU] < dist[u]) {
                    u = possibleU;
                }
            }

            if (u == target) {
                break;
            }

            unvisited.Remove(u);

            foreach (Node v in u.neighbours) {
                float alt = dist[u] + u.DistanceTo(v);

                if (alt < dist[v]) {
                    dist[v] = alt;
                    prev[v] = u;
                }
            }
        }

        //Found shortest path to target, or no route is found
        if (prev[target] == null) {
            // No route
            return;
        }

        currentPath = new List<Node>();
        Node curr = target;

        //Step through prev chain and add it to path
        while (curr != null) {
            currentPath.Add(curr);
            curr = prev[curr];
        }

        //currentPath describes a route from target to our source, so invert
        currentPath.Reverse();
    }
}
