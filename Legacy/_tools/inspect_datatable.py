import openpyxl

for path in ["DataTable/__tables__.xlsx", "DataTable/__enums__.xlsx"]:
    print("=====", path)
    wb = openpyxl.load_workbook(path, data_only=False)
    for ws in wb.worksheets:
        print("--- sheet:", ws.title, "rows:", ws.max_row, "cols:", ws.max_column)
        for row in ws.iter_rows(values_only=True):
            print(row)
