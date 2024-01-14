using Microsoft.Unity.VisualStudio.Editor;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
public class UImanager : MonoBehaviour
{
    public List<Sprite> cardsUI = new List<Sprite>();
    [SerializeField] GameObject PrefabTile;
  
    public Card InstinitanteCard(Card GivvenCard,GameObject tileslot)
    {
        GameObject card =Instantiate(PrefabTile);
        int index = (GivvenCard.Number - 1) * 4 + (int)GivvenCard.Color; // jokers is when number = 14
        card.GetComponent<Image>().sprite=cardsUI[index];
        card.transform.parent=tileslot.transform;
        return card.GetComponent<Card>();
    }

}
