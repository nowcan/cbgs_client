using System.Configuration;
using System.Diagnostics;
using System;
using System.Collections;
using System.Data;
using System.IO;

namespace Reversi
{
    public class TBopb
    {

        protected static internal int Outside = -1;
        protected internal const byte Empty = 0;
        protected internal const byte PBlack = 1;
        protected internal const byte PWhite = 2;
        protected internal const short A1 = 10;
        protected internal const short D4 = 40;
        protected internal const short E4 = 41;
        protected internal const short D5 = 49;
        protected internal const short E5 = 50;
        protected internal const short H8 = 80;
        protected static internal int[] TurnVect = { 104, 104, 80, 80, 80, 80, 108, 108, 0, 104, 
		104, 80, 80, 80, 80, 108, 108, 0, 86, 86, 
		71, 71, 71, 71, 92, 92, 0, 86, 86, 71, 
		71, 71, 71, 92, 92, 0, 86, 86, 71, 71, 
		71, 71, 92, 92, 0, 86, 86, 71, 71, 71, 
		71, 92, 92, 0, 112, 112, 98, 98, 98, 98, 
		116, 116, 0, 112, 112, 98, 98, 98, 98, 116, 
		116, -10, -9, -8, -1, 1, 8, 9, 10, 0, 
		-1, 1, 8, 9, 10, 0, -9, -8, 1, 9, 
		10, 0, -10, -9, -1, 8, 9, 0, -10, -9, 
		-8, -1, 1, 0, 1, 9, 10, 0, -1, 8, 
		9, 0, -9, -8, 1, 0, -10, -9, -1, 0
		 };
        protected internal int[] Board;
        protected internal int[] EmptyList;
        protected internal int[] TStack;
        protected internal int BWTotal;
        protected internal int BWDiff;
        protected internal int NTurn;
        protected internal int Tsp;

        protected internal TBopb(int side, string bd = null)
        {
            Board = new int[92];
            EmptyList = new int[92];
            TStack = new int[500];
            if (bd != null)
            {
                int pos, idx = 0;
                int spaces = 0;
                BWTotal = 8 * 8;
                BWDiff = 0;
                Tsp = 0;
                NTurn = 1;
                for (int x = 0; x < 92; x++)
                    Board[x] = -1;

                if (side == 1)
                {
                    for (int i = 1; i <= 8; i++)
                    {
                        for (int j = 1; j <= 8; j++)
                        {
                            pos = i * 9 + j;
                            switch (bd[idx])
                            {
                                case 'b': Board[pos] = 1; BWDiff++; break;
                                case 'w': Board[pos] = 2; BWDiff--; break;
                                case '.': Board[pos] = 0; spaces++; break;
                            }

                            idx++;
                        }
                    }
                }
                else
                {
                    for (int i = 1; i <= 8; i++)
                    {
                        for (int j = 1; j <= 8; j++)
                        {
                            pos = i * 9 + j;
                            switch (bd[idx])
                            {
                                case 'b': Board[pos] = 2; BWDiff--; break;
                                case 'w': Board[pos] = 1; BWDiff++; break;
                                case '.': Board[pos] = 0; spaces++; break;
                            }

                            idx++;
                        }
                    }
                }

                BWTotal -= spaces;
            }
            else
            {
                ClearBoard();
            }
        }

        protected internal object Clone()
        {
            TBopb cl = new TBopb(NTurn);
            cl.BWTotal = BWTotal;
            cl.BWDiff = BWDiff;
            cl.NTurn = NTurn;
            cl.Tsp = Tsp;
            System.Array.Copy(Board, 0, cl.Board, 0, Board.Length);
            System.Array.Copy(EmptyList, 0, cl.EmptyList, 0, EmptyList.Length);
            System.Array.Copy(TStack, 0, cl.TStack, 0, TStack.Length);
            return cl;
        }

        protected internal void ClearBoard()
        {
            for (int x = 0; x < 92; x++)
                Board[x] = -1;

            for (int y = 1; y <= 8; y++)
            {
                for (int x = 1; x <= 8; x++)
                    Board[y * 9 + x] = 0;

            }

            Board[41] = Board[49] = 1;
            Board[40] = Board[50] = 2;
            BWTotal = 4;
            BWDiff = 0;
            NTurn = 1;
            Tsp = 0;
        }

        protected internal bool CompareBoard(int[] bd)
        {
            for (int i = 10; i <= 80; i++)
                if (Board[i] != bd[i]) return false;

            return true;
        }

        protected internal int MPerform(int Mv)
        {
            int c1 = (int)(3 - NTurn);
            int TurnCnt = 0;
            if (Mv != 0)
            {
                int Tvp = TurnVect[Mv - 10];
                int vect = TurnVect[Tvp];
                do
                {
                    int m = Mv + vect;
                    if (Board[m] == c1)
                    {
                        int i = 0;
                        do
                        {
                            i++;
                            m += vect;
                        }
                        while (Board[m] == c1);
                        if (Board[m] == NTurn)
                        {
                            TurnCnt += i;
                            do
                            {
                                m -= vect;
                                Board[m] = (byte)NTurn;
                                TStack[Tsp++] = (byte)m;
                            }
                            while (--i != 0);
                        }
                    }
                }
                while ((vect = TurnVect[++Tvp]) != 0);
                if (TurnCnt == 0) return TurnCnt;
                Board[Mv] = (byte)NTurn;
                TStack[Tsp++] = (byte)Mv;
                BWDiff += TurnCnt + TurnCnt + 1;
                BWTotal++;
            }
            TStack[Tsp++] = (byte)TurnCnt;
            BWDiff = -BWDiff;
            NTurn = c1;
            return TurnCnt;
        }

