using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DarkCChess
{
    enum CANNON_RULE
    {
        CANNON_RULE_USELESS = 1,      //�з��ڣ�ֻ�ܳԱ������
        CANNON_RULE_NORMAL = 2,       //��ͨ�ڣ���һ�ӳ���
        CANNON_RULE_SUPER = 3         //�����ڣ������ӳ��ӣ�����һ��
    };

    enum GAME_LEVELS
    {
        EASY = 1,
        NORMAL = 3,
        HARD = 5
    };

    class MoveCompare : IComparer<Move>
    {
        private Move hashmove;
        public MoveCompare(Move hashmv)
        {
            hashmove=new Move(hashmv);
        }

        public int Compare(Move x, Move y)
        {
            if(x.equal_to(hashmove))
            {
                return 100;
            }
            else if (y.equal_to(hashmove))
            {
                return -100;
            }
            else if (x.cpt != 0 && y.cpt == 0)
            {
                return 1;
            }
            else if (x.cpt == 0 && y.cpt != 0)
            {
                return -1;
            }
            else if (x.cpt != 0 && y.cpt != 0)
            {
                return y.cpt - x.cpt;
            }
            else
            {
                if (x.flip == true && y.flip == false)
                {
                    return -1;
                }
                else if (x.flip == false && y.flip == true)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
    }

    class Move
    {
        public int srcx;
        public int srcy;
        public int dstx;
        public int dsty;
        public int cpt;
        public bool flip;
        public Move(Move mv)
        {
            srcx = mv.srcx;
            srcy = mv.srcy;
            dstx = mv.dstx;
            dsty = mv.dsty;
            cpt = mv.cpt;
            flip = mv.flip;
        }

        public Move()
        {
            srcx = 0;
            srcy = 0;
            dstx = 0;
            dsty = 0;
            cpt = 0;
            flip = false;
        }

        public bool equal_to(Move mv)
        {
            if (srcx != mv.srcx)
            {
                return false;
            }

            if (srcy != mv.srcy)
            {
                return false;
            }

            if (dstx != mv.dstx)
            {
                return false;
            }

            if (dsty != mv.dsty)
            {
                return false;
            }

            if (cpt != mv.cpt)
            {
                return false;
            }

            if (flip != mv.flip)
            {
                return false;
            }

            return true;
        }
    }

    class Engine
    {
        public const int BOARD_WIDTH = 4;
        public const int BOARD_HEIGHT = 8;

        public const int SIDE_RED = 0;
        public const int SIDE_BLACK = 1;
        public const int SIDE_NONE = -1;

        public const int PIECE_HIDE = 0x100;
        public const int PIECE_NONE = 0;
        public const int PIECE_OUTSIDE = -1;

        public const int PIECE_TYPE_MASK = 0x07;
        public const int PIECE_PIECE_MASK = 0xff;

        public const int PIECE_KING = 1;
        public const int PIECE_ADVISOR = 2;
        public const int PIECE_BISHOP = 3;
        public const int PIECE_ROOK = 4;
        public const int PIECE_KNIGHT = 5;
        public const int PIECE_CANNON = 6;
        public const int PIECE_PAWN = 7;

        public const int RED_KING = 1;
        public const int RED_ADVISOR = 2;
        public const int RED_BISHOP = 3;
        public const int RED_ROOK = 4;
        public const int RED_KNIGHT = 5;
        public const int RED_CANNON = 6;
        public const int RED_PAWN = 7;

        public const int BLACK_KING = 9;
        public const int BLACK_ADVISOR = 10;
        public const int BLACK_BISHOP = 11;
        public const int BLACK_ROOK = 12;
        public const int BLACK_KNIGHT = 13;
        public const int BLACK_CANNON = 14;
        public const int BLACK_PAWN = 15;

        private const int MAT_KING = 1500;
        private const int MAT_ADVISOR = 800;
        private const int MAT_BISHOP = 700;
        private const int MAT_ROOK = 600;
        private const int MAT_KNIGHT = 500;
        private const int MAT_CANNON = 400;
        private const int MAT_PAWN = 200;
        private const int MAT_TOTAL = MAT_KING + 2 * MAT_ADVISOR + 2 * MAT_BISHOP + 2 * MAT_ROOK + 2 * MAT_KNIGHT + 2 * MAT_CANNON + 5 * MAT_PAWN;

        private const int VALUE_LOSE = -100000;

        private int[] PIECE_SIDE = new int[16]
            {
                SIDE_NONE, SIDE_RED, SIDE_RED, SIDE_RED, SIDE_RED, SIDE_RED, SIDE_RED, SIDE_RED,
                SIDE_NONE, SIDE_BLACK, SIDE_BLACK, SIDE_BLACK, SIDE_BLACK, SIDE_BLACK, SIDE_BLACK, SIDE_BLACK
            };
        private int[] PIECE_MAT = new int[8]
            {
                0, MAT_KING, MAT_ADVISOR, MAT_BISHOP, MAT_ROOK, MAT_KNIGHT, MAT_CANNON, MAT_PAWN,
            };
        private UInt64[,,] HASH_PIECE = new UInt64[BOARD_WIDTH + 2, BOARD_HEIGHT + 2, 16];
        private UInt64[,] HASH_HIDE = new UInt64[BOARD_WIDTH + 2, BOARD_HEIGHT + 2];
        private UInt64 HASH_KEY;
        private CANNON_RULE cannon_rule; //�ڵĹ���
        private int hide_piece;  //δ����������

        public int[,] square = new int[BOARD_WIDTH + 2, BOARD_HEIGHT + 2];
        public int sd;  //�ֵ�˭��
        public int sd_first;    //������ɫ
        public Stack<Move> stackMvs = new Stack<Move>();    //��ʷ�з������ڻ����
        public List<UInt64> stackKey = new List<UInt64>();  //��ʷHASHKEY������ѭ���ж�
        public List<int>[] lstDead = new List<int>[2];//��������

        public Engine(CANNON_RULE new_cannon_rule)
        {
            int x1, y1, x2, y2, tmp;
            Random rnd = new Random();
            lstDead[0] = new List<int>();
            lstDead[1] = new List<int>();
            cannon_rule = new_cannon_rule;
            for (int y = 0; y < BOARD_HEIGHT + 2; y++)
            {
                for (int x = 0; x < BOARD_WIDTH + 2; x++)
                {
                    square[x, y] = PIECE_OUTSIDE;
                    HASH_HIDE[x, y] = (UInt64)rnd.Next();
                    HASH_HIDE[x, y] <<= 32;
                    HASH_HIDE[x, y] += (UInt64)rnd.Next();
                    for (int z = 0; z < 16; z++)
                    {
                        HASH_PIECE[x, y, z] = (UInt64)rnd.Next();
                        HASH_PIECE[x, y, z] <<= 32;
                        HASH_PIECE[x, y, z] += (UInt64)rnd.Next();
                    }
                }
            }

            HASH_KEY = 0;
            hide_piece = 32;
            square[1, 1] = RED_KING | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[1, 1];
            square[1, 2] = RED_ADVISOR | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[1, 2];
            square[1, 3] = RED_ADVISOR | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[1, 3];
            square[1, 4] = RED_BISHOP | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[1, 4];
            square[1, 5] = RED_BISHOP | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[1, 5];
            square[1, 6] = RED_ROOK | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[1, 6];
            square[1, 7] = RED_ROOK | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[1, 7];
            square[1, 8] = RED_KNIGHT | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[1, 8];
            square[2, 1] = RED_KNIGHT | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[2, 1];
            square[2, 2] = RED_CANNON | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[2, 2];
            square[2, 3] = RED_CANNON | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[2, 3];
            square[2, 4] = RED_PAWN | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[2, 4];
            square[2, 5] = RED_PAWN | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[2, 5];
            square[2, 6] = RED_PAWN | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[2, 6];
            square[2, 7] = RED_PAWN | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[2, 7];
            square[2, 8] = RED_PAWN | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[2, 8];
            square[3, 1] = BLACK_KING | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[3, 1];
            square[3, 2] = BLACK_ADVISOR | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[3, 2];
            square[3, 3] = BLACK_ADVISOR | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[3, 3];
            square[3, 4] = BLACK_BISHOP | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[3, 4];
            square[3, 5] = BLACK_BISHOP | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[3, 5];
            square[3, 6] = BLACK_ROOK | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[3, 6];
            square[3, 7] = BLACK_ROOK | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[3, 7];
            square[3, 8] = BLACK_KNIGHT | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[3, 8];
            square[4, 1] = BLACK_KNIGHT | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[4, 1];
            square[4, 2] = BLACK_CANNON | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[4, 2];
            square[4, 3] = BLACK_CANNON | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[4, 3];
            square[4, 4] = BLACK_PAWN | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[4, 4];
            square[4, 5] = BLACK_PAWN | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[4, 5];
            square[4, 6] = BLACK_PAWN | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[4, 6];
            square[4, 7] = BLACK_PAWN | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[4, 7];
            square[4, 8] = BLACK_PAWN | PIECE_HIDE;
            HASH_KEY ^= HASH_HIDE[4, 8];
            for (int i = 0; i < 128; i++)
            {
                x1 = rnd.Next(1, 5);
                x2 = rnd.Next(1, 5);
                y1 = rnd.Next(1, 9);
                y2 = rnd.Next(1, 9);
                tmp = square[x1, y1];
                square[x1, y1] = square[x2, y2];
                square[x2, y2] = tmp;
            }
        }

        public void from_fen(string fen)
        {
            lstDead[0].Clear();
            lstDead[1].Clear();
            hide_piece = 0;

            int idx = 0;
            for (int i = 1; i <= BOARD_HEIGHT; i++)
            {
                for (int j = 1; j <= BOARD_WIDTH; j++)
                {
                    square[j, i] = char2piece(fen[idx++]);
                    if ((square[j, i] & PIECE_HIDE) != 0)
                    {
                        hide_piece++;
                    }
                }
            }

            idx++;
            char cs = fen[idx];
            idx++;
            if (fen[idx] == 'b')
            {
                sd_first = SIDE_BLACK;
                if (cs == 'f')
                {
                    sd = SIDE_BLACK;
                }
                else
                {
                    sd = SIDE_RED;
                }
            }
            else if (fen[idx] == 'r')
            {
                sd_first = SIDE_RED;
                if (cs == 'f')
                {
                    sd = SIDE_RED;
                }
                else
                {
                    sd = SIDE_BLACK;
                }
            }
            else
            {
                sd_first = -1;
            }

            idx++;
            while (idx < fen.Length)
            {
                int pc = char2piece(fen[idx++]);
                if (pc < 8)
                {
                    lstDead[0].Add(pc);
                }
                else
                {
                    lstDead[1].Add(pc);
                }
            }
        }

        int char2piece(char c)
        {
            switch (c)
            {
                case '*': return PIECE_HIDE;
                case 'K': return RED_KING;
                case 'A': return RED_ADVISOR;
                case 'B': return RED_BISHOP;
                case 'R': return RED_ROOK;
                case 'C': return RED_CANNON;
                case 'N': return RED_KNIGHT;
                case 'P': return RED_PAWN;
                case 'k': return BLACK_KING;
                case 'a': return BLACK_ADVISOR;
                case 'b': return BLACK_BISHOP;
                case 'r': return BLACK_ROOK;
                case 'c': return BLACK_CANNON;
                case 'n': return BLACK_KNIGHT;
                case 'p': return BLACK_PAWN;
                case '.': return PIECE_NONE;
                default: return PIECE_NONE;
            }
        }

        private string piece2chs(int pc)
        {
            if ((pc & PIECE_HIDE) == PIECE_HIDE)
            {
                return "��";
            }

            switch (pc & PIECE_PIECE_MASK)
            {
                case RED_KING: return "˧";
                case RED_ADVISOR: return "��";
                case RED_BISHOP: return "��";
                case RED_ROOK: return "��";
                case RED_KNIGHT: return "��";
                case RED_CANNON: return "��";
                case RED_PAWN: return "��";
                case BLACK_KING: return "��";
                case BLACK_ADVISOR: return "ʿ";
                case BLACK_BISHOP: return "��";
                case BLACK_ROOK: return "��";
                case BLACK_KNIGHT: return "��";
                case BLACK_CANNON: return "�h";
                case BLACK_PAWN: return "��";
                default: return "��";
            }
        }
#if !WINDOWS_PHONE && !NETFX_CORE
        public void print()
        {
            string str = null;
            for (int y = 1; y <= BOARD_HEIGHT; y++)
            {
                Console.ResetColor();
                Console.Write("{0} ", y);
                for (int x = 1; x <= BOARD_WIDTH; x++)
                {
                    str = piece2chs(square[x, y]);
                    if ((square[x, y] & PIECE_HIDE) == PIECE_HIDE)
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    else if (PIECE_SIDE[square[x, y] & PIECE_PIECE_MASK] == SIDE_RED)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else if (PIECE_SIDE[square[x, y] & PIECE_PIECE_MASK] == SIDE_BLACK)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    }

                    Console.Write(str);
                }

                Console.Write("\n");
            }

            Console.ResetColor();
            Console.WriteLine("  a b c d");
            Console.WriteLine("HASHKEY:{0}, MOVE IDX:{1}", HASH_KEY, stackMvs.Count);
        }
#endif

        //���ı�mv.cpt,mv.flip
        public bool make_move(ref Move mv)
        {
            if (mv.srcx <= 0 || mv.srcx > BOARD_WIDTH || mv.srcy <= 0 || mv.srcy > BOARD_HEIGHT)
            {
                return false;
            }

            if (mv.flip == false)
            {
                if (mv.dstx <= 0 || mv.dstx > BOARD_WIDTH || mv.dsty <= 0 || mv.dsty > BOARD_HEIGHT)
                {
                    return false;
                }
            }

            if (square[mv.srcx, mv.srcy] == PIECE_NONE)
            {
                //��ʼλ���ǿհ�
                return false;
            }

            if ((square[mv.srcx, mv.srcy] & PIECE_HIDE) != 0 && mv.srcx == mv.dstx && mv.srcy == mv.dsty)
            {
                //��ʼλ��δ��������ʼλ�ú�Ŀ��λ����ͬ��������Ҳ��һ���Ϸ��з�
                square[mv.srcx, mv.srcy] &= ~PIECE_HIDE;
                hide_piece--;
                HASH_KEY ^= HASH_HIDE[mv.srcx, mv.srcy];
                mv.flip = true;
                if (hide_piece == 32)
                {
                    //ȷ��������ɫ
                    sd = PIECE_SIDE[square[mv.srcx, mv.srcy]];
                    sd_first = sd;
                }

                stackMvs.Push(new Move(mv));
                stackKey.Add(0);//������ǰ����з������ܹ���ѭ��
                sd = 1 - sd;
                return true;
            }

            if ((square[mv.srcx, mv.srcy] & PIECE_HIDE) != 0)
            {
                //��ʼλ��δ����
                return false;
            }

            if (PIECE_SIDE[square[mv.srcx, mv.srcy]] != sd)
            {
                //��ʼλ�÷Ǽ�������
                return false;
            }

            if ((square[mv.dstx, mv.dsty] & PIECE_HIDE) != 0)
            {
                //Ŀ��λ��δ����
                return false;
            }

            int dist = (mv.srcx - mv.dstx) * (mv.srcx - mv.dstx) + (mv.srcy - mv.dsty) * (mv.srcy - mv.dsty);
            if ((cannon_rule == CANNON_RULE.CANNON_RULE_NORMAL || cannon_rule == CANNON_RULE.CANNON_RULE_SUPER) && (square[mv.srcx, mv.srcy] & PIECE_TYPE_MASK) == PIECE_CANNON)
            {
                //�����ڵĸ��ӹ���
                if (square[mv.dstx, mv.dsty] != PIECE_NONE && dist == 1)
                {
                    //�ߵ��ڽ��񣬵������ﲻ��
                    return false;
                }

                if (square[mv.dstx, mv.dsty] != PIECE_NONE)
                {
                    if (PIECE_SIDE[square[mv.dstx, mv.dsty] & PIECE_PIECE_MASK] == PIECE_SIDE[square[mv.srcx, mv.srcy] & PIECE_PIECE_MASK])
                    {
                        //���ܳԼ�������
                        return false;
                    }
                }

                if (dist > 1 && square[mv.dstx, mv.dsty] == PIECE_NONE)
                {
                    //�ߵ����ڽ��񣬵��������ǿ�
                    return false;
                }

                int step = 0, yy = 0, xx = 0;
                if (mv.dstx == mv.srcx)
                {
                    if (mv.dsty < mv.srcy)
                    {
                        yy = mv.srcy - 1;
                        while (yy > mv.dsty)
                        {
                            if (square[mv.srcx, yy] != PIECE_NONE)
                            {
                                step++;
                            }

                            yy--;
                        }
                    }
                    else if (mv.dsty > mv.srcy)
                    {
                        yy = mv.srcy + 1;
                        while (yy < mv.dsty)
                        {
                            if (square[mv.srcx, yy] != PIECE_NONE)
                            {
                                step++;
                            }

                            yy++;
                        }
                    }
                }
                else if (mv.dsty == mv.srcy)
                {
                    if (mv.dstx < mv.srcx)
                    {
                        xx = mv.srcx - 1;
                        while (xx > mv.dstx)
                        {
                            if (square[xx, mv.srcy] != PIECE_NONE)
                            {
                                step++;
                            }

                            xx--;
                        }
                    }
                    else if (mv.dstx > mv.srcx)
                    {
                        xx = mv.srcx + 1;
                        while (xx < mv.dstx)
                        {
                            if (square[xx, mv.srcy] != PIECE_NONE)
                            {
                                step++;
                            }

                            xx++;
                        }
                    }
                }

                if (step == 0 && dist != 1)
                {
                    //û���ڼܣ����ߵ����ڽ���
                    return false;
                }

                if (step > 1 && cannon_rule == CANNON_RULE.CANNON_RULE_NORMAL)
                {
                    //��ͨ��ֻ�ܸ�һ��
                    return false;
                }
            }
            else
            {
                if (dist != 1)
                {
                    //�ߵ����ڽ���
                    return false;
                }

                if (square[mv.dstx, mv.dsty] != PIECE_NONE)
                {
                    if (PIECE_SIDE[square[mv.dstx, mv.dsty] & PIECE_PIECE_MASK] == PIECE_SIDE[square[mv.srcx, mv.srcy] & PIECE_PIECE_MASK])
                    {
                        //���ܳԼ�������
                        return false;
                    }

                    int my_type, opp_type;
                    my_type = square[mv.srcx, mv.srcy] & PIECE_TYPE_MASK;
                    opp_type = square[mv.dstx, mv.dsty] & PIECE_TYPE_MASK;
                    if (my_type != PIECE_PAWN || opp_type != PIECE_KING)
                    {
                        if (my_type > opp_type)
                        {
                            //���ܳԱ��Լ��߼������ӣ�������������ԳԽ�˧
                            return false;
                        }

                        if (my_type == PIECE_KING && opp_type == PIECE_PAWN)
                        {
                            //��������˧���ܳԱ���
                            return false;
                        }
                    }
                }
            }

            mv.cpt = square[mv.dstx, mv.dsty];
            mv.flip = false;
            square[mv.dstx, mv.dsty] = square[mv.srcx, mv.srcy];
            HASH_KEY ^= HASH_PIECE[mv.srcx, mv.srcy, square[mv.srcx, mv.srcy] & PIECE_PIECE_MASK];
            square[mv.srcx, mv.srcy] = PIECE_NONE;
            HASH_KEY ^= HASH_PIECE[mv.dstx, mv.dsty, square[mv.dstx, mv.dsty] & PIECE_PIECE_MASK];
            stackMvs.Push(new Move(mv));
            sd = 1 - sd;
            if (mv.cpt != PIECE_NONE)
            {
                HASH_KEY ^= HASH_PIECE[mv.dstx, mv.dsty, mv.cpt & PIECE_PIECE_MASK];
                lstDead[sd].Add(mv.cpt);
                stackKey.Add(0);//�����з�ǰ���ܹ���ѭ��
            }
            else
            {
                stackKey.Add(HASH_KEY);
            }

            return true;
        }

        public void unmake_move()
        {
            Move mv = (Move)stackMvs.Pop();
            stackKey.RemoveAt(stackKey.Count - 1);//������ʷ
            if (mv.flip)
            {
                //�����з�
                HASH_KEY ^= HASH_HIDE[mv.srcx, mv.srcy];
                square[mv.srcx, mv.srcy] |= PIECE_HIDE;
                hide_piece++;
            }
            else
            {
                HASH_KEY ^= HASH_PIECE[mv.dstx, mv.dsty, square[mv.dstx, mv.dsty] & PIECE_PIECE_MASK];
                square[mv.srcx, mv.srcy] = square[mv.dstx, mv.dsty];
                HASH_KEY ^= HASH_PIECE[mv.srcx, mv.srcy, square[mv.srcx, mv.srcy] & PIECE_PIECE_MASK];
                square[mv.dstx, mv.dsty] = mv.cpt;
                if (mv.cpt != PIECE_NONE)
                {
                    HASH_KEY ^= HASH_PIECE[mv.dstx, mv.dsty, square[mv.dstx, mv.dsty] & PIECE_PIECE_MASK];
                    lstDead[sd].Remove(mv.cpt);
                }
            }

            sd = 1 - sd;
        }

        private List<Move> gen_flip_move()
        {
            List<Move> flips = new List<Move>();
            for (int x = 1; x <= BOARD_WIDTH; x++)
            {
                for (int y = 1; y <= BOARD_HEIGHT; y++)
                {
                    if ((square[x, y] & PIECE_HIDE) == PIECE_HIDE)
                    {
                        Move mv = new Move();
                        mv.srcx = x;
                        mv.srcy = y;
                        mv.dstx = x;
                        mv.dsty = y;
                        mv.flip = true;
                        flips.Add(mv);
                    }
                }
            }

            return flips;
        }

        private List<Move> gen_normal_move()
        {
            List<Move> moves = new List<Move>();
            List<int> cannon_x = new List<int>();
            List<int> cannon_y = new List<int>();
            int xx, yy;
            int[] dx = new int[4] { 1, 0, -1, 0 };
            int[] dy = new int[4] { 0, 1, 0, -1 };
            for (int x = 1; x <= BOARD_WIDTH; x++)
            {
                for (int y = 1; y <= BOARD_HEIGHT; y++)
                {
                    if (((square[x, y] & PIECE_HIDE) == 0) && (PIECE_SIDE[square[x, y] & PIECE_PIECE_MASK] == sd))
                    {
                        if ((square[x, y] & PIECE_TYPE_MASK) == PIECE_CANNON && cannon_rule != CANNON_RULE.CANNON_RULE_USELESS)
                        {
                            //���ǲз��ڣ���������
                            cannon_x.Add(x);
                            cannon_y.Add(y);
                        }
                        else
                        {
                            for (int d = 0; d < 4; d++)
                            {
                                xx = x + dx[d];
                                yy = y + dy[d];
                                if (square[xx, yy] == PIECE_OUTSIDE)
                                {
                                    //Ŀ��λ�ó�������
                                    continue;
                                }
                                else if (square[xx, yy] == PIECE_NONE)
                                {
                                    //Ŀ��λ���ǿ�
                                    Move mv = new Move();
                                    mv.srcx = x;
                                    mv.srcy = y;
                                    mv.dstx = xx;
                                    mv.dsty = yy;
                                    mv.cpt = 0;
                                    mv.flip = false;
                                    moves.Add(mv);
                                }
                                else if (((square[xx, yy] & PIECE_HIDE) == 0) && (PIECE_SIDE[square[xx, yy] & PIECE_PIECE_MASK] == 1 - sd))
                                {
                                    //Ŀ��λ���Ѿ����������Ƕ�������
                                    int my_type, opp_type;
                                    my_type = square[x, y] & PIECE_TYPE_MASK;
                                    opp_type = square[xx, yy] & PIECE_TYPE_MASK;
                                    if (my_type == PIECE_PAWN && opp_type == PIECE_KING)
                                    {
                                        //����Խ�˧
                                        Move mv = new Move();
                                        mv.srcx = x;
                                        mv.srcy = y;
                                        mv.dstx = xx;
                                        mv.dsty = yy;
                                        mv.cpt = square[xx, yy];
                                        mv.flip = false;
                                        moves.Add(mv);
                                    }
                                    else if (my_type == PIECE_KING && opp_type == PIECE_PAWN)
                                    {
                                        ;//��˧���ܳԱ���
                                    }
                                    else if (my_type == PIECE_CANNON && cannon_rule != CANNON_RULE.CANNON_RULE_USELESS)
                                    {
                                        ;//���ǲз��ڣ����ܳ��ڽ���
                                    }
                                    else if (opp_type >= my_type)
                                    {
                                        //��ͬ����ͼ�������
                                        Move mv = new Move();
                                        mv.srcx = x;
                                        mv.srcy = y;
                                        mv.dstx = xx;
                                        mv.dsty = yy;
                                        mv.cpt = square[xx, yy];
                                        mv.flip = false;
                                        moves.Add(mv);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //�����ڵĸ��ӳ��ӹ���
            if (cannon_rule != CANNON_RULE.CANNON_RULE_USELESS && cannon_x.Count > 0)
            {
                for (int i = 0; i < cannon_x.Count; i++)
                {
                    int x, y, step;
                    x = cannon_x[i];
                    y = cannon_y[i];
                    for (int d = 0; d < 4; d++)
                    {
                        List<int> capt_x = new List<int>();
                        List<int> capt_y = new List<int>();
                        step = 0;
                        xx = x + dx[d];
                        yy = y + dy[d];
                        //�ȿ������ĸ�
                        if (square[xx, yy] == PIECE_NONE)
                        {
                            //Ŀ��λ���ǿ�
                            Move mv = new Move();
                            mv.srcx = x;
                            mv.srcy = y;
                            mv.dstx = xx;
                            mv.dsty = yy;
                            mv.cpt = 0;
                            mv.flip = false;
                            moves.Add(mv);
                        }

                        while (true)
                        {
                            if (square[xx, yy] == PIECE_OUTSIDE)
                            {
                                //Ŀ��λ�ó�������
                                break;
                            }
                            else if ((PIECE_SIDE[square[xx, yy] & PIECE_PIECE_MASK] == 1 - sd) && ((square[xx, yy] & PIECE_HIDE) == 0))
                            {
                                if (step > 0)
                                {
                                    capt_x.Add(xx);
                                    capt_y.Add(yy);
                                    if (cannon_rule == CANNON_RULE.CANNON_RULE_NORMAL)
                                    {
                                        //��ͨ�ڣ��ҵ���һ���ɳԵľ���ֹ����
                                        break;
                                    }
                                }
                            }

                            if (square[xx, yy] != PIECE_NONE)
                            {
                                step++;
                            }

                            xx += dx[d];
                            yy += dy[d];
                        }

                        if ((step == 1 && cannon_rule == CANNON_RULE.CANNON_RULE_NORMAL) || (step >= 1 && cannon_rule == CANNON_RULE.CANNON_RULE_SUPER))
                        {
                            for (int k = 0; k < capt_x.Count; k++)
                            {
                                Move mv = new Move();
                                mv.srcx = x;
                                mv.srcy = y;
                                mv.dstx = capt_x[k];
                                mv.dsty = capt_y[k];
                                mv.cpt = square[mv.dstx, mv.dsty];
                                mv.flip = false;
                                moves.Add(mv);
                            }
                        }
                    }
                }
            }

            return moves;
        }

        public bool lose()
        {
            if (lstDead[sd].Count == 16)
            {
                //���������������䣡
                return true;
            }
            else
            {
                if (hide_piece == 0 && gen_normal_move().Count == 0)
                {
                    //�����ӿɷ���Ҳ�����ӿɶ����䣡
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool repeat(int repcnt)
        {
            int idx = stackKey.Count - 2;
            if (idx > 4)
            {
                while (idx >= 0 && stackKey[idx] != 0)
                {
                    if (stackKey[idx] == HASH_KEY)
                    {
                        repcnt--;
                        if (repcnt == 0)
                        {
                            return true;
                        }
                    }

                    idx--;
                }
            }

            return false;
        }

        private int eval()
        {
            int my_mat;
            int op_mat;
            int my_att = 0;
            int op_att = 0;
            my_mat = MAT_TOTAL;
            op_mat = MAT_TOTAL;
            for (int i = 0; i < lstDead[sd].Count; i++)
            {
                my_mat -= PIECE_MAT[lstDead[sd][i] & PIECE_TYPE_MASK];
            }

            for (int i = 0; i < lstDead[1 - sd].Count; i++)
            {
                op_mat -= PIECE_MAT[lstDead[1 - sd][i] & PIECE_TYPE_MASK];
            }

            return my_mat - op_mat + new Random().Next(30);
        }

        private int alphabeta(int alpha, int beta, int depth)
        {
            if (depth <= 0)
            {
                //����Ҷ�ӽڵ�
                return eval();
            }

            if (repeat(1))
            {
                //�ж�����
                return -100;
            }

            List<Move> lstMove = gen_normal_move();
            if (lstMove.Count == 0)
            {
                //û�пɶ������ӣ�ע�⣬��Ҫ�ж��Ƿ��пɷ�������
                if (hide_piece == 0)
                {
                    //û�пɷ������ӣ�����
                    return VALUE_LOSE + depth;
                }
                else
                {
                    //���пɷ������ӣ������������в�������Է��ӣ���������жϣ���������ֵ
                    return eval();
                }
            }
            else
            {
                int this_val;
                lstMove.Sort(new MoveCompare(new Move()));
                for (int i = 0; i < lstMove.Count; i++)
                {
                    Move mv = new Move(lstMove[i]);
                    if (make_move(ref mv))
                    {
                        this_val = -alphabeta(-beta, -alpha, depth - 1);
                        unmake_move();
                        if (this_val >= beta)
                        {
                            return this_val;
                        }

                        if (this_val > alpha)
                        {
                            alpha = this_val;
                        }
                    }
                }

                return alpha;
            }
        }

        public Move search_bestmove(int depth, int millsecs)
        {
            //��һ��ʱ��һ���������������з�
            int my_mat;
            int op_mat;
            Move mv = new Move();
            List<Move> lstFlip = gen_flip_move();
            if (lstFlip.Count > 0)
            {
                //���ڷ����з�������˫�����������Ӻ��ѷ��������ӣ������δ���������ӵ�������
                my_mat = MAT_TOTAL;
                op_mat = MAT_TOTAL;
                for (int i = 0; i < lstDead[sd].Count; i++)
                {
                    my_mat -= PIECE_MAT[lstDead[sd][i] & PIECE_TYPE_MASK];
                }

                for (int i = 0; i < lstDead[1 - sd].Count; i++)
                {
                    op_mat -= PIECE_MAT[lstDead[1 - sd][i] & PIECE_TYPE_MASK];
                }

                for (int x = 1; x <= BOARD_WIDTH; x++)
                {
                    for (int y = 1; y <= BOARD_HEIGHT; y++)
                    {
                        if (square[x, y] != PIECE_NONE)
                        {
                            if ((square[x, y] & PIECE_HIDE) == 0)
                            {
                                //�ѷ���������
                                if (PIECE_SIDE[square[x, y] & PIECE_PIECE_MASK] == sd)
                                {
                                    my_mat -= PIECE_MAT[square[x, y] & PIECE_TYPE_MASK];
                                }
                                else if (PIECE_SIDE[square[x, y] & PIECE_PIECE_MASK] == 1 - sd)
                                {
                                    op_mat -= PIECE_MAT[square[x, y] & PIECE_TYPE_MASK];
                                }
                            }
                        }
                    }
                }

                if (my_mat > op_mat + 100)
                {
                    //�������δ�������������ڶԷ�����ô��һ������
                    return lstFlip[new Random().Next(lstFlip.Count)];
                }
            }

            List<Move> lstMove = gen_normal_move();
            if (lstMove.Count == 0)
            {
                //���û�����ӿɶ���ֻ�ܷ���
                if (lstFlip.Count > 0)
                {
                    return lstFlip[new Random().Next(lstFlip.Count)];
                }
                else
                {
                    //���ؿ��з�
                    return mv;
                }
            }
            else if (lstMove.Count == 1 && lstFlip.Count == 0)
            {
                //Ψһ�з�
                return lstMove[0];
            }
            else
            {
                //�������������У�����������
                int best_val = VALUE_LOSE;
                for (int d = 2; d < depth; d++)
                {
                    int this_val;
                    best_val = VALUE_LOSE;
                    lstMove.Sort(new MoveCompare(mv));
                    for (int i = 0; i < lstMove.Count; i++)
                    {
                        Move thismv = new Move(lstMove[i]);
                        if (make_move(ref thismv))
                        {
                            this_val = -alphabeta(VALUE_LOSE, -VALUE_LOSE, d);
                            unmake_move();
                            if (this_val > best_val)
                            {
                                best_val = this_val;
                                mv = new Move(thismv);
                            }
                        }
                    }
                }

                if (lstFlip.Count > 0)
                {
                    if (best_val < eval() - 200)
                    {
                        //����������ѣ����ǳ��Է���
                        return lstFlip[new Random().Next(lstFlip.Count)];
                    }
                }
            }

            return mv;
        }
    }
}
