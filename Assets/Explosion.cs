using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : NetworkBehaviour
{

    public float lifetime;

	// Use this for initialization
	void Start () {
        Destroy(transform.gameObject, lifetime);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
