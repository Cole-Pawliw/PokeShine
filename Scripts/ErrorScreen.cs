using Godot;
using System;

public partial class ErrorScreen : Control
{
	string errorMessage;
	Label label;
	
	[Signal]
	public delegate void BackSignalEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		label = GetNode<Label>("ScrollContainer/Label");
	}
	
	public void SetColors()
	{
		ColorRect bg = GetNode<ColorRect>("Background");
		bg.Color = new Color(GlobalSettings.backgrounds[GlobalSettings.colorMode - 1]);
	}
	
	public void DisplayError(string e)
	{
		errorMessage = e;
		label.Text = $"An error has occurred:\n\n{errorMessage}";
	}
	
	private void CopyToClipboard()
	{
		DisplayServer.ClipboardSet(errorMessage);
	}
	
	private void CloseScreen()
	{
		EmitSignal("BackSignal");
	}
	
	public void Cleanup()
	{
		QueueFree();
	}
}
