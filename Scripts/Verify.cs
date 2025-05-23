using Godot;
using System;

public partial class Verify : Control
{
	bool screenVisible = true;
	
	[Signal]
	public delegate void CancelEventHandler();
	[Signal]
	public delegate void ConfirmEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetColors();
	}
	
	public void SetColors()
	{
		ColorRect bg = GetNode<ColorRect>("Background");
		bg.Color = new Color(GameHuntInformation.backgrounds[GameHuntInformation.colorMode - 1]);
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationWMGoBackRequest && screenVisible)
		{
			EmitCancel();
		}
	}
	
	private void EmitCancel()
	{
		screenVisible = false;
		EmitSignal("Cancel");
	}
	
	private void EmitConfirm()
	{
		screenVisible = false;
		EmitSignal("Confirm");
	}
}
