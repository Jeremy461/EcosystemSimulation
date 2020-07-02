using UnityEngine;

public class Mating : BunnyBehaviour {

    public override void Move(Bunny bunny) {
        if (!bunny.animation.isPlaying) {
            bunny.animation.Play();
        }
        
        if (bunny.reproductiveUrge > 0)
        {
            bunny.reproductiveUrge -= Time.deltaTime * 20;
        } else if (bunny.mate.reproductiveUrge <= 0)
        {
            bunny.mate.lookingFor = Bunny.LookingFor.Nothing;
            bunny.lookingFor = Bunny.LookingFor.Nothing;
            bunny.mate.foundMate = false;
            bunny.foundMate = false;
            bunny.mate.behaviour = new Walking();
            bunny.behaviour = new Walking();
        }
    }
}
