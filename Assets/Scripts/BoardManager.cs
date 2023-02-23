using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEngine;
using static ChessRules;
using static ChessInfo;

// chess x = a-h, y = 1-8
public class BoardManager : MonoBehaviour
{
    public List<Piece> pieces;

    private float _screenWidth;
    private const float TOLERANCE = 0.01f;
    private float _xMax;
    private float _yMax;
    private float _xMin;
    private float _yMin;
    private float _dx = 1;
    private float _dy = 1;
    private PieceColor _turn = PieceColor.WHITE;
    private int _ticksSinceLastMove;
    public int ticksPerMove = 10;
    private bool _weAreLive;
    private ChessAI _ai;
    public TextMeshProUGUI topText;
    public TextMeshProUGUI bottomText;
    private Piece _movingPiece = null;
    public GameObject prefabPiece;

    private void GetPieceGridBounds()
    {
        _xMax = _yMax = 0.77f;
        _yMin = _xMin = -0.77f;
        _dx = (_xMax - _xMin) / 7;
        _dy = (_yMax - _yMin) / 7;
        Debug.Log($"BOUNDS_UPDATE: {_xMin}=>{_xMax} dx={_dx}");
    }

    // ReSharper disable once InconsistentNaming
    private float cx2tx(int cx)
    {
        return _xMin + (cx - 1) * _dx;
    }

    // ReSharper disable once InconsistentNaming
    private float cy2ty(int cy)
    {
        return _yMin + (cy - 1) * _dy;
    }

    private Vector3 PieceTargetPos(Piece piece)
    {
        return PieceTargetPosXY(piece.cx, piece.cy);
        // return new Vector3(cx2tx(piece.cx), cy2ty(piece.cy), 0);
    }

    private Vector3 PieceTargetPosXY(int x, int y)
    {
        return new Vector3(cx2tx(x), cy2ty(y), 0);
    }

    private float DistanceToTarget(Piece piece)
    {
        return Vector3.Distance(piece.transform.localPosition, PieceTargetPos(piece));
    }

    private void MoveAPieceToTarget(Piece piece, Vector3 target)
    {
        piece.transform.localPosition = Vector3.Lerp(piece.transform.localPosition, target, 0.25f);
    }

    private bool UpdatePieceLocations()
    {
        var moved = false;
        if (pieces == null) return false;
        foreach (var piece in pieces.Where(piece => piece.Alive() && DistanceToTarget(piece) > TOLERANCE))
        {
            MoveAPieceToTarget(piece, PieceTargetPos(piece));
            // piece.transform.localPosition = Vector3.Lerp(piece.transform.localPosition, PieceTargetPos(piece), 0.25f);
            moved = true;
        }

        return moved;
    }

    private void DoAIBoardMove()
    {
        _ai ??= new ChessAISimple();

        if (_turn == PieceColor.BLACK)
        {
            var livePieces = pieces.Cast<IPieceData>().Where(piece => piece.Alive()).ToList();
            if (livePieces.Any(piece => piece.Color() == _turn))
            {
                var bestMove = _ai.BestMove(ref livePieces, _turn);
                if (bestMove != null && bestMove.NotZero())
                {
                    var pieceToMove = pieces.FirstOrDefault(piece =>
                        piece.X() == bestMove.piece.X() && piece.Y() == bestMove.piece.Y());
                    MoveOnePiece(ref livePieces, pieceToMove, bestMove.x, bestMove.y);
                }

                EndTurn();
            }
            else
            {
                Debug.Log($"No pieces!");
                Debug.Break();
            }
        }
    }

