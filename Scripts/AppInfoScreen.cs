using Godot;
using System;

public partial class AppInfoScreen : Control
{
	string donateLink = "";
	
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	
	private void Donate()
	{
		// Open donateLink in the users browser
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
