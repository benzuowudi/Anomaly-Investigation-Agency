# -*- coding: utf-8 -*-
"""Render the project framework UML class diagram to PNG (Pillow only)."""
import os
import sys
import math
from PIL import Image, ImageDraw, ImageFont

OUT = sys.argv[1] if len(sys.argv) > 1 else "业务框架类图.png"

FONT_DIR = "C:/Windows/Fonts"


def pick(*names):
    for n in names:
        p = os.path.join(FONT_DIR, n)
        if os.path.exists(p):
            return p
    return None


REG = pick("msyh.ttc", "simhei.ttf", "simsun.ttc", "arial.ttf")
BOLD = pick("msyhbd.ttc", "simhei.ttf", "arialbd.ttf") or REG

_fc = {}


def F(size, bold=False):
    key = (size, bold)
    if key not in _fc:
        _fc[key] = ImageFont.truetype(BOLD if bold else REG, size)
    return _fc[key]


W, H = 2680, 2020
img = Image.new("RGB", (W, H), "#FFFFFF")
d = ImageDraw.Draw(img)

INK = "#1F2A37"
MUTED = "#5B6675"
LINE = "#7A8797"
BORDER = "#C6D0DC"
HDR_GAME = "#16365C"
HDR_MGR = "#2F5597"
HDR_BASE = "#1F6F63"
API_CLR = "#3A5578"

COL = {"game": (60, 500), "mgr": (660, 560), "base": (1330, 560), "impl": (2000, 560)}
ROW_Y0 = 320
PITCH = 118
ROW_H = 104


# ---------- primitives ----------
def dashed(p1, p2, fill=LINE, width=2, dash=9, gap=7):
    x1, y1 = p1
    x2, y2 = p2
    L = math.hypot(x2 - x1, y2 - y1)
    if L == 0:
        return
    ux, uy = (x2 - x1) / L, (y2 - y1) / L
    t = 0.0
    while t < L:
        a, b = t, min(t + dash, L)
        d.line([(x1 + ux * a, y1 + uy * a), (x1 + ux * b, y1 + uy * b)], fill=fill, width=width)
        t = b + gap


def arrow_open(tip, direction, size=10, fill=LINE):
    x, y = tip
    if direction == "right":
        d.line([(x, y), (x - size, y - size * 0.65)], fill=fill, width=2)
        d.line([(x, y), (x - size, y + size * 0.65)], fill=fill, width=2)
    elif direction == "left":
        d.line([(x, y), (x + size, y - size * 0.65)], fill=fill, width=2)
        d.line([(x, y), (x + size, y + size * 0.65)], fill=fill, width=2)


def hollow_triangle(tip, direction, h=13, w=11):
    x, y = tip
    if direction == "left":
        pts = [(x, y), (x + 2 * h, y - w), (x + 2 * h, y + w)]
    else:
        pts = [(x, y), (x - 2 * h, y - w), (x - 2 * h, y + w)]
    d.polygon(pts, fill="#FFFFFF")
    d.line(pts + [pts[0]], fill=LINE, width=2)


def fit(text, size, bold, maxw, minsize=11):
    s = size
    while s > minsize:
        f = F(s, bold)
        if d.textlength(text, font=f) <= maxw:
            return f
        s -= 1
    return F(minsize, bold)


def class_box(x, y, w, h, title, desc=None, api=None, header=HDR_MGR):
    r = 12
    d.rounded_rectangle([x, y, x + w, y + h], radius=r, fill="#FFFFFF", outline=BORDER, width=2)
    hh = 44
    d.rounded_rectangle([x, y, x + w, y + hh], radius=r, fill=header)
    d.rectangle([x, y + hh - r, x + w, y + hh], fill=header)
    d.rounded_rectangle([x, y, x + w, y + h], radius=r, outline=BORDER, width=2)

    ft = fit(title, 25, True, w - 32)
    d.text((x + 16, y + (hh - 25) / 2 - 3), title, font=ft, fill="#FFFFFF")

    yy = y + hh + 12
    if desc:
        fd = fit(desc, 18, False, w - 30)
        d.text((x + 16, yy), desc, font=fd, fill=MUTED)
        yy += 25
    if api:
        fa = fit(api, 17, False, w - 30)
        d.text((x + 16, yy), api, font=fa, fill=API_CLR)


def row_y(i):
    return ROW_Y0 + i * PITCH


