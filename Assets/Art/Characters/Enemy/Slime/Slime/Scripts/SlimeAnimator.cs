using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeAnimator : MonoBehaviour
{
    public enum SlimeAnimation
    { idle, jump_attack, hurt, die }
    public SlimeAnimation CurrentAnimation = SlimeAnimation.idle;
    public Animator ani;

    // <Animation Testing Area>
    bool TestAllAnimations;
    float TestAnimationsTime = 3f;
    bool debug;
    // </Animation Testing Area>

    // Start is called before the first frame update
    void Start()
    {
        ani = GetComponent<Animator>();
        CurrentAnimation = SlimeAnimation.idle;

        // <Animation Testing Area>
            TestAllAnimations = false; //Change this to true for all slime animations cycle test. Fuck!, I am over engineering it :(
        if (TestAllAnimations)
        {
            debug = true;
            TestAllAnimations = true;
        }
        else
        {
            debug = false;
            TestAllAnimations=false;
        }
        // </Animation Testing Area>

    }

    // Update is called once per frame
    void Update()
    {
        if (!TestAllAnimations&&!debug)
        {
            if (CurrentAnimation==SlimeAnimation.idle)
                ani.SetTrigger("idle");
            else if (CurrentAnimation == SlimeAnimation.jump_attack)
                ani.SetTrigger("jump_attack");
            else if (CurrentAnimation == SlimeAnimation.hurt)
                ani.SetTrigger("hurt");
            else if (CurrentAnimation == SlimeAnimation.die)
                ani.SetTrigger("die");
            else ani.SetTrigger("idle");
        }
        else if (TestAllAnimations) { StartCoroutine(AutoTest());}
    }

    private IEnumerator AutoTest()
    {
        TestAllAnimations = false;
        ani.SetTrigger("idle");
        yield return new WaitForSeconds(TestAnimationsTime);
        ani.SetTrigger("jump_attack");
        yield return new WaitForSeconds(TestAnimationsTime);
        ani.SetTrigger("hurt");
        yield return new WaitForSeconds(TestAnimationsTime);
        ani.SetTrigger("die");
        yield return new WaitForSeconds(TestAnimationsTime);
        ani.SetTrigger("idle");
        TestAllAnimations = true;
    }

}
