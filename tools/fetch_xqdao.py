#!/usr/bin/env python3
"""
从 xqdao.com 抓取《梅花谱》八局完整走法，
解析传统记谱法为坐标，并镜像转换为炮二平五视角。
"""
import re
import json
import urllib.request
import sys

# Fullwidth -> ASCII digit mapping
FW_DIGITS = str.maketrans('０１２３４５６７８９', '0123456789')

def fw_to_hw(s):
    return s.translate(FW_DIGITS)

# ============================================================
# 棋盘模拟器
# ============================================================
class Board:
    def __init__(self):
        self.grid = {}  # (row, col) -> (side, type_char, display)
        self._init_board()

    def _init_board(self):
        # Red back rank (row 0): 車馬相仕帥仕相馬車
        pieces_r = [("Red","Ju","車"),("Red","Ma","馬"),("Red","Xiang","相"),
                    ("Red","Shi","仕"),("Red","Shuai","帥"),
                    ("Red","Shi","仕"),("Red","Xiang","相"),
                    ("Red","Ma","馬"),("Red","Ju","車")]
        for c, p in enumerate(pieces_r):
            self.grid[(0,c)] = p
        self.grid[(2,1)] = ("Red","Pao","炮")
        self.grid[(2,7)] = ("Red","Pao","炮")
        for c in [0,2,4,6,8]:
            self.grid[(3,c)] = ("Red","Bing","兵")
        # Black back rank (row 9): 車馬象士将士象馬車
        pieces_b = [("Black","Ju2","車"),("Black","Ma2","馬"),("Black","Xiang2","象"),
                    ("Black","Shi2","士"),("Black","Jiang","將"),
                    ("Black","Shi2","士"),("Black","Xiang2","象"),
                    ("Black","Ma2","馬"),("Black","Ju2","車")]
        for c, p in enumerate(pieces_b):
            self.grid[(9,c)] = p
        self.grid[(7,1)] = ("Black","Pao2","砲")
        self.grid[(7,7)] = ("Black","Pao2","砲")
        for c in [0,2,4,6,8]:
            self.grid[(6,c)] = ("Black","Zu","卒")

    def get_pieces_on_col(self, side, col):
        """Return list of (row, col) for given side on given notation column."""
        result = []
        for (r,c), (s,t,d) in self.grid.items():
            if s != side: continue
            if side == "Red":
                nc = 9 - c
            else:
                nc = c + 1
            if nc == col:
                result.append((r,c))
        return result

    def move(self, fr, fc, tr, tc):
        piece = self.grid.pop((fr,fc))
        self.grid.pop((tr,tc), None)
        self.grid[(tr,tc)] = piece

RED_COL_MAP = {'九':9,'八':8,'七':7,'六':6,'五':5,'四':4,'三':3,'二':2,'一':1}
# Reverse: number -> char
RED_COL_REV = {9:'九',8:'八',7:'七',6:'六',5:'五',4:'四',3:'三',2:'二',1:'一'}

STRAIGHT_PIECES = {"Ju","Ju2","Pao","Pao2","Bing","Zu","Shuai","Jiang"}

def parse_one_move(board, side, notation):
    """
    Parse a single move notation (half-width digits, no spaces).
    Returns (fromRow, fromCol, toRow, toCol) or None.
    """
    notation = fw_to_hw(notation.strip())
    if len(notation) < 4:
        return None

    piece_char = notation[0]
    col_str = notation[1]  # Could be Chinese digit or ASCII digit
    action = notation[2]
    target_str = notation[3:]

    # Parse column number
    if side == "Red":
        col_num = RED_COL_MAP.get(col_str)
    else:
        try:
            col_num = int(col_str)
        except:
            return None

    if col_num is None:
        return None

    # Find candidate pieces
    candidates = board.get_pieces_on_col(side, col_num)
    if not candidates:
        return None

    # Pick the right one
    if len(candidates) == 1:
        fr, fc = candidates[0]
    else:
        candidates.sort(key=lambda x: x[0])
        if action == '进':
            # Advancing: pick frontmost (highest row for Red, lowest for Black)
            fr, fc = candidates[-1] if side == "Red" else candidates[0]
        elif action == '退':
            fr, fc = candidates[0] if side == "Red" else candidates[-1]
        else:
            fr, fc = candidates[0]

    piece_info = board.grid.get((fr,fc))
    is_straight = piece_info and piece_info[1] in STRAIGHT_PIECES

    # Parse target
    if action == '平':
        tr = fr
        if side == "Red":
            tc = 8 - (RED_COL_MAP.get(target_str, int(target_str)) - 1)
        else:
            tc = int(target_str) - 1

    elif action == '进':
        try:
            num = int(target_str)
        except:
            num = RED_COL_MAP.get(target_str, 1)

        if is_straight:
            # Straight: advance N rows
            tr = fr + num if side == "Red" else fr - num
            tc = fc
        else:
            # Diagonal (Ma, Xiang, Shi): target column number
            if side == "Red":
                tc = 8 - (num - 1)
            else:
                tc = num - 1
            # Estimate row change
            if side == "Red":
                tr = fr + (2 if piece_info and piece_info[1] in ("Xiang","Xiang2") else 1)
            else:
                tr = fr - (2 if piece_info and piece_info[1] in ("Xiang","Xiang2") else 1)

    elif action == '退':
        try:
            num = int(target_str)
        except:
            num = RED_COL_MAP.get(target_str, 1)

        if is_straight:
            tr = fr - num if side == "Red" else fr + num
            tc = fc
        else:
            if side == "Red":
                tc = 8 - (num - 1)
            else:
                tc = num - 1
            if side == "Red":
                tr = fr - (2 if piece_info and piece_info[1] in ("Xiang","Xiang2") else 1)
            else:
                tr = fr + (2 if piece_info and piece_info[1] in ("Xiang","Xiang2") else 1)
    else:
        return None

    # Apply move
    board.move(fr, fc, tr, tc)
    return (fr, fc, tr, tc)


