using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bunny : MonoBehaviour
{
    public float hopFrequency = 1;
    public float walkingSpeed = 3;
    public float rotationSpeed = 3;

    private List<Node> currentPath = null;

    private bool isWalking = true;
    private Transform currentTile;
    private Transform targetTile;
    private Animation animation;

    private MapGenerator mapGenerator;
    // Start is called before the first frame update
    void Start()
    {
        mapGenerator = MapGenerator.instance;
        animation = GetComponentInChildren<Animation>();
        hopFrequency = hopFrequency * Random.Range(0.7f, 1.3f);
        currentTile = GetTileFromPosition(transform.position);
        List<Transform> surroundingTiles = mapGenerator.GetSurroundingTiles(currentTile.GetComponent<Tile>().x, currentTile.GetComponent<Tile>().y, true);
        targetTile = surroundingTiles[Random.Range(0, surroundingTiles.Count)];
        StartCoroutine(Move());
    }

    public Transform GetTileFromPosition(Vector3 position)
    {
        return mapGenerator.tileMap[(int) position.x, (int) position.z];
    }

    private void Update()
    {
        if (currentPath != null) {
            int currNode = 0;

            while(currNode < currentPath.Count - 1) {
                Vector3 start = mapGenerator.CoordToPosition(currentPath[currNode].x, currentPath[currNode].y) - new Vector3(0, 1, 0);
                Vector3 end = mapGenerator.CoordToPosition(currentPath[currNode + 1].x, currentPath[currNode + 1].y) - new Vector3(0, 1, 0);

                currNode++;

                Debug.DrawLine(start, end, Color.red, Time.deltaTime, false);
            }
        }

        Vector3 direction = (targetTile.transform.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }

        if (Vector3.Distance(transform.position, targetTile.transform.position) > 0f)
        {

            transform.position = Vector3.MoveTowards(transform.position, targetTile.transform.position, walkingSpeed * Time.deltaTime);
            if (!animation.isPlaying)
            {
                animation.Play();
            }
        }
    }

    private IEnumerator Move()
    {
        yield return new WaitForSeconds(hopFrequency);

        while (true)
        {
            GeneratePathTo(0, 0, mapGenerator.graph);

            currentTile = GetTileFromPosition(transform.position);

            List<Transform> surroundingTiles = mapGenerator.GetSurroundingTiles(currentTile.GetComponent<Tile>().x, currentTile.GetComponent<Tile>().y, true);
            targetTile = surroundingTiles[Random.Range(0, surroundingTiles.Count)];

            yield return new WaitForSeconds(hopFrequency);
        }
    }

    public void GeneratePathTo(int x, int y, Node[,] graph) {

        currentPath = null;

        Dictionary<Node, float> dist = new Dictionary<Node, float>();
        Dictionary<Node, Node> prev = new Dictionary<Node, Node>();

        List<Node> unvisited = new List<Node>();

        Node source = graph[currentTile.GetComponent<Tile>().x, currentTile.GetComponent<Tile>().y];
        Node target = graph[x, y];

        dist[source] = 0;
        prev[source] = null;

        //Init everything to have infinity distance
        foreach(Node v in graph) {
            if (v != source) {
                dist[v] = Mathf.Infinity;
                prev[v] = null;
            }

            unvisited.Add(v);
        }

        while(unvisited.Count > 0) {
            //U is going to be the unvisited node with the smallest distance
            Node u = null;

            foreach(Node possibleU in unvisited) {
                if (u == null || dist[possibleU] < dist[u]) {
                    u = possibleU;
                }
            }

            if (u == target) {
                break;
            }

            unvisited.Remove(u);

            foreach(Node v in u.neighbours) {
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
            Debug.Log("NO PATH");
            return;
        }

        currentPath = new List<Node>();
        Node curr = target;

        //Step through prev chain and add it to path
        while(curr != null) {
            currentPath.Add(curr);
            curr = prev[curr];
        }

        //currentPath describes a route from target to our source, so invert
        currentPath.Reverse();
    }
}
