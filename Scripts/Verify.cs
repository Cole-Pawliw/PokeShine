using Godot;
using System;

public partial class Verify : Control
{
	bool screenVisible = true;
	
	[Signal]
	public delegate void CancelEventHandler();
	[Signal]
	public delegate void ConfirmEventHandler();
	
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
