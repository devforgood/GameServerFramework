using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MaykerStudio.Demo
{
    public class FPSCounter : MonoBehaviour
    {
        private TextMeshProUGUI textFPS;

        // Add this to the class variables
        [Range(1, 100)] public int smoothingFrames = 60;
        private Queue<float> frameTimes = new Queue<float>();

        void Start()
        {
            textFPS = GetComponent<TextMeshProUGUI>();
        }

        void Update()
        {
            frameTimes.Enqueue(Time.deltaTime);
            if (frameTimes.Count > smoothingFrames)
            {
                frameTimes.Dequeue();
            }

            float averageDelta = 0;
            foreach (float t in frameTimes) averageDelta += t;
            averageDelta /= frameTimes.Count;

            textFPS.text = $"FPS: {Mathf.RoundToInt(1f / averageDelta)}";
        }
    }
}
