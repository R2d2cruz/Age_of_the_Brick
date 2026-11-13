using UnityEngine;

/// <summary>
/// Pure helpers for flee destination points (XZ, away from threat).
/// </summary>
public static class FleeDestination
{
    /// <summary>
    /// Returns a world point <paramref name="distance"/> from <paramref name="self"/>
    /// away from <paramref name="threat"/> on XZ. Uses <paramref name="fallbackForward"/> if direction is degenerate.
    /// Preserves <paramref name="self"/>.y.
    /// </summary>
    public static Vector3 ComputeAwayPoint(Vector3 self, Vector3 threat, float distance, Vector3 fallbackForward)
    {
        Vector3 away = self - threat;
        away.y = 0f;
        if (away.sqrMagnitude < 1e-6f)
        {
            away = fallbackForward;
            away.y = 0f;
            if (away.sqrMagnitude < 1e-6f)
                away = Vector3.forward;
        }

        away.Normalize();
        float d = Mathf.Max(0f, distance);
        return new Vector3(self.x + away.x * d, self.y, self.z + away.z * d);
    }
}
