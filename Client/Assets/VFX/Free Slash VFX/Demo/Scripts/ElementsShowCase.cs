using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaykerStudio.Demo
{

    public class ElementsShowCase : MonoBehaviour
    {
        public ParticleSystem[] particles;

        private Coroutine coroutine;

        IEnumerator StartParts()
        {
            while (coroutine != null)
            {
                foreach (ParticleSystem particle in particles)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }

                yield return new WaitForSeconds(1f);

                foreach (ParticleSystem particle in particles)
                {
                    particle.Play(true);
                }

                yield return new WaitForSeconds(1.5f);
            }
        }

        public void StopAllParticles()
        {
            if (coroutine != null)
                StopCoroutine(coroutine);

            coroutine = null;

            foreach (ParticleSystem particle in particles)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        public void StartAllParticles()
        {
            foreach (ParticleSystem particle in particles)
            {
                particle.Play(true);
            }

            coroutine = StartCoroutine(StartParts());
        }
    }

}