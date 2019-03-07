using UnityEngine;

public static class CustomExtensions
{

    public static readonly float EPSILON = 0.01f;

    public static bool NearlyEqual(this float num1, float num2)
    {

        return NearlyEqual(num1, num2, EPSILON);

    }

    public static bool NearlyEqual(this float num1, float num2, float customEpsilon)
    {

        return Mathf.Abs(num1 - num2) < customEpsilon;

    }

}
