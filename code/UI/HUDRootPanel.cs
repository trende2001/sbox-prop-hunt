using Sandbox;
using Sandbox.UI;

namespace MyGame;

public class HUDRootPanel : RootPanel
{
	public static HUDRootPanel Current;

	public HUDRootPanel()
	{

		if ( Current != null )
		{
			Current.Delete();
			Current = this;
		}
		Current = this;

		AddChild<Crosshair>();
		AddChild<Health>();
		AddChild<WeaponUI>();
		AddChild<TimerUI>();
		AddChild<Blind>();
		AddChild<KillFeed>();
		AddChild<InputHints>();
		AddChild<Scoreboard>();
		AddChild<Chat>();
		AddChild<HudHints>();
		AddChild<AmmoUI>();
		AddChild<PopupList>();
	}
}