        protected internal void MTakeBack()
        {
            if (Tsp > 0)
            {
                BWDiff = -BWDiff;
                int TurnCnt = TStack[--Tsp];
                if (TurnCnt != 0)
                {
                    BWDiff -= TurnCnt + TurnCnt + 1;
                    BWTotal--;
                    Board[TStack[--Tsp]] = 0;
                    do
                        Board[TStack[--Tsp]] = (byte)NTurn;
                    while (--TurnCnt != 0);
                }
                NTurn = 3 - NTurn;
            }
        }

        protected internal int MExamine(int Mv)
        {
            int TurnCnt = 0;
            int c1 = (int)(3 - NTurn);
            int Tvp = TurnVect[Mv - 10];
            int vect = TurnVect[Tvp];
            do
            {
                int p = Mv + vect;
                if (Board[p] == c1)
                {
                    int i = 0;
                    do
                    {
                        i++;
                        p += vect;
                    }
                    while (Board[p] == c1);
                    if (Board[p] > 0) TurnCnt += i;
                }
            }
            while ((vect = TurnVect[++Tvp]) != 0);
            return TurnCnt;
        }

        protected internal bool IsGameOver()
        {
            return !HasMove() && !OppHasMove();
        }

        protected internal bool OppHasMove()
        {
            bool ret = false;
            NTurn = 3 - NTurn;
            for (int y = 1; y <= 8; y++)
            {
                for (int x = 1; x <= 8; x++)
                {
                    if (Board[y * 9 + x] == 0)
                    {
                        if (MExamine(y * 9 + x) > 0)
                        {
                            ret = true;
                        }
                    }
                }
            }

            NTurn = 3 - NTurn;
            return ret;
        }

