#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""从 content/*.md 重新生成 src/PawnshopMod/Localization/zh.csv。

修改文案时只改 markdown，然后跑一次这个脚本（在仓库根目录执行）：

    python content/build_localization.py

规则与游戏一致：键、第一个逗号、文本；文本里的换行写成字面 \\n；
文本含双引号时用 CSV 引号包起来（"" 转义）。
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
NOTES_MD = os.path.join(ROOT, "content", "notes_zh.md")
NEWS_MD = os.path.join(ROOT, "content", "news_zh.md")
NPC_MD = os.path.join(ROOT, "content", "npc_notes_zh.md")

# 人物名 -> 代码里用的 id（客户端 identifier 前缀）
NPC_IDS = {
    "乔治奥": "georgio",
    "埃莉诺": "eleanor",
    "塞尔皮科": "serpico",
    "卢": "lou",
    "乔·王尔德": "joe",
    "索耶": "sawyer",
    "杰克逊博士": "jackson",
    "屠夫": "butcher",
    "詹姆斯·贝利": "bailey",
    "亚瑟·拉弗勒": "arthur",
    "李北文": "li",
    "胡安": "juan",
    "约翰·史密斯": "john",
}
OUT_CSV = os.path.join(ROOT, "src", "PawnshopMod", "Localization", "zh.csv")

NL = chr(92) + "n"  # 字面量 \n，游戏会把它解析成换行


def read(path):
    with open(path, encoding="utf-8") as f:
        return f.read()


def parse_notes(text):
    rows = []

    # 纸条正文：| `psm_note_tXX_NN` | 正文 |
    current = None
    for line in text.splitlines():
        m = re.match(r"^## T(\d\d)", line)
        if m:
            current = int(m.group(1))
            continue
        m = re.match(r"^\|\s*`(psm_note_t\d\d_\d\d)`\s*\|\s*(.+?)\s*\|", line)
        if m and current is not None:
            rows.append((m.group(1), m.group(2).replace("🎁", "").strip()))

    # 开场信：只有正文（标题、署名都留空，不生成键）
    m = re.search(r"\|\s*`psm_note_intro_body`\s*\|\s*(.+?)\s*\|\s*$", text, re.M)
    if m:
        rows.append(("psm_note_intro_body", m.group(1).replace("<br>", NL)))

    return rows


def parse_news(text):
    rows = []
    for line in text.splitlines():
        m = re.match(r"^\|\s*`(psm_news_\d\d)`\s*\|\s*(.+?)\s*\|\s*(.+?)\s*\|\s*$", line)
        if m:
            rows.append((m.group(1) + "_title", m.group(2)))
            rows.append((m.group(1) + "_desc", m.group(3)))
    return rows


def parse_npc(text):
    """人物纸条：## 乔治奥 / | 1 | 正文 |  ->  psm_npc_georgio_t1"""
    rows = []
    current = None
    for line in text.splitlines():
        m = re.match(r"^##\s+(.+?)\s*$", line)
        if m:
            current = NPC_IDS.get(m.group(1).strip())
            continue
        m = re.match(r"^\|\s*([123])\s*\|\s*(.+?)\s*\|\s*$", line)
        if m and current:
            rows.append((f"psm_npc_{current}_t{m.group(1)}", m.group(2)))
    return rows


def escape(value):
    if '"' in value:
        return '"' + value.replace('"', '""') + '"'
    return value


def main():
    rows = parse_news(read(NEWS_MD)) + parse_notes(read(NOTES_MD)) + parse_npc(read(NPC_MD))

    seen = set()
    for key, _ in rows:
        if key in seen:
            print("duplicate key: " + key, file=sys.stderr)
            return 1
        seen.add(key)

    os.makedirs(os.path.dirname(OUT_CSV), exist_ok=True)
    with open(OUT_CSV, "w", encoding="utf-8", newline="\n") as f:
        f.write("# PawnshopMod localization (zh) — generated from content/*.md\n")
        for key, value in rows:
            f.write(key + "," + escape(value) + "\n")

    print("wrote %d keys -> %s" % (len(rows), os.path.relpath(OUT_CSV, ROOT)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
