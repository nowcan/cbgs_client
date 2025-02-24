using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using cbgs_client_cs;
using XqEngine;

namespace cbgs_test
{
    abstract class cbgs_main
    {
        protected cbgs_client cli = null;
        protected ConcurrentQueue<string> cmd_que = null;
        protected UInt64 m_match_id = 0;
        protected bool need_quit = false;
        protected bool need_quit_thread_cbgs = false;
        protected List<string> user_list = null;
        protected string my_name = null;
        protected string my_pass = null;
        protected string my_server = null;
        protected bool challenge = false;
        protected UInt64 last_server_time = 1;
        protected UInt64 server_time = 0;
        protected Timer my_timer = null;
        protected Int32 my_time_initial = 0;
        protected Int32 my_time_inc = 0;
        protected bool guest_mode = false;

        abstract protected bool cbgs_accept(cbgs_client.DATA_CHALLENGE_IND ci);

        public cbgs_main()
        {
            cmd_que = new ConcurrentQueue<string>();
            user_list = new List<string>();
        }

        void cbgs_on_timer()
        {
            if (last_server_time == server_time)
            {
                Console.WriteLine("[CBGS]no heartbeat now, relogin");
                need_quit_thread_cbgs = true;
                Thread.Sleep(1000);
                need_quit_thread_cbgs = false;
                cbgs_login();
            }

            last_server_time = server_time;
            cli.heartbeat();
        }

        protected void cbgs_login()
        {
            m_match_id = 0;
            if (cli.open(my_server, 7000) > 0)
            {
                string md5psw = null;
                Console.WriteLine("{CBGS]connect to server");
                if (guest_mode)
                {
                    cli.login_guest();
                }
                else
                {
                    cli.login(my_name, my_pass, ref md5psw);
                }
            }
            else
            {
                Console.WriteLine("{CBGS]cannot connect to server");
            }
        }

        protected void on_login(UInt32 data, UInt32 version, UInt32 result)
        {
            if (result == (UInt32)cbgs_client.LOGIN_RESULT.LOGIN_OK)
            {
                Console.WriteLine("[{0}]login, server VER: {1}", data, version);
                if (my_timer != null)
                {
                    my_timer.Dispose();
                    my_timer = null;
                }

                my_timer = new Timer((obj) =>
                {
                    cbgs_on_timer();
                }, null, 60000, 60000);

                cli.aes_key_xchg_req();
                new Thread(new ThreadStart(thread_cbgs)).Start();
            }
            else
            {
                Console.WriteLine("[{0}]login fail {1}", data, result);
            }
        }

        protected void on_login_guest(UInt32 data, UInt32 version, string user_name, UInt32 result)
        {
            if (result == (UInt32)cbgs_client.LOGIN_RESULT.LOGIN_OK)
            {
                Console.WriteLine("[{0}]login guest, server VER: {1}", data, version);
                Console.WriteLine("[{0}]your user name is {1}", data, user_name);
                my_name = user_name;
                if (my_timer != null)
                {
                    my_timer.Dispose();
                    my_timer = null;
                }

                my_timer = new Timer((obj) =>
                {
                    cbgs_on_timer();
                }, null, 60000, 60000);

                cli.aes_key_xchg_req();
                new Thread(new ThreadStart(thread_cbgs)).Start();
            }
            else
            {
                Console.WriteLine("[{0}]login fail {1}", data, result);
            }
        }

        protected void on_logout(UInt32 data)
        {
            Console.WriteLine("[{0}]logout", data);
        }

        protected void on_query(UInt32 data, List<cbgs_client.DATA_USER> users, List<cbgs_client.DATA_MATCH> matchs)
        {
            user_list.Clear();
            Console.WriteLine("[{0}]USERS: {1}", data, users.Count);
            foreach (cbgs_client.DATA_USER u in users)
            {
                Console.WriteLine("    {0}", u.name);
                user_list.Add(u.name);
            }

            Console.WriteLine("[{0}]MATCHS: {1}", data, matchs.Count);
            foreach (cbgs_client.DATA_MATCH m in matchs)
            {
                Console.WriteLine("    {0} - {1}", m.user1.name, m.user2.name);
            }
        }

