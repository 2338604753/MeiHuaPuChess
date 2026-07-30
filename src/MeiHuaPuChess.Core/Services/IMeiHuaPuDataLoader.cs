using MeiHuaPuChess.Core.Models;

namespace MeiHuaPuChess.Core.Services;

/// <summary>
/// 梅花谱数据加载器接口（实现在 Data 层）
/// </summary>
public interface IMeiHuaPuDataLoader
{
    List<MeiHuaPuRecord> LoadAllRecords();
}
