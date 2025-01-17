using Godot;
using System;

public partial class Captured : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
}
