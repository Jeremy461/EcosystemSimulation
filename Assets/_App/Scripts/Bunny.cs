using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bunny : MonoBehaviour
{
    //Genes
    public float hopFrequency = 1;
    public float awarenessRadius = 4;

    //Stats
    public float hunger = 0;
    public float thirst = 0;
    public float walkingSpeed = 3;
    private float rotationSpeed = 3;

    //States
    public BunnyBehaviour behaviour;

    public bool lookingForFood = false;
    public bool foundFood = false;
    public bool isEating = false;
    public Transform targetPlant;

    public bool lookingForWater = true;
    public bool foundWater = false;
    public bool isDrinking = false;

    //Other
    public List<Node> currentPath = null;
    private bool isWalking = true;
    private Transform currentTile;
    public Animation animation;
    private Coroutine movementCoroutine;
    public MapGenerator mapGenerator;

    //UI
    public RectTransform hungerBar;
    public RectTransform thirstBar;

    void Start()
    {
        mapGenerator = MapGenerator.instance;
        animation = GetComponentInChildren<Animation>();
        hopFrequency = hopFrequency * Random.Range(0.7f, 1.3f);

        behaviour = new Walking();
    }

    private void Update() {
        behaviour.Move(this);      

        if (hunger > 60 && !foundFood) {
            lookingForFood = true;
        }

        if (thirst > 60 && !foundWater) {
            lookingForWater = true;
        }

        if (thirst >= 100 || hunger >= 100)
        {
            Destroy(gameObject);
        }

        hungerBar.localScale = new Vector3(hunger / 100, 1, 1);
        thirstBar.localScale = new Vector3(thirst / 100, 1, 1);
    }

    private void OnTriggerStay(Collider other) {
        if (other.gameObject.layer == 9 && lookingForFood && other.gameObject.GetComponent<Plant>().eatenBy == null) {
            GeneratePathTo(Mathf.FloorToInt(other.transform.position.x), Mathf.FloorToInt(other.transform.position.z), mapGenerator.graph);
            targetPlant = other.gameObject.transform;
            lookingForFood = false;
            foundFood = true;
        }

        if (other.gameObject.layer == 10 && lookingForWater)
        {
            GeneratePathTo(Mathf.FloorToInt(other.transform.position.x), Mathf.FloorToInt(other.transform.position.z), mapGenerator.graph);
            lookingForWater = false;
            foundWater = true;
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
