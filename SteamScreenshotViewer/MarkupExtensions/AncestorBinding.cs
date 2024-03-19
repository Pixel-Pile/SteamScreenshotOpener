using System.Windows.Data;

namespace SteamScreenshotViewer.MarkupExtensions;

public class AncestorBinding : Binding
{
    private const string RelativeSourceAccessedExceptionMessage =
        "AncestorBinding does not support setting RelativeSource; set AncestorType instead";

    public AncestorBinding()
    {
        base.RelativeSource = new RelativeSource();
        base.RelativeSource.Mode = RelativeSourceMode.FindAncestor;
    }

    public new RelativeSource RelativeSource
    {
        get => throw new InvalidOperationException(RelativeSourceAccessedExceptionMessage);
        set => throw new InvalidOperationException(RelativeSourceAccessedExceptionMessage);
    }

    public Type Type
    {
        get => base.RelativeSource.AncestorType;
        set => base.RelativeSource.AncestorType = value;
    }
}