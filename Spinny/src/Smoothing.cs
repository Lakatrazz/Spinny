using UnityEngine;

namespace Spinny;

public static class Smoothing
{
    public static float CalculateDecay(float decay, float deltaTime)
    {
        return 1f - Mathf.Exp(-decay * deltaTime);
    }
}