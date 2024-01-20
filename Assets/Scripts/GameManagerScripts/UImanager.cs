using Microsoft.Unity.VisualStudio.Editor;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
public class UImanager : MonoBehaviour
{
    public List<Sprite> cardsUI = new List<Sprite>();
    [SerializeField] GameObject PrefabTile;
    [SerializeField] TextMeshProUGUI btnDeckText;
    [SerializeField] Human human;
    [SerializeField] Computer computer;
    [SerializeField] GameManager gameManager;
    [SerializeField] private GameBoard board;


    public Card InstinitanteCard(Card GivvenCard, GameObject tileslot)
    {
        GameObject card = Instantiate(PrefabTile);
        int index = (GivvenCard.Number - 1) * 4 + (int)GivvenCard.Color; // jokers is when number = 14
        card.GetComponent<Image>().sprite = cardsUI[index];
        card.transform.parent = tileslot.transform;
        return card.GetComponent<Card>();
    }
    public Card InstinitanteCard(Card GivvenCard)
    {
        GameObject card = Instantiate(PrefabTile);
        int index = (GivvenCard.Number - 1) * 4 + (int)GivvenCard.Color; // jokers is when number = 14
        card.GetComponent<Image>().sprite = cardsUI[index];
        return card.GetComponent<Card>();
    }
    public void UpdateBtnDeckText()
    {
        if (board.GetRummikubDeckInstance().GetDeckLength() != 0)
        {
            if (gameManager.GetTurn() == 0)
            {
                human.DrawCard();
                gameManager.ChangeTurn();
            }
            else
            {
                computer.DrawCard();
                gameManager.ChangeTurn();

            }
            btnDeckText.text = "Deck: " + board.GetRummikubDeckInstance().GetDeckLength();
        }
        else
        {
            btnDeckText.text = "Deck: Empty";
        }
    }
}
