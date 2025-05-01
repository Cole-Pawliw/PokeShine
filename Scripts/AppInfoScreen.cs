using Godot;
using System;

public partial class AppInfoScreen : Control
{
	string donateLink = "https://buymeacoffee.com/colesapps";
	TabContainer tabContainer;
	
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		tabContainer = GetNode<TabContainer>("TabContainer");
	}
	
	private void Donate()
	{
		OS.ShellOpen(donateLink);
	}
	
	private void SetInfoPanel(bool button_pressed)
	{
		if (button_pressed == true) {
			tabContainer.CurrentTab = 0;
		}
	}
	
	private void SetCopyrightPanel(bool button_pressed)
	{
		if (button_pressed == true) {
			tabContainer.CurrentTab = 1;
		}
	}
	
	private void BackToMenu()
	{
		EmitSignal("BackButtonPressed");
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
	
}
