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
                bunny.mate = null;
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
                    Debug.Log("Reached destination");
                    bunny.currentPath = null;
                    bunny.reachedPath = true;
                }
            }

            // Update bunny LookingFor variable based on stats
            if (bunny.hunger >= bunny.thirst && bunny.hunger > 60)
            {
                bunny.lookingFor = Bunny.LookingFor.Food;
                Debug.Log("Looking for Food");
            } else if (bunny.thirst >= bunny.hunger && bunny.thirst > 60)
            {
                Debug.Log("Looking for Water");
                bunny.lookingFor = Bunny.LookingFor.Water;
            } else if (bunny.reproductiveUrge > bunny.hunger && bunny.reproductiveUrge > bunny.thirst && bunny.reproductiveUrge > 60)
            {
                Debug.Log("Looking for Mate");
                bunny.lookingFor = Bunny.LookingFor.Mate;
            }

            bunny.transform.LookAt(targetTile);
            hasTarget = true;
            timer = 0;
        }

        // If not at target, move to target and animate
        if (Vector3.Distance(bunny.transform.position, targetTile) > 0f && hasTarget)
        {
            bunny.transform.position = Vector3.MoveTowards(bunny.transform.position, targetTile, bunny.walkingSpeed * Time.deltaTime);
            if (!bunny.animation.isPlaying)
            {
                bunny.animation.Play();
            }
        }

        // Update bunny behaviour if bunny reached path
        // FIX: IT REACHES PATH WITHOUT HAVING A PATH
        if (bunny.reachedPath)
        {
            if (bunny.lookingFor == Bunny.LookingFor.Food)
            {
                bunny.behaviour = new Eating();
            } else if (bunny.lookingFor == Bunny.LookingFor.Water)
            {
                bunny.behaviour = new Drinking();
            } else if (bunny.lookingFor == Bunny.LookingFor.Mate)
            {
                bunny.mate.behaviour = new Mating();
                bunny.behaviour = new Mating();
            }
        }
    }
}