        protected void on_challenge(UInt32 data, cbgs_client.DATA_CHALLENGE_IND ci)
        {
            Console.WriteLine("[{0}]challenge from {1} : {2}", data, ci.user.name, cbgs_client.GameName[(int)ci.game_type]);
            if (cbgs_accept(ci))
            {
                cli.send_accept_req(ci, 1);
            }
            else
            {
                cli.send_accept_req(ci, 0);
            }
        }

        protected void on_challenge_fail(UInt32 data)
        {
            Console.WriteLine("[{0}]challenge denied", data);
            challenge = false;
        }

        protected void on_netork_err(UInt32 data)
        {
            Console.WriteLine("[{0}]network error", data);
        }

        protected void on_aes_key_xchg(UInt32 data, UInt32 result)
        {
            Console.WriteLine("[{0}]AES key xchg -> {1}", data, result);
        }

        protected void on_heart_beat(UInt32 data, cbgs_client.DATA_HEART_BEAT_IND hbi)
        {
            Console.WriteLine("[{0}]heart beat from server -> {1}", data, hbi.time_stamp);
            server_time = hbi.time_stamp;
        }

        protected void on_game_over(UInt32 data, UInt64 match_id, cbgs_client.GAME_RESULT res)
        {
            Console.WriteLine("[{0}]game over -> {1}:{2}", data, match_id, res);
            cmd_que.Enqueue("stop");
            m_match_id = 0;
        }

        protected void on_game_start(UInt32 data, cbgs_client.DATA_MATCH match)
        {
            Console.WriteLine("[{0}]game start -> {1}", data, match.id);
            m_match_id = match.id;
            challenge = false;
        }

        protected void on_user_delta(UInt32 data, cbgs_client.DATA_USER_DELTA_IND udi)
        {
            Console.WriteLine("[{0}]user delta -> {1}, {2}", data, udi.user.name, udi.login);
        }

        protected void on_match_delta(UInt32 data, cbgs_client.DATA_MATCH_DELTA_IND mdi)
        {
            Console.WriteLine("[{0}]match delta -> {1}, {2}", data, mdi.match.id, mdi.add);
        }

        protected void on_watch(UInt32 data, cbgs_client.DATA_WATCH_IND wi)
        {
            Console.WriteLine("[{0}]watch -> {1}", data, wi.res);
        }

        protected void on_unwatch(UInt32 data, cbgs_client.DATA_UNWATCH_IND ui)
        {
            Console.WriteLine("[{0}]unwatch -> {1}", data, ui.res);
        }

        protected void on_query_user(UInt32 data, List<cbgs_client.DATA_USER> users)
        {
            Console.WriteLine("[{0}]QUERY USERS: {1}", data, users.Count);
            foreach (cbgs_client.DATA_USER u in users)
            {
                Console.WriteLine("    {0}", u.name);
            }
        }

        protected void on_invalid_move(UInt32 data, UInt64 match_id)
        {
            Console.WriteLine("[{0}]invalid move: {1}", data, match_id);
        }

        protected void on_draw(UInt32 data, UInt64 match_id)
        {
            Console.WriteLine("[{0}]draw request: {1}", data, match_id);
        }

        protected void on_resign(UInt32 data, UInt64 match_id)
        {
            Console.WriteLine("[{0}]opp resign: {1}", data, match_id);
        }

        protected void on_board_ocr_work(UInt32 data, UInt64 ocr_id)
        {
            Console.WriteLine("[{0}]board ocr work id: {1}", data, ocr_id);
        }

        protected void on_board_ocr_done(UInt32 data, UInt64 ocr_id, UInt32 result, string fen)
        {
            Console.WriteLine("[{0}]board ocr done id: {1} result: {2} fen: {3}", data, ocr_id, result, fen);
        }

