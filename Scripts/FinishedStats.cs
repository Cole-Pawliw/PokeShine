using Godot;
using System;

public partial class FinishedStats : Control
{
	
	public HuntData data;
	Label statsLabel;
	Label countLabel;
	Label nameLabel;
	Sprite2D sprite;
	
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		statsLabel = GetNode<Label>("ScrollContainer/Background/StatsLabel");
		countLabel = GetNode<Label>("ScrollContainer/Background/CountLabel");
		nameLabel = GetNode<Label>("ScrollContainer/Background/NameLabel");
		sprite = GetNode<Sprite2D>("ScrollContainer/Background/ShinySprite");
	}
	
	public void InitializeStats(HuntData hunt)
	{
		data = hunt;
		SetSprite();
		SetName();
		SetCount();
		SetStats();
	}
	
	private void SetSprite()
	{
		sprite.Texture = (Texture2D)GD.Load($"res://Sprites/{data.huntFolder}/Shiny/{data.pokemonName}.png");
	}
	
	private void SetName()
	{
		if (data.nickname == "" || data.nickname == null)
		{
			nameLabel.Text = data.pokemonName;
		}
		else
		{
			nameLabel.Text = data.nickname;
		}
	}
	
	private void SetCount()
	{
		countLabel.Text = $"{data.count}";
	}
	
	private void SetStats()
	{
		string stats;
		
		string startDate = data.startDate.Split('T')[0];
		string endDate = data.endDate.Split('T')[0];
		
		stats = $"Started on: {startDate}\nEnded on: {endDate}\n"; // Dates
		stats += $"Hunted in: {data.huntGame}\n"; // Game
		stats += $"Method: {data.huntMethod}\n"; // Method
		stats += $"Gender: {data.capturedGender}\n"; // Gender
		stats += $"Ball: {data.capturedBall}\n"; // Ball
		
		// Find the time spent in the hunt
		int fullTime = data.timeSpent;
		int hours = fullTime / 3600;
		fullTime %= 3600; // Remove the hours to count seconds and minutes
		int minutes = fullTime / 60;
		int seconds = fullTime % 60;
		string timerInHourFormat = $"{hours:00}:{minutes:00}:{seconds:00}"; // :00 pads 2 zeros to everything
		stats += $"Time spent: {timerInHourFormat}"; // Time spent
		
		statsLabel.Text = stats;
	}
	
	// Close this screen
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
