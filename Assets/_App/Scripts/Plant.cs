using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plant : MonoBehaviour
{
    public Bunny eatenBy;

    void Update()
    {
        if (transform.localScale.x < 1 && eatenBy == null)
        {
            transform.localScale = Vector3.Scale(transform.localScale, new Vector3(1.0005f, 1.0005f, 1.0005f));
        }

        if (transform.localScale.x <= 0) 
        {
            eatenBy.behaviour = new Walking();
            Destroy(gameObject);
        }
    }
}
