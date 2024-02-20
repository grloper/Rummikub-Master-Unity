using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    // The image of the card
    public Image image;
    // The parent of the card after it has been dragged
    [HideInInspector] public Transform parentAfterDrag;
    [HideInInspector] public Transform parentBeforeDrag;
    // The original position of the card
    private Vector3 originalPosition;


    [HideInInspector] private GameManager gameManager;
    private void Start()
    {

        this.gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

    }
    public void OnBeginDrag(PointerEventData eventData)
    {

        if (this.gameManager.GetTurn() == Constants.HumanTurn)
        {   // Store the original parent of the card
            parentBeforeDrag = transform.parent;

            // Set the parent of the card to the canvas
            parentAfterDrag = transform.parent;
            transform.SetParent(transform.root);
            // Set the card to the top of the hierarchy so it renders on top of other cards
            transform.SetAsLastSibling();
            // Disable raycasting so the card doesn't get caught on other cards
            image.raycastTarget = false;
        }

    }

    public void OnDrag(PointerEventData eventData)
    {
        if (this.gameManager.GetTurn() == Constants.HumanTurn)
        {
            // Set the position of the card to the mouse position with the offset
            transform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (this.gameManager.GetTurn() == Constants.HumanTurn)
        {
            // Set the parent of the card to the tile slot
            transform.SetParent(parentAfterDrag);
            // Enable raycasting again
            image.raycastTarget = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (this.gameManager.GetTurn() == Constants.HumanTurn)
        {
            // Store the original position of the card
            originalPosition = image.transform.position;
            // Move the card up by a certain amount
            image.transform.position = new Vector3(image.transform.position.x, image.transform.position.y + 6, image.transform.position.z);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (this.gameManager.GetTurn() == Constants.HumanTurn)
        {
            // Move the card back to its original position
            image.transform.position = originalPosition;
        }
    }
}