        void thread_cbgs()
        {
            int step = 0;
            int idx = 0;
            cbgs_client.DATA_TIME dt = new cbgs_client.DATA_TIME();

            dt.initial = my_time_initial;
            dt.inc = my_time_inc;
            dt.dead = 10000;

            while (need_quit_thread_cbgs == false)
            {
                if (m_match_id == 0)
                {
                    if (step == 0)
                    {
                        cli.query();
                        step = 1;
                        idx = 0;
                    }
                    else
                    {
                        if (idx < user_list.Count)
                        {
                            if (user_list[idx] != my_name)
                            {
                                challenge = true;
                                if (cli.GetType() == typeof(cch_client))
                                {
                                    cli.challenge(user_list[idx], cbgs_client.GAME_TYPE.GAME_CCHESS, dt, 0xff);//0xff is random side
                                }
                                else if (cli.GetType() == typeof(dxq_client))
                                {
                                    cli.challenge(user_list[idx], cbgs_client.GAME_TYPE.GAME_DARKCCHESS, dt, 0xff);//0xff is random side
                                }
                                else if (cli.GetType() == typeof(oth_client))
                                {
                                    cli.challenge(user_list[idx], cbgs_client.GAME_TYPE.GAME_OTHELLO, dt, 0xff);//0xff is random side
                                }
                            }

                            idx++;
                        }
                        else
                        {
                            step = 0;
                        }
                    }
                }

                for (int i = 0; i < 100; i++)
                {
                    Thread.Sleep(100);
                    if (need_quit_thread_cbgs == true)
                    {
                        break;
                    }
                }
            }
        }

    }

    class cbgs_main_cch : cbgs_main
    {
        XqEngineAdapter engine = null;
        cch_client cli_cch = null;
        public cbgs_main_cch()
        {
            engine = new XqEngineAdapter();
            cli_cch = new cch_client();

            cli_cch.set_userdata(0);
            cli_cch.on_login = on_login;
            cli_cch.on_login_guest = on_login_guest;
            cli_cch.on_logout = on_logout;
            cli_cch.on_query = on_query;
            cli_cch.on_challenge = on_challenge;
            cli_cch.on_challenge_fail = on_challenge_fail;
            cli_cch.on_heart_beat = on_heart_beat;
            cli_cch.on_game_over = on_game_over;
            cli_cch.on_game_start = on_game_start;
            cli_cch.on_user_delta = on_user_delta;
            cli_cch.on_match_delta = on_match_delta;
            cli_cch.on_board_ind = on_board_ind;
            cli_cch.on_move = on_move;
            cli_cch.on_invalid_move = on_invalid_move;
            cli_cch.on_watch = on_watch;
            cli_cch.on_unwatch = on_unwatch;
            cli_cch.on_query_user = on_query_user;
            cli_cch.on_draw = on_draw;
            cli_cch.on_resign = on_resign;
            cli_cch.on_network_err = on_netork_err;
            cli_cch.on_aes_key_xchg = on_aes_key_xchg;
            cli_cch.on_board_ocr_work = on_board_ocr_work;
            cli_cch.on_board_ocr_done = on_board_ocr_done;

            cli = cli_cch;
        }

        public void test_board_ocr()
        {
            System.IO.FileStream fs = new System.IO.FileStream("1.jpg", System.IO.FileMode.Open);
            byte[] buf = new byte[fs.Length];
            fs.Read(buf, 0, (int)fs.Length);
            Console.WriteLine("[Engine]read 1.jpg {0} bytes", fs.Length);
            cli.board_ocr_req(buf, (UInt16)fs.Length, 1234, (int)cbgs_client.GAME_TYPE.GAME_CCHESS, 35, 40, 0, 0, 0, 0, 290, 320);
            fs.Close();
        }

        public bool run(string server, string user, string pass, string time_init, string time_inc, string file, string protocol)
        {
            engine.load(file, protocol);
            if (engine.m_type == "UNKNOWN")
            {
                Console.WriteLine("{Engine]load fail");
                return false;
            }

            Console.WriteLine("[Engine]load ok, engine {0}, protocol {1}", engine.m_name, engine.m_type);
            guest_mode = false;
            my_name = user;
            my_pass = pass;
            my_server = server;
            my_time_initial = Convert.ToInt32(time_init) * 60 * 1000;
            my_time_inc = Convert.ToInt32(time_inc) * 1000;
            new Thread(new ThreadStart(thread_engine_UI)).Start();
            new Thread(new ThreadStart(thread_UI_engine)).Start();
            cbgs_login();
            return true;
        }

