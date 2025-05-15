using Godot;
using System;
using System.Collections.Generic;

public partial class HuntSettings : Control
{
	// Hunt Settings
	NumberInputField counter, increment;
	DateInputField date;
	TimeInputField timer;
	CheckButton shiny, regular, odds, huntTimer, encounterTimer;
	
	// Functional Buttons
	TextureButton backButton;
	Button deleteButton;
	
	Control verify;
	
	public HuntData settings;
	bool huntChanged = false;
	bool screenVisible = false;
	
	[Signal]
	public delegate void CloseSettingsEventHandler(bool importantChange);
	[Signal]
	public delegate void DeleteHuntEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		counter = GetNode<NumberInputField>("CounterValue");
		increment = GetNode<NumberInputField>("IncrementValue");
		timer = GetNode<TimeInputField>("TimerValue");
		date = GetNode<DateInputField>("DateValue");
	
		shiny = GetNode<CheckButton>("ShinySprite");
		regular = GetNode<CheckButton>("RegularSprite");
		odds = GetNode<CheckButton>("HuntOdds");
		huntTimer = GetNode<CheckButton>("HuntTimer");
		encounterTimer = GetNode<CheckButton>("EncounterTimer");
	
		backButton = GetNode<TextureButton>("BackButton");
		deleteButton = GetNode<Button>("DeleteButton");
		
		verify = GetNode<Control>("Verify");
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationWMGoBackRequest && screenVisible)
		{
			BackButtonPressed();
		}
	}
	
	public void SetInitialSettings(HuntData data)
	{
		settings = data;
		counter.Text = $"{settings.count}";
		increment.Text = $"{settings.incrementValue}";
		timer.UpdateTime(settings.timeSpent);
		date.UpdateDate(settings.startDate);
		
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
		screenVisible = true;
	}
	
	private void OpenHuntCreator()
	{
		HuntCreator startHuntScreen = (HuntCreator)GD.Load<PackedScene>("res://Scenes/HuntCreator.tscn").Instantiate();
		AddChild(startHuntScreen);
		
		startHuntScreen.StartHunt += ChangeHunt;
		startHuntScreen.BackButtonPressed += CloseCreator;
		startHuntScreen.SetPreSelections(settings);
		screenVisible = false;
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
		screenVisible = true;
		RemoveChild(startHuntScreen);
		startHuntScreen.Cleanup();
	}
	
	private void BackButtonPressed()
	{
		settings.count = (int)counter.Value;
		settings.incrementValue = (int)increment.Value;
		settings.timeSpent = timer.totalTime;
		settings.startDate = date.date;
		
		settings.showShiny = shiny.ButtonPressed;
		settings.showRegular = regular.ButtonPressed;
		settings.showOdds = odds.ButtonPressed;
		settings.showFullTimer = huntTimer.ButtonPressed;
		settings.showMiniTimer = encounterTimer.ButtonPressed;
		screenVisible = false;
		EmitSignal("CloseSettings", huntChanged);
	}
	
	private void DeleteButtonPressed()
	{
		verify.Visible = true;
		screenVisible = false;
	}
	
	private void VerifyCancelPressed()
	{
		verify.Visible = false;
		screenVisible = true;
	}
	
	private void VerifyDeletePressed()
	{
		screenVisible = false;
		EmitSignal("DeleteHunt");
	}
	
	public void Cleanup()
	{
		QueueFree();
	}
}
