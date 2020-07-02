using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drinking : BunnyBehaviour
{
    public override void Move(Bunny bunny)
    {
        if (bunny.thirst > 0)
        {
            bunny.thirst -= Time.deltaTime * 20;
        } else
        {
            bunny.lookingFor = Bunny.LookingFor.Nothing;
            bunny.foundWater = false;
            bunny.behaviour = new Walking();
        }
    }
}
