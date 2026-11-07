"""移除 Luban 的 UI 配置表：__tables__.xlsx 中的 GameData_UI.ui 行、__enums__.xlsx 中的 UI 相关枚举。
同时把 ui.xlsx 复制一份到 Legacy/DataTable 作为归档（原文件被 Excel 占用，无法移动）。
"""
import shutil
from pathlib import Path

import openpyxl

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "DataTable"
LEGACY = ROOT / "Legacy" / "DataTable"

TABLES = DATA / "__tables__.xlsx"
ENUMS = DATA / "__enums__.xlsx"

REMOVE_ENUMS = {"GameType.UIType", "GameType.UINameType", "AnimType.UIEnterAnimType"}


def remove_table_rows():
    wb = openpyxl.load_workbook(TABLES, data_only=False)
    ws = wb.worksheets[0]
    targets = []
    for row in range(1, ws.max_row + 1):
        if ws.cell(row=row, column=2).value == "GameData_UI.ui":
            targets.append(row)
    for row in sorted(targets, reverse=True):
        ws.delete_rows(row, 1)
    wb.save(TABLES)
    return len(targets)


def remove_enums():
    wb = openpyxl.load_workbook(ENUMS, data_only=False)
    ws = wb.worksheets[0]
    starts = {}
    for row in range(1, ws.max_row + 1):
        name = ws.cell(row=row, column=2).value
        if name:
            starts[str(name).strip()] = row
    ordered = sorted(starts.items(), key=lambda kv: kv[1])
    spans = []
    for index, (name, start) in enumerate(ordered):
        end = ordered[index + 1][1] - 1 if index + 1 < len(ordered) else ws.max_row
        if name in REMOVE_ENUMS:
            spans.append((name, start, end))
    for name, start, end in sorted(spans, key=lambda item: item[1], reverse=True):
        ws.delete_rows(start, end - start + 1)
    wb.save(ENUMS)
    return [item[0] for item in spans]


tables_removed = remove_table_rows()
enums_removed = remove_enums()

LEGACY.mkdir(parents=True, exist_ok=True)
shutil.copy2(DATA / "ui.xlsx", LEGACY / "ui.xlsx")

print("tables removed:", tables_removed)
print("enums removed:", enums_removed)

# 校验
wb = openpyxl.load_workbook(TABLES, data_only=False)
ws = wb.worksheets[0]
print("remaining tables:", [ws.cell(row=r, column=2).value for r in range(1, ws.max_row + 1) if ws.cell(row=r, column=2).value])

wb = openpyxl.load_workbook(ENUMS, data_only=False)
ws = wb.worksheets[0]
print("remaining enums:", [ws.cell(row=r, column=2).value for r in range(1, ws.max_row + 1) if ws.cell(row=r, column=2).value])
