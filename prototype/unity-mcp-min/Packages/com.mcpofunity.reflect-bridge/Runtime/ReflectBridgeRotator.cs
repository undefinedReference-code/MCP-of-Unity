using UnityEngine;

namespace UnityMcpMin
{
    /// <summary>
    /// Sample behaviour for HTTP reflect demos: Y-axis spin. Add via GameObject.AddComponent(typeof(ReflectBridgeRotator)).
    /// </summary>
    public class ReflectBridgeRotator : MonoBehaviour
    {
        [SerializeField]
        float degreesPerSecond = 90f;

        void Update()
        {
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
        }
    }
}
