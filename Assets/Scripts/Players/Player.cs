
using UnityEngine;

public abstract class Player : MonoBehaviour
{
    protected bool initialMove;
    private void Start()
    {
        initialMove = false;
    }

    public abstract void DrawCard();
    public abstract void InitBoard();


}
