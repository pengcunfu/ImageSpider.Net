# ImageSpider

基于 **.NET 10** 的 Windows 桌面图片搜索工具，支持 **多种 API + 爬虫** 多源聚合搜索。

## 技术栈

| 层级 | 技术 |
|------|------|
| UI | WPF (`net10.0-windows`) |
| 架构 | MVVM（CommunityToolkit.Mvvm） |
| 依赖注入 | Microsoft.Extensions.Hosting |
| API | Bing、Google Custom Search、Pexels、Pixabay、Unsplash |
| 爬虫 | 百度、360、搜狗、必应、DuckDuckGo |

## 项目结构

```
ImageSpider.App/           # WPF 桌面端
ImageSpider.Core/          # 模型与接口
ImageSpider.Infrastructure/ # API + 爬虫实现
```

## 快速开始

### 环境

- Windows 10+
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### 运行

```bash
cd d:\Projects\DevTools\ImageSpider
dotnet run --project ImageSpider.App
```

### API 密钥配置

在应用 **设置** 中填写对应平台的密钥并勾选启用：

| 来源 | 申请地址 |
|------|----------|
| Bing | [Azure Portal](https://portal.azure.com) → Bing Search v7 |
| Google | [Google Cloud Console](https://console.cloud.google.com/) → Custom Search API + [可编程搜索引擎](https://programmablesearchengine.google.com/) 获取 cx |
| Pexels | [pexels.com/api](https://www.pexels.com/api/) |
| Pixabay | [pixabay.com/api/docs](https://pixabay.com/api/docs/) |
| Unsplash | [unsplash.com/developers](https://unsplash.com/developers) |

爬虫来源无需密钥，在设置中开关即可。

用户配置保存在：

`%AppData%\ImageSpider\appsettings.user.json`

### 搜索来源一览

**API（需密钥）**：Bing、Google、Pexels、Pixabay、Unsplash  

**爬虫（免密钥）**：百度、360、搜狗、必应、DuckDuckGo  

主界面勾选需要的来源后搜索，结果自动去重合并。请求间隔可在设置中调整（默认 300ms）。

## 功能

- [x] 关键词搜索、多源勾选
- [x] 缩略图网格、点击打开来源页
- [x] 分页加载更多
- [x] 勾选批量下载
- [x] 设置（API Key、爬虫开关、下载目录）
- [ ] 以图搜图、尺寸高级筛选（可后续扩展）

## 合规提示

本工具仅供个人学习与研究。使用 API 须遵守微软服务条款；爬虫须遵守目标站点 robots 与使用协议，勿用于商业爬取或大规模分发。

## 许可证

MIT（可按需修改）
