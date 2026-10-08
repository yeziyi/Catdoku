# -*- coding: utf-8 -*-
import os
from PIL import Image, ImageDraw, ImageFont

BASE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(BASE)
GV = os.path.join(ROOT, "Catdoku", "Assets", "Resources", "Images", "GameView")
MM = os.path.join(ROOT, "Catdoku", "Assets", "Resources", "Images", "MainMenu")

cat = Image.open(os.path.join(GV, "IconCat3.png")).convert("RGBA")
mouse = Image.open(os.path.join(GV, "IconMouse.png")).convert("RGBA")

BROWN = (139, 74, 42, 255)
ORANGE = (232, 134, 46, 255)

def font(size):
    for p in [r"C:\Windows\Fonts\arialbd.ttf", r"C:\Windows\Fonts\msyhbd.ttc", r"C:\Windows\Fonts\arial.ttf"]:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()

W, H = 1000, 660
img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
dr = ImageDraw.Draw(img)

def center_text(text, f, y, color):
    bbox = dr.textbbox((0, 0), text, font=f)
    tw = bbox[2] - bbox[0]
    dr.text(((W - tw) / 2, y), text, font=f, fill=color)

# icons on top, side by side
icon_h = 190
def scale_h(im, h):
    return im.resize((int(im.width * h / im.height), h), Image.LANCZOS)
ci = scale_h(cat, icon_h)
mi = scale_h(mouse, int(icon_h * 0.92))
gap = 30
total_w = ci.width + gap + mi.width
x0 = (W - total_w) // 2
img.paste(ci, (x0, 20), ci)
img.paste(mi, (x0 + ci.width + gap, 20 + (icon_h - mi.height)), mi)

# text lines
center_text("Cat&Mouse", font(150), 230, BROWN)
center_text("DOKU", font(165), 410, ORANGE)

img.save(os.path.join(MM, "Title.png"))
print("Title.png written", img.size)
