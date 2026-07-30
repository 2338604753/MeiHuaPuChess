# 梅花谱象棋

中国传统象棋棋盘应用，内置《梅花谱》29 局经典棋谱，支持棋谱观看和残局训练两种模式。

## 功能

- **全局观看** — 自动播放或手动翻阅《梅花谱》经典对局，含走棋记录和吃子统计
- **训练模式** — 隐去下一步，由你来走，走对才继续；支持提示和悔棋
- **棋盘渲染** — 仿天天象棋风格，手工绘制棋子渐变、边框、内圈金线
- **适配窗口** — 棋盘随窗口大小自适应缩放

## 截图

> TODO：运行后截一张棋盘图放到这里

## 技术栈

| 层 | 技术 |
|---|------|
| UI | WPF (.NET 10) |
| 架构 | MVVM (CommunityToolkit.Mvvm) |
| DI | Microsoft.Extensions.DependencyInjection |
| 棋局数据 | JSON 内嵌资源 |

## 项目结构

```
src/
├── MeiHuaPuChess.App/       # WPF 界面
│   ├── Views/               # 棋盘控件
│   ├── ViewModels/          # MVVM ViewModel
│   ├── Converters/          # 值转换器
│   └── Resources/           # 图标等资源
├── MeiHuaPuChess.Core/      # 核心引擎
│   ├── Engine/              # 走棋逻辑、将军检测、合法性校验
│   ├── Models/              # 棋盘/棋子/棋谱数据模型
│   ├── Enums/               # 枚举定义
│   ├── MeiHuaPu/            # 梅花谱专用引擎
│   └── Services/            # 游戏服务
├── MeiHuaPuChess.Data/      # 数据加载
│   └── meiHuaPu_records.json  # 梅花谱 29 局棋谱
└── tests/
    └── MeiHuaPuChess.RegressionTests/  # 回归测试
```

## 运行

需要 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。

```bash
git clone https://github.com/你的用户名/MeiHuaPuChess.git
cd MeiHuaPuChess
dotnet run --project src/MeiHuaPuChess.App
```

## 下载

前往 [Releases](https://github.com/你的用户名/MeiHuaPuChess/releases) 下载最新版本，解压双击 `MeiHuaPuChess.App.exe` 即可运行（无需安装 .NET 运行时）。

## License

MIT
