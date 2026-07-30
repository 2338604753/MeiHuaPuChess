namespace MeiHuaPuChess.Core.Enums;

/// <summary>
/// 应用模式
/// </summary>
public enum AppMode
{
    /// <summary>全局观看模式：自由导航，棋盘只读</summary>
    Review,
    /// <summary>训练模式：用户走黑方，红方按谱自动走</summary>
    Training
}
