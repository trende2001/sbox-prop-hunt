using Sandbox;

namespace MyGame;

public partial class PopupSystem
{
	[ClientRpc]
	public static void DisplayPopup( string text, string title = "", float duration = 8f )
	{
		var Popup = new PopupPanel();
		Popup.Text = text;
		Popup.Title = title;
		Popup.Duration = duration;
		PopupList.Current.AddChild(Popup);
	}
}