def row_cy(i):
    return row_y(i) + ROW_H // 2


# ---------- title ----------
d.text((60, 46), "UnityProject_xx 业务框架 · 主类图", font=F(46, True), fill=HDR_GAME)
d.text(
    (62, 114),
    "热更程序集 HotUpdate  |  门面 Game + 13 个单例管理器 + 5 个扩展基类  |  依据 Assets/HotUpdate 源码静态分析",
    font=F(23),
    fill=MUTED,
)

# column captions
caps = [
    ("门面 Facade", COL["game"][0], HDR_GAME),
    ("管理器层 Managers（单例）", COL["mgr"][0], HDR_MGR),
    ("扩展基类 Base Classes", COL["base"][0], HDR_BASE),
    ("业务实现 GameMain", COL["impl"][0], "#6E7B8B"),
]
for text, cx, clr in caps:
    d.text((cx, 268), text, font=F(22, True), fill=clr)
    d.line([(cx, 300), (cx + 120, 300)], fill=clr, width=3)

# ---------- Game facade ----------
gx, gw = COL["game"]
GY = ROW_Y0
GH = 520
d.rounded_rectangle([gx, GY, gx + gw, GY + GH], radius=12, fill="#FFFFFF", outline=BORDER, width=2)
d.rounded_rectangle([gx, GY, gx + gw, GY + 44], radius=12, fill=HDR_GAME)
d.rectangle([gx, GY + 32, gx + gw, GY + 44], fill=HDR_GAME)
d.rounded_rectangle([gx, GY, gx + gw, GY + GH], radius=12, outline=BORDER, width=2)
d.text((gx + 16, GY + 9), "Game", font=F(27, True), fill="#FFFFFF")
d.text((gx + 120, GY + 13), "«门面»  MonoBehaviour · partial", font=F(17), fill="#BFD0E8")

attrs = [
    "Manager : GameManager", "Res : ResManager", "Data : DataManager",
    "UI : UIManager", "Logic : LogicManager", "State : StateManager",
    "Operate : OperateManager", "Event : EventManager", "Scene : SceneManager",
    "Language : LanguageManager", "Log : LogManager", "Web : WebManager",
    "Helper : GameHelper",
]
yy = GY + 58
for a in attrs:
    d.text((gx + 18, yy), "+", font=F(18), fill=API_CLR)
    d.text((gx + 34, yy), a, font=F(17), fill=INK)
    yy += 26

d.line([(gx + 10, yy + 4), (gx + gw - 10, yy + 4)], fill=BORDER, width=1)
yy += 14
for m in ["+ StartGame()", "+ InitInstence()", "+ CreateUI()"]:
    d.text((gx + 18, yy), m, font=F(17), fill=INK)
    yy += 26

# ---------- managers ----------
MANAGERS = [
    ("GameManager", "节点 / 相机引用容器", "Init(Game)"),
    ("ResManager", "Addressables 资源封装", "Load · GetObject · LoadAsset"),
    ("DataManager", "配置表 + 运行时数据 + 存档", "RegisterData · GetGameData"),
    ("LogicManager", "业务逻辑注册表", "GetLogic · DestroyLogic"),
    ("StateManager", "多有限状态机（FSM）", "StartState · ChangeState"),
    ("OperateManager", "输入操作组件加载", "LoadOperate · DestroyOperate"),
    ("UIManager", "窗口池 / 分层 / 生命周期", "ShowWindow · Hide · CloseAllUI"),
    ("EventManager", "订阅 / 广播（字符串主题）", "Subscribe · SendMessage"),
    ("SceneManager", "关界面 + 切场景", "ChangeScene(SceneType)"),
    ("LanguageManager", "按 GameConst 取多语言词条", "GetLanguage · ChangeLanguage"),
    ("LogManager", "带开关的日志", "LogInfo · LogWarning · LogError"),
    ("WebManager", "网络（空实现）", "Init()"),
    ("GameHelper", "DOTween 动画助手", "Pan · Reveal · Deformation"),
]

mx, mw = COL["mgr"]
for i, (name, desc, api) in enumerate(MANAGERS):
    class_box(mx, row_y(i), mw, ROW_H, name, desc, api, header=HDR_MGR)

