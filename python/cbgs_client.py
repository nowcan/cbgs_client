from random import randbytes
import socket
import threading
import struct
import time
import hashlib
from abc import abstractmethod, ABC


class cbgs_client(ABC):
    GameName = ["NULL", "Chinese Chess", "Othello", "Dark Chinese Chess"]
    SOCKET_BUFFER = 65536
    SOCKET_TIMEOUT = 600  # seconds?
    MAX_USERS = 256
    MAX_NAME_LEN = 64
    MAX_PASSWORD_LEN = 64
    MAX_HASH_LEN = 16
    CBGS_VERSION = 20230918
    DATA_MSG_HEAD_LEN = 36  # 参见DATA_MSG结构体
    DATA_MSG_MAGIC_CODE = 0x123DE7CB34A490F7
    CHESS_SIDE_FIRST = 1
    CHESS_SIDE_SECOND = 0
    CHESS_SIDE_RAND = 0xFF

    MSG_NULL = 0
    MSG_STATUS_REQ = 1  # 查询服务器状态请求，上行
    MSG_STATUS_IND = 2  # 查询服务器状态确认，下行
    MSG_LOGIN_REQ = 3  # 用户登陆请求，上行
    MSG_LOGIN_IND = 4  # 用户登陆确认，下行
    MSG_LOGOUT_REQ = 5  # 用户注销请求，上行
    MSG_LOGOUT_IND = 6  # 用户注销确认，下行
    MSG_BOARD_IND = 7  # 当前棋盘状态，下行
    MSG_CHESS_REQ = 8  # 用户走棋信息，上行
    MSG_CHESS_IND = 9  # 用户走棋确认，下行
    MSG_MATCH_IND = 10  # 游戏结果信息，下行
    MSG_CHALLENGE_REQ = 11  # 挑战请求，上行
    MSG_CHALLENGE_IND = 12  # 挑战确认，下行，下行，服务器提问
    MSG_CHALLENGE_FAIL_IND = 13  # 挑战失败确认
    MSG_ACCEPT_REQ = 14  # 是否接受挑战请求，上行，客户回答
    MSG_ACCEPT_IND = 15  # 是否接受挑战请求
    MSG_CHAT_REQ = 16  # 聊天信息，上行
    MSG_CHAT_IND = 17  # 聊天信息，下行
    MSG_CHAT_FAIL_IND = 18  # 聊天失败，找不到用户
    MSG_GAME_START_IND = 19  # 比赛开始，下行
    MSG_USER_DELTA_IND = 20  # 用户登录、注销消息，下行
    MSG_MATCH_DELTA_IND = 21  # 比赛开始、结束消息，下行
    MSG_HEART_BEAT_REQ = 22  # 客户端的心跳消息，上行
    MSG_HEART_BEAT_IND = 23  # 回复心跳消息，包含服务器当前时间，下行
    MSG_WATCH_REQ = 24  # 观战请求，上行
    MSG_WATCH_IND = 25  # 观战确认，下行
    MSG_UNWATCH_REQ = 26  # 退出观战请求，上行
    MSG_UNWATCH_IND = 27  # 退出观战确认，下行
    MSG_QUERY_USERS_REQ = 28  # 查询用户请求，上行
    MSG_QUERY_USERS_IND = 29  # 查询用户回复，下行
    MSG_DRAW_REQ = 30  # 提和请求，上行
    MSG_DRAW_IND = 31  # 对手提和通知，下行
    MSG_RESIGN_REQ = 32  # 认输请求，上行
    MSG_RESIGN_IND = 33  # 对手认输通知，下行
    MSG_CHANGE_PASS_REQ = 34  # 更改密码请求，上行
    MSG_CHANGE_PASS_IND = 35  # 更改密码回复，下行
    MSG_AES_KEY_XCHG_REQ = 36  # AES密钥交换，上行
    MSG_AES_KEY_XCHG_IND = 37  # AES密钥交换，下行
    MSG_USER_ID_AUTH_REQ = 38  # 用户认证，上行
    MSG_USER_ID_AUTH_IND = 39  # 用户认证，下行
    MSG_BOARD_OCR_REQ = 40  # 棋盘识别请求，上行
    MSG_BOARD_OCR_WORK_IND = 41  # 棋盘识别进行中回复，下行
    MSG_BOARD_OCR_DONE_IND = 42  # 棋盘识别完成回复，下行
    MSG_CMD_REQ = 43  # 命令信息，上行
    MSG_CMD_IND = 44  # 命令信息，下行
    MSG_CMD_FAIL_IND = 45  # 命令失败，找不到用户
    MSG_LOGIN_GUEST_REQ = 46  # 用户登陆请求，上行
    MSG_LOGIN_GUEST_IND = 47  # 用户登陆确认，下行
    MSG_MAX = 0xFFFFFFF

    GAME_NULL = 0
    GAME_CCHESS = 1  # 中国象棋
    GAME_OTHELLO = 2  # 黑白棋
    GAME_DARKCCHESS = 3  # 翻翻棋
    GAME_END = 4
    GAME_MAX = 0xFFFFFFF

    LOGIN_OK = 0
    LOGIN_PASSWORD_INVALID = 1
    LOGIN_ALREADY_ONLINE = 2
    LOGIN_VERSION_NOT_MATCH = 3
    LOGIN_MAX = 0xFFFFFFF

    LOGOUT_OK = 0
    LOGOUT_USER_INVALID = 1
    LOGOUT_NOT_ONLINE = 2
    LOGOUT_MAX = 0xFFFFFFF

    CHANGE_PASSWORD_OK = 0
    CHANGE_PASSWORD_USER_NOT_FOUND = 1
    CHANGE_PASSWORD_INVALID_OLD = 2
    CHANGE_PASSWORD_FAIL = 3
    CHANGE_PASSWORD_GUEST_NOT_ALLOW = 4
    CHANGE_PASSWORD_MAX = 0xFFFFFFF

    CHALLENGE_ACCEPT = 0  # 接受挑战
    CHALLENGE_DENIED = 1  # 拒绝挑战
    CHALLENGE_OFFLINE = 2  # 对手离线
    CHALLENGE_NOT_VALID = 3  # 挑战已失效
    CHALLENGE_MAX = 0xFFFFFFF

    DENINED_NO_SUCH_USER = 0
    DENINED_USER_IS_BUSY = 1
    DENINED_PENDING = 2
    DENINED_NO_ACK = 3
    DENINED_MAX = 0xFFFFFFF

    WATCH_OK = 0
    WATCH_FAIL = 1
    WATCH_MAX = 0xFFFFFFF

    UNWATCH_OK = 0
    UNWATCH_FAIL = 1
    UNWATCH_MAX = 0xFFFFFFF

    RES_NULL = 0
    RES_WIN = 1
    RES_DRAW = 2
    RES_LOSS = 3
    RES_TIMEOUT = 4
    RES_RESIGN = 5
    RES_OPP_RESIGN = 6  # Opponent resigns
    RES_OPP_EXIT = 7  # Opponent exits
    RES_OPP_TIMEOUT = 8  # Opponent times out
    RES_GOING = 9  # Game in progress
    RES_MAX = 0xFFFFFFF

    AUTH_OK = 0
    AUTH_FAIL = 1  # Authentication failed
    AUTH_NOT_FOUND = 2  # HWID not found
    AUTH_EXPIRE = 3  # Authentication information expired
    AUTH_GUEST_NOT_ALLOW = 4  # NOT allowed for guest
    AUTH_MAX = 0xFFFFFFF

    # 用户数据，服务器版本，结果（0－成功，1－密码不对，2 - 用户已登录）
    def on_login(self, ud, version, result):
        print(f"login: ud {ud} version {version} result {result}")

    # 游客登录，服务器版本，临时用户名，结果（0－成功）
    def on_login_guest(self, ud, version, user_name, result):
        print(f"login as guest: ud {ud} version {version} name {user_name} result {result}")

    # 用户注销（下线，非销户）
    def on_logout(self, ud):
        print(f"logout: ud {ud}")

    # 查询结果,用户列表，比赛列表
    def on_query(self, ud, list_user, list_match):
        print(f"query: ud {ud} users {len(list_user)} matches {len(list_match)}")

    # 有用户向你挑战
    def on_challenge(self, ud, chlg):
        print(f"challenge: ud {ud} challenge {chlg}")

    # 向别人挑战失败
    def on_challenge_fail(self, ud):
        print(f"challenge_fail: ud {ud}")

    # 走法不合法
    def on_invalid_move(self, ud, match):
        print(f"invalid_move: ud {ud} match {match}")

    # 比赛开始
    def on_game_start(self, ud, match):
        print(f"game_start: ud {ud} match {match}")

    # 比赛结束，给出结果
    def on_game_over(self, ud, match, result):
        print(f"game_over: ud {ud} match {match} result {result}")

    # 网络断线等错误
    def on_network_err(self, ud):
        print(f"network_err: ud {ud}")

    # 用户登录、注销
    def on_user_delta(self, ud, delta):
        print(f"user_delta: ud {ud} delta {delta}")

    # 比赛开始、结束
    def on_match_delta(self, ud, delta):
        print(f"match_delta: ud {ud} delta {delta}")

    # 服务器回复心跳消息，包含服务器时间
    def on_heart_beat(self, ud, hbt):
        print(f"heart_beat: ud {ud} hbt {hbt}")

    # 对手提和
    def on_draw(self, ud, match):
        print(f"draw req: ud {ud} match {match}")

    # 对手认输
    def on_resign(self, ud, match):
        print(f"resign: ud {ud} match {match}")

    def __init__(self):
        super().__init__()
        # Initialize the client with default values
        self.cli = None
        self.user_data = None
        self.user_id = 0
        self.pub_key = None
        self.aes_key = randbytes(16)
        self.aes_iv = randbytes(16)
        self.crypto_mode = False

    def connect(self, host, port):
        # Establish a TCP connection
        self.cli = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.thread_sock = threading.Thread(target=self.thread_recv)
        self.cli.connect((host, port))
        self.thread_sock.start()

    def unpack_time(self, dat):
        time_init, time_inc, time_dead = struct.unpack("<LLL", dat)
        # 初始时限，每步加时，超时时限
        return {"init": time_init, "inc": time_inc, "dead": time_dead}

    def unpack_user(self, dat):
        name, login_time, user_id, chess_side = struct.unpack("<64sQQL", dat[:84])
        tmr = self.unpack_time(dat[84:96])
        lst_scores = []
        for offset in [96, 96 + 28, 96 + 2 * 28, 96 + 3 * 28]:
            win, loss, draw, score, k = struct.unpack("<LLLdd", dat[offset : offset + 28])
            lst_scores.append((win, loss, draw, score, k))
        dec_name = b""  # bytes类型会将\0后的所有字符都包含进去，这里需要去掉多余字符
        for c in name:
            if c != 0:
                dec_name += c.to_bytes()
            else:
                break

        # 用户名，登录时间，用户ID，积分
        return {"name": dec_name.decode(), "login_time": login_time, "id": user_id, "score": lst_scores}

    def unpack_match(self, dat):
        id, typ = struct.unpack("<QL", dat[:12])
        usr1 = self.unpack_user(dat[12:220])
        usr2 = self.unpack_user(dat[220:428])
        chess_usr = struct.unpack("<Q", dat[428:436])
        tmr1 = self.unpack_time(dat[436:448])
        tmr2 = self.unpack_time(dat[448:460])
        # 比赛ID，类别，用户1，用户2，当前走棋方的用户ID，用户1时限，用户2时限
        return {"id": id, "game": typ, "user1": usr1, "user2": usr2, "chess_user": chess_usr, "user1_time": tmr1, "user2_time": tmr2}

    # 针对每种棋类处理方式不同的数据，定义了虚函数
    @abstractmethod
    def on_special_data(self, type, dat):
        pass

    def thread_recv(self):
        print("thread_recv start")
        while True:
            try:
                dat = self.receive_data(36)
                print(f"[RECV HEAD]{dat.hex()}")
                magic, crypt_len, dat_len, dat_hash, dat_type = struct.unpack("<QLL16sL", dat)
                print(f"magic {hex(magic)} crypt len {crypt_len} dat len {dat_len} type {dat_type} hash {dat_hash.hex()}")
                if magic == self.DATA_MSG_MAGIC_CODE:
                    total_len = crypt_len
                    if crypt_len == 0:
                        total_len = dat_len
                    buf = self.receive_data(total_len)
                    print(f"[RECV CONTENT]{buf.hex()}")
                    bufmd5 = hashlib.md5(buf).digest()
                    if dat_hash == bufmd5:
                        if dat_type == self.MSG_LOGIN_GUEST_IND:  # 游客登录
                            login_ok, server_ver, user_id, user_name = struct.unpack("<LLQ64s", buf[:80])
                            if login_ok == 0:
                                self.pub_key = buf[80:].decode()
                                self.user_id = user_id
                                print(f"pubkey:{self.pub_key}")
                            else:
                                self.pub_key = None
                            self.on_login_guest(self.user_data, server_ver, user_name.decode(), login_ok)
                        elif dat_type == self.MSG_LOGIN_IND:  # 用户登录
                            login_ok, server_ver, user_id = struct.unpack("<LLQ", buf[:16])
                            if login_ok == 0:
                                self.pub_key = buf[16:].decode()
                                self.user_id = user_id
                                print(f"pubkey:{self.pub_key}")
                            else:
                                self.pub_key = None
                            self.on_login(self.user_data, server_ver, login_ok)
                        elif dat_type == self.MSG_LOGOUT_IND:  # 用户下线
                            (logout_ok,) = struct.unpack("<L", buf)
                            self.user_id = 0
                            self.crypto_mode = False
                            if logout_ok == 0:  # 正常下线
                                self.on_logout(self.user_data)
                        elif dat_type == self.MSG_CHESS_IND:  # 出招
                            match_id, chess_ok = struct.unpack("<QL", buf)
                            if chess_ok == 1:  # 招法不合理
                                self.on_invalid_move(self.user_data, match_id)
                        elif dat_type == self.MSG_STATUS_IND:  # 查询用户和比赛列表
                            users, matches = struct.unpack("<LL", buf[:8])
                            lst_users = []
                            lst_matches = []
                            offset = 8
                            for i in range(users):
                                usr = self.unpack_user(buf[offset + i * 208 : offset + (i + 1) * 208])
                                lst_users.append(usr)
                            offset = 8 + users * 208
                            for i in range(matches):
                                match = self.unpack_match(buf[offset + i * 504 : offset + (i + 1) * 504])
                                lst_matches.append(match)
                            self.on_query(self.user_data, lst_users, lst_matches)
                        elif dat_type == self.MSG_CHALLENGE_IND:  # 别人发来挑战
                            usr = self.unpack_user(buf[0:208])
                            game_type, side = struct.unpack("<LL", buf[208:216])
                            tmr = self.unpack_time(buf[216:])
                            self.on_challenge(self.user_data, {"user_id": usr["id"], "game": game_type, "side": side, "time": tmr})
                        elif dat_type == self.MSG_CHALLENGE_FAIL_IND:  # 挑战别人失败
                            pass
                        elif dat_type == self.MSG_MATCH_IND:  # 比赛结束
                            pass
                        elif dat_type == self.MSG_GAME_START_IND:  # 比赛开始
                            pass
                        elif dat_type == self.MSG_HEART_BEAT_IND:  # 心跳应答
                            pass
                        elif dat_type == self.MSG_MATCH_DELTA_IND:  # 比赛列表变化
                            pass
                        elif dat_type == self.MSG_USER_DELTA_IND:  # 用户列表变化
                            pass
                        elif dat_type == self.MSG_DRAW_IND:  # 对手提和
                            pass
                        elif dat_type == self.MSG_RESIGN_IND:  # 对手认输
                            pass
                        else:
                            self.on_special_data(dat_type, buf)  # 子类消息处理
                    else:
                        print("pkg hash wrong")

            except Exception as e:
                print(f"recv error: {e.args}")
                self.on_network_err(self.user_data)
                break

    def send_data(self, data):
        # Send data over the TCP connection
        print(f"[SEND]{data.hex()}")
        self.cli.sendall(data)

    def receive_data(self, size):
        # Receive data from the TCP connection
        return self.cli.recv(size)

    def close(self):
        # Close the TCP connection
        self.cli.close()

    def send_pkg(self, dat, type):
        dat_len = len(dat)
        dat_md5 = hashlib.md5(dat).digest()
        # 数据包头信息：幻数，密文长度，明文长度，数据内容的MD5，数据类型
        all_dat = struct.pack("<QLL16sL", self.DATA_MSG_MAGIC_CODE, 0, dat_len, dat_md5, type) + dat
        self.send_data(all_dat)

    def login_guest(self):
        # 游客登录，数据内容仅有版本号
        self.send_pkg(struct.pack("<L", self.CBGS_VERSION), self.MSG_LOGIN_GUEST_REQ)

    def login(self, name, pswd):
        md5psw = hashlib.md5(pswd.encode()).hexdigest().upper()
        # 用户登录，数据内容为版本号，用户名，密码的MD5值（字符串形式）
        self.send_pkg(struct.pack("<L64s64s", self.CBGS_VERSION, name.encode(), md5psw.encode()), self.MSG_LOGIN_REQ)

    def logout(self):
        # 用户注销（下线），数据内容为用户ID
        self.send_pkg(struct.pack("<Q", self.user_id), self.MSG_LOGOUT_REQ)

    def query(self):
        # 查询，数据内容为4字节0
        self.send_pkg(struct.pack("<L", 0), self.MSG_STATUS_REQ)

    def heartbeat(self):
        # 心跳，数据内容为4字节0
        self.send_pkg(struct.pack("<L", 0), self.MSG_HEART_BEAT_REQ)

    def draw_req(self, match_id):
        # 提和，数据内容为8字节match_id
        self.send_pkg(struct.pack("<Q", match_id), self.MSG_DRAW_REQ)

    def resign_req(self, match_id):
        # 认输，数据内容为8字节match_id
        self.send_pkg(struct.pack("<Q", match_id), self.MSG_RESIGN_REQ)

    def challenge(self, user_name, game_type, ts_init, ts_inc, ts_dead, turn):
        # 发起挑战
        buf = struct.pack("<64sLLLLL", user_name.encode(), game_type, turn, ts_init, ts_inc, ts_dead)
        self.send_pkg(buf, self.MSG_CHALLENGE_REQ)

    def accept_req(self, user_id, game_type, accept):
        # 接受挑战，accept=1接受，accept=0拒绝
        self.send_pkg(
            struct.pack("<L64sQQ128sL", self.CHALLENGE_ACCEPT if accept == True else self.CHALLENGE_DENIED, b"", 0, user_id, b"", game_type),
            self.MSG_ACCEPT_REQ,
        )


