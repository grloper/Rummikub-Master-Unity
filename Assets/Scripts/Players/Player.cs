using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Player : MonoBehaviour
{
    protected bool intialMove;
    private void Start()
    {
        intialMove = false;
    }

    public abstract void DrawCard();
    public abstract void InitBoard();


}
