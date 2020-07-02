using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waiting : BunnyBehaviour {
    public override void Move(Bunny bunny) {
        if (bunny.mate != null)
        {
            bunny.transform.LookAt(bunny.mate.transform);
        } else
        {
            bunny.behaviour = new Walking();
        }
    }
}