class cbgs_client_oth(cbgs_client):

    def on_special_data(self, type, dat):
        if type == self.MSG_BOARD_IND:
            match_id, curr_side, chess_req = struct.unpack("<QLL", dat[4:20])
            tmr1 = self.unpack_time(dat[20:32])
            tmr2 = self.unpack_time(dat[32:44])
            last_val, last_mv = struct.unpack("<LL", dat[44:52])
            board = dat[52:].decode()
            self.curr_side = curr_side
            if curr_side == self.CHESS_SIDE_FIRST:
                self.on_board_ind(self.user_data, match_id, board, tmr1, tmr2, last_mv, last_val)
            elif curr_side == self.CHESS_SIDE_SECOND:
                self.on_board_ind(self.user_data, match_id, board, tmr2, tmr1, last_mv, last_val)
            else:
                pass
            if chess_req == 1:
                if curr_side == self.CHESS_SIDE_FIRST:
                    self.on_move(self.user_data, match_id, board, curr_side, tmr1, tmr2)
                elif curr_side == self.CHESS_SIDE_SECOND:
                    self.on_move(self.user_data, match_id, board, curr_side, tmr2, tmr1)
                else:
                    pass

    # 收到棋盘，此时应更新界面
    def on_board_ind(self, ud, match_id, board, ts_me, ts_opp, last_mv, last_val):
        print(f"om_board_ind: ud {ud} match {match_id} board {board} {ts_me} {ts_opp} {last_mv} {last_val}")

    # 轮到你走棋，收到此消息后应使用make_move走棋
    def on_move(self, ud, match_id, board, side, ts_me, ts_opp):
        print(f"om_move: ud {ud} match {match_id} board {board} side {side} {ts_me} {ts_opp}")

    def make_move(self, match_id, x, y, pas, val):
        buf = struct.pack("<LQLLBBBB", self.GAME_OTHELLO, match_id, self.curr_side, val, pas, x, y, 0)
        self.send_pkg(buf, self.MSG_CHESS_REQ)