        public bool run(string server, string time_init, string time_inc, string file, string protocol)
        {
            engine.load(file, protocol);
            if (engine.m_type == "UNKNOWN")
            {
                Console.WriteLine("{Engine]load fail");
                return false;
            }

            Console.WriteLine("[Engine]load ok, engine {0}, protocol {1}", engine.m_name, engine.m_type);
            guest_mode = true;
            my_server = server;
            my_time_initial = Convert.ToInt32(time_init) * 60 * 1000;
            my_time_inc = Convert.ToInt32(time_inc) * 1000;
            new Thread(new ThreadStart(thread_engine_UI)).Start();
            new Thread(new ThreadStart(thread_UI_engine)).Start();
            cbgs_login();
            return true;
        }

        public void quit()
        {
            cli.logout();
            cli.close();
            engine.unload();
            need_quit = true;
        }

        void on_board_ind(UInt32 data, UInt64 match_id, string fen, cbgs_client.DATA_TIME time, cbgs_client.DATA_TIME opptime, UInt32 lastmove, Int32 lastvalue)
        {
            Console.WriteLine("[{0}]board ind -> {1}, {2}, {3}, {4}", data, match_id, fen, lastmove, lastvalue);
        }

        void on_move(UInt32 data, UInt64 match_id, string fen, Int32 side, cbgs_client.DATA_TIME time, cbgs_client.DATA_TIME opptime)
        {
            Console.WriteLine("[{0}]move -> {1}, {2}, {3}", data, match_id, fen, side);

            StringBuilder cmd = new StringBuilder();

            cmd.Append("position fen ");
            cmd.Append(fen);
            cmd_que.Enqueue(cmd.ToString());
            cmd.Clear();

            //cmd.Append("fen ");
            //cmd.Append(fen);
            //cmd_que.Enqueue(cmd.ToString());
            //cmd.Clear();

            cmd.Append("go ");
            if (engine.m_type == "UCCI")
            {
                cmd.AppendFormat("time {0} increment {1} opptime {2} oppincrement {3}", time.initial, time.inc, opptime.initial, opptime.inc);
                cmd_que.Enqueue(cmd.ToString());
            }
            else if (engine.m_type == "UCI")
            {
                if (side == cch_client.CCHESS_SIDE_RED)
                {
                    cmd.AppendFormat("btime {0} wtime {1} binc {2} winc {3}", opptime.initial, time.initial, opptime.inc, time.inc);
                    cmd_que.Enqueue(cmd.ToString());
                }
                else if (side == cch_client.CCHESS_SIDE_BLACK)
                {
                    cmd.AppendFormat("btime {0} wtime {1} binc {2} winc {3}", time.initial, opptime.initial, time.inc, opptime.inc);
                    cmd_que.Enqueue(cmd.ToString());
                }
                else
                {
                    Console.WriteLine("Wrong side");
                }
            }
            else
            {
                Console.WriteLine("Wrong engine type.");
            }
        }

