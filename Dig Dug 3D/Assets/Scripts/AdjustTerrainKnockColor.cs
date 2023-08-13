using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustTerrainKnockColor : MonoBehaviour
{
    public Color primary_color, secondary_color;
    private ParticleSystem particle;

    // Start is called before the first frame update
    void Start()
    {
        particle = GetComponent<ParticleSystem>();
        var main = particle.main;

        Gradient particle_gradient = new Gradient();
        particle_gradient.colorKeys = new GradientColorKey[2] { new GradientColorKey(primary_color, .9f), new GradientColorKey(secondary_color, 1f)};
        particle_gradient.alphaKeys = new GradientAlphaKey[2] { new GradientAlphaKey(1f, .9f), new GradientAlphaKey(1f, 1f) };
        particle_gradient.mode = GradientMode.Fixed;

        ParticleSystem.MinMaxGradient particle_color = new ParticleSystem.MinMaxGradient(particle_gradient);
        particle_color.mode = ParticleSystemGradientMode.RandomColor;
        main.startColor = particle_color;
    }
}
