using BlankProject;
using UnityEngine;

public static class Vector3Extensions
{
    public static int ToInt100k(this float value)
    {
        return Mathf.RoundToInt(value * 100000);
    }

    public static float ToFloat100k(this int value)
    {
        return ((float)value) / 100000;
    }

    public static IntAbsolute ToIntAbsolute(this Vector3 value)
    {
        return new IntAbsolute
        {
            X = value.x.ToInt100k(),
            Y = value.y.ToInt100k(),
            Z = value.z.ToInt100k()
        };
    }

    public static Vector3 ToVector3(this IntAbsolute value)
    {
        return new Vector3(value.X.ToFloat100k(), value.Y.ToFloat100k(), value.Z.ToFloat100k());
    }
}
