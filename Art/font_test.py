from PIL import Image, ImageDraw, ImageFont

fonts = [
    ("FZYTK", r"Assets\Resources\Fonts\FZYTK.TTF", 64),
    ("SimSun", r"C:\Windows\Fonts\simsun.ttc", 64),
    ("SimHei", r"C:\Windows\Fonts\simhei.ttf", 64),
    ("Deng", r"C:\Windows\Fonts\Deng.ttf", 64),
    ("Agency", r"Assets\Resources\Fonts\AGENCYB.TTF", 64),
]
im = Image.new("RGB", (1400, 560), "#e8e8e4")
d = ImageDraw.Draw(im)
y = 10
for name, path, size in fonts:
    f = ImageFont.truetype(path, size)
    d.text((20, y), f"{name}  出击布阵 人才招募 SORTIE", font=f, fill="#111")
    y += 105
im.save(r"Art\font_test.png")
