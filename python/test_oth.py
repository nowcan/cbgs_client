import time
from cbgs_client import cbgs_client_oth


class cbgs_client_test(cbgs_client_oth):
    def __init__(self):
        super().__init__()
        self.my_turn = False
        self.new_challenge = False

    def on_query(self, ud, list_user, list_match):
        print(f"query: ud {ud} users {list_user} matches {list_match}")

    # 有用户向你挑战
    def on_challenge(self, ud, chlg):
        if chlg["game"] == self.GAME_OTHELLO:
            self.chlg = chlg
            self.new_challenge = True

    # 收到棋盘，此时应更新界面
    def on_board_ind(self, ud, match_id, board, ts_me, ts_opp, last_mv, last_val):
        print("  abcdefgh")
        for i in range(8):
            print(f"{i + 1} {board[0 + 8 * i : 8 + 8 * i]}")
        pas = last_mv & 0xFF
        x = (last_mv >> 8) & 0xFF
        y = (last_mv >> 16) & 0xFF
        print(f"last move: pass {pas} x {x} y {y}")

    # 轮到你走棋，收到此消息后应使用make_move走棋
    def on_move(self, ud, match_id, board, side, ts_me, ts_opp):
        self.match_id = match_id
        self.my_turn = True

    def send_accept(self):
        self.accept_req(self.chlg["user_id"], self.GAME_OTHELLO, True)


if __name__ == "__main__":
    cli = cbgs_client_test()
    cli.connect("nowcan.cn", 7000)
    time.sleep(1)
    cli.login("123123", "123123")
    time.sleep(1)
    cli.query()
    time.sleep(5)
    cnt = 0
    while True:
        time.sleep(0.1)
        cnt += 1
        if cnt > 10 * 60:
            cnt = 0
            cli.heartbeat()
        if cli.new_challenge:
            print(cli.chlg)
            cli.new_challenge = False
            cli.send_accept()
        if cli.my_turn:
            cli.my_turn = False
            pos = input("input pos(like e3, input pass if no move)")
            if pos == "pass":
                pas = 1
                x = -1
                y = -1
            else:
                pas = 0
                y = int(pos[1]) - 1
                x = ord(pos[0]) - ord("a")
            cli.make_move(cli.match_id, x, y, pas, 0)
    cli.logout()
    time.sleep(5)
    cli.close()