        protected internal bool HasMove()
        {
            for (int y = 1; y <= 8; y++)
            {
                for (int x = 1; x <= 8; x++)
                {
                    if (Board[y * 9 + x] == 0)
                    {
                        if (MExamine(y * 9 + x) > 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }

    public class CReversi
    {
        protected internal const int STEN = 300;
        protected internal const int STEN2 = 200;
        protected internal const int CTEN1 = 400;
        protected internal const int CTEN2 = 300;
        protected internal const int CTEN3 = 200;
        protected static internal int CTEN4 = -50;
        protected internal const int PERF = 3000;
        protected static internal int Vect1 = -10;
        protected static internal int Vect2 = -9;
        protected static internal int Vect3 = -8;
        protected static internal int Vect4 = -1;
        protected internal const int Vect5 = 1;
        protected internal const int Vect6 = 8;
        protected internal const int Vect7 = 9;
        protected internal const int Vect8 = 10;
        protected static internal int[] ScanVect = { 9, 4, 10, 17, 0, 1, 1, 10, 19, 28, 
		37, 46, 55, 64, 73, 0, 9, 4, 11, 12, 
		13, 14, 15, 16, 0, 10, 2, 15, 14, 13, 
		12, 11, 10, 19, 28, 37, 46, 55, 0, 8, 
		8, 12, 13, 14, 15, 16, 17, 26, 35, 44, 
		53, 62, 0, 0 };
        protected static internal int[] PStart = { 10, 10, 17, 17, 80, 80, 73, 73 };
        protected static internal int[] VectF = { 1, 9, 9, -1, -1, -9, -9, 1 };
        protected static internal int[] VectI = { 9, 1, -1, 9, -9, -1, 1, -9 };
        protected static internal int[] BitF = { 1, 4, 4, 1, 1, 4, 4, 1 };
        protected static internal int[] BitFI = { 2, 2, 8, 8, 2, 2, 8, 8 };
        protected static internal int[] BitI = { 4, 1, 1, 4, 4, 1, 1, 4 };
        protected static internal int[] BitBI = { 8, 8, 2, 2, 8, 8, 2, 2 };
        protected static internal int[] PTbl1 = { 0, 75, 75, 105, 105, 75, 75, 0, 0, 75, 
		96, 45, 108, 108, 45, 96, 75, 0, 75, 45, 
		45, 54, 54, 45, 45, 75, 0, 105, 108, 54, 
		39, 39, 54, 108, 105, 0, 105, 108, 54, 39, 
		39, 54, 108, 105, 0, 75, 45, 45, 54, 54, 
		45, 45, 75, 0, 75, 96, 45, 108, 108, 45, 
		96, 75, 0, 0, 75, 75, 105, 105, 75, 75, 
		0, 0 };
        protected static internal int[] PTbl2 = { 0, 0, 0, 0, 0, 69, 0, 0, 0, 1, 
		57, 0, 0, 1, 0, 93, 0, 0, 4, 3, 
		69, 0, 1, 0, 0, 96, 0, 3, 0, 3, 
		93, 0, 3, 4, 0, 120, 0, 5, 6, 5, 
		57, 1, 0, 0, 0, 93, 4, 0, 0, 3, 
		84, 4, 0, 4, 0, 105, 6, 0, 6, 5, 
		93, 4, 3, 0, 0, 120, 6, 5, 0, 5, 
		105, 6, 5, 6, 0, 123, 7, 6, 7, 6, 
		30, 0, 0, 0, 0, 81, 0, 0, 0, 3, 
		63, 0, 0, 3, 0, 102, 0, 0, 5, 4, 
		81, 0, 3, 0, 0, 108, 0, 4, 0, 4, 
		102, 0, 4, 5, 0, 120, 0, 6, 7, 6, 
		63, 3, 0, 0, 0, 102, 5, 0, 0, 4, 
		93, 5, 0, 5, 0, 108, 7, 0, 7, 6, 
		102, 5, 4, 0, 0, 120, 7, 6, 0, 6, 
		108, 7, 6, 7, 0, 123, 7, 6, 7, 6, 
		0, 0, 0, 0, 0, 69, 0, 0, 0, 6, 
		57, 0, 0, 1, 0, 93, 0, 0, 4, 3, 
		69, 0, 6, 0, 0, 96, 0, 3, 0, 3, 
		93, 0, 3, 4, 0, 120, 0, 5, 6, 5, 
		57, 1, 0, 0, 0, 93, 4, 0, 0, 3, 
		84, 4, 0, 4, 0, 105, 6, 0, 6, 5, 
		93, 4, 3, 0, 0, 120, 6, 5, 0, 5, 
		105, 6, 5, 6, 0, 123, 7, 6, 7, 6, 
		30, 0, 0, 0, 0, 81, 0, 0, 0, 3, 
		63, 0, 0, 3, 0, 102, 0, 0, 5, 4, 
		81, 0, 3, 0, 0, 108, 0, 4, 0, 4, 
		102, 0, 4, 5, 0, 120, 0, 6, 7, 6, 
		63, 3, 0, 0, 0, 102, 5, 0, 0, 4, 
		93, 5, 0, 5, 0, 108, 7, 0, 7, 6, 
		102, 5, 4, 0, 0, 120, 7, 6, 0, 6, 
		108, 7, 6, 7, 0, 123, 7, 6, 7, 6
		 };
        protected static internal int[] PTbl4 = { 0, 172, 60, 118, 94, 76, 60, 48, 38, 30, 
		24, 20, 16, 14, 12, 10 };
        protected static internal int[] kPosOrder = { 10, 17, 73, 80, 40, 41, 49, 50, 31, 32, 
		39, 48, 42, 51, 58, 59, 30, 33, 57, 60, 
		22, 23, 38, 47, 43, 52, 67, 68, 12, 15, 
		28, 55, 35, 62, 75, 78, 13, 14, 37, 46, 
		44, 53, 76, 77, 21, 24, 29, 56, 34, 61, 
		66, 69, 11, 16, 19, 64, 26, 71, 74, 79, 
		20, 25, 65, 70 };
        private int[] Board1;
        private int[] Board2;
        private int[] Board3;
        private int[] Board4;
        protected static internal byte[] OLData = null;
        protected internal int Mv1;
        private int CurDepth;
        private int SearchDepth;
        private bool EndGame;
        protected static internal int PT4Ofs;
        private int Ev1;
        private int Ev2;
        private int Ev3;
        private int Ev4;
        private int Ev5;
        private int Ev6;
        private int tf;

        protected internal CReversi()
        {
            Board1 = new int[92];
            Board2 = new int[92];
            Board3 = new int[92];
            Board4 = new int[92];
            //if (OLData == null) try {
            //    OLData = new byte[22000];
            //    Uri u = new System.Uri("/Reversi;component/data/koji256.bin", UriKind.RelativeOrAbsolute);
            //    Stream _is = Application.GetResourceStream(u).Stream;
            //    int i = 0;
            //    int j;
            //    do {
            //        j = _is.Read(OLData, i, 22000 - i);
            //        i += j;
            //    }
            //    while (j > 0);
            //}
            //catch (System.IO.IOException e) {
            //    OLData = null;
            //}
        }

        private static int EvCurve(int v)
        {
            if (v <= 200) return v << 2;
            if (v <= 500) return v + v + 400; else return v + 900;
        }

        private void Pass1(TBopb pb)
        {
            int bcnt = pb.BWTotal + pb.BWDiff >> 1;
            int wcnt = bcnt - pb.BWDiff;
            int vp = 0;
            int p2 = 0;
            int vect = ScanVect[vp++];
            do
            {
                int k = ScanVect[vp++];
                do
                {
                    int p = ScanVect[vp++];
                    int nest = 1;
                    int spcs1 = 0;
                    if (pb.Board[p] == 0)
                    {
                        nest = 0;
                        spcs1 = 1;
                        if (pb.Board[p + vect] == 0) spcs1 = 2;
                    }
                    do
                    {
                        int p1 = p;
                        int cnt = 0;
                        int b1 = pb.Board[p];
                        do
                        {
                            cnt++;
                            p += vect;
                        }
                        while (pb.Board[p] == b1);
                        if (pb.Board[p] < 0)
                        {
                            if (nest == 1 && spcs1 > 0 && cnt > 1)
                            {
                                p -= vect;
                                if (Board1[p] < 0)
                                {
                                    Board1[p1] |= 16;
                                    do
                                    {
                                        Board1[p1] |= 128;
                                        p1 += vect;
                                    }
                                    while (p1 != p);
                                }
                            }
                            break;
                        }
                        if (pb.Board[p] == 0)
                        {
                            int spcs2 = spcs1;
                            spcs1 = 1;
                            if (pb.Board[p + vect] == 0)
                            {
                                if (nest > 2)
                                {
                                    spcs1 = 0;
                                    do
                                    {
                                        Board1[p2] |= 128;
                                        spcs1++;
                                        p2 += vect;
                                    }
                                    while (p2 != p1);
                                    if (pb.Board[p1] == 2 && spcs1 == bcnt) Board2[p] = 50;
                                }
                                spcs1 = 2;
                            }
                            if (nest > 1)
                            {
                                nest = 0;
                                if ((Board1[p2] & k) != 0)
                                {
                                    Board1[p2] |= 32;
                                    Board1[p - vect] |= 32;
                                }
                                Board1[p - vect] |= k;
                                if (pb.Board[p1] == 2 && cnt == wcnt) Ev2 = 3000;
                                b1 = -64;
                            }
                            else
                            {
                                nest = 0;
                                if (spcs2 > 0)
                                {
                                    Board1[p1] |= 16;
                                    Board1[p - vect] |= 16;
                                    b1 = -128;
                                }
                                else
                                {
                                    if (Board1[p1] >= 0 || cnt <= 1) continue;
                                    p1 += vect;
                                    Board1[p - vect] |= 16;
                                    b1 = -128;
                                }
                            }
                        }
                        else
                        {
                            nest++;
                            if (nest == 3 && spcs1 > 1)
                            {
                                if (pb.Board[p2] == 2 && cnt == bcnt) Board2[p2 - vect] = 50;
                                p2 = p1;
                                b1 = -128;
                            }
                            else
                            {
                                p2 = p1;
                                if (nest != 2 || spcs1 <= 0) continue;
                                Board1[p1] |= k;
                                if (pb.Board[p1] == 2 && cnt == wcnt) Ev2 = 3000;
                                b1 = -64;
                            }
                        }
                        do
                        {
                            Board1[p1] |= b1;
                            p1 += vect;
                        }
                        while (p1 != p);
                    }
                    while (true);
                }
                while (ScanVect[vp] != 0);
                vp++;
                vect = ScanVect[vp++];
            }
            while (vect != 0);
        }

        private void Pass2(TBopb pb)
        {
            int c;
            for (int i = 10; i <= 80; i++)
                switch (pb.Board[i])
                {
                    default:
                        break;
                    case 1:

                        // '\001'
                        c = Board1[i];
                        if (c >= 0)
                        {
                            Ev1++;
                        }
                        else
                        {
                            c &= 63;
                            if (c != 0)
                            {
                                int p2 = c * 5;
                                Ev5 += PTbl1[i - 10] + PTbl2[p2++];
                                c = PTbl2[p2++];
                                Board3[i + -8] += c;
                                Board3[i + 8] += c;
                                c = PTbl2[p2++];
                                Board3[i + -9] += c;
                                Board3[i + 9] += c;
                                c = PTbl2[p2++];
                                Board3[i + -10] += c;
                                Board3[i + 10] += c;
                                c = PTbl2[p2];
                                Board3[i + -1] += c;
                                Board3[i + 1] += c;
                            }
                        }

                        break;
                    case 2:

                        // '\002'
                        c = Board1[i];
                        if (c >= 0)
                        {
                            Ev1--;
                            break;
                        }

                        c &= 31;
                        if (c != 0)
                        {
                            int p2 = c * 5;
                            Ev6 += PTbl1[i - 10] + PTbl2[p2++];
                            c = PTbl2[p2++];
                            Board2[i + -8] += c;
                            Board2[i + 8] += c;
                            c = PTbl2[p2++];
                            Board2[i + -9] += c;
                            Board2[i + 9] += c;
                            c = PTbl2[p2++];
                            Board2[i + -10] += c;
                            Board2[i + 10] += c;
                            c = PTbl2[p2];
                            Board2[i + -1] += c;
                            Board2[i + 1] += c;
                        }

                        break;
                }

        }

        private int Pscan1s(TBopb pb, int p, int vect, int hyo)
        {
            for (; pb.Board[p] == 0 || pb.Board[p] > 0 && Board1[p] < 0; p += vect)
                hyo += 30;

            return hyo;
        }

        private int Pscan3s(TBopb pb, int p, int vect, int hh)
        {
            int h = hh -= 50;
            do
            {
                int n;
                do
                {
                    do
                    {
                        hh -= 50;
                        if (pb.Board[p] == 2) h = hh;
                        if (Board1[p] >= 0) return h;
                        p += vect;
                    }
                    while (pb.Board[p] > 0);
                    n = Board2[p];
                    if (hh == -290 && n == 0 && Board3[p] == 0) return hh - 200;
                    p += vect;
                    if (pb.Board[p] != 1 || (Board1[p] & 32) != 0) return Pscan1s(pb, p, vect, h);
                    hh -= 50;
                }
                while (n != 0);
                h = hh;
            }
            while (true);
        }

        private int Pscan1(TBopb pb, int p, int vect)
        {
            int hyo = 0;
            while (pb.Board[p += vect] == 1)
                ;
            if (pb.Board[p] == 2)
            {
                if ((Board1[p] & 64) == 0) return Pscan3s(pb, p + vect, vect, 0);
                do
                    hyo += 100;
                while (pb.Board[p += vect] == 2);
                p += vect;
            }
            if (pb.Board[p] < 0) return 0;
            if (pb.Board[p] == 0 && Board3[p] != 0 && (Board2[p] == 0 || hyo != 0))
            {
                p += vect;
                if (pb.Board[p] == 1 && (Board1[p] & 32) == 0) return Pscan3s(pb, p, vect, hyo);
            }
            else
            {
                p += vect;
            }
            return Pscan1s(pb, p, vect, hyo);
        }

        private int Pscan4s(TBopb pb, int p, int vect, int hh)
        {
            int h = hh -= 50;
            do
            {
                int n;
                do
                {
                    do
                    {
                        hh -= 50;
                        if (pb.Board[p] == 1) h = hh;
                        if (Board1[p] >= 0) return h;
                        p += vect;
                    }
                    while (pb.Board[p] > 0);
                    n = Board3[p];
                    if (hh == -290 && n == 0 && Board2[p] == 0) return hh - 200;
                    p += vect;
                    if (pb.Board[p] != 2 || (Board1[p] & 32) != 0) return Pscan1s(pb, p, vect, h);
                    hh -= 50;
                }
                while (n != 0);
                h = hh;
            }
            while (true);
        }

        private int Pscan2(TBopb pb, int p, int vect)
        {
            int hh;
            int hyo = hh = 0;
            while (pb.Board[p += vect] == 2)
                ;
            if (pb.Board[p] < 0) return hyo;
            if (pb.Board[p] != 0)
            {
                if ((Board1[p] & 64) == 0) return Pscan4s(pb, p + vect, vect, 0);
                do
                    hh += 50;
                while (pb.Board[p += vect] == 1);
                switch (pb.Board[p + vect])
                {
                    case 0:
                        // '\0'
                        hyo = hh + hh;
                        hh = 0;
                        p += vect;
                        break;
                    case -1:

                        return 0;
                }
            }
            int n = Board2[p];
            p += vect;
            if (n != 0) switch (pb.Board[p])
                {
                    default:
                        break;
                    case 1:

                        // '\001'
                        if (Board1[p] >= -64) return Pscan4s(pb, p, vect, hyo - hh);
                        break;
                    case 2:

                        // '\002'
                        if ((Board1[p] & 32) == 0) return Pscan4s(pb, p, vect, hyo - hh);
                        break;
                }
            return Pscan1s(pb, p, vect, hyo + hh + hh);
        }

        private int Pscan3(TBopb pb, int p, int psv)
        {
            int hyo = 0;
            int vect = VectF[psv];
            p += vect;
            if (pb.Board[p] == 2 && (Board1[p] & 64) != 0) do
                    hyo += 100;
                while (pb.Board[p += vect] == 2);

            if (pb.Board[p] == 1)
            {
                if (Board1[p] >= 0) return hyo;
                do
                    hyo += 100;
                while (pb.Board[p += vect] == 1);
            }
            int hh = 0;
            if (pb.Board[p] == 2)
            {
                if (Board1[p] >= 0) return hyo;
                do
                    hh += 50;
                while (pb.Board[p += vect] == 2);
            }
            int n = Board3[p];
            if (hyo + hh == 0 && pb.Board[p + VectI[psv] * 2] == 2) n = 1;
            p += vect;
            if (n != 0) switch (pb.Board[p])
                {
                    case 0:
                    default:
                        // '\0'
                        break;
                    case 1:

                        // '\001'
                        if ((Board1[p] & 32) == 0) return Pscan3s(pb, p, vect, (hyo - hh) + 10);
                        break;
                    case 2:

                        // '\002'
                        if (Board1[p] >= -64) return Pscan3s(pb, p, vect, (hyo - hh) + 10);
                        break;
                    case -1:

                        return hyo;
                }
            return Pscan1s(pb, p, vect, hyo + hh + hh);
        }

        private int Pscan4(TBopb pb, int p, int psv)
        {
            int hyo = 0;
            int vect = VectF[psv];
            p += vect;
            if (pb.Board[p] == 1 && (Board1[p] & 64) != 0)
            {
                do
                    hyo += 100;
                while (pb.Board[p += vect] == 1);
                if (Board1[p] >= 0)
                {
                    tf = 1;
                    return hyo;
                }
                do
                    hyo += 100;
                while (pb.Board[p += vect] == 2);
                if (pb.Board[p] == 1)
                {
                    tf = 1;
                }
                else
                {
                    int p1;
                    if (hyo == 500)
                    {
                        p1 = p + VectI[psv];
                        if (pb.Board[p1] == 2 && (Board1[p1] & BitI[psv]) != 0) tf = 1;
                    }
                    else if (hyo == 400)
                    {
                        p1 = p + vect + VectI[psv];
                        if (pb.Board[p1] == 2 && (Board1[p1] & BitFI[psv]) != 0) tf = 1;
                    }
                    p1 = p + vect;
                    switch (pb.Board[p1])
                    {
                        case 1:
                            // '\001'
                            if ((Board1[p1] & 64) != 0) tf = 1;
                            break;
                        case 2:

                            // '\002'
                            if ((Board1[p1] & 64) == 0) tf = 1;
                            break;
                    }
                }
            }
            else if (pb.Board[p] == 2)
            {
                if (Board1[p] >= 0) return hyo;
                do
                    hyo += 100;
                while (pb.Board[p += vect] == 2);
            }
            int hh = 0;
            if (pb.Board[p] == 1)
            {
                if (Board1[p] >= 0) return hyo;
                do
                    hh += 50;
                while (pb.Board[p += vect] == 1);
            }
            int n = Board2[p];
            if (hyo + hh == 0 && pb.Board[p + VectI[psv] * 2] == 1) n = 1;
            p += vect;
            if (n != 0) switch (pb.Board[p])
                {
                    case 0:
                    default:
                        // '\0'
                        break;
                    case 1:

                        // '\001'
                        if (Board1[p] >= -64) return Pscan4s(pb, p, vect, (hyo - hh) + 10);
                        break;
                    case 2:

                        // '\002'
                        if ((Board1[p] & 32) == 0) return Pscan4s(pb, p, vect, (hyo - hh) + 10);
                        break;
                    case -1:

                        return hyo;
                }
            return Pscan1s(pb, p, vect, hyo + hh + hh);
        }

        private bool Pscan5s(TBopb pb, int p, int psv, int n)
        {
            int b;
            if (n == 4)
            {
                if (pb.Board[p] != 0 || Board2[p] != 0) return false;
                p = (p - VectF[psv]) + VectI[psv];
                b = BitI[psv];
            }
            else if (n == 3)
            {
                p += VectI[psv];
                b = BitFI[psv];
            }
            else
            {
                return false;
            }
            return pb.Board[p] == 2 && (Board1[p] & b) != 0;
        }

        private int Pscan5(TBopb pb, int p, int psv)
        {
            int t = Board1[p];
            if (t >= 0) return 0;
            int vect = VectF[psv];
            int n = 0;
            do
                n++;
            while (pb.Board[p += vect] > 0);
            int p1 = p;
            if ((t & 64) != 0)
            {
                p1 += vect;
                if (pb.Board[p1] > 0)
                {
                    n++;
                    do
                        n++;
                    while (pb.Board[p1 += vect] > 0);
                    p = p1;
                }
            }
            if (n > 5) return -50;
            if (n == 5 && Board2[p] != 0)
            {
                p += VectI[psv];
                if (pb.Board[p] != 2 || (Board1[p] & BitI[psv]) == 0) return -50;
            }
            p = p1 + vect;
            switch (pb.Board[p])
            {
                case 1:
                    // '\001'
                    if (Board2[p1] != 0 || ((Board1[p] & 64) == 0 ? Board3[p1] == 0 : (Board1[p] & 32) != 0)) return 300;
                    break;
                case 2:

                    // '\002'
                    if (Board3[p1] == 0) return 200;
                    t = Board1[p];
                    if (t >= 0) break;
                    if (t < -64)
                    {
                        do
                            n++;
                        while (pb.Board[p += vect] == 2);
                        p += vect;
                        if (Pscan5s(pb, p, psv, n) || pb.Board[p] == 2) break;
                    }

                    return 300;
                default:

                    p += vect;
                    if (pb.Board[p] == 0 && n <= 2) return 400;
                    if (Board3[p1] == 0) return 200;
                    if (!Pscan5s(pb, p, psv, n) && (pb.Board[p] != 2 || Board2[p1] != 0 || (Board1[p] & 64) != 0)) return 300;
                    break;
            }
            return 200 + n * 100;
        }

        private bool Pscan6s(TBopb pb, int p, int psv, int n)
        {
            int b;
            if (n == 4)
            {
                if (pb.Board[p] != 0) return false;
                p = (p - VectF[psv]) + VectI[psv];
                b = BitI[psv];
            }
            else if (n == 3)
            {
                p += VectI[psv];
                b = BitFI[psv];
            }
            else
            {
                return false;
            }
            return pb.Board[p] == 1 && (Board1[p] & b) != 0;
        }

        private int Pscan6(TBopb pb, int p, int psv)
        {
            if (Board1[p] >= 0) return 0;
            int vect = VectF[psv];
            int n = 0;
            do
                n++;
            while (pb.Board[p += vect] == 2);
            if (n > 5) return -50;
            if (n == 5 && pb.Board[p + vect] <= 0 && Board3[0] != 0)
            {
                p += VectI[psv];
                if (pb.Board[p] <= 0)
                {
                    if (Board2[p] == 0) return -50;
                }
                else if (pb.Board[p] == 2 || (Board1[p] & BitI[psv]) == 0)
                    return -50;
                p -= VectI[psv];
            }
            int t = Board2[p];
            p += vect;
        label0:
            switch (pb.Board[p])
            {
                case 2:
                    // '\002'
                    if ((Board1[p] & 64) == 0 ? t == 0 : (Board1[p] & 32) != 0) return 300;
                    break;
                case 1:

                    // '\001'
                    if (t == 0) return 200;
                    if (Board1[p] >= -64) break;
                    do
                        n++;
                    while (pb.Board[p += vect] == 1);

                    p += vect;
                    if (!Pscan6s(pb, p, psv, n) && pb.Board[p] != 1) return 300;
                    break;
                default:

                    if (n <= 2 && pb.Board[p + vect] == 0) return 400;
                    if (t == 0) return 200;
                    p += vect;
                    if (Pscan6s(pb, p, psv, n)) break;
                    switch (pb.Board[p])
                    {
                        case 2:
                            // '\002'
                            if ((Board1[p] & 64) == 0) return 200;
                            goto label0;
                        case 1:

                            // '\001'
                            if ((Board1[p] & 64) != 0) return 300;
                            break;
                        default:

                            return 300;
                    }
                    break;
            }
            return tf = 200 + n * 100;
        }

        private void Pscan7(TBopb pb, int p, int psv)
        {
            int p1 = p + VectF[psv];
            int t = Board1[p1];
            int u = Board2[p];
            if (u != 0)
            {
                switch (pb.Board[p1])
                {
                    case 1:
                        // '\001'
                        if ((t & 64) != 0) u += 6;
                        break;
                    case 2:

                        // '\002'
                        if ((t & 32) == 0) u += 6;
                        break;
                    default:

                        u += 3;
                        break;
                }
                int p2 = p + VectI[psv];
                if (pb.Board[p2] == 2 && (Board1[p2] & BitI[psv]) != 0) u += 5;
                Board2[p] = u;
            }
            u = Board3[p];
            if (u != 0)
            {
                switch (pb.Board[p1])
                {
                    case 1:
                        // '\001'
                        if ((t & 32) == 0) u += 6;
                        break;
                    case 2:

                        // '\002'
                        if ((t & 64) != 0) u += 6;
                        break;
                    default:

                        u += 3;
                        break;
                }
                int p2 = p + VectI[psv];
                if (pb.Board[p2] == 1 && (Board1[p2] & BitI[psv]) != 0) u += 5;
                Board3[p] = u;
            }
        }

        private int Pscan(TBopb pb, int psv)
        {
            int p = PStart[psv];
            int vec1 = VectF[psv];
            int vec2 = VectI[psv];
            switch (pb.Board[p])
            {
                case 1:
                    // '\001'
                    return Pscan1(pb, p, vec1) + Pscan1(pb, p, vec2) + 300;
                case 2:

                    // '\002'
                    return -(Pscan2(pb, p, vec1) + Pscan2(pb, p, vec2) + 300);
            }
            int hh;
            if (Board2[p] != 0)
            {
                hh = Pscan3(pb, p, psv) + Pscan3(pb, p, psv + 1) + 100 + 300;
                if (hh < 200) hh = 200;
                return hh;
            }
            int p1 = p + vec1 + vec2;
            if (Board3[p] != 0)
            {
                tf = 0;
                if (pb.Board[p1] == 1) tf = 1;
                hh = Pscan4(pb, p, psv) + Pscan4(pb, p, psv + 1) + 100 + 300;
                if (hh < 200) hh = 200;
                if (tf != 0) return -hh;
            }
            hh = 0;
            switch (pb.Board[p1])
            {
                case 1:
                    // '\001'
                    hh = -200;
                    break;
                case 2:

                    // '\002'
                    for (; hh < 4; hh++)
                    {
                        p1 += vec1 + vec2;
                        if ((Board1[p1] & 64) != 0)
                        {
                            hh = Pscan3(pb, p, psv) + Pscan3(pb, p, psv + 1) + 100 + 300;
                            if (hh < 200) hh = 200;
                            return hh;
                        }
                    }


                    hh = 200;
                    break;
                default:

                    if (Board2[p1] != 0) Board2[p1] += 6;
                    if (Board3[p1] != 0) Board3[p1] += 6;
                    break;
            }
            tf = 0;
            switch (pb.Board[p += vec1])
            {
                case 1:
                    // '\001'
                    hh -= Pscan5(pb, p, psv);
                    break;
                case 2:

                    // '\002'
                    hh += Pscan6(pb, p, psv);
                    if (tf != 0) return tf + Pscan3(pb, p - vec1, psv + 1);
                    break;
                default:

                    Pscan7(pb, p, psv);
                    break;
            }
            p -= vec1;
            switch (pb.Board[p += vec2])
            {
                case 1:
                    // '\001'
                    hh -= Pscan5(pb, p, psv + 1);
                    break;
                case 2:

                    // '\002'
                    hh += Pscan6(pb, p, psv + 1);
                    if (tf != 0) return tf + Pscan3(pb, p - vec2, psv);
                    break;
                default:

                    Pscan7(pb, p, psv + 1);
                    break;
            }
            return hh;
        }

        private void Pass4(TBopb pb)
        {
            int e4;
            int e3 = e4 = 0;
            for (int i = 10; i <= 80; i++)
            {
                if (pb.Board[i] != 0) continue;
                int c = Board3[i];
                int t = Board2[i];
                if (t != 0)
                {
                    if (c != 0)
                    {
                        t += 2;
                        c += 3;
                    }
                    if (t < 50)
                    {
                        if (t > 15) t = 15;
                        e3 += PTbl4[t] + PT4Ofs;
                    }
                }
                t = c;
                if (t == 0 || t >= 50) continue;
                if (t > 15) t = 15;
                e4 += PTbl4[t] + PT4Ofs;
            }

            Ev3 = EvCurve(e3);
            Ev4 = EvCurve(e4);
        }

        private void DoAnalyze(TBopb pb)
        {
            int[] savedBoard = new int[92];
            if (pb.NTurn != 1)
            {
                for (int i = 10; i <= 80; i++)
                {
                    int t;
                    savedBoard[i] = t = pb.Board[i];
                    if (t >= 1) pb.Board[i] = (3 - t);
                }

            }
            for (int i = 10; i <= 80; i++)
                Board1[i] = Board2[i] = Board3[i] = 0;

            Ev1 = Ev2 = Ev5 = Ev6 = 0;
            Pass1(pb);
            Pass2(pb);
            Ev2 += Pscan(pb, 0);
            Ev2 += Pscan(pb, 2);
            Ev2 += Pscan(pb, 4);
            Ev2 += Pscan(pb, 6);
            Pass4(pb);
            if (pb.NTurn != 1) System.Array.Copy(savedBoard, 10, pb.Board, 10, 71);
        }

        private int Evaluate(TBopb pb)
        {
            DoAnalyze(pb);
            if (Ev3 + Ev4 == 0)
            {
                if (pb.BWDiff > 0) if (pb.BWDiff == pb.BWTotal) return 9999; else return 3000 + pb.BWDiff * 100;
                if (pb.BWDiff < 0)
                {
                    if (-pb.BWDiff == pb.BWTotal) return -9999; else return -3000 + pb.BWDiff * 100;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return ((Ev1 * 100 + Ev2 + Ev3) - Ev4 - Ev5) + Ev6;
            }
        }

        private int ABSearch(ref TBopb pb, int Alpha, int Beta)
        {
            int Mv = 0;
            int MaxVal;
            if (EndGame && pb.BWTotal >= 63)
            {
                MaxVal = pb.BWDiff;
                if (pb.BWTotal == 63)
                {
                    Mv = pb.EmptyList[81];
                    int b = pb.MExamine(Mv);
                    if (b != 0)
                    {
                        MaxVal += b + b + 1;
                    }
                    else
                    {
                        pb.NTurn = (int)(3 - pb.NTurn);
                        b = pb.MExamine(Mv);
                        pb.NTurn = (int)(3 - pb.NTurn);
                        if (b != 0) MaxVal -= b + b + 1;
                        Mv = 0;
                    }
                }
            }
            else if (CurDepth >= SearchDepth)
            {
                if (EndGame) MaxVal = pb.BWDiff; else MaxVal = Evaluate(pb);
            }
            else
            {
                MaxVal = -2147483648;
                CurDepth++;
                int ap = 81;
                for (int b = pb.EmptyList[ap]; b != 81 && MaxVal < Beta; b = pb.EmptyList[ap])
                {
                    if (pb.MPerform(b) != 0)
                    {
                        pb.EmptyList[ap] = pb.EmptyList[b];
                        int Ev = -ABSearch(ref pb, -Beta, -Alpha);
                        pb.EmptyList[ap] = (byte)b;
                        pb.MTakeBack();
                        if (Ev > MaxVal)
                        {
                            MaxVal = Ev;
                            Mv = b;
                            if (MaxVal > Alpha) Alpha = MaxVal;
                        }
                    }
                    ap = b;
                }

                if (Mv == 0)
                {
                    int b = pb.MPerform(0);
                    MaxVal = -ABSearch(ref pb, -Beta, -Alpha);
                    pb.MTakeBack();
                }
                CurDepth--;
            }
            if (CurDepth == 0) Mv1 = Mv;
            return MaxVal;
        }

        private static void MakeEmptyList(TBopb pb)
        {
            int j = 81;
            for (int i = 0; i < 64; i++)
            {
                int b = kPosOrder[i];
                if (pb.Board[b] == 0)
                {
                    pb.EmptyList[j] = b;
                    j = b;
                }
            }

            pb.EmptyList[j] = 81;
        }

        private int SearchMove(TBopb pb, int lvl)
        {
            pb.Tsp = 0;
            MakeEmptyList(pb);
            SearchDepth = lvl + lvl;
            if (pb.BWTotal < 50 - SearchDepth) SearchDepth -= 2;
            if (SearchDepth < 1) SearchDepth = 1;
            EndGame = false;
            int EvMax = 2147483647;
            if (pb.BWTotal + lvl + lvl >= 57)
            {
                EndGame = true;
                SearchDepth = 24;
            }
            CurDepth = 0;
            ABSearch(ref pb, -EvMax, EvMax);
            return Mv1;
        }

        protected internal void Joseki(TBopb pb)
        {
            Mv1 = 0;
            if (OLData != null)
            {
                for (int i = 10; i <= 80; i++)
                    Board1[i] = Board2[i] = Board3[i] = Board4[i] = -1;

                for (int j = 8; j > 0; j--)
                {
                    for (int i = 8; i > 0; i--)
                    {
                        int t = j * 9 + i;
                        int c = pb.Board[t];
                        Board1[t] = c;
                        Board2[90 - t] = c;
                        t = i * 9 + j;
                        Board3[t] = c;
                        Board4[90 - t] = c;
                    }

                }

                int OrgBWTotal = pb.BWTotal;
                int OrgBWDiff = pb.BWDiff;
                pb.ClearBoard();
                int p2 = 4;
                do
                {
                    byte c;
                    if ((c = OLData[p2 + 1]) < 0 || Mv1 != 0) break;
                    int p1 = p2 + 4;
                    p2 += OLData[p2] & 127;
                    if (OrgBWTotal - 4 >= c)
                    {
                        while (pb.BWTotal - 4 > c)
                            pb.MTakeBack();
                        while (pb.BWTotal < OrgBWTotal && p1 + 1 < p2)
                            pb.MPerform(OLData[p1++]);
                        if (pb.BWTotal == OrgBWTotal && pb.BWDiff == OrgBWDiff)
                        {
                            int p3;
                            for (p3 = p2; OLData[p3 + 1] + 4 > OrgBWTotal; p3 += OLData[p3] & 63)
                                ;
                            if (OLData[p3 + 1] + 4 == OrgBWTotal && new Random().NextDouble() <= (double)((OLData[p3] & 192) - 32) / 256.0)
                            {
                                p2 = p3;
                            }
                            else
                            {
                                int t = OLData[p1];
                                if (pb.CompareBoard(Board1)) Mv1 = t;
                                else if (pb.CompareBoard(Board2))
                                {
                                    Mv1 = (int)(90 - t);
                                }
                                else
                                {
                                    t = (int)((t % 9) * 9 + t / 9);
                                    if (pb.CompareBoard(Board3)) Mv1 = t;
                                    else if (pb.CompareBoard(Board4))
                                        Mv1 = (int)(90 - t);
                                }
                            }
                        }
                    }
                }
                while (true);
            }
        }

        protected internal int Go(TBopb pb, int lvl)
        {
            Joseki(pb);
            if (Mv1 == 0)
            {
                SearchMove(pb, lvl);
            }

            return Mv1;
        }
    }
}
