using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MeiHuaPuChess.Core.Models;

/// <summary>
/// 走棋记录显示模型，用于 UI 列表绑定
/// </summary>
public class MoveViewModel : INotifyPropertyChanged
{
    public int StepNumber { get; set; }
    public string RedNotation { get; set; } = string.Empty;
    public string BlackNotation { get; set; } = string.Empty;

    public string DisplayText => HasBlackMove
        ? $"{StepNumber}. {RedNotation}    {BlackNotation}"
        : $"{StepNumber}. {RedNotation}";

    public bool HasBlackMove => !string.IsNullOrEmpty(BlackNotation);

    private bool _isCurrentStep;
    public bool IsCurrentStep
    {
        get => _isCurrentStep;
        set { _isCurrentStep = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
