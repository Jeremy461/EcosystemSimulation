using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waiting : BunnyBehaviour {
    public override void Move(Bunny bunny) {
        bunny.transform.LookAt(bunny.mate.transform);
    }
}
