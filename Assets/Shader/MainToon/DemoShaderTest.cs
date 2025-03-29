using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DemolitionCar
{
    public class DemoShaderTest : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Shader.EnableKeyword("_BLACK");
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                Shader.DisableKeyword("_BLACK");
            }
        }
    }
}