# bus from Game to every manager
bus_x = 610
d.line([(gx + gw, GY + 20), (bus_x, GY + 20)], fill=LINE, width=2)
d.line([(bus_x, GY + 20), (bus_x, row_cy(len(MANAGERS) - 1))], fill=LINE, width=2)
for i in range(len(MANAGERS)):
    cy = row_cy(i)
    d.line([(bus_x, cy), (mx - 12, cy)], fill=LINE, width=2)
    arrow_open((mx, cy), "right")

# ---------- base classes ----------
BASES = [
    (2, "GameData", "«数据» 运行时数据结构", "Init / Update / Destroy"),
    (3, "GameLogic", "«逻辑» 无渲染规则运算", "Init / Destroy"),
    (4, "GameState", "«状态» FSM 状态基类", "Init / Enter / Change / Destroy"),
    (5, "GameOperate", "«操作» MB 输入组件", "OnEnable / Update / OnDisable"),
    (6, "UIWindow", "«UI» 窗口基类 MB", "BindContent / Init / Enter / Leave"),
]
bx, bw = COL["base"]
for row, name, desc, api in BASES:
    class_box(bx, row_y(row), bw, ROW_H, name, desc, api, header=HDR_BASE)
    cy = row_cy(row)
    d.line([(mx + mw, cy), (bx - 12, cy)], fill=LINE, width=2)
    arrow_open((bx, cy), "right")

# ---------- implementations ----------
IMPLS = [
    (2, ["BattleData", "FightProcessData", "TestFightData", "RoleSystemData"]),
    (3, ["BattleLogic", "ATLogic", "RoleLogic", "LotteryLogic", "TestFightLogic"]),
    (4, ["BattleStartState", "BattleOrderState", "BattleActionState", "BattleFinishState"]),
    (5, ["BattleOperate"]),
    (6, ["BackGroundUI", "ForeGroundUI", "CommandBaseView"]),
]
ix, iw = COL["impl"]
f_item = F(17)


def pack(items, maxw, sep=" · "):
    lines, cur = [], ""
    for it in items:
        cand = it if not cur else cur + sep + it
        if d.textlength(cand, font=f_item) <= maxw:
            cur = cand
        else:
            if cur:
                lines.append(cur)
            cur = it
    if cur:
        lines.append(cur)
    return lines


for row, items in IMPLS:
    cy = row_cy(row)
    lines = pack(items, iw - 40)
    h = max(ROW_H, 26 + len(lines) * 24 + 12)
    y = cy - h // 2
    d.rounded_rectangle([ix, y, ix + iw, y + h], radius=10, fill="#F4F7FB", outline="#C9D4E3", width=2)
    ty = y + 14
    for ln in lines:
        d.text((ix + 20, ty), ln, font=f_item, fill="#2E3B4E")
        ty += 24
    # inheritance arrow: impl ─▷ base
    hollow_triangle((bx + bw, cy), "left")
    d.line([(bx + bw + 26, cy), (ix, cy)], fill=LINE, width=2)

# ---------- module index panel ----------
PX, PY, PW, PH = 1330, 1160, 1230, 690
d.rounded_rectangle([PX, PY, PX + PW, PY + PH], radius=14, fill="#FCFDFF", outline=BORDER, width=2)
d.text((PX + 28, PY + 22), "业务模块分布（GameMain/Script/Game）", font=F(22, True), fill=HDR_GAME)
d.line([(PX + 28, PY + 60), (PX + PW - 28, PY + 60)], fill="#E6EBF2", width=1)

MODULES = [
    ("Battle", "BattleLogic · BattleData · BattleOperate · BattleStart/Order/Action/FinishState"),
    ("Role", "RoleLogic · RoleSystemData（唯一已注册进 DataManager 的业务数据）"),
    ("Lottery", "LotteryLogic"),
    ("TestFight", "TestFightLogic · TestFightData"),
    ("FightProcess", "FightProcessData"),
    ("Ground", "BackGroundUI · ForeGroundUI"),
    ("CommandRoom", "CommandBaseView"),
]

f_mod = F(20, True)
f_cls = F(17)
ey = PY + 70
for name, items in MODULES:
    d.text((PX + 28, ey), name, font=f_mod, fill=HDR_MGR)
    lines = pack(items.split(" · "), PW - 250, sep=" · ")
    ty = ey + 1
    for ln in lines:
        d.text((PX + 228, ty), ln, font=f_cls, fill="#3C4A5C")
        ty += 23
    d.line([(PX + 28, ey + 74), (PX + PW - 28, ey + 74)], fill="#EDF1F6", width=1)
    ey += 84

