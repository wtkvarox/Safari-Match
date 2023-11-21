using UnityEngine;
using DG.Tweening;

public class Piece : MonoBehaviour
{
    public int x;
    public int y;
    public Board board;
    public enum Type
    {
        elephant,
        giraffe,
        hippo,
        monkey,
        panda,
        parrot,
        penguin,
        pig,
        rabbit,
        snake
    };
    public Type type;

    public void Setup(int x_, int y_, Board board_)
    {
        x = x_;
        y = y_;
        board = board_;

        transform.localScale = Vector3.one * 0.35F;
        transform.DOScale(Vector3.one, 0.35F);
    }

    public void Move(int desX, int desY)
    {
        transform.DOMove(new Vector3(desX, desY, -5f), 0.25F).SetEase(Ease.InOutCubic).onComplete = () =>
        {
            x = desX;
            y = desY;
        };
    }

    public void Remove(bool animated)
    {
        if (animated)
        {
            transform.DOScale(Vector3.one * 1.2F, 0.085F).onComplete = () =>
            {
                transform.DOScale(Vector3.zero, 0.1F).onComplete = () =>
                {
                    Destroy(gameObject);
                };
            };
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [ContextMenu("Test Move")]
    public void MoveTest()
    {
        Move(0, 0);
    }
}
