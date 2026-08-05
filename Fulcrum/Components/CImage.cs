using Microsoft.Xna.Framework;

namespace Fulcrum;

public class CImage : BaseComponent, IDraw, ILayout
{
    public OLayout Layout { get; set; }
    public ImageAsset Asset;

    public CImage(ImageAsset asset)
    {
        Asset = asset;
    }

    public void OnLayout()
    {
        Layout.Width = Asset.Width;
        Layout.Height = Asset.Height;
    }

    public void OnDraw(Tick frameTime)
    {
        Asset.BlitStretched(Layout.TransformedLayout, Color.White);
    }
}