def fetch_game(game_id):
    url = f"http://www.xqdao.com/qipu/show/{game_id}/"
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    try:
        resp = urllib.request.urlopen(req, timeout=15)
        html = resp.read().decode('utf-8')
    except Exception as e:
        return None

    # Extract moves paragraph
    match = re.search(r'<p\s+style="margin:30px;">([^<]+)</p>', html)
    if not match:
        return None
    return match.group(1)


def parse_move_list(raw):
    """Parse raw move text from xqdao into list of (side, notation) pairs."""
    raw = fw_to_hw(raw.strip())
    moves = []

    # Split into lines by move number pattern (e.g., "1. " or "3. ")
    # First normalize: ensure each move number is preceded by newline or start
    raw = re.sub(r'(\d+)\.\s+', r'\n\1. ', raw)
    # Remove extra newlines from normalization
    raw = raw.replace('\n\n', '\n')

    lines = raw.strip().split('\n')
    for line in lines:
        line = line.strip()
        if not line: continue
        # Remove leading number
        line = re.sub(r'^\d+\.\s+', '', line)
        # Split Red and Black moves by spaces (they're separated by space)
        parts = line.split()
        for token in parts:
            token = token.strip()
            if token and re.search(r'[一-鿿]', token):
                side = "Red" if len(moves) % 2 == 0 else "Black"
                moves.append((side, token))

    return moves


# ============================================================
GAME_IDS = {
    "MH-001": 64458, "MH-002": 64457, "MH-003": 64456, "MH-004": 64455,
    "MH-005": 64454, "MH-006": 64453, "MH-007": 64452, "MH-008": 64451,
}

GAME_TITLES = {
    "MH-001": "第一局：破巡河车吃卒用炮打象",
    "MH-002": "第二局：破炮先去象后上三路马",
    "MH-003": "第三局：破炮打象后换士上右马",
    "MH-004": "第四局：退炮破巡河车挺兵兑卒",
    "MH-005": "第五局：退炮横车破巡河车边马",
    "MH-006": "第六局：退右炮破过河车贪吃卒",
    "MH-007": "第七局：飞象进马破过河车边马",
    "MH-008": "第八局：挺马前卒破直横车边马",
}

GAME_DESC = {
    "MH-001": "红方巡河车贪吃黑卒，黑方趁机进马、用炮打红方底象，形成凌厉攻势。本局共19回合，黑胜。",
    "MH-002": "红方急于用炮打象，黑方从容应对，骑河炮妙手阻隔，逐步反夺先手。本局黑胜。",
    "MH-003": "红方炮打象后继续打士，黑方将外出后从容调整，上右马强攻。本局黑胜。",
    "MH-004": "红方巡河车挺兵兑卒，黑方退炮灵活调动，以柔克刚取势。本局黑胜。",
    "MH-005": "红方巡河车配合边马进攻，黑方退炮横车巧妙反击。本局黑胜。",
    "MH-006": "红方过河车急于吃卒，黑方退右炮诱敌深入，运子围攻。本局黑胜。",
    "MH-007": "红方过河车配合边马进攻，黑方飞象进马稳固反击。本局黑胜。",
    "MH-008": "红方直横车边马进攻，黑方挺马前卒以静制动，后发制人。本局黑胜。",
}


def main():
    all_records = []

    for rid, gid in GAME_IDS.items():
        raw = fetch_game(gid)
        if not raw:
            print(f"SKIP {rid}: no data", file=sys.stderr)
            continue

        move_list = parse_move_list(raw)
        print(f"{rid}: {len(move_list)} half-moves", file=sys.stderr)

        board = Board()
        moves_out = []
        ok = True

        for i, (side, notation) in enumerate(move_list):
            result = parse_one_move(board, side, notation)
            if result is None:
                print(f"  FAIL at move {i+1}: {side} {notation}", file=sys.stderr)
                ok = False
                break
            fr, fc, tr, tc = result

            # Mirror columns: 炮八平五 -> 炮二平五 perspective
            new_fc = 8 - fc
            new_tc = 8 - tc

            hints = None
            if side == "Black":
                hints = [f"应走{notation}"]

            moves_out.append({
                "step": i + 1,
                "side": side,
                "notation": notation,
                "fromRow": fr,
                "fromCol": new_fc,
                "toRow": tr,
                "toCol": new_tc,
                "hints": hints,
            })

        if ok and moves_out:
            all_records.append({
                "id": rid,
                "title": GAME_TITLES.get(rid, rid),
                "category": "卷上·屏风马破当头炮",
                "description": GAME_DESC.get(rid, ""),
                "moves": moves_out,
            })
            print(f"  OK: {len(moves_out)} steps", file=sys.stderr)

    result = {"records": all_records}
    json.dump(result, sys.stdout, ensure_ascii=False, indent=2)


if __name__ == "__main__":
    main()
