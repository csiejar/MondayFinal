# 365 存錢日記

這是一個使用 ASP.NET Core MVC 建立的 365 天存錢日記應用程式。

## 功能

- 記錄每日存款金額
- 隨機存款功能
- 顯示存款總額
- 存款進度條顯示（完成百分比）
- 存款歷史記錄
- 視覺化存款紀錄表

## 技術架構

- ASP.NET Core MVC 9.0
- Entity Framework Core 9.0
- SQLite 資料庫
- Bootstrap 5

## 如何執行

1. 確保已安裝 .NET 9.0 SDK
2. 在專案根目錄執行以下命令：

```bash
dotnet run
```

3. 在瀏覽器中開啟 https://localhost:7123 或 http://localhost:5123

## 資料庫

專案使用 Entity Framework Core 與 SQLite 進行資料儲存。資料庫檔案將自動創建在專案根目錄。

## 授權

MIT License
