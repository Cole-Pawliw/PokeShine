using Godot;
using System;

public partial class Captured : Control
{
	public HuntData data;
	
	[Signal]
	public delegate void SelectButtonPressedEventHandler(int selectedHuntID);
	
	public void InitializeInfo(HuntData hunt)
	{
		data = hunt;
		Sprite2D sprite = GetNode<Sprite2D>("ShinySprite");
		sprite.Texture = (Texture2D)GD.Load($"res://Sprites/{data.huntFolder}/Shiny/{data.pokemonName}.png");
		
		// Scale the size of the image to fit the Captured scene
		float scaleFactor = Math.Min(90f / sprite.Texture.GetWidth(), 85f / sprite.Texture.GetHeight());
		sprite.Scale = new Vector2(scaleFactor, scaleFactor);
		
		UpdateLabel();
	}
	
	public void UpdateLabel()
	{
		Label info = GetNode<Label>("Info");
		string date = data.endDate.Split('T')[0];
		info.Text = $"{data.count}\n{date}";
	}
	
	private void SelectButton()
	{
		EmitSignal("SelectButtonPressed", data.huntID);
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
}
