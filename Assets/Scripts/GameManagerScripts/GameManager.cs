using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager :MonoBehaviour
{
    private int turn = 0; // 0 = Human, 1 = Computer

    public void ChangeTurn()
    {
        this.turn = this.turn ^ 1; // 0 = 0^1 = 1, 1 = 1^1 = 0
    }
    public int GetTurn()
    {
        return this.turn;

    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
