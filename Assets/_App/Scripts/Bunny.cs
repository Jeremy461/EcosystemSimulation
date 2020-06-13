using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bunny : MonoBehaviour
{
    public float hopFrequency = 1;
    public float walkingSpeed = 3;
    public float rotationSpeed = 3;

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

        StartCoroutine(Move());
    }

    public Transform GetTileFromPosition(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x / mapGenerator.tileSize + (mapGenerator.mapSize.x - 1) / 2f);
        int y = Mathf.RoundToInt(position.z / mapGenerator.tileSize + (mapGenerator.mapSize.y - 1) / 2f);
        x = Mathf.Clamp(x, 0, mapGenerator.tileMap.GetLength(0));
        y = Mathf.Clamp(y, 0, mapGenerator.tileMap.GetLength(1));
        return mapGenerator.tileMap[x, y];
    }

    private void Update()
    {
        Vector3 direction = (targetTile.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        if (direction != Vector3.zero)
        {
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
            currentTile = GetTileFromPosition(transform.position);

            List<Transform> surroundingTiles = mapGenerator.GetSurroundingTiles(currentTile.GetComponent<Tile>().x, currentTile.GetComponent<Tile>().y, true);
            targetTile = surroundingTiles[Random.Range(0, surroundingTiles.Count)];

            yield return new WaitForSeconds(hopFrequency);
        }
    }
}