# ---------- boot sequence panel (bottom-left) ----------
def wrap_text(text, fnt, maxw):
    out, cur = [], ""
    for ch in text:
        if d.textlength(cur + ch, font=fnt) <= maxw:
            cur += ch
        else:
            sp = cur.rfind(" ")
            if sp > len(cur) * 0.5:
                out.append(cur[:sp])
                cur = cur[sp + 1:] + ch
            else:
                out.append(cur)
                cur = ch
    if cur:
        out.append(cur)
    return out


def numbered(x, y, w, items, size=17, line_h=30, num_w=28, color="#2E3B4E", num_color="#8A93A0"):
    fnum = F(size, True)
    fbody = F(size)
    for k, it in enumerate(items):
        d.text((x, y), str(k + 1), font=fnum, fill=num_color)
        for ln in wrap_text(it, fbody, w - num_w):
            d.text((x + num_w, y), ln, font=fbody, fill=color)
            y += line_h
    return y


SPX, SPY, SPW, SPH = 60, 884, 500, 620
d.rounded_rectangle([SPX, SPY, SPX + SPW, SPY + SPH], radius=14, fill="#FCFDFF", outline=BORDER, width=2)
d.text((SPX + 22, SPY + 20), "① 启动链路（AOT → 热更）", font=F(20, True), fill=HDR_GAME)
sy = numbered(
    SPX + 22, SPY + 58, SPW - 44,
    [
        "GameAwake 加载热更 DLL",
        "反射调用 GameLaunch.Run()",
        "Res.Init() 初始化 Addressables",
        "实例化 DontDestroy → Game",
        "InitInstence() 建 13 个单例",
        "StartGame() 按序初始化",
    ],
)

sy += 12
d.line([(SPX + 22, sy), (SPX + SPW - 22, sy)], fill="#E6EBF2", width=1)
sy += 16
d.text((SPX + 22, sy), "② StartGame() 初始化顺序", font=F(20, True), fill=HDR_GAME)
sy += 38
fnum2 = F(17, True)
fbody2 = F(17)
for k, (m, desc) in enumerate([
    ("Manager.Init(this)", "取节点 / 相机"),
    ("Res.Init()", "Addressables"),
    ("Data.Init()", "Luban 表 + 数据"),
    ("Logic.Init()", "逻辑注册表"),
    ("Operate.Init()", "操作组件预加载"),
    ("UI.Init()", "FairyGUI Stage"),
    ("CreateUI()", "常驻三窗口"),
    ("Web/State/Lang.Init()", "空实现"),
]):
    d.text((SPX + 22, sy), str(k + 1), font=fnum2, fill="#8A93A0")
    d.text((SPX + 50, sy), m, font=fbody2, fill="#2E3B4E")
    d.text((SPX + 262, sy), desc, font=fbody2, fill="#5B6675")
    sy += 30

# ---------- legend ----------
LX, LY, LW, LH = 60, 1888, 760, 104
d.rounded_rectangle([LX, LY, LX + LW, LY + LH], radius=10, fill="#FBFCFE", outline=BORDER, width=2)
d.text((LX + 18, LY + 12), "图例", font=F(19, True), fill=INK)

sx, sy = LX + 24, LY + 46
d.line([(sx, sy), (sx + 52, sy)], fill=LINE, width=2)
hollow_triangle((sx + 52, sy), "right", h=10, w=9)
d.text((sx + 86, sy - 12), "继承 / 实现（空心三角指向父类）", font=F(17), fill=MUTED)

sy2 = LY + 74
dashed((sx, sy2), (sx + 52, sy2))
arrow_open((sx + 52, sy2), "right")
d.text((sx + 86, sy2 - 12), "引用 / 持有（Game 与各管理器）", font=F(17), fill=MUTED)

d.text(
    (LX + LW + 40, LY + 30),
    "说明：13 个管理器均为单例，入口方法名混用 GetInstence() / GetInstance()；管理器之间不互相持有，运行时通过 Game.XXX 横向调用。",
    font=F(18),
    fill=MUTED,
)
d.text(
    (LX + LW + 40, LY + 62),
    "扩展方式：继承基类 → 在对应 Manager 的 LoadXxx() 中注册 → 业务侧用泛型 GetXxx<T>() 取回。",
    font=F(18),
    fill=MUTED,
)

img.save(OUT)
print("saved:", os.path.abspath(OUT), img.size)
