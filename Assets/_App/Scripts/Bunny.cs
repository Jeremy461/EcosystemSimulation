using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bunny : MonoBehaviour
{
    //Genes
    public string gender;
    public float hopFrequency = 1;
    public float awarenessRadius = 4;

    //Stats
    public float hunger = 0;
    public float thirst = 0;
    public float reproductiveUrge = 0;
    public float walkingSpeed = 3;
    private float rotationSpeed = 3;

    //States
    public BunnyBehaviour behaviour;

    public bool foundMate = false;
    public Bunny mate = null;

    public bool foundFood = false;
    public bool isEating = false;
    public Transform targetPlant;

    public bool foundWater = false;
    public bool isDrinking = false;

    //Other
    public List<Node> currentPath = null;
    public bool reachedPath = false;
    private bool isWalking = true;
    private Transform currentTile;
    public Animation animation;
    private Coroutine movementCoroutine;
    public MapGenerator mapGenerator;

    //UI
    public RectTransform hungerBar;
    public RectTransform thirstBar;
    public RectTransform urgeBar;

    public LookingFor lookingFor;

    public enum LookingFor
    {
        Nothing, Food, Water, Mate
    }

    void Start()
    {
        mapGenerator = MapGenerator.instance;
        animation = GetComponentInChildren<Animation>();
        hopFrequency = hopFrequency * Random.Range(0.7f, 1.3f);

        behaviour = new Walking();
    }

    private void Update() {
        behaviour.Move(this);      

        if (thirst >= 100 || hunger >= 100)
        {
            Destroy(gameObject);
        }

        if (reproductiveUrge > 100)
        {
            reproductiveUrge = 100;
        }

        switch(behaviour.GetType().ToString()) {
            case "Walking":
                hunger += Time.deltaTime * 2;
                thirst += Time.deltaTime * 2;
                reproductiveUrge += Time.deltaTime * 2;
                break;
            case "Eating":
                thirst += Time.deltaTime * 2;
                reproductiveUrge += Time.deltaTime * 2;
                break;
            case "Drinking":
                hunger += Time.deltaTime * 2;
                reproductiveUrge += Time.deltaTime * 2;
                break;
            case "Mating":
                thirst += Time.deltaTime * 2;
                hunger += Time.deltaTime * 2;
                break;
        }

        hungerBar.localScale = new Vector3(hunger / 100, 1, 1);
        thirstBar.localScale = new Vector3(thirst / 100, 1, 1);
        urgeBar.localScale = new Vector3(reproductiveUrge / 100, 1, 1);
    }

    private void OnTriggerStay(Collider other) {
        if (other.gameObject.layer == 8 &&
            lookingFor == LookingFor.Mate &&
            other.gameObject.GetComponent<Bunny>().gender != gender &&
            other.gameObject.GetComponent<Bunny>().lookingFor == LookingFor.Mate &&
            other.gameObject.GetComponent<Bunny>().mate == null &&
            !foundMate && !other.gameObject.GetComponent<Bunny>().foundMate) {
            mate = other.gameObject.GetComponent<Bunny>();
            foundMate = true;

            mate.mate = this;
            mate.foundMate = true;
            reachedPath = false;

            if (gender == "Male")
            {
                GeneratePathTo(Mathf.FloorToInt(other.transform.position.x), Mathf.FloorToInt(other.transform.position.z), mapGenerator.graph);
                mate.behaviour = new Waiting();
            }
        } else if (other.gameObject.layer == 9 && lookingFor == LookingFor.Food && other.gameObject.GetComponent<Plant>().eatenBy == null && !foundFood) {
            reachedPath = false;

            GeneratePathTo(Mathf.FloorToInt(other.transform.position.x), Mathf.FloorToInt(other.transform.position.z), mapGenerator.graph);
            targetPlant = other.gameObject.transform;
            targetPlant.GetComponent<Plant>().eatenBy = this;
            foundFood = true;
        } else if (other.gameObject.layer == 10 && lookingFor == LookingFor.Water && !foundWater)
        {
            reachedPath = false;

            GeneratePathTo(Mathf.FloorToInt(other.transform.position.x), Mathf.FloorToInt(other.transform.position.z), mapGenerator.graph);
            foundWater = true;
        }
    }

    public Transform GetTileFromPosition(Vector3 position) {
        return mapGenerator.tileMap[Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.z)];
    }

    public void GeneratePathTo(int x, int y, Node[,] graph) {
        reachedPath = false;
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
