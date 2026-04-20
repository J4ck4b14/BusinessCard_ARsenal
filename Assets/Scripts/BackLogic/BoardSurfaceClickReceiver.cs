using UnityEngine;

public class BoardSurfaceClickReceiver : MonoBehaviour
{
    [SerializeField] private Transform boardSpaceRoot;
    [SerializeField] private ShieldPlacementController shieldPlacementController;

    public void ReceiveBoardHit(Vector3 worldHitPoint)
    {
        if (boardSpaceRoot == null || shieldPlacementController == null)
            return;

        Vector3 boardLocalPoint = boardSpaceRoot.InverseTransformPoint(worldHitPoint);
        shieldPlacementController.TryPlaceShieldAtBoardLocal(boardLocalPoint);
    }
}