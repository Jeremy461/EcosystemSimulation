﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Eating : BunnyBehaviour
{
    public override void Move(Bunny bunny)
    {
        if (bunny.hunger > 0)
        {
            bunny.targetPlant.GetComponent<Plant>().eatenBy = bunny;
            bunny.hunger -= Time.deltaTime * 20;
            bunny.targetPlant.localScale = Vector3.Scale(bunny.targetPlant.localScale, new Vector3(0.997f, 0.997f, 0.997f));
        } else
        {
            bunny.foundFood = false;
            bunny.targetPlant.GetComponent<Plant>().eatenBy = null;
            bunny.behaviour = new Walking();
        }
    }
}