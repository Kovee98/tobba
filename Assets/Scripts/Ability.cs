using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability : MonoBehaviour {
    // Start is called before the first frame update
    void Start () {
        
    }

    public void UsePrimary () {
        Debug.Log("using primary...");
    }

    public void UseSecondary () {
        Debug.Log("using secondary...");
    }

    public void UseLegendary () {
        Debug.Log("using legendary...");
    }
}
