
using UnityEngine;

public class InitializeGame : MonoBehaviour
{
    //Grids of the game
    [SerializeField] GameObject BoardGrid;
    [SerializeField] GameObject HumanGrid;
    //Tile Slot Prefabs 
    [SerializeField] GameObject TileSlotPrefab;
    [SerializeField] GameObject TileSlotPlayerPrefab;

    //Save Human to tell him to start generating
    public Human human;
    public Computer computer;

    // Start is called before the first frame update
    void Start()
    {
        //Empty Slot Creater
        Init();
    }

    private void Init()
    {
        // Initialize Game Board with 232 slots 8HEIGHT*29WIDTH
        for (int i = 0; i < Constants.MaxGameBoardSlots; i++)
        {
            Instantiate(TileSlotPrefab, BoardGrid.transform);
        }

        // Initialize Human Board with 40 slots 2HEIGHT*20WIDTH
        for (int i = 0; i < Constants.MaxHumanBoardSlots; i++)
        {
            Instantiate(TileSlotPlayerPrefab, HumanGrid.transform);
        }
        // call Players to start generating their decks with 14 random cards
        human.InitBoard();
        computer.InitBoard();
    }
}
