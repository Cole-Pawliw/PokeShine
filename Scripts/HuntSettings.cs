using Godot;
using System;

public partial class HuntSettings : Control
{
	// Hunt Settings
	SpinBox counter, increment, timer;
	CheckButton shiny, regular, odds, huntTimer, encounterTimer;
	
	// Functional Buttons
	TextureButton backButton;
	Button deleteButton;
	
	public HuntData settings;
	
	[Signal]
	public delegate void CloseSettingsEventHandler();
	[Signal]
	public delegate void DeleteHuntEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		counter = GetNode<SpinBox>("CounterValue");
		increment = GetNode<SpinBox>("IncrementValue");
		timer = GetNode<SpinBox>("TimerValue");
	
		shiny = GetNode<CheckButton>("ShinySprite");
		regular = GetNode<CheckButton>("RegularSprite");
		odds = GetNode<CheckButton>("HuntOdds");
		huntTimer = GetNode<CheckButton>("HuntTimer");
		encounterTimer = GetNode<CheckButton>("EncounterTimer");
	
		backButton = GetNode<TextureButton>("BackButton");
		deleteButton = GetNode<Button>("DeleteButton");
	}
	
	public void SetInitialSettings(HuntData data)
	{
		settings = data;
		counter.Value = settings.count;
		increment.Value = settings.incrementValue;
		timer.Value = settings.timeSpent;
		
		shiny.ButtonPressed = settings.showShiny;
		regular.ButtonPressed = settings.showRegular;
		odds.ButtonPressed = settings.showOdds;
		huntTimer.ButtonPressed = settings.showFullTimer;
		encounterTimer.ButtonPressed = settings.showMiniTimer;
	}
	
	private void BackButtonPressed()
	{
		settings.count = (int)counter.Value;
		settings.incrementValue = (int)increment.Value;
		settings.timeSpent = (int)timer.Value;
		
		settings.showShiny = shiny.ButtonPressed;
		settings.showRegular = regular.ButtonPressed;
		settings.showOdds = odds.ButtonPressed;
		settings.showFullTimer = huntTimer.ButtonPressed;
		settings.showMiniTimer = encounterTimer.ButtonPressed;
		EmitSignal("CloseSettings");
	}
	
	private void DeleteButtonPressed()
	{
		EmitSignal("DeleteHunt");
	}
	
	public void Cleanup()
	{
		QueueFree();
	}
}
