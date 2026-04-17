using UnityEngine;

public class SetCanvasTransform
{
    public static void Execute()
    {
        // Board objesini bul
        GameObject board = GameObject.Find("sinif/digerMobilya/board1 (2)");
        
        if (board == null)
        {
            board = GameObject.Find("board1 (2)");
        }
        
        if (board == null)
        {
            Debug.LogError("Board object not found!");
            return;
        }
        
        // UI Canvas'ı bul
        GameObject uiCanvas = GameObject.Find("sinif/UI_Canvas");
        if (uiCanvas == null)
        {
            uiCanvas = GameObject.Find("UI_Canvas");
        }
        
        if (uiCanvas == null)
        {
            Debug.LogError("UI Canvas not found!");
            return;
        }
        
        // Board'un konumunu al
        Vector3 boardPos = board.transform.position;
        Vector3 boardScale = board.transform.localScale;
        
        Debug.Log("Board position: " + boardPos);
        Debug.Log("Board scale: " + boardScale);
        
        // Canvas'ı board'un önüne yerleştir
        // Board z ekseninde duruyor, canvas'ı board'un önüne (z ekseni boyunca) koy
        uiCanvas.transform.position = new Vector3(boardPos.x, boardPos.y, boardPos.z - 1.5f);
        
        // Ölçeği küçült - World Space Canvas için uygun bir ölçek
        uiCanvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        
        // Rotasyonu board'a paralel yap
        uiCanvas.transform.rotation = board.transform.rotation;
        
        Debug.Log("Canvas positioned in front of board at: " + uiCanvas.transform.position);
    }
}