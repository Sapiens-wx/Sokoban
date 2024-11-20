using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Ctrller : MonoBehaviour
{
    public MGridObject go;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(Input.GetKey(KeyCode.A))
            go.Move(2);
        else if(Input.GetKey(KeyCode.D))
            go.Move(3);
        else if(Input.GetKey(KeyCode.W))
            go.Move(0);
        else if(Input.GetKey(KeyCode.S))
            go.Move(1);
    }
}
