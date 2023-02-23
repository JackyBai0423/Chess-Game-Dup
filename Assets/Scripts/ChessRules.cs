using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ChessInfo;

public static class ChessRules
{
    public static bool ValidXY(int x, int y)
    {
        return x is >= 1 and <= 8 && y is >= 1 and <= 8;
    }

    
    private static bool DuplicatesExist(ref List<IPieceData> pieces)
    {
        return pieces
            .GroupBy(x => x.X() + x.Y() * 100).Count(g => g.Count() > 1) > 0;
    }

    private static bool MovePiece(ref List<IPieceData> pieces, int startX, int startY, int targetX, int targetY)
    {
        if (!ValidXY(startX, startY) || !ValidXY(targetX, targetY)) return false;
        if (DuplicatesExist(ref pieces))
        {
            // Debug.Log("MovePiece: Duplicates exist!");
            return false;
        }

        var movingPiece = pieces.Find(piece => piece.X() == startX && piece.Y() == startY);
        if (movingPiece == null)
        {
            return false;
        }

        var targetPiece = pieces.Find(piece => piece.X() == targetX && piece.Y() == targetY);
        targetPiece?.RemoveSelf();

        movingPiece.SetXY(targetX, targetY);
        return true;
    }

    public static void MoveXY(ref List<IPieceData> pieces, int startX, int startY, int dcx, int dcy)
    {
        MoveOnePiece(ref pieces, GetPieceAt(pieces, startX, startY), dcx, dcy);
    }

    public static bool MoveOnePiece(ref List<IPieceData> pieces, IPieceData pieceToMove, int dcx, int dcy)
    {
        if (CheckValidMove(ref pieces, pieceToMove, dcx, dcy))
        {
            if (!MovePiece(ref pieces, pieceToMove.X(), pieceToMove.Y(), pieceToMove.X() + dcx, pieceToMove.Y() + dcy))
            {
                Debug.Log($"Fail M1P: {pieceToMove.X()},{pieceToMove.Y()} to {pieceToMove.X() + dcx},{pieceToMove.Y() + dcy}");
            }

            return true;
        }

        Debug.Log($"Invalid M1P: {pieceToMove.PType()} ({pieceToMove.X()},{pieceToMove.Y()}) to ({pieceToMove.X() + dcx},{pieceToMove.Y() + dcy})\n{PieceListToString(pieces)}");
        return false;
    }

    public static IPieceData GetPieceAt(List<IPieceData> pieces, int x, int y)
    {
        return pieces.Find(piece => piece.X() == x && piece.Y() == y);
    }

    public static PieceColor OtherColor(PieceColor color)
    {
        return color == PieceColor.WHITE ? PieceColor.BLACK : PieceColor.WHITE;
    }
    
    private static bool AnyPieceAt(ref List<IPieceData> pieces, int x, int y)
    {
        return pieces.Any(piece => piece.X() == x && piece.Y() == y);
    }

    private static bool RulePawn(ref List<IPieceData> pieces, IPieceData pieceToMove, int dcx, int dcy)
    {
        var validYDir = pieceToMove.Color() == PieceColor.WHITE ? 1 : -1;
        if (0 == dcx)
        {
            if ((dcy == validYDir || 
                (dcy == 2 * validYDir && pieceToMove.Y() == (pieceToMove.Color() == PieceColor.WHITE ? 2 : 7))) &&
                !AnyPieceAt(ref pieces, pieceToMove.X(), pieceToMove.Y() + dcy))
                return true;
        }

        if (1 != Mathf.Abs(dcx) || dcy != validYDir) return false;
        var targetPiece = GetPieceAt(pieces, pieceToMove.X() + dcx, pieceToMove.Y() + dcy);
        return null != targetPiece && targetPiece.Color() != pieceToMove.Color();
    }

    private static bool RuleKing(ref List<IPieceData> pieces, IPieceData pieceToMove, int dcx, int dcy)
    {
        if (Mathf.Abs(dcx) > 1 || Mathf.Abs(dcy) > 1) return false;
        var targetPiece = GetPieceAt(pieces, pieceToMove.X() + dcx, pieceToMove.Y() + dcy);
        return !AnyPieceAt(ref pieces, pieceToMove.X() + dcx, pieceToMove.Y() + dcy) ||
               targetPiece.Color() != pieceToMove.Color();
    }

    private static bool RuleQueen(ref List<IPieceData> pieces, IPieceData pieceToMove, int dcx, int dcy)
    {
        return RuleRook(ref pieces, pieceToMove, dcx, dcy) || RuleBishop(ref pieces, pieceToMove, dcx, dcy);
    }

