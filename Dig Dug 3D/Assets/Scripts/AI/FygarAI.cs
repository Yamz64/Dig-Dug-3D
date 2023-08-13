using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FygarAI : BaseEnemyAI
{
    [SerializeField]
    private float fire_min_delay, fire_delay;
    private float fire_delay_max;
    private bool firing, interrupt_fire_sequence;
    private Vector3 look_dir;
    private Animator anim;
    private AudioSource fygar_flame;
    [SerializeField]
    private GameObject fire_particle;

    public override IEnumerator DeathSequence()
    {
        pop_sound.Play();
        //calculate points based on how high up the enemy is and if the player itself is close to the same height of the fygar
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            PlayerMovement player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();

            int score_mult = Mathf.Abs(player.transform.position.y - transform.position.y) < 0.75f ? 2 : 1;

            if (transform.position.y > player.GetLayerOffsets()[1])
                GameManager.instance.score += 200 * score_mult;
            else if (transform.position.y > player.GetLayerOffsets()[2])
                GameManager.instance.score += 300 * score_mult;
            else if (transform.position.y > player.GetLayerOffsets()[3])
                GameManager.instance.score += 400 * score_mult;
            else
                GameManager.instance.score += 500 * score_mult;
        }
        yield return new WaitForSeconds(.4f);
        GameManager.instance.alive_enemies--;
        Destroy(gameObject);
    }

    //Coroutine handles the actual sequence of events firing involves
    IEnumerator FireSequence()
    {
        //first start firing, start the fire animation, emit a particle at a certain time in the animation, then stop when the animation is completed
        firing = true;
        anim.SetBool("Fire", true);

        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(1).IsName("FireTelegraph"));

        if (interrupt_fire_sequence)
            yield break;

        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(1).normalizedTime >= 20.0f/30.0f);

        if (interrupt_fire_sequence)
            yield break;

        fygar_flame.Play();
        Instantiate(fire_particle, transform.position, Quaternion.LookRotation(look_dir, Vector3.up));

        if (interrupt_fire_sequence)
            yield break;

        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(1).normalizedTime >= 1.0f);

        firing = false;
        anim.SetBool("Fire", false);
        fire_delay = Random.Range(fire_min_delay, fire_delay_max);
    }

    //Overridden Fygar AI behaves nearly the same to that of a base AI
    //It decrements a timer before entering the fire state, in which it emits flames
    public override void AI()
    {
        //interrupt any firing attempts if you're inflating
        if (pump_level > 0)
        {
            interrupt_fire_sequence = true;
            firing = false;
            anim.SetBool("Fire", false);
            if (fire_delay <= 0.0f)
                fire_delay = Random.Range(fire_min_delay, fire_delay_max);
        }
        else
            interrupt_fire_sequence = false;

        //don't fire at all if you are in the ghost state or being pumped
        if (!ghost || pump_level > 0)
        {
            //start firing when it is time to fire
            if (fire_delay <= 0.0f && !firing)
                StartCoroutine(FireSequence());
            else
                fire_delay -= Time.deltaTime;            
        }   
        //don't do any normal AI stuff if you are currently firing
        if (firing)
        {
            rb.velocity = Vector3.zero;
            return;
        }
        base.AI();
        look_dir = rb.velocity;
        look_dir.y = 0.0f;
        look_dir.Normalize();
    }

    public override void Start()
    {
        base.Start();

        fire_delay_max = fire_delay;
        fire_delay = Random.Range(fire_min_delay, fire_delay_max);

        firing = false;
        interrupt_fire_sequence = false;

        anim = transform.GetChild(0).GetComponent<Animator>();
        fygar_flame = GetComponents<AudioSource>()[2];
    }
}
