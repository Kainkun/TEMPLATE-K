using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MovingKinematic
{
    private bool forward = true;
    private float t = 0;
    public float time = 1;
    public Vector2 direction = new Vector2(5, 0);

    protected override Vector2 Move()
    {
        if (forward)
        {
            if (t >= 1)
                forward = false;

            t += Time.fixedDeltaTime * 1 / time;
        }
        else
        {
            if (t <= 0)
                forward = true;

            t -= Time.fixedDeltaTime * 1 / time;
        }

        return Vector2.Lerp(startPosition, startPosition + direction, t);
    }
}