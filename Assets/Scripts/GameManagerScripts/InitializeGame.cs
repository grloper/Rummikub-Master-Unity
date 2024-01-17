using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitializeGame : MonoBehaviour
{
    //game Board
    [SerializeField] GameObject BoardGrid;
    [SerializeField] GameObject HumanGrid;
    //game Tiles
    [SerializeField] GameObject TileSlotPrefab;
    [SerializeField] GameObject TileSlotPlayerPrefab;

    //Save Human to tell him to start generating
    public Human human;

    // Start is called before the first frame update
    void Start()
    {
       
        //Empty Slot Creater
        Init();
    }

    private void Init()
    {
        // Initialize Game Board with 175 slots
        for (int i = 0; i < 175; i++)
        {
            Instantiate(TileSlotPrefab, BoardGrid.transform);
        }

        // Initialize Human Board with 40 slots
        for (int i = 0; i < 40; i++)
        {
            Instantiate(TileSlotPlayerPrefab, HumanGrid.transform);
        }
        human.InitBoard();


    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
