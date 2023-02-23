using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static ChessInfo;


public interface IPieceData
{
    public void SetXY(int x, int y);
    public int X();
    public int Y();
    public bool Alive();
    public PieceColor Color();
    public PieceType PType();
    public int PieceID();
    public void RemoveSelf();
}

public class PieceInfo : IPieceData
{
    private PieceType _pieceType;
    private PieceColor _pieceColor;
    private PieceState _pieceState;
    private int _cx;
    private int _cy;
    private int _pieceID;

    public PieceInfo(IPieceData pieceData)
    {
        CopyInfo(pieceData);
    }

    public PieceInfo(PieceType type, PieceColor color, int cx, int cy)
    {
        _pieceType = type;
        _pieceColor = color;
        _cx = cx;
        _cy = cy;
    }

    public void CopyInfo(IPieceData other)
    {
        _pieceType = other.PType();
        _pieceColor = other.Color();
        _pieceState = other.Alive() ? PieceState.ALIVE : PieceState.DEAD;
        _cx = other.X();
        _cy = other.Y();
        _pieceID = other.PieceID();
    }

    public void SetXY(int x, int y) { _cx = x; _cy = y; }
    
    public int X() { return _cx; }
    public int Y() { return _cy; }
    public int PieceID() { return _pieceID; }

    public bool Alive() { return _pieceState == PieceState.ALIVE; }
    public PieceColor Color() { return _pieceColor; }
    public PieceType PType() { return _pieceType; }

    public void RemoveSelf()
    {
        _pieceState = PieceState.DEAD;
        SetXY(-1,-1);
    }
}


public class Piece :  MonoBehaviour, IPieceData
{
    public PieceType pieceType;
    public PieceColor pieceColor;
    public PieceState pieceState;
    public int cx;
    public int cy;
    public int pieceID;
    public Sprite[] sprites;
    
    private Piece()
    {
    }

    public void SetXY(int x, int y) { cx = x; cy = y; }
    
    public int X() { return cx; }
    public int Y() { return cy; }

    public bool Alive() { return pieceState == PieceState.ALIVE; }
    public PieceColor Color() { return pieceColor; }
    public PieceType PType() { return pieceType; }
    public int PieceID()
    {
        return pieceID;
    }

    public void CopyInfo(IPieceData other)
    {
        pieceType = other.PType();
        pieceColor = other.Color();
        cx = other.X();
        cy = other.Y();
    }
    
    public void RemoveSelf()
    {
        if (pieceState != PieceState.ALIVE) return;
        pieceState = PieceState.DEAD;
        Hide();
        SetXY(-1,-1);
    }

    private void Hide()
    {
        GetComponent<SpriteRenderer>().enabled = false;
    }

    private void Show()
    {
        var sr = GetComponent<SpriteRenderer>();
        var spriteName = $"{pieceColor.ToString().ToLower()}_{pieceType.ToString().ToLower()}";
        var sprite = sprites.FirstOrDefault(s => s.name == spriteName);
        if (sprite == null)
        {
            Debug.LogError($"Sprite not found: {spriteName}");
            return;
        }
        sr.sprite = sprite;
        sr.enabled = true;
    }

    void OnMouseDown()
    {
        Debug.Log("Clicked on " + pieceType + " " + pieceColor);
    }
    
    void Start()
    {
        name = $"{pieceColor}_{pieceType}_{cx}_{cy}";
        pieceState = PieceState.ALIVE;
        Show();
        pieceID = Random.Range(10000000, 100000000);
    }
    
}
