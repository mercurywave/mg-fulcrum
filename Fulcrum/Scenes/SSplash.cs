namespace Fulcrum;

[AutoInitialize( AutoInitialize.eLoadBy.Launch, eAssetLocation.Content)]
public class SSplash : IScene
{
    public static ImageAsset _imgTitle = new ImageAsset("FulcrumCore/Mercury Wave.png");
    public static ImageAsset _imgM = new ImageAsset("FulcrumCore/M.png");
    public static ImageAsset _imgW = new ImageAsset("FulcrumCore/W.png");

    CImage ctlTitle = new CImage(_imgTitle);
    CImage ctlM = new CImage(_imgM);
    CImage ctlW = new CImage(_imgW);
    public override void OnLayout()
    {
        ctlM.Layout.CenterHorizontally(GScreen.Width / 2);
        ctlW.Layout.CenterHorizontally(GScreen.Width / 2);
        ctlTitle.Layout.CenterHorizontally(GScreen.Width / 2);

        ctlM.Layout.Top = GScreen.Height / 2 - GScreen.Pad(50);
        ctlW.Layout.Top = ctlM.Layout.Bottom;
        ctlTitle.Layout.Top = ctlW.Layout.Bottom + GScreen.Pad(50);
    }
}