    private void KillAndRestartBoard()
    {
        if (pieces != null)
        {
            foreach (var piece in pieces)
            {
                Destroy(piece.gameObject);
            }
            pieces = null;
            _weAreLive = false;
        } 
        pieces = new List<Piece>();
        var pieceInfoList = DefaultBoard();
        foreach (var pieceInfo in pieceInfoList)
        {
            var pieceObject = Instantiate(prefabPiece, transform);
            var piece = pieceObject.GetComponent<Piece>();
            piece.CopyInfo(pieceInfo);
            pieces.Add(piece);
        }
        _turn = PieceColor.WHITE;
        _weAreLive = true;
    
    }

    public void OnClick(string buttonName)
    {
        Debug.Log($"BoardManager.OnClick: {buttonName}");
        _weAreLive = true;
        _ai = buttonName switch
        {
            // "random" => new ChessAIRandom(),
            "simple" => new ChessAISimple(),
            "deep" => new ChessAIDeep(3, 10),
            _ => _ai
        };
        if (buttonName == "random")
            KillAndRestartBoard();
        else EndTurn();
    }

    private string TopText()
    {
        var livePieces = pieces.Cast<IPieceData>().Where(piece => piece.Alive()).ToList();
        return new[] {PieceColor.WHITE, PieceColor.BLACK}.Aggregate("",
            (current, turn) => current + $"{turn,6}:{ChessAI.BoardScore(ref livePieces, turn),-6:G}");
    }

    private void UpdateText()
    {
        if (!_weAreLive) return;
        topText.text = TopText();
        if (null != _ai)
            bottomText.text = _ai.GetAIDescription();
    }

    private void EndTurn()
    {
        UpdatePieceLocations();
        UpdateText();
        _turn = OtherColor(_turn);
    }


    void FixedUpdate()
    {
        if (!_weAreLive) return;
        if (UpdatePieceLocations()) return;
        if (++_ticksSinceLastMove % ticksPerMove != 0) return;
        DoAIBoardMove();
        _ticksSinceLastMove = 0;
    }

    Vector2Int GetCxy()
    {
        if (null == Camera.main) return new Vector2Int(0, 0);
        var mousePos = Input.mousePosition - transform.position;
        var worldPos = transform.InverseTransformPoint(Camera.main.ScreenToWorldPoint(mousePos));
        var cx = (int) Math.Floor(worldPos.x / .22) + 5;
        var cy = (int) Math.Floor(worldPos.y / .22) + 5;
        return ValidXY(cx, cy) ? new Vector2Int(cx, cy) : new Vector2Int(0, 0);
    }


    private void OnMouseDown()
    {
        var mouseCxy = GetCxy();
        if (_turn == PieceColor.WHITE)
        {
            if (null == _movingPiece)
            {
                var tp = pieces.FirstOrDefault(p => p.Alive() &&
                                                    p.Color() == _turn &&
                                                    p.X() == mouseCxy.x &&
                                                    p.Y() == mouseCxy.y);
                if (tp != null)
                {
                    _movingPiece = tp;
                }
            }
            else
            {
                var livePieces = pieces.Cast<IPieceData>().Where(piece => piece.Alive()).ToList();
                var dcx = mouseCxy.x - _movingPiece.X();
                var dcy = mouseCxy.y - _movingPiece.Y();
                if (CheckValidMove(ref livePieces, _movingPiece, dcx, dcy))
                {
                    MoveOnePiece(ref livePieces, _movingPiece, dcx, dcy);
                    EndTurn();
                }
                _movingPiece = null;
                _weAreLive = true;
            }
        }

        Debug.Log($"Clicked on Board (cx={mouseCxy.x}, cy={mouseCxy.y})");
    }

    // Update is called once per frame
    void Update()
    {
        if (Math.Abs(_screenWidth - Screen.width) > TOLERANCE)
        {
            GetPieceGridBounds();
            _screenWidth = Screen.width;
        }

        if (null != _movingPiece)
        {
            _weAreLive = false;
            var mouseCxy = GetCxy();
            MoveAPieceToTarget(_movingPiece, PieceTargetPosXY(mouseCxy.x, mouseCxy.y));
        }
    }
}