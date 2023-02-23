using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public static class ChessInfo
{

    public static void Shuffle<T>(this IList<T> list)  
    {  
        var n = list.Count;  
        while (n-- > 1) {
            var k = Random.Range(0,n + 1);  
            (list[k], list[n]) = (list[n], list[k]);
        }  
    }
    
    public enum PieceType
    {
        NONE, PAWN, KNIGHT, BISHOP, ROOK, QUEEN, KING
    }
    public enum PieceState
    {
        NONE, ALIVE, DEAD
    }

    public enum PieceColor
    {
        NONE, BLACK, WHITE
    }

    public class PieceMove
    {
        public readonly int x;
        public readonly int y;
        public readonly IPieceData piece;
        public int score;
        public List<IPieceData> simBoard = null;

        public PieceMove(IPieceData curPiece, int cx, int cy)
        {
            piece = curPiece;
            x = cx;
            y = cy;
        }

        public PieceMove()
        {
            x = y = 0;
            score = 0;
        }

        public bool NotZero()
        {
            return 0 != x || 0 != y;
        }
    }
    
   public static string PieceListToString(List<IPieceData> pieces)
   {
       var output = "";
       for (var y = 8; y >= 1; y--)
       {
           for (var x = 1; x <= 8; x++)
           {
               var piece = ChessRules.GetPieceAt(pieces, x, y);
               if (null == piece) output += ".";
               else
               {
                   var t = piece.PType() switch
                   {
                       PieceType.PAWN => "p",
                       PieceType.KING => "k",
                       PieceType.QUEEN => "q",
                       PieceType.ROOK => "r",
                       PieceType.BISHOP => "b",
                       PieceType.KNIGHT => "n",
                       _ => "."
                   };
                   if (piece.Color() == PieceColor.WHITE) t = t.ToUpper();
                   output += t;
               }
           }
           output += "\n";
       }

       return output;
   }
   
    public static List<IPieceData> StringToPieceInfoList(string input)
    {
         var pieces = new List<IPieceData>();
         var lines = input.Split('\n');
         if (lines.Length != 8)
         {
             Debug.Log($"Invalid input string [len={lines.Length}]:" + input);
             return pieces;
         }
         for (var y = 8; y >= 1; y--)
         {
              var line = lines[8 - y];
              for (var x = 1; x <= 8; x++)
              {
                var c = line[x - 1];
                
                var p = c switch
                {
                     'p' => new PieceInfo(PieceType.PAWN, PieceColor.BLACK, x, y),
                     'P' => new PieceInfo(PieceType.PAWN, PieceColor.WHITE, x, y),
                     'k' => new PieceInfo(PieceType.KING, PieceColor.BLACK, x, y),
                     'K' => new PieceInfo(PieceType.KING, PieceColor.WHITE, x, y),
                     'q' => new PieceInfo(PieceType.QUEEN, PieceColor.BLACK, x, y),
                     'Q' => new PieceInfo(PieceType.QUEEN, PieceColor.WHITE, x, y),
                     'r' => new PieceInfo(PieceType.ROOK, PieceColor.BLACK, x, y),
                     'R' => new PieceInfo(PieceType.ROOK, PieceColor.WHITE, x, y),
                     'b' => new PieceInfo(PieceType.BISHOP, PieceColor.BLACK, x, y),
                     'B' => new PieceInfo(PieceType.BISHOP, PieceColor.WHITE, x, y),
                     'n' => new PieceInfo(PieceType.KNIGHT, PieceColor.BLACK, x, y),
                     'N' => new PieceInfo(PieceType.KNIGHT, PieceColor.WHITE, x, y),
                     _ => null
                };
                if (null != p) pieces.Add(p);
              }
         }
         return pieces;
    }
    
    private static string _defaultBoard = "rnbqkbnr\npppppppp\n........\n........\n........\n........\nPPPPPPPP\nRNBQKBNR";
    public static List<IPieceData> DefaultBoard()
    {
        return StringToPieceInfoList(_defaultBoard);
    }

}
