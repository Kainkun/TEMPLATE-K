using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MovingKinematic
{
    private bool right = true;
    
    protected override Vector2 Move()
    {
        if (right)
        {
            if (Vector2.Distance(transform.position, startPosition) >= 5)
                right = false;

            return (Vector2)transform.position + new Vector2(25 * Time.fixedDeltaTime, 0);
        }
        else
        {
            if (Vector2.Distance(transform.position, startPosition) <= 0)
                right = true;
            
            return (Vector2)transform.position + new Vector2(-25 * Time.fixedDeltaTime, 0);
        }
        
    }
    
}
