using Godot;
using System;
using System.Collections.Generic;

public partial class HuntSettings : Control
{
	// Hunt Settings
	SpinBox counter, increment, timer;
	CheckButton shiny, regular, odds, huntTimer, encounterTimer;
	
	// Functional Buttons
	TextureButton backButton;
	Button deleteButton;
	
	public HuntData settings;
	bool huntChanged = false;
	
	[Signal]
	public delegate void CloseSettingsEventHandler(bool importantChange);
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
		
		if (settings.pokemon.Count > 1)
		{
			regular.Disabled = true; // Regular sprites not available for multi-hunts
		}
		else
		{
			regular.Disabled = false;
		}
	}
	
	private void OpenHuntCreator()
	{
		HuntCreator startHuntScreen = (HuntCreator)GD.Load<PackedScene>("res://Scenes/HuntCreator.tscn").Instantiate();
		AddChild(startHuntScreen);
		
		startHuntScreen.StartHunt += ChangeHunt;
		startHuntScreen.BackButtonPressed += CloseCreator;
		startHuntScreen.SetPreSelections(settings);
	}
	
	private void ChangeHunt(string gameName, string method, bool charm)
	{
		List<string> pokemon = GetNode<HuntCreator>("HuntCreator").pokemonSelected;
		
		// A catch-all to tell ShinyHuntScreen that important information has changed
		if (gameName != settings.huntGame || method != settings.huntMethod || pokemon != settings.pokemon)
		{
			huntChanged = true;
			settings.huntGame = gameName;
			settings.huntMethod = method;
			settings.pokemon = pokemon;
		}
		
		// Charm information is a minor change
		settings.charm = charm;
		
		CloseCreator();
	}
	
	private void CloseCreator()
	{
		HuntCreator startHuntScreen = GetNode<HuntCreator>("HuntCreator");
		startHuntScreen.Visible = false;
		startHuntScreen.Cleanup();
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
		EmitSignal("CloseSettings", huntChanged);
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