        void thread_UI_engine()
        {
            string cmd;
            while (need_quit == false)
            {
                if (cmd_que.TryDequeue(out cmd))
                {
                    Console.WriteLine("[UI -> Engine]{0}", cmd);
                    engine.send_cmd(cmd);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        void thread_engine_UI()
        {
            string line, sz_score;
            int score, score_end;

            while (need_quit == false)
            {
                score = 0;
                while (need_quit == false)
                {
                    line = engine.recv_rsp();
                    if (line == null)
                    {
                        Thread.Sleep(10);
                    }
                    else
                    {
                        Console.WriteLine("[Engine -> UI]{0}", line);
                        if (line.Contains("score "))
                        {
                            if (line.Contains("score cp "))
                            {
                                sz_score = line.Substring(line.IndexOf("score cp ") + 9);
                            }
                            else
                            {
                                sz_score = line.Substring(line.IndexOf("score ") + 6);
                            }

                            score_end = sz_score.IndexOf(' ');
                            sz_score = sz_score.Substring(0, score_end);
                            try
                            {
                                score = Convert.ToInt32(sz_score);
                            }
                            catch
                            {
                                score = 0;
                            }
                        }

                        if (line.Contains("resign"))
                        {
                            cli.resign_req(m_match_id);
                            break;
                        }

                        if (line.Contains("bestmove "))
                        {
                            string mvstr = line.Substring(9, 4);
                            cli_cch.makemove(m_match_id, COORD_MOVE(mvstr), score);
                            break;
                        }

                        if (line.Contains("nobestmove"))
                        {
                            cli.resign_req(m_match_id);
                            break;
                        }
                    }
                }
            }
        }

        private int MOVE(int sqSrc, int sqDst)
        {
            return sqSrc + (sqDst << 8);
        }

        private int COORD_XY(int x, int y)
        {
            return x + (y << 4);
        }

        private ushort COORD_MOVE(string strmv)
        {
            const int FILE_LEFT = 3;
            const int RANK_TOP = 3;
            int sqSrc, sqDst;
            sqSrc = COORD_XY(strmv[0] - 'a' + FILE_LEFT, '9' - strmv[1] + RANK_TOP);
            sqDst = COORD_XY(strmv[2] - 'a' + FILE_LEFT, '9' - strmv[3] + RANK_TOP);
            return (ushort)MOVE(sqSrc, sqDst);
        }

        protected override bool cbgs_accept(cbgs_client.DATA_CHALLENGE_IND ci)
        {
            if (ci.game_type == cbgs_client.GAME_TYPE.GAME_CCHESS && challenge == false)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    class cbgs_main_dxq : cbgs_main
    {
        dxq_client cli_dxq = null;
        DarkCChess.Engine engine = null;

        public cbgs_main_dxq()
        {
            cli_dxq = new dxq_client();
            engine = new DarkCChess.Engine(DarkCChess.CANNON_RULE.CANNON_RULE_SUPER);

            cli_dxq.set_userdata(0);
            cli_dxq.on_login = on_login;
            cli_dxq.on_login_guest = on_login_guest;
            cli_dxq.on_logout = on_logout;
            cli_dxq.on_query = on_query;
            cli_dxq.on_challenge = on_challenge;
            cli_dxq.on_challenge_fail = on_challenge_fail;
            cli_dxq.on_heart_beat = on_heart_beat;
            cli_dxq.on_game_over = on_game_over;
            cli_dxq.on_game_start = on_game_start;
            cli_dxq.on_user_delta = on_user_delta;
            cli_dxq.on_match_delta = on_match_delta;
            cli_dxq.on_board_ind = on_board_ind;
            cli_dxq.on_move = on_move;
            cli_dxq.on_invalid_move = on_invalid_move;
            cli_dxq.on_watch = on_watch;
            cli_dxq.on_unwatch = on_unwatch;
            cli_dxq.on_query_user = on_query_user;
            cli_dxq.on_draw = on_draw;
            cli_dxq.on_resign = on_resign;
            cli_dxq.on_network_err = on_netork_err;
            cli_dxq.on_aes_key_xchg = on_aes_key_xchg;
            cli = cli_dxq;
        }

        public bool run(string server, string user, string pass, string time_init, string time_inc)
        {
            guest_mode = false;
            my_name = user;
            my_pass = pass;
            my_server = server;
            my_time_initial = Convert.ToInt32(time_init) * 60 * 1000;
            my_time_inc = Convert.ToInt32(time_inc) * 1000;
            cbgs_login();
            return true;
        }

        public bool run(string server, string time_init, string time_inc)
        {
            guest_mode = true;
            my_server = server;
            my_time_initial = Convert.ToInt32(time_init) * 60 * 1000;
            my_time_inc = Convert.ToInt32(time_inc) * 1000;
            cbgs_login();
            return true;
        }

        public void quit()
        {
            cli.logout();
            cli.close();
            need_quit = true;
        }

        void on_board_ind(UInt32 data, UInt64 match_id, string fen, cbgs_client.DATA_TIME time, cbgs_client.DATA_TIME opptime, UInt32 lastmove, Int32 lastvalue)
        {
            Console.WriteLine("[{0}]board ind -> {1}, {2}, {3:X}, {4}", data, match_id, fen, lastmove, lastvalue);
        }

        void on_move(UInt32 data, UInt64 match_id, string fen, Int32 side, cbgs_client.DATA_TIME time, cbgs_client.DATA_TIME opptime)
        {
            Console.WriteLine("[{0}]move -> {1}, {2}, {3}", data, match_id, fen, side);
            new Thread(new ThreadStart(() => 
            {
                engine.from_fen(fen);
                DarkCChess.Move mv = engine.search_bestmove(5, 3000);
                Console.WriteLine("[{0}]makemove -> {1}{2}{3}{4}", data, mv.srcy, mv.srcx, mv.dsty, mv.dstx);
                cli_dxq.makemove(match_id, (UInt32)mv.srcy, (UInt32)mv.srcx, (UInt32)mv.dsty, (UInt32)mv.dstx, 10);
            })).Start();
        }

        protected override bool cbgs_accept(cbgs_client.DATA_CHALLENGE_IND ci)
        {
            if (ci.game_type == cbgs_client.GAME_TYPE.GAME_DARKCCHESS && challenge == false)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    class cbgs_main_oth : cbgs_main
    {
        oth_client cli_oth = null;

        public cbgs_main_oth()
        {
            cli_oth = new oth_client();

            cli_oth.set_userdata(0);
            cli_oth.on_login = on_login;
            cli_oth.on_login_guest = on_login_guest;
            cli_oth.on_logout = on_logout;
            cli_oth.on_query = on_query;
            cli_oth.on_challenge = on_challenge;
            cli_oth.on_challenge_fail = on_challenge_fail;
            cli_oth.on_heart_beat = on_heart_beat;
            cli_oth.on_game_over = on_game_over;
            cli_oth.on_game_start = on_game_start;
            cli_oth.on_user_delta = on_user_delta;
            cli_oth.on_match_delta = on_match_delta;
            cli_oth.on_board_ind = on_board_ind;
            cli_oth.on_move = on_move;
            cli_oth.on_invalid_move = on_invalid_move;
            cli_oth.on_watch = on_watch;
            cli_oth.on_unwatch = on_unwatch;
            cli_oth.on_query_user = on_query_user;
            cli_oth.on_draw = on_draw;
            cli_oth.on_resign = on_resign;
            cli_oth.on_network_err = on_netork_err;
            cli_oth.on_aes_key_xchg = on_aes_key_xchg;
            cli = cli_oth;
        }

        public bool run(string server, string user, string pass, string time_init, string time_inc)
        {
            guest_mode = false;
            my_name = user;
            my_pass = pass;
            my_server = server;
            my_time_initial = Convert.ToInt32(time_init) * 60 * 1000;
            my_time_inc = Convert.ToInt32(time_inc) * 1000;
            cbgs_login();
            return true;
        }

        public bool run(string server, string time_init, string time_inc)
        {
            guest_mode = true;
            my_server = server;
            my_time_initial = Convert.ToInt32(time_init) * 60 * 1000;
            my_time_inc = Convert.ToInt32(time_inc) * 1000;
            cbgs_login();
            return true;
        }

        public void quit()
        {
            cli.logout();
            cli.close();
            need_quit = true;
        }

        void on_board_ind(UInt32 data, UInt64 match_id, string board, cbgs_client.DATA_TIME time, cbgs_client.DATA_TIME opptime, UInt32 lastmove, Int32 lastvalue)
        {
            Console.WriteLine("[{0}]board ind -> {1}, {2}, {3:X}, {4}", data, match_id, board, lastmove, lastvalue);
        }

        void on_move(UInt32 data, UInt64 match_id, string board, Int32 side, cbgs_client.DATA_TIME time, cbgs_client.DATA_TIME opptime)
        {
            Console.WriteLine("[{0}]move -> {1}, {2}, {3}", data, match_id, board, side);
            new Thread(new ThreadStart(() =>
            {
                Reversi.TBopb pb = new Reversi.TBopb(side, board);
                Reversi.CReversi engine = new Reversi.CReversi();
                var mv = engine.Go(pb, 5);
                int mx = (mv % 9) - 1;
                int my = mv / 9 - 1;
                int pass = mv != 0 ? 0 : 1;
                Console.WriteLine("[{0}]makemove -> {1},{2}", data, mx, my);
                cli_oth.makemove(match_id, (byte)mx, (byte)my, (byte)pass, 10);
            })).Start();
        }

        protected override bool cbgs_accept(cbgs_client.DATA_CHALLENGE_IND ci)
        {
            if (ci.game_type == cbgs_client.GAME_TYPE.GAME_OTHELLO && challenge == false)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
