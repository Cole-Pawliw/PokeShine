using Godot;
using System;

public partial class UserSettings : Control
{
	ColorRect bg;
	TextureButton backButton, infoButton;
	public CheckButton shiny, regular, odds, huntTimer, encounterTimer, combo;
	bool screenVisible = false;
	
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	[Signal]
	public delegate void NewColorsEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		shiny = GetNode<CheckButton>("ShinySprite");
		regular = GetNode<CheckButton>("RegularSprite");
		odds = GetNode<CheckButton>("HuntOdds");
		huntTimer = GetNode<CheckButton>("HuntTimer");
		encounterTimer = GetNode<CheckButton>("EncounterTimer");
		combo = GetNode<CheckButton>("Combo");
		
		bg = GetNode<ColorRect>("Background");
		backButton = GetNode<TextureButton>("BackButton");
		infoButton = GetNode<TextureButton>("InfoButton");
		screenVisible = true;
		SetColors();
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationWMGoBackRequest && screenVisible)
		{
			EmitBackButtonPressed();
		}
	}
	
	public void SetSettings(bool[] settings)
	{
		shiny.ButtonPressed = settings[0];
		regular.ButtonPressed = settings[1];
		odds.ButtonPressed = settings[2];
		combo.ButtonPressed = settings[3];
		huntTimer.ButtonPressed = settings[4];
		encounterTimer.ButtonPressed = settings[5];
	}
	
	public void SetColors()
	{
		backButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GameHuntInformation.colorMode}/back.png");
		infoButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GameHuntInformation.colorMode}/info.png");
		bg.Color = new Color(GameHuntInformation.backgrounds[GameHuntInformation.colorMode - 1]);
		EmitNewColors();
	}
	
	private void Color1Pressed()
	{
		GameHuntInformation.colorMode = 1;
		SetColors();
	}
	
	private void Color2Pressed()
	{
		GameHuntInformation.colorMode = 2;
		SetColors();
	}
	
	private void Color3Pressed()
	{
		GameHuntInformation.colorMode = 3;
		SetColors();
	}
	
	private void Color4Pressed()
	{
		GameHuntInformation.colorMode = 4;
		SetColors();
	}
	
	private void OpenInfoScreen()
	{
		AppInfoScreen infoScreen = (AppInfoScreen)GD.Load<PackedScene>("res://Scenes/AppInfoScreen.tscn").Instantiate();
		AddChild(infoScreen);
		infoScreen.BackButtonPressed += CloseInfoScreen;
		screenVisible = false;
	}
	
	private void CloseInfoScreen()
	{
		AppInfoScreen infoScreen = GetNode<AppInfoScreen>("AppInfoScreen");
		screenVisible = true;
		RemoveChild(infoScreen);
		infoScreen.Cleanup();
	}
	
	private void EmitBackButtonPressed()
	{
		screenVisible = false;
		EmitSignal("BackButtonPressed");
	}
	
	private void EmitNewColors()
	{
		EmitSignal("NewColors");
	}
	
	public void Cleanup()
	{
		QueueFree();
	}
}
