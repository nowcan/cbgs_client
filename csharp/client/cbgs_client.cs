using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace cbgs_client_cs
{
    public abstract class cbgs_client
    {
        public static string[] GameName = { "NULL", "Chinese Chess", "Othello", "Dark Chinese Chess" };
        const int SOCKET_BUFFER = 65536;
        const int SOCKET_TIMEOUT = 600;                 //seconds?
        const int MAX_USERS = 256;
        const int MAX_NAME_LEN = 64;
        const int MAX_PASSWORD_LEN = 64;
        const int MAX_HASH_LEN = 16;
        const int CBGS_VERSION = 20230918;
        const int DATA_MSG_HEAD_LEN = 36;               //参见DATA_MSG结构体
        const UInt64 DATA_MSG_MAGIC_CODE = 0x123de7cb34a490f7;
        protected const int CHESS_SIDE_FIRST = 1;
        protected const int CHESS_SIDE_SECOND = 0;
        protected const int CHESS_SIDE_RAND = 0xff;

        public enum MSG_TYPE
        {
            MSG_NULL = 0,
            MSG_STATUS_REQ,                         //查询服务器状态请求，上行
            MSG_STATUS_IND,                         //查询服务器状态确认，下行
            MSG_LOGIN_REQ,                          //用户登陆请求，上行
            MSG_LOGIN_IND,                          //用户登陆确认，下行
            MSG_LOGOUT_REQ,                         //用户注销请求，上行
            MSG_LOGOUT_IND,                         //用户注销确认，下行
            MSG_BOARD_IND,                          //当前棋盘状态，下行
            MSG_CHESS_REQ,                          //用户走棋信息，上行
            MSG_CHESS_IND,                          //用户走棋确认，下行
            MSG_MATCH_IND,                          //游戏结果信息，下行
            MSG_CHALLENGE_REQ,                      //挑战请求，上行
            MSG_CHALLENGE_IND,                      //挑战确认，下行，下行，服务器提问
            MSG_CHALLENGE_FAIL_IND,                 //挑战失败确认
            MSG_ACCEPT_REQ,                         //是否接受挑战请求，上行，客户回答
            MSG_ACCEPT_IND,                         //是否接受挑战请求
            MSG_CHAT_REQ,                           //聊天信息，上行
            MSG_CHAT_IND,                           //聊天信息，下行
            MSG_CHAT_FAIL_IND,                      //聊天失败，找不到用户
            MSG_GAME_START_IND,                     //比赛开始，下行
            MSG_USER_DELTA_IND,                     //用户登录、注销消息，下行
            MSG_MATCH_DELTA_IND,                    //比赛开始、结束消息，下行
            MSG_HEART_BEAT_REQ,                     //客户端的心跳消息，上行
            MSG_HEART_BEAT_IND,                     //回复心跳消息，包含服务器当前时间，下行
            MSG_WATCH_REQ,                          //观战请求，上行
            MSG_WATCH_IND,                          //观战确认，下行
            MSG_UNWATCH_REQ,                        //退出观战请求，上行
            MSG_UNWATCH_IND,                        //退出观战确认，下行
            MSG_QUERY_USERS_REQ,                    //查询用户请求，上行
            MSG_QUERY_USERS_IND,                    //查询用户回复，下行
            MSG_DRAW_REQ,                           //提和请求，上行
            MSG_DRAW_IND,                           //对手提和通知，下行
            MSG_RESIGN_REQ,                         //认输请求，上行
            MSG_RESIGN_IND,                         //对手认输通知，下行
            MSG_CHANGE_PASS_REQ,                    //更改密码请求，上行
            MSG_CHANGE_PASS_IND,                    //更改密码回复，下行
            MSG_AES_KEY_XCHG_REQ,                   //AES密钥交换，上行
            MSG_AES_KEY_XCHG_IND,                   //AES密钥交换，下行
            MSG_USER_ID_AUTH_REQ,                   //用户认证，上行
            MSG_USER_ID_AUTH_IND,                   //用户认证，下行
            MSG_BOARD_OCR_REQ,                      //棋盘识别请求，上行
            MSG_BOARD_OCR_WORK_IND,                 //棋盘识别进行中回复，下行
            MSG_BOARD_OCR_DONE_IND,                 //棋盘识别完成回复，下行
            MSG_CMD_REQ,                            //命令信息，上行
            MSG_CMD_IND,                            //命令信息，下行
            MSG_CMD_FAIL_IND,                       //命令失败，找不到用户
            MSG_LOGIN_GUEST_REQ,                    //用户登陆请求，上行
            MSG_LOGIN_GUEST_IND,                    //用户登陆确认，下行
            MSG_MAX = 0xfffffff
        };

        public enum GAME_TYPE
        {
            GAME_NULL = 0,
            GAME_CCHESS,                            //中国象棋
            GAME_OTHELLO,                           //黑白棋
            GAME_DARKCCHESS,                        //翻翻棋
            GAME_END,
            GAME_MAX = 0xfffffff
        };

        public enum LOGIN_RESULT
        {
            LOGIN_OK = 0,
            LOGIN_PASSWORD_INVALID,
            LOGIN_ALREADY_ONLINE,
            LOGIN_VERSION_NOT_MATCH,
            LOGIN_MAX = 0xfffffff
        };

        public enum LOGOUT_RESULT
        {
            LOGOUT_OK = 0,
            LOGOUT_USER_INVALID,
            LOGOUT_NOT_ONLINE,
            LOGOUT_MAX = 0xfffffff
        };

        public enum CHANGE_PASSWORD_RESULT
        {
            CHANGE_PASSWORD_OK = 0,
            CHANGE_PASSWORD_USER_NOT_FOUND = 1,
            CHANGE_PASSWORD_INVALID_OLD = 2,
            CHANGE_PASSWORD_FAIL = 3,
            CHANGE_PASSWORD_GUEST_NOT_ALLOW = 4,
            CHANGE_PASSWORD_MAX = 0xfffffff
        };

        public enum CHALLENGE_RESULT
        {
            CHALLENGE_ACCEPT = 0,                   //接受挑战
            CHALLENGE_DENIED,                       //拒绝挑战
            CHALLENGE_OFFLINE,                      //对手离线
            CHALLENGE_NOT_VALID,                    //挑战已失效
            CHALLENGE_MAX = 0xfffffff
        };

        public enum CHALLENGE_DENINED_REASON
        {
            DENINED_NO_SUCH_USER = 0,
            DENINED_USER_IS_BUSY,
            DENINED_PENDING,
            DENINED_NO_ACK,
            DENINED_MAX = 0xfffffff
        };

        public enum WATCH_RESULT
        {
            WATCH_OK = 0,
            WATCH_FAIL,
            WATCH_MAX = 0xfffffff
        };

        public enum UNWATCH_RESULT
        {
            UNWATCH_OK = 0,
            UNWATCH_FAIL,
            UNWATCH_MAX = 0xfffffff
        };

        public enum GAME_RESULT
        {
            RES_NULL = 0,
            RES_WIN,
            RES_DRAW,
            RES_LOSS,
            RES_TIMEOUT,
            RES_RESIGN,
            RES_OPP_RESIGN,                         //对手认输
            RES_OPP_EXIT,                           //对手退出
            RES_OPP_TIMEOUT,                        //对手超时
            RES_GOING,                              //对局中
            RES_MAX = 0xfffffff
        };

        public enum USER_ID_AUTH_RESULT
        {
            AUTH_OK = 0,
            AUTH_FAIL,                              //认证失败
            AUTH_NOT_FOUND,                         //找不到对应的HWID
            AUTH_EXPIRE,                            //认证信息过期
            AUTH_GUEST_NOT_ALLOW,                   //游客不能认证身份
            AUTH_MAX = 0xfffffff
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct DATA_USER_SCORE
        {
            public UInt32 win;                            //获胜次数，对手退出算获胜
            public UInt32 loss;                           //失败次数
            public UInt32 draw;                           //平局次数
            public Double score;                          //分值
            public Double k;                              //方差
        };

        public struct DATA_TIME
        {
            public Int32 initial;                        //初始时限(ms)
            public Int32 inc;                            //每步加时(ms)
            public Int32 dead;                           //超时时限(ms)
        };

        public struct DATA_USER
        {
            public string name;                            //用户名
            public UInt64 login_time;                      //登陆时刻(基准1970.1.1，即函数time的返回值)
            public UInt64 user_id;                         //用户ID,根据此ID可以得到相应用户的socket
            public Int32 chess_side;                       //你执黑还是白，客户端填写无效
            public DATA_TIME time;                         //时限，客户端填写无效
            public List<DATA_USER_SCORE> score;            //用户的分值，客户端填写无效
        };

        public struct DATA_USER_DELTA_IND
        {
            public DATA_USER user;                         //用户信息
            public UInt32 login;                           //1 － 登录，0 － 注销
        };

        public struct DATA_CHALLENGE_IND
        {
            public DATA_USER user;                         //谁向你挑战
            public GAME_TYPE game_type;
            public Int32 chess_side;                       //你执黑还是白
            public DATA_TIME time;                         //时限
        };

        public struct DATA_MATCH
        {
            public UInt64 id;
            public GAME_TYPE game_type;
            public DATA_USER user1;                        //游戏两方
            public DATA_USER user2;
            public UInt64 chess_user;                      //走棋方的ID
            public DATA_TIME time1;
            public DATA_TIME time2;
            public UInt32 time1_dead;                      //超时容限1，客户端勿使用
            public UInt32 time2_dead;                      //超时容限2，客户端勿使用
            public UInt64 game_core;                       //用于分析走法、判断游戏结束等，客户端勿使用，服务器64位，指针64bit
            public UInt64 watchers;                        //观战用户列表，客户端勿用
            public UInt32 user1_draw_req;                  //用户1提和请求
            public UInt32 user2_draw_req;                  //用户2提和请求
            public DATA_TIME game_time;                    //比赛时限
        };

        public struct DATA_MATCH_DELTA_IND
        {
            public DATA_MATCH match;                       //比赛信息
            public UInt32 add;                             //1 － 开始，0 － 结束
        };

        public struct DATA_HEART_BEAT_IND
        {
            public UInt64 time_stamp;                      //回复心跳消息，包含服务器当前时间
        };

        public struct DATA_WATCH_IND
        {
            public UInt32 res;
        };

        public struct DATA_UNWATCH_IND
        {
            public UInt32 res;
        };

        public delegate void OnLogin(UInt32 data, UInt32 version, UInt32 result);                           //用户登陆确认信息，0－成功，1－密码不对，2 - 用户已登录
        public delegate void OnLoginGuest(UInt32 data, UInt32 version, string user_name, UInt32 result);    //临时用户登陆确认信息，1－成功
        public delegate void OnQuery(UInt32 data, List<DATA_USER> Users, List<DATA_MATCH> Matchs);          //查询服务器状态
        public delegate void OnLogout(UInt32 data);                                                         //用户注销
        public delegate void OnChallenge(UInt32 data, DATA_CHALLENGE_IND Challenge);                        //有用户向你挑战
        public delegate void OnChallengeFail(UInt32 data);                                                  //向别人挑战失败
        public delegate void OnInvalidMove(UInt32 data, UInt64 match_id);                                   //走法不合法
        public delegate void OnGameOver(UInt32 data, UInt64 match_id, GAME_RESULT res);                     //比赛结束，给出结果
        public delegate void OnChat(UInt32 data, DATA_USER user, byte[] msg, UInt32 msg_len);               //聊天信息来自user
        public delegate void OnChatFail(UInt32 data, string user);                                          //聊天失败
        public delegate void OnNetworkErr(UInt32 data);                                                     //网络断线等错误
        public delegate void OnGameStart(UInt32 data, DATA_MATCH match);                                    //比赛开始
        public delegate void OnUserDelta(UInt32 data, DATA_USER_DELTA_IND udi);                             //用户登录、注销
        public delegate void OnMatchDelta(UInt32 data, DATA_MATCH_DELTA_IND mdi);                           //比赛开始、结束
        public delegate void OnHeartBeat(UInt32 data, DATA_HEART_BEAT_IND hbi);                             //服务器回复心跳消息，包含服务器时间
        public delegate void OnWatch(UInt32 data, DATA_WATCH_IND wi);                                       //服务器回复观战信息
        public delegate void OnUnWatch(UInt32 data, DATA_UNWATCH_IND wi);                                   //服务器回复退出观战信息
        public delegate void OnQueryUser(UInt32 data, List<DATA_USER> users);                               //服务器回复用户信息
        public delegate void OnDraw(UInt32 data, UInt64 match_id);                                          //对手提和
        public delegate void OnResign(UInt32 data, UInt64 match_id);                                        //对手认输
        public delegate void PFOnChangePassword(UInt32 data, UInt32 result);                                //用户改密码
        public delegate void PFOnAesKeyXchg(UInt32 data, UInt32 result);                                    //AES密钥交换完成
        public delegate void PFOnUserIdAuth(UInt32 data, USER_ID_AUTH_RESULT result, string detail);        //用户认证完成（AES加密后base64编码，客户端收到后先base64解码，再AES解密）
        public delegate void PFOnBoardOcrWork(UInt32 data, UInt64 ocr_id);                                  //棋盘识别进行中
        public delegate void PFOnBoardOcrDone(UInt32 data, UInt64 ocr_id, UInt32 result, string fen);       //棋盘识别完成
        public delegate void OnCmd(UInt32 data, DATA_USER user, byte[] msg, UInt32 msg_len);                //命令信息来自user
        public delegate void OnCmdFail(UInt32 data, string user);                                           //命令失败

        private TcpClient cli = null;
        protected UInt32 userdata;
        private UInt64 user_id;
        private string pub_key = null;
        private byte[] aes_key = new byte[16];
        private byte[] aes_iv = new byte[16];
        private bool crypto_mode = false;

        public OnLogin on_login = null;
        public OnLoginGuest on_login_guest = null;
        public OnLogout on_logout = null;
        public OnQuery on_query = null;
        public OnChallenge on_challenge = null;
        public OnChallengeFail on_challenge_fail = null;
        public OnInvalidMove on_invalid_move = null;
        public OnGameOver on_game_over = null;
        public OnChat on_chat = null;
        public OnChatFail on_chat_fail = null;
        public OnNetworkErr on_network_err = null;
        public OnGameStart on_game_start = null;
        public OnUserDelta on_user_delta = null;
        public OnMatchDelta on_match_delta = null;
        public OnHeartBeat on_heart_beat = null;
        public OnWatch on_watch = null;
        public OnUnWatch on_unwatch = null;
        public OnQueryUser on_query_user = null;
        public OnDraw on_draw = null;
        public OnResign on_resign = null;
        public PFOnChangePassword on_change_password = null;
        public PFOnAesKeyXchg on_aes_key_xchg = null;
        public PFOnUserIdAuth on_user_id_auth = null;
        public PFOnBoardOcrDone on_board_ocr_done = null;
        public PFOnBoardOcrWork on_board_ocr_work = null;
        public OnCmd on_cmd = null;
        public OnCmdFail on_cmd_fail = null;

        protected UInt16 bytes_uint16(byte[] buf, int s)
        {
            UInt16 ret;
            ret = buf[s + 1];
            ret <<= 8;
            ret += buf[s];
            return ret;
        }

        protected void uint16_bytes(UInt16 n, ref byte[] buf, int s)
        {
            buf[s] = (byte)n;
            n >>= 8;
            buf[s + 1] = (byte)n;
        }

        protected UInt32 bytes_uint32(byte[] buf, int s)
        {
            UInt32 ret;
            ret = buf[s + 3];
            ret <<= 8;
            ret += buf[s + 2];
            ret <<= 8;
            ret += buf[s + 1];
            ret <<= 8;
            ret += buf[s];
            return ret;
        }

        protected void uint32_bytes(UInt32 n, ref byte[] buf, int s)
        {
            buf[s] = (byte)n;
            n >>= 8;
            buf[s + 1] = (byte)n;
            n >>= 8;
            buf[s + 2] = (byte)n;
            n >>= 8;
            buf[s + 3] = (byte)n;
        }

        protected UInt64 bytes_uint64(byte[] buf, int s)
        {
            UInt64 ret;
            ret = buf[s + 7];
            ret <<= 8;
            ret += buf[s + 6];
            ret <<= 8;
            ret += buf[s + 5];
            ret <<= 8;
            ret += buf[s + 4];
            ret <<= 8;
            ret += buf[s + 3];
            ret <<= 8;
            ret += buf[s + 2];
            ret <<= 8;
            ret += buf[s + 1];
            ret <<= 8;
            ret += buf[s];
            return ret;
        }

        protected void uint64_bytes(UInt64 n, ref byte[] buf, int s)
        {
            buf[s] = (byte)n;
            n >>= 8;
            buf[s + 1] = (byte)n;
            n >>= 8;
            buf[s + 2] = (byte)n;
            n >>= 8;
            buf[s + 3] = (byte)n;
            n >>= 8;
            buf[s + 4] = (byte)n;
            n >>= 8;
            buf[s + 5] = (byte)n;
            n >>= 8;
            buf[s + 6] = (byte)n;
            n >>= 8;
            buf[s + 7] = (byte)n;
        }

        protected void bytes_data_user(byte[] buf, ref DATA_USER user, int s)
        {
            byte[] usr = new byte[MAX_NAME_LEN];
            for (int i = 0; i < MAX_NAME_LEN; i++)
            {
                usr[i] = buf[i + s];
            }
            user.name = System.Text.Encoding.ASCII.GetString(usr);
            user.name = user.name.Remove(user.name.IndexOf('\0'));
            user.login_time = bytes_uint64(buf, 64 + s);
            user.user_id = bytes_uint64(buf, 72 + s);
            user.chess_side = (Int32)bytes_uint32(buf, 80 + s);
            user.time.initial = (Int32)bytes_uint32(buf, 84 + s);
            user.time.inc = (Int32)bytes_uint32(buf, 88 + s);
            user.time.dead = (Int32)bytes_uint32(buf, 92 + s);
            DATA_USER_SCORE dus;
            user.score = new List<DATA_USER_SCORE>();
            dus = (DATA_USER_SCORE)UtilConvert.MarshalConvert.BytesToStruct(buf, 96 + s, 28, typeof(DATA_USER_SCORE));
            user.score.Add(dus);
            dus = (DATA_USER_SCORE)UtilConvert.MarshalConvert.BytesToStruct(buf, 96 + s + 28, 28, typeof(DATA_USER_SCORE));
            user.score.Add(dus);
            dus = (DATA_USER_SCORE)UtilConvert.MarshalConvert.BytesToStruct(buf, 96 + s + 28 * 2, 28, typeof(DATA_USER_SCORE));
            user.score.Add(dus);
            dus = (DATA_USER_SCORE)UtilConvert.MarshalConvert.BytesToStruct(buf, 96 + s + 28 * 3, 28, typeof(DATA_USER_SCORE));
            user.score.Add(dus);
        }

        protected void bytes_data_time(byte[] buf, ref DATA_TIME tmr, int s)
        {
            tmr.initial = (Int32)bytes_uint32(buf, s);
            tmr.inc = (Int32)bytes_uint32(buf, 4 + s);
            tmr.dead = (Int32)bytes_uint32(buf, 8 + s);
        }

        protected void bytes_data_match(byte[] buf, ref DATA_MATCH match, int s)
        {
            match.id = bytes_uint64(buf, s);
            match.game_type = (GAME_TYPE)bytes_uint32(buf, 8 + s);
            bytes_data_user(buf, ref match.user1, 12 + s);
            bytes_data_user(buf, ref match.user2, 220 + s);
            match.chess_user = bytes_uint64(buf, 428 + s);
            bytes_data_time(buf, ref match.time1, 436 + s);
            bytes_data_time(buf, ref match.time2, 448 + s);
            match.time1_dead = bytes_uint32(buf, 460 + s);
            match.time2_dead = bytes_uint32(buf, 464 + s);
            match.game_core = bytes_uint64(buf, 468 + s);
            match.watchers = bytes_uint64(buf, 476 + s);
            match.user1_draw_req = bytes_uint32(buf, 484 + s);
            match.user2_draw_req = bytes_uint32(buf, 488 + s);
            bytes_data_time(buf, ref match.game_time, 492 + s);
        }

        private void thread_recv()
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5CSP = new System.Security.Cryptography.MD5CryptoServiceProvider();
            int ret, data_len, data_crypt_len;
            UInt64 magic;
            byte[] buf = new byte[SOCKET_BUFFER];
            while (true)
            {
                try
                {
                    ret = cli.Client.Receive(buf, 16, SocketFlags.None);
                    if (ret <= 0)
                    {
                        user_id = 0;
                        crypto_mode = false;
                        if (on_network_err != null)
                        {
                            on_network_err(userdata);
                        }

                        break;
                    }

                    magic = bytes_uint64(buf, 0);
                    if (magic == DATA_MSG_MAGIC_CODE)
                    {
                        int total_len;
                        data_crypt_len = (int)bytes_uint32(buf, 8);
                        data_len = (int)bytes_uint32(buf, 12);
                        ret = 0;
                        if (data_crypt_len != 0)
                        {
                            total_len = data_crypt_len;
                        }
                        else
                        {
                            total_len = data_len;
                        }

                        while (ret < total_len + 20)
                        {
                            ret += cli.Client.Receive(buf, ret, total_len + 20 - ret, SocketFlags.None);
                        }

                        byte[] buf_data = new byte[total_len];
                        byte[] buf_md5 = new byte[MAX_HASH_LEN];
                        for (int i = 0; i < total_len; i++)
                        {
                            buf_data[i] = buf[20 + i];
                        }

                        if (data_crypt_len != 0)
                        {
                            var buf_decrypt = Crypto.AESHelper.Decrypt(buf_data, aes_iv, aes_key);
                            buf_data = new byte[data_len];
                            Array.Copy(buf_decrypt, buf_data, data_len);
                        }

                        for (int i = 0; i < MAX_HASH_LEN; i++)
                        {
                            buf_md5[i] = buf[i];
                        }

                        byte[] md5 = md5CSP.ComputeHash(buf_data);
                        if (md5.SequenceEqual(buf_md5))
                        {
                            UInt32 typ;
                            typ = bytes_uint32(buf, 16);
                            switch ((MSG_TYPE)typ)
                            {
                                case MSG_TYPE.MSG_ACCEPT_IND:   //Only denied challenge is feedback.
                                case MSG_TYPE.MSG_CHALLENGE_FAIL_IND:
                                    if (on_challenge_fail != null)
                                    {
                                        on_challenge_fail(userdata);
                                    }
                                    break;

                                case MSG_TYPE.MSG_CHALLENGE_IND:
                                    if (on_challenge != null)
                                    {
                                        DATA_CHALLENGE_IND ci = new DATA_CHALLENGE_IND();

                                        bytes_data_user(buf_data, ref ci.user, 0);
                                        ci.game_type = (GAME_TYPE)bytes_uint32(buf_data, 208);
                                        ci.chess_side = (Int32)bytes_uint32(buf_data, 212);
                                        bytes_data_time(buf_data, ref ci.time, 216);
                                        on_challenge(userdata, ci);
                                    }
                                    break;

                                case MSG_TYPE.MSG_CHAT_FAIL_IND:
                                    if (on_chat_fail != null)
                                    {
                                        byte[] usr = new byte[MAX_NAME_LEN];
                                        for (int i = 0; i < MAX_NAME_LEN; i++)
                                        {
                                            usr[i] = buf_data[i];
                                        }

                                        string user = System.Text.Encoding.ASCII.GetString(usr);
                                        user = user.Remove(user.IndexOf('\0'));
                                        on_chat_fail(userdata, user);
                                    }
                                    break;

                                case MSG_TYPE.MSG_CHAT_IND:
                                    if (on_chat != null)
                                    {
                                        UInt32 msg_len = 0;
                                        DATA_USER user = new DATA_USER();
                                        bytes_data_user(buf_data, ref user, 0);
                                        msg_len = bytes_uint32(buf_data, 208);
                                        byte[] msg = new byte[msg_len];
                                        for (int i = 0; i < msg_len; i++)
                                        {
                                            msg[i] = buf_data[i + 212];
                                        }

                                        on_chat(userdata, user, msg, msg_len);
                                    }
                                    break;

                                case MSG_TYPE.MSG_CHESS_IND:
                                    if (on_invalid_move != null)
                                    {
                                        UInt64 match_id;
                                        UInt32 chess_ok;
                                        match_id = bytes_uint64(buf_data, 0);
                                        chess_ok = bytes_uint32(buf_data, 8);
                                        if (chess_ok == 1)
                                        {
                                            on_invalid_move(userdata, match_id);
                                        }
                                    }
                                    break;

                                case MSG_TYPE.MSG_GAME_START_IND:
                                    if (on_game_start != null)
                                    {
                                        DATA_MATCH match = new DATA_MATCH();

                                        bytes_data_match(buf_data, ref match, 0);
                                        on_game_start(userdata, match);
                                    }
                                    break;

                                case MSG_TYPE.MSG_HEART_BEAT_IND:
                                    if (on_heart_beat != null)
                                    {
                                        DATA_HEART_BEAT_IND hbi = new DATA_HEART_BEAT_IND();
                                        hbi.time_stamp = bytes_uint64(buf_data, 0);
                                        on_heart_beat(userdata, hbi);
                                    }
                                    break;

                                case MSG_TYPE.MSG_WATCH_IND:
                                    if (on_watch != null)
                                    {
                                        DATA_WATCH_IND wi = new DATA_WATCH_IND();
                                        wi.res = bytes_uint32(buf_data, 0);
                                        on_watch(userdata, wi);
                                    }
                                    break;

                                case MSG_TYPE.MSG_UNWATCH_IND:
                                    if (on_unwatch != null)
                                    {
                                        DATA_UNWATCH_IND ui = new DATA_UNWATCH_IND();
                                        ui.res = bytes_uint32(buf_data, 0);
                                        on_unwatch(userdata, ui);
                                    }
                                    break;

                                case MSG_TYPE.MSG_LOGIN_IND:
                                    if (on_login != null)
                                    {
                                        UInt32 login_ok, server_ver;
                                        login_ok = bytes_uint32(buf_data, 0);
                                        server_ver = bytes_uint32(buf_data, 4);
                                        if (login_ok == 0)
                                        {
                                            user_id = bytes_uint64(buf_data, 8);
                                            pub_key = Encoding.ASCII.GetString(buf_data, 16, data_len - 16 - 1);
                                        }
                                        else
                                        {
                                            pub_key = null;
                                        }

                                        on_login(userdata, server_ver, login_ok);
                                    }
                                    break;

                                case MSG_TYPE.MSG_LOGIN_GUEST_IND:
                                    if (on_login_guest != null)
                                    {
                                        UInt32 login_ok, server_ver;
                                        string user_name = null;
                                        login_ok = bytes_uint32(buf_data, 0);
                                        server_ver = bytes_uint32(buf_data, 4);
                                        if (login_ok == 0)
                                        {
                                            user_id = bytes_uint64(buf_data, 8);
                                            user_name = Encoding.ASCII.GetString(buf_data, 16, 64);
                                            pub_key = Encoding.ASCII.GetString(buf_data, 80, data_len - 80 - 1);
                                        }
                                        else
                                        {
                                            pub_key = null;
                                        }

                                        on_login_guest(userdata, server_ver, user_name, login_ok);
                                    }
                                    break;

                                case MSG_TYPE.MSG_LOGOUT_IND:
                                    if (on_logout != null)
                                    {
                                        UInt32 logout_ok;
                                        logout_ok = bytes_uint32(buf_data, 0);
                                        user_id = 0;
                                        crypto_mode = false;
                                        if (logout_ok == 0)
                                        {
                                            on_logout(userdata);
                                        }
                                    }
                                    break;

                                case MSG_TYPE.MSG_MATCH_DELTA_IND:
                                    if (on_match_delta != null)
                                    {
                                        DATA_MATCH_DELTA_IND mdi = new DATA_MATCH_DELTA_IND();
                                        bytes_data_match(buf_data, ref mdi.match, 0);
                                        mdi.add = bytes_uint32(buf_data, 504);
                                        on_match_delta(userdata, mdi);
                                    }
                                    break;

                                case MSG_TYPE.MSG_MATCH_IND:
                                    if (on_game_over != null)
                                    {
                                        UInt64 id;
                                        GAME_RESULT res;
                                        id = bytes_uint64(buf_data, 0);
                                        res = (GAME_RESULT)bytes_uint32(buf_data, 8);
                                        on_game_over(userdata, id, res);
                                    }
                                    break;

                                case MSG_TYPE.MSG_STATUS_IND:
                                    if (on_query != null)
                                    {
                                        List<DATA_USER> lst_usr = new List<DATA_USER>();
                                        List<DATA_MATCH> lst_match = new List<DATA_MATCH>();
                                        DATA_USER u = new DATA_USER();
                                        DATA_MATCH m = new DATA_MATCH();
                                        UInt32 users, matchs;
                                        int offset;
                                        users = bytes_uint32(buf_data, 0);
                                        matchs = bytes_uint32(buf_data, 4);
                                        for (int i = 0; i < users; i++)
                                        {
                                            bytes_data_user(buf_data, ref u, 8 + i * 208);
                                            lst_usr.Add(u);
                                        }

                                        offset = 8 + (int)users * 208;
                                        for (int i = 0; i < matchs; i++)
                                        {
                                            bytes_data_match(buf_data, ref m, offset + i * 504);
                                            lst_match.Add(m);
                                        }

                                        on_query(userdata, lst_usr, lst_match);
                                    }
                                    break;

                                case MSG_TYPE.MSG_QUERY_USERS_IND:
                                    if (on_query_user != null)
                                    {
                                        List<DATA_USER> lst_usr = new List<DATA_USER>();
                                        DATA_USER u = new DATA_USER();
                                        UInt32 users;
                                        users = bytes_uint32(buf_data, 0);
                                        for (int i = 0; i < users; i++)
                                        {
                                            bytes_data_user(buf_data, ref u, 4 + i * 208);
                                            lst_usr.Add(u);
                                        }

                                        on_query_user(userdata, lst_usr);
                                    }
                                    break;

                                case MSG_TYPE.MSG_USER_DELTA_IND:
                                    if (on_user_delta != null)
                                    {
                                        DATA_USER_DELTA_IND udi = new DATA_USER_DELTA_IND();
                                        bytes_data_user(buf_data, ref udi.user, 0);
                                        udi.login = bytes_uint32(buf_data, 208);
                                        on_user_delta(userdata, udi);
                                    }
                                    break;

                                case MSG_TYPE.MSG_DRAW_IND:
                                    if (on_draw != null)
                                    {
                                        on_draw(userdata, bytes_uint64(buf_data, 0));
                                    }
                                    break;

                                case MSG_TYPE.MSG_RESIGN_IND:
                                    if (on_resign != null)
                                    {
                                        on_resign(userdata, bytes_uint64(buf_data, 0));
                                    }
                                    break;

                                case MSG_TYPE.MSG_CHANGE_PASS_IND:
                                    if (on_change_password != null)
                                    {
                                        on_change_password(userdata, bytes_uint32(buf_data, 0));
                                    }
                                    break;

                                case MSG_TYPE.MSG_AES_KEY_XCHG_IND:
                                    if (bytes_uint32(buf_data, 0) == 0)
                                    {
                                        crypto_mode = true;
                                    }

                                    if (on_aes_key_xchg != null)
                                    {
                                        on_aes_key_xchg(userdata, bytes_uint32(buf_data, 0));
                                    }
                                    break;

                                case MSG_TYPE.MSG_USER_ID_AUTH_IND:
                                    if (on_user_id_auth != null)
                                    {
                                        var result = bytes_uint32(buf_data, 0);
                                        if ((USER_ID_AUTH_RESULT)result == USER_ID_AUTH_RESULT.AUTH_OK)
                                        {
                                            var detail = Encoding.ASCII.GetString(buf_data, 4, data_len - 4 - 1);
                                            on_user_id_auth(userdata, (USER_ID_AUTH_RESULT)result, detail);
                                        }
                                        else
                                        {
                                            on_user_id_auth(userdata, (USER_ID_AUTH_RESULT)result, null);
                                        }
                                    }
                                    break;

                                case MSG_TYPE.MSG_BOARD_OCR_WORK_IND:
                                    if (on_board_ocr_work != null)
                                    {
                                        on_board_ocr_work(userdata, bytes_uint64(buf_data, 0));
                                    }
                                    break;

                                case MSG_TYPE.MSG_BOARD_OCR_DONE_IND:
                                    if (on_board_ocr_done != null)
                                    {
                                        var id = bytes_uint64(buf_data, 0);
                                        var result = bytes_uint32(buf_data, 8);
                                        var fen = Encoding.ASCII.GetString(buf_data, 12, data_len - 12 - 1);
                                        on_board_ocr_done(userdata, id, result, fen);
                                    }
                                    break;

                                case MSG_TYPE.MSG_CMD_FAIL_IND:
                                    if (on_cmd_fail != null)
                                    {
                                        byte[] usr = new byte[MAX_NAME_LEN];
                                        for (int i = 0; i < MAX_NAME_LEN; i++)
                                        {
                                            usr[i] = buf_data[i];
                                        }

                                        string user = System.Text.Encoding.ASCII.GetString(usr);
                                        user = user.Remove(user.IndexOf('\0'));
                                        on_cmd_fail(userdata, user);
                                    }
                                    break;

                                case MSG_TYPE.MSG_CMD_IND:
                                    if (on_cmd != null)
                                    {
                                        UInt32 msg_len = 0;
                                        DATA_USER user = new DATA_USER();
                                        bytes_data_user(buf_data, ref user, 0);
                                        msg_len = bytes_uint32(buf_data, 208);
                                        byte[] msg = new byte[msg_len];
                                        for (int i = 0; i < msg_len; i++)
                                        {
                                            msg[i] = buf_data[i + 212];
                                        }

                                        on_cmd(userdata, user, msg, msg_len);
                                    }
                                    break;

                                default:
                                    handle_special_msg(buf_data, (MSG_TYPE)typ);
                                    break;
                            }
                        }
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        protected int send_data(byte[] buf, MSG_TYPE type)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5CSP = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] md5 = md5CSP.ComputeHash(buf);
            UInt32 len = (UInt32)buf.Length;
            byte[] buf_all = null;
            UInt32 tp = (UInt32)type;

            if (crypto_mode)
            {
                UInt32 crypt_len = (UInt32)len;

                if ((crypt_len % 16) != 0)
                {
                    crypt_len /= 16;
                    crypt_len++;
                    crypt_len *= 16;
                }

                buf_all = new byte[DATA_MSG_HEAD_LEN + crypt_len];
                uint32_bytes(crypt_len, ref buf_all, 8);
                Crypto.AESHelper.Encrypt(buf, aes_iv, aes_key).CopyTo(buf_all, DATA_MSG_HEAD_LEN);
            }
            else
            {
                buf_all = new byte[DATA_MSG_HEAD_LEN + buf.Length];
                uint32_bytes(0, ref buf_all, 8);
                buf.CopyTo(buf_all, DATA_MSG_HEAD_LEN);
            }

            uint64_bytes(DATA_MSG_MAGIC_CODE, ref buf_all, 0);
            uint32_bytes(len, ref buf_all, 12);
            md5.CopyTo(buf_all, 16);
            uint32_bytes(tp, ref buf_all, 32);
            try
            {
                return cli.Client.Send(buf_all);
            }
            catch
            {
                return 0;
            }
        }

        public int open(string host, int port)  //链接到服务器
        {
            try
            {
                cli = new TcpClient(host, port);
                Thread thread = new Thread(new ThreadStart(thread_recv));
                thread.Start();
                return 1;
            }
            catch
            {
                return 0;
            }
        }

        public int close()  //断开连接
        {
            if (cli != null)
            {
                cli.Close();
            }

            return 0;
        }

        public int login_with_md5(string user, string md5psw)    //登陆，首次登陆会自动注册
        {
            byte[] md5 = System.Text.Encoding.ASCII.GetBytes(md5psw);
            byte[] usr = System.Text.Encoding.ASCII.GetBytes(user);
            UInt32 ver = CBGS_VERSION;
            byte[] buf = new byte[132];
            buf[0] = (byte)ver;
            ver >>= 8;
            buf[1] = (byte)ver;
            ver >>= 8;
            buf[2] = (byte)ver;
            ver >>= 8;
            buf[3] = (byte)ver;
            usr.CopyTo(buf, 4);
            md5.CopyTo(buf, 4 + MAX_NAME_LEN);
            if (send_data(buf, MSG_TYPE.MSG_LOGIN_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int login(string user, string passwd, ref string md5psw)    //登陆，首次登陆会自动注册
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5CSP = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] psw = System.Text.Encoding.ASCII.GetBytes(passwd);
            md5psw = UtilConvert.NumberConvert.bytes_to_hex_string(md5CSP.ComputeHash(psw));
            return login_with_md5(user, md5psw);
        }

        public int login_guest()    //游客登陆
        {
            UInt32 ver = CBGS_VERSION;
            byte[] buf = new byte[4];
            buf[0] = (byte)ver;
            ver >>= 8;
            buf[1] = (byte)ver;
            ver >>= 8;
            buf[2] = (byte)ver;
            ver >>= 8;
            buf[3] = (byte)ver;
            if (send_data(buf, MSG_TYPE.MSG_LOGIN_GUEST_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int logout() //注销
        {
            if (user_id != 0)
            {
                byte[] buf = new byte[8];
                uint64_bytes(user_id, ref buf, 0);
                if (send_data(buf, MSG_TYPE.MSG_LOGOUT_REQ) > 0)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public int query()  //查询用户列表和比赛列表
        {
            byte[] buf = new byte[4];
            uint32_bytes(0, ref buf, 0);
            if (send_data(buf, MSG_TYPE.MSG_STATUS_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int challenge(string name, GAME_TYPE GameType, DATA_TIME time, Int32 turn)   //发起挑战，turn为CHESS_SIDE_RAND则自动分配先后手
        {
            byte[] buf = new byte[84];
            byte[] opp = System.Text.Encoding.ASCII.GetBytes(name);
            opp.CopyTo(buf, 0);
            uint32_bytes((UInt32)GameType, ref buf, 64);
            uint32_bytes((UInt32)turn, ref buf, 68);
            uint32_bytes((UInt32)time.initial, ref buf, 72);
            uint32_bytes((UInt32)time.inc, ref buf, 76);
            uint32_bytes((UInt32)time.dead, ref buf, 80);
            if (send_data(buf, MSG_TYPE.MSG_CHALLENGE_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int send_accept_req(DATA_CHALLENGE_IND ci, UInt32 accept)    //接受挑战，accept=1接受，accept=0拒绝
        {
            byte[] buf = new byte[216];
            byte[] usr = System.Text.Encoding.ASCII.GetBytes(ci.user.name);
            uint32_bytes((UInt32)(accept != 0 ? CHALLENGE_RESULT.CHALLENGE_ACCEPT : CHALLENGE_RESULT.CHALLENGE_DENIED), ref buf, 0);
            usr.CopyTo(buf, 4);
            uint64_bytes(ci.user.login_time, ref buf, 68);
            uint64_bytes(ci.user.user_id, ref buf, 76);
            uint32_bytes((UInt32)ci.user.chess_side, ref buf, 84);
            uint32_bytes((UInt32)ci.user.time.initial, ref buf, 88);
            uint32_bytes((UInt32)ci.user.time.inc, ref buf, 92);
            uint32_bytes((UInt32)ci.user.time.dead, ref buf, 96);
            uint32_bytes((UInt32)ci.game_type, ref buf, 212);
            if (send_data(buf, MSG_TYPE.MSG_ACCEPT_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int chat(string name, string msg)    //发起聊天
        {
            UInt32 msg_len = (UInt32)msg.Length;
            byte[] buf = new byte[msg_len + 64 + 4 + 1];
            byte[] usr = System.Text.Encoding.ASCII.GetBytes(name);
            byte[] sz = System.Text.Encoding.ASCII.GetBytes(msg);
            usr.CopyTo(buf, 0);
            uint32_bytes(msg_len, ref buf, 64);
            sz.CopyTo(buf, 68);
            if (send_data(buf, MSG_TYPE.MSG_CHAT_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int watch(UInt64 match_id)
        {
            byte[] buf = new byte[8];
            uint64_bytes(match_id, ref buf, 0);
            if (send_data(buf, MSG_TYPE.MSG_WATCH_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int unwatch(UInt64 match_id)
        {
            byte[] buf = new byte[8];
            uint64_bytes(match_id, ref buf, 0);
            if (send_data(buf, MSG_TYPE.MSG_UNWATCH_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int query_users(GAME_TYPE game_type, UInt32 rank_begin, UInt32 numbers)
        {
            byte[] buf = new byte[12];
            uint32_bytes((UInt32)game_type, ref buf, 0);
            uint32_bytes(rank_begin, ref buf, 4);
            uint32_bytes(numbers, ref buf, 8);
            if (send_data(buf, MSG_TYPE.MSG_QUERY_USERS_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int heartbeat()  //发送心跳信号，通常客户端需要1分钟左右发送一次心跳信号，避免被服务器强制下线
        {
            byte[] buf = new byte[4];
            uint32_bytes(0, ref buf, 0);
            if (send_data(buf, MSG_TYPE.MSG_HEART_BEAT_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int draw_req(UInt64 match_id)
        {
            byte[] buf = new byte[8];
            uint64_bytes(match_id, ref buf, 0);
            if (send_data(buf, MSG_TYPE.MSG_DRAW_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int resign_req(UInt64 match_id)
        {
            byte[] buf = new byte[8];
            uint64_bytes(match_id, ref buf, 0);
            if (send_data(buf, MSG_TYPE.MSG_RESIGN_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }


        public int change_password(string old_pwd, string new_pwd)
        {
            byte[] buf = new byte[128];
            System.Security.Cryptography.MD5CryptoServiceProvider md5CSP = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] old_psw = System.Text.Encoding.ASCII.GetBytes(old_pwd);
            byte[] old_md5 = System.Text.Encoding.ASCII.GetBytes(UtilConvert.NumberConvert.bytes_to_hex_string(md5CSP.ComputeHash(old_psw)));
            byte[] new_psw = System.Text.Encoding.ASCII.GetBytes(new_pwd);
            byte[] new_md5 = System.Text.Encoding.ASCII.GetBytes(UtilConvert.NumberConvert.bytes_to_hex_string(md5CSP.ComputeHash(new_psw)));
            old_md5.CopyTo(buf, 0);
            new_md5.CopyTo(buf, 64);
            if (send_data(buf, MSG_TYPE.MSG_CHANGE_PASS_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int aes_key_xchg_req()
        {
            byte[] key_buf = new byte[32];//KEY + IV

            new Random().NextBytes(aes_key);
            new Random().NextBytes(aes_iv);
            aes_key.CopyTo(key_buf, 0);
            aes_iv.CopyTo(key_buf, 16);
            var base64key = Crypto.Base64Helper.Encode(key_buf);
            var rsakey = Crypto.RSAHelper.ImportCryptoppPubKey(Crypto.Base64Decoder.Decoder.GetDecoded(pub_key));
            var cryptkey = Crypto.RSAHelper.Encrypt(Encoding.ASCII.GetBytes(base64key), rsakey);
            var cryptkeybase64 = Crypto.Base64Helper.Encode(cryptkey);
            var cryptkeybytes = Encoding.ASCII.GetBytes(cryptkeybase64);

            if (send_data(cryptkeybytes, MSG_TYPE.MSG_AES_KEY_XCHG_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int user_id_auth_req(string req) //RSA加密后的用户ID信息（base64编码，解码后依次为AES KEY、AES IV、硬件序号(base64 string)）
        {
            byte[] dat = new byte[req.Length + 1];
            System.Text.Encoding.ASCII.GetBytes(req, 0, req.Length, dat, 0);

            if (send_data(dat, MSG_TYPE.MSG_USER_ID_AUTH_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int board_ocr_req(byte[] img, UInt32 len, UInt64 id, UInt32 type,
            UInt32 left_top_x, UInt32 left_top_y, UInt32 right_top_x, UInt32 right_top_y,
            UInt32 left_bottom_x, UInt32 left_bottom_y, UInt32 right_bottom_x, UInt32 right_bottom_y)
        {
            byte[] dat = new byte[8 + 4 * 8 + 4 + 4 + len];

            uint64_bytes(id, ref dat, 0);
            uint32_bytes(left_top_x, ref dat, 8);
            uint32_bytes(left_top_y, ref dat, 12);
            uint32_bytes(right_top_x, ref dat, 16);
            uint32_bytes(right_top_y, ref dat, 20);
            uint32_bytes(left_bottom_x, ref dat, 24);
            uint32_bytes(left_bottom_y, ref dat, 28);
            uint32_bytes(right_bottom_x, ref dat, 32);
            uint32_bytes(right_bottom_y, ref dat, 36);
            uint32_bytes(len, ref dat, 40);
            uint32_bytes(type, ref dat, 44);
            img.CopyTo(dat, 48);

            if (send_data(dat, MSG_TYPE.MSG_BOARD_OCR_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int send_cmd(string name, string msg)    //发起命令
        {
            UInt32 msg_len = (UInt32)msg.Length;
            byte[] buf = new byte[msg_len + 64 + 4 + 1];
            byte[] usr = System.Text.Encoding.ASCII.GetBytes(name);
            byte[] sz = System.Text.Encoding.ASCII.GetBytes(msg);
            usr.CopyTo(buf, 0);
            uint32_bytes(msg_len, ref buf, 64);
            sz.CopyTo(buf, 68);
            if (send_data(buf, MSG_TYPE.MSG_CMD_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public string get_pub_key()
        {
            return pub_key;
        }

        public void set_userdata(UInt32 data)   //设置用户数据
        {
            userdata = data;
        }

        protected abstract void handle_special_msg(Byte[] dat, MSG_TYPE type);
    }

    public class cch_client : cbgs_client
    {
        public delegate void OnBoardInd(UInt32 data, UInt64 match_id, string fen, DATA_TIME time, DATA_TIME opptime, UInt32 lastmove, Int32 lastvalue); //收到棋盘，此时应更新界面
        public delegate void OnMove(UInt32 data, UInt64 match_id, string fen, Int32 Side, DATA_TIME time, DATA_TIME opptime);         //轮到你走棋，收到此消息后应使用MakeMove走棋

        public OnBoardInd on_board_ind = null;
        public OnMove on_move = null;

        public const int CCHESS_SIDE_RED = CHESS_SIDE_FIRST;
        public const int CCHESS_SIDE_BLACK = CHESS_SIDE_SECOND;
        public const int CCHESS_SIDE_RAND = CHESS_SIDE_RAND;

        private Int32 CurrSide;

        protected override void handle_special_msg(Byte[] dat, MSG_TYPE type)
        {
            switch (type)
            {
                case MSG_TYPE.MSG_BOARD_IND:
                    Int32 value;
                    UInt32 move;
                    UInt64 match;
                    UInt32 chess_req;
                    DATA_TIME tmr1 = new DATA_TIME();
                    DATA_TIME tmr2 = new DATA_TIME();
                    string fen;

                    match = bytes_uint64(dat, 4);
                    CurrSide = (Int32)bytes_uint32(dat, 12);
                    chess_req = bytes_uint32(dat, 16);
                    bytes_data_time(dat, ref tmr1, 20);
                    bytes_data_time(dat, ref tmr2, 32);
                    value = (Int32)bytes_uint32(dat, 44);
                    move = bytes_uint32(dat, 48);
                    fen = System.Text.Encoding.ASCII.GetString(dat, 52, dat.Length - 52);
                    if (on_board_ind != null)
                    {
                        if (CurrSide == CCHESS_SIDE_RED)
                        {
                            on_board_ind(userdata, match, fen, tmr1, tmr2, move, value);
                        }
                        else if (CurrSide == CCHESS_SIDE_BLACK)
                        {
                            on_board_ind(userdata, match, fen, tmr2, tmr1, move, value);
                        }
                        else
                        {
                        }
                    }

                    if (chess_req == 1)
                    {
                        if (on_move != null)
                        {
                            if (CurrSide == CCHESS_SIDE_RED)
                            {
                                on_move(userdata, match, fen, CurrSide, tmr1, tmr2);
                            }
                            else if (CurrSide == CCHESS_SIDE_BLACK)
                            {
                                on_move(userdata, match, fen, CurrSide, tmr2, tmr1);
                            }
                            else
                            {
                            }
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        public int makemove(UInt64 match_id, UInt32 move, Int32 value)   //走一步棋
        {
            byte[] buf = new byte[24];
            uint32_bytes((UInt32)GAME_TYPE.GAME_CCHESS, ref buf, 0);
            uint64_bytes(match_id, ref buf, 4);
            uint32_bytes((UInt32)CurrSide, ref buf, 12);
            uint32_bytes((UInt32)value, ref buf, 16);
            uint32_bytes(move, ref buf, 20);
            if (send_data(buf, MSG_TYPE.MSG_CHESS_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }

    public class oth_client : cbgs_client
    {
        /* move结构如下
            int8_t pass;
            int8_t srcx;
            int8_t dsty;
            int8_t dummy;
         */

        /* board结构如下
            int8_t [8][8];
         */

        public delegate void OnBoardInd(UInt32 data, UInt64 match_id, string board, DATA_TIME time, DATA_TIME opptime, UInt32 lastmove, Int32 lastvalue);//收到棋盘，此时应更新界面
        public delegate void OnMove(UInt32 data, UInt64 match_id, string board, Int32 side, DATA_TIME time, DATA_TIME opptime);//轮到你走棋，收到此消息后应使用MakeMove走棋

        public OnBoardInd on_board_ind = null;
        public OnMove on_move = null;

        public const int OTHELLO_SIDE_FIRST = CHESS_SIDE_FIRST;
        public const int OTHELLO_SIDE_SECOND = CHESS_SIDE_SECOND;
        public const int OTHELLO_SIDE_SPACE = 2;
        public const int OTHELLO_SIDE_RAND = CHESS_SIDE_RAND;

        private Int32 CurrSide;

        protected override void handle_special_msg(Byte[] dat, MSG_TYPE type)
        {
            switch (type)
            {
                case MSG_TYPE.MSG_BOARD_IND:
                    Int32 value;
                    UInt32 move;
                    UInt64 match;
                    UInt32 chess_req;
                    DATA_TIME tmr1 = new DATA_TIME();
                    DATA_TIME tmr2 = new DATA_TIME();
                    string board = null;

                    match = bytes_uint64(dat, 4);
                    CurrSide = (Int32)bytes_uint32(dat, 12);
                    chess_req = bytes_uint32(dat, 16);
                    bytes_data_time(dat, ref tmr1, 20);
                    bytes_data_time(dat, ref tmr2, 32);
                    value = (Int32)bytes_uint32(dat, 44);
                    move = bytes_uint32(dat, 48);
                    board = System.Text.Encoding.ASCII.GetString(dat, 52, dat.Length - 52);
                    if (on_board_ind != null)
                    {
                        if (CurrSide == OTHELLO_SIDE_FIRST)
                        {
                            on_board_ind(userdata, match, board, tmr1, tmr2, move, value);
                        }
                        else if (CurrSide == OTHELLO_SIDE_SECOND)
                        {
                            on_board_ind(userdata, match, board, tmr2, tmr1, move, value);
                        }
                        else
                        {
                        }
                    }

                    if (chess_req == 1)
                    {
                        if (on_move != null)
                        {
                            if (CurrSide == OTHELLO_SIDE_FIRST)
                            {
                                on_move(userdata, match, board, CurrSide, tmr1, tmr2);
                            }
                            else if (CurrSide == OTHELLO_SIDE_SECOND)
                            {
                                on_move(userdata, match, board, CurrSide, tmr2, tmr1);
                            }
                            else
                            {
                            }
                        }
                    }

                    break;

                default:
                    break;
            }
        }

        public int makemove(UInt64 match_id, byte x, byte y, byte pass, Int32 value)   //走一步棋
        {
            UInt32 move = 0;
            byte[] buf = new byte[24];

            move = y;
            move <<= 8;
            move |= x;
            move <<= 8;
            move |= pass;
            uint32_bytes((UInt32)GAME_TYPE.GAME_OTHELLO, ref buf, 0);
            uint64_bytes(match_id, ref buf, 4);
            uint32_bytes((UInt32)CurrSide, ref buf, 12);
            uint32_bytes((UInt32)value, ref buf, 16);
            uint32_bytes(move, ref buf, 20);
            if (send_data(buf, MSG_TYPE.MSG_CHESS_REQ) > 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }

    public class dxq_client : cbgs_client
    {
        /* move结构如下
            uint8_t srcx : 4;   //取值1~4
            uint8_t srcy : 4;   //取值1~8
            uint8_t dstx : 4;   //取值1~4
            uint8_t dsty : 4;   //取值1~8
            uint8_t cpt;
            uint8_t flip;
         */

        public delegate void OnBoardInd(UInt32 data, UInt64 match_id, string fen, DATA_TIME time, DATA_TIME opptime, UInt32 lastmove, Int32 lastvalue); //收到棋盘，此时应更新界面
        public delegate void OnMove(UInt32 data, UInt64 match_id, string fen, Int32 Side, DATA_TIME time, DATA_TIME opptime);         //轮到你走棋，收到此消息后应使用MakeMove走棋

        public OnBoardInd on_board_ind = null;
        public OnMove on_move = null;

        public const int DARKCCHESS_SIDE_FIRST = CHESS_SIDE_FIRST;
        public const int DARKCCHESS_SIDE_SECOND = CHESS_SIDE_SECOND;
        public const int DARKCCHESS_SIDE_RAND = CHESS_SIDE_RAND;

        private Int32 CurrSide;

        protected override void handle_special_msg(Byte[] dat, MSG_TYPE type)
        {
            switch (type)
            {
                case MSG_TYPE.MSG_BOARD_IND:
                    Int32 value;
                    UInt32 move;
                    UInt64 match;
                    UInt32 chess_req;
                    DATA_TIME tmr1 = new DATA_TIME();
                    DATA_TIME tmr2 = new DATA_TIME();
                    string fen;

                    match = bytes_uint64(dat, 4);
                    CurrSide = (Int32)bytes_uint32(dat, 12);
                    chess_req = bytes_uint32(dat, 16);
                    bytes_data_time(dat, ref tmr1, 20);
                    bytes_data_time(dat, ref tmr2, 32);
                    value = (Int32)bytes_uint32(dat, 44);
                    move = bytes_uint32(dat, 48);
                    fen = System.Text.Encoding.ASCII.GetString(dat, 52, dat.Length - 52);
                    if (on_board_ind != null)
                    {
                        if (CurrSide == DARKCCHESS_SIDE_FIRST)
                        {
                            on_board_ind(userdata, match, fen, tmr1, tmr2, move, value);
                        }
                        else if (CurrSide == DARKCCHESS_SIDE_SECOND)
                        {
                            on_board_ind(userdata, match, fen, tmr2, tmr1, move, value);
                        }
                        else
                        {
                        }
                    }

                    if (chess_req == 1)
                    {
                        if (on_move != null)
                        {
                            if (CurrSide == DARKCCHESS_SIDE_FIRST)
                            {
                                on_move(userdata, match, fen, CurrSide, tmr1, tmr2);
                            }
                            else if (CurrSide == DARKCCHESS_SIDE_SECOND)
                            {
                                on_move(userdata, match, fen, CurrSide, tmr2, tmr1);
                            }
                            else
                            {
                            }
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        public int makemove(UInt64 match_id, UInt32 srcx, UInt32 srcy, UInt32 dstx, UInt32 dsty, Int32 value)   //走一步棋
        {
            UInt32 move = 0;
            byte[] buf = new byte[24];

            move = dstx;
            move <<= 4;
            move |= dsty;
            move <<= 4;
            move |= srcx;
            move <<= 4;
            move |= srcy;
            //move <<= 16;
            uint32_bytes((UInt32)GAME_TYPE.GAME_DARKCCHESS, ref buf, 0);
            uint64_bytes(match_id, ref buf, 4);
            uint32_bytes((UInt32)CurrSide, ref buf, 12);
            uint32_bytes((UInt32)value, ref buf, 16);
            uint32_bytes(move, ref buf, 20);
            if (send_data(buf, MSG_TYPE.MSG_CHESS_REQ) > 0)
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
