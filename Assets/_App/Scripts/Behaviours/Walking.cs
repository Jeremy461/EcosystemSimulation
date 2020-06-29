using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Walking : BunnyBehaviour
{
    private Transform currentTile;
    private float timer = 0;
    private bool hasTarget = false;

    private Vector3 targetTile;

    public override void Move(Bunny bunny)
    {
        timer += Time.deltaTime;
        // Hop every "hopFrequency"-seconds
        if (timer % 60 >= bunny.hopFrequency) {
            // If bunny has no specific path, generate a random target tile
            if (bunny.currentPath == null)
            {
                currentTile = bunny.GetTileFromPosition(bunny.transform.position);

                List<Transform> surroundingTiles = bunny.mapGenerator.GetSurroundingTiles(currentTile.GetComponent<Tile>().x, currentTile.GetComponent<Tile>().y, true);
                targetTile = surroundingTiles[Random.Range(0, surroundingTiles.Count)].position;
            } 
            // Bunny has a path
            else
            {
                bunny.currentPath.RemoveAt(0);

                targetTile = bunny.mapGenerator.CoordToPosition(bunny.currentPath[0].x, bunny.currentPath[0].y);

                if (bunny.currentPath.Count == 1)
                {
                    bunny.currentPath = null;
                }
            }

            hasTarget = true;
            timer = 0;
            bunny.transform.LookAt(targetTile);
        }

        // If not at target, move to target and animate
        if (Vector3.Distance(bunny.transform.position, targetTile) > 0f && hasTarget)
        {
            bunny.transform.position = Vector3.MoveTowards(bunny.transform.position, targetTile, bunny.walkingSpeed * Time.deltaTime);
            if (!bunny.animation.isPlaying)
            {
                bunny.animation.Play();
            }
        } else
        {
            if (bunny.currentPath == null && bunny.foundFood)
            {
                bunny.behaviour = new Eating();
            }

            if (bunny.currentPath == null && bunny.foundWater)
            {
                bunny.behaviour = new Drinking();
            }

            if (bunny.currentPath == null && bunny.foundMate) {
                bunny.mate.behaviour = new Mating();
                bunny.behaviour = new Mating();
            }
        }
    }
}
