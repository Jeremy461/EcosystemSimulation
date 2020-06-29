public class Mating : BunnyBehaviour {
    public override void Move(Bunny bunny) {
        float timer = 0;
        if (!bunny.animation.isPlaying) {
            bunny.animation.Play();
        }
        timer ++;
        if (timer == 180) {
            bunny.mate.mate = null;
            bunny.mate = null;
            bunny.mate.behaviour = new Walking();
            bunny.behaviour = new Walking();
        }
    }
}