    private static bool RuleRook(ref List<IPieceData> pieces, IPieceData pieceToMove, int dcx, int dcy)
    {
        // only move in the x or y direction
        if (dcx * dcy != 0 || (dcx == 0 && dcy == 0)) return false;
        
        // cannot be anyone on the way there
        var xDir = dcx != 0 ? dcx / Mathf.Abs(dcx) : 0;
        var yDir = dcy != 0 ? dcy / Mathf.Abs(dcy) : 0;
    
        var startX = pieceToMove.X() + xDir;
        var startY = pieceToMove.Y() + yDir;
        for (int x  = startX,  y  = startY; 
                 x != pieceToMove.X() + dcx || y != pieceToMove.Y() + dcy; 
                 x += xDir,                   y += yDir)
        {
            if (AnyPieceAt(ref pieces,x,y))
                return false;
        }
        
        // if the way was clean, check the target
        var targetPiece = GetPieceAt(pieces,pieceToMove.X() + dcx, pieceToMove.Y() + dcy);
        return null == targetPiece || targetPiece.Color() != pieceToMove.Color();
    }
    

    private static bool RuleBishop(ref List<IPieceData> pieces, IPieceData pieceToMove, int dcx, int dcy)
    {
        // only move diagonal
        if (dcx == 0 || dcy == 0 || Mathf.Abs(dcx) != Mathf.Abs(dcy)) return false;

        // cannot be anyone on the way there
        var yDir = dcy / Mathf.Abs(dcy);
        var xDir = dcx / Mathf.Abs(dcx);
        for (int x = pieceToMove.X() + xDir, y = pieceToMove.Y() + yDir; x != pieceToMove.X() + dcx; x += xDir, y += yDir)
        {
            if (AnyPieceAt(ref pieces, x, y))
                return false;
        }

        // if the way was clean, check the target
        var targetPiece = GetPieceAt(pieces, pieceToMove.X() + dcx, pieceToMove.Y() + dcy);
        return !AnyPieceAt(ref pieces, pieceToMove.X() + dcx, pieceToMove.Y() + dcy) ||
               targetPiece.Color() != pieceToMove.Color();
    }

    private static bool RuleKnight(ref List<IPieceData> pieces, IPieceData pieceToMove, int dcx, int dcy)
    {
        if ((Mathf.Abs(dcx) == 2 && Mathf.Abs(dcy) == 1) ||
            (Mathf.Abs(dcx) == 1 && Mathf.Abs(dcy) == 2))
        {
            var targetPiece = GetPieceAt(pieces, pieceToMove.X() + dcx, pieceToMove.Y() + dcy);
            if (!AnyPieceAt(ref pieces, pieceToMove.X() + dcx, pieceToMove.Y() + dcy) ||
                targetPiece.Color() != pieceToMove.Color())
                return true;
        }

        return false;
    }


    private static bool CheckPieceRules(ref List<IPieceData> pieces, IPieceData pieceToMove, int dcx, int dcy)
    {
        if (!ValidXY(pieceToMove.X() + dcx, pieceToMove.Y() + dcy)) return false;
        return pieceToMove.PType() switch
        {
            PieceType.PAWN => RulePawn(ref pieces, pieceToMove, dcx, dcy),
            PieceType.KING => RuleKing(ref pieces, pieceToMove, dcx, dcy),
            PieceType.QUEEN => RuleQueen(ref pieces, pieceToMove, dcx, dcy),
            PieceType.ROOK => RuleRook(ref pieces, pieceToMove, dcx, dcy),
            PieceType.BISHOP => RuleBishop(ref pieces, pieceToMove, dcx, dcy),
            PieceType.KNIGHT => RuleKnight(ref pieces, pieceToMove, dcx, dcy),
            _ => false
        };
    }


    public static bool CheckValidMove(ref List<IPieceData> pieces, IPieceData pieceToMove, int dcx, int dcy)
    {
        if (null == pieceToMove ||
            !ValidXY(pieceToMove.X(), pieceToMove.Y()) ||
            !ValidXY(pieceToMove.X() + dcx, pieceToMove.Y() + dcy))
            return false;
        return CheckPieceRules(ref pieces, pieceToMove, dcx, dcy);
    }

    public static IEnumerable<PieceMove> GetValidMoves(ref List<IPieceData> pieces, IPieceData pieceToMove)
    {
        var validMoves = new List<PieceMove>();
        for (var dx = -7; dx <= 7; dx++)
        {
            for (var dy = -7; dy <= 7; dy++)
            {
                if ((dx != 0 || dy != 0) &&
                    ValidXY(dx + pieceToMove.X(), dy + pieceToMove.Y()) &&
                    CheckValidMove(ref pieces, pieceToMove, dx, dy))
                {
                    validMoves.Add(new PieceMove(pieceToMove, dx, dy));
                }
            }
        }

        return validMoves;
    }

    public static List<PieceMove> GetValidMovesByTurn(ref List<IPieceData> pieces, PieceColor turn)
    {
        var validMoves = new List<PieceMove>();
        var turnPieces = pieces.Where(piece => piece.Color() == turn && piece.Alive());
        foreach (var piece in turnPieces)
        {
            validMoves.AddRange(GetValidMoves(ref pieces, piece));
        }
        validMoves.Shuffle();
        return validMoves;
    }
    
}