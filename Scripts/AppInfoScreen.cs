using Godot;
using System;

public partial class AppInfoScreen : Control
{
	string donateLink = "https://buymeacoffee.com/colesapps";
	TabContainer tabContainer;
	bool screenVisible = false;
	
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		tabContainer = GetNode<TabContainer>("TabContainer");
		screenVisible = true;
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationWMGoBackRequest && screenVisible)
		{
			BackToMenu();
		}
	}
	
	private void Donate()
	{
		OS.ShellOpen(donateLink);
	}
	
	private void SetInfoTab(bool button_pressed)
	{
		if (button_pressed == true) {
			tabContainer.CurrentTab = 0;
		}
	}
	
	private void SetTutorialTab(bool button_pressed)
	{
		if (button_pressed == true) {
			tabContainer.CurrentTab = 1;
		}
	}
	
	private void SetCopyrightTab(bool button_pressed)
	{
		if (button_pressed == true) {
			tabContainer.CurrentTab = 2;
		}
	}
	
	private void BackToMenu()
	{
		screenVisible = false;
		EmitSignal("BackButtonPressed");
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
	
}
