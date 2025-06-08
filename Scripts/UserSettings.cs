using Godot;
using System;

public partial class UserSettings : Control
{
	ColorRect bg;
	TextureButton backButton, infoButton, volumeButton;
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
		volumeButton = GetNode<TextureButton>("VolumeButton");
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
	
	public void SetSettings()
	{
		shiny.ButtonPressed = GlobalSettings.huntInfo[0];
		regular.ButtonPressed = GlobalSettings.huntInfo[1];
		odds.ButtonPressed = GlobalSettings.huntInfo[2];
		combo.ButtonPressed = GlobalSettings.huntInfo[3];
		huntTimer.ButtonPressed = GlobalSettings.huntInfo[4];
		encounterTimer.ButtonPressed = GlobalSettings.huntInfo[5];
		volumeButton.ButtonPressed = GlobalSettings.soundOn;
	}
	
	public void SetColors()
	{
		backButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GlobalSettings.colorMode}/back.png");
		infoButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GlobalSettings.colorMode}/info.png");
		volumeButton.TextureNormal = (Texture2D)GD.Load($"res://Assets/Buttons/{GlobalSettings.colorMode}/volume_off.png");
		volumeButton.TexturePressed = (Texture2D)GD.Load($"res://Assets/Buttons/{GlobalSettings.colorMode}/volume.png");
		bg.Color = new Color(GlobalSettings.backgrounds[GlobalSettings.colorMode - 1]);
		EmitNewColors();
	}
	
	private void Color1Pressed()
	{
		GlobalSettings.colorMode = 1;
		SetColors();
	}
	
	private void Color2Pressed()
	{
		GlobalSettings.colorMode = 2;
		SetColors();
	}
	
	private void Color3Pressed()
	{
		GlobalSettings.colorMode = 3;
		SetColors();
	}
	
	private void Color4Pressed()
	{
		GlobalSettings.colorMode = 4;
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
		GlobalSettings.soundOn = volumeButton.ButtonPressed; // Set volume on close
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
