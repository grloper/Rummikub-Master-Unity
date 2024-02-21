using UnityEngine;

public class GameBoard : MonoBehaviour
{
    private RummikubDeck rummikubDeck = new RummikubDeck();
    [SerializeField] private UImanager uiManager;


    // Return instance of rummikub deck
    public RummikubDeck GetRummikubDeckInstance() => rummikubDeck;

    // Start is called before the first frame update
    void Start()
    {
        // this class is actually the board grid so we send "transform" 
        uiManager.InitBoardTileSlots(transform);
    }
}