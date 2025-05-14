using Godot;
using System;

public partial class FinishedStats : Control
{
	
	public CapturedData data;
	Label statsLabel, countLabel, nameLabel;
	Sprite2D sprite;
	
	Control verify;
	bool screenVisible = false;
	
	[Signal]
	public delegate void HuntChangedEventHandler();
	[Signal]
	public delegate void BackButtonPressedEventHandler();
	[Signal]
	public delegate void DeleteHuntEventHandler();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		statsLabel = GetNode<Label>("ScrollContainer/Background/StatsLabel");
		countLabel = GetNode<Label>("ScrollContainer/Background/CountLabel");
		nameLabel = GetNode<Label>("ScrollContainer/Background/NameLabel");
		sprite = GetNode<Sprite2D>("ScrollContainer/Background/Panel/ShinySprite");
		verify = GetNode<Control>("Verify");
	}
	
	public override void _Notification(int what)
	{
		if (what == NotificationWMGoBackRequest && screenVisible)
		{
			BackToMenu();
		}
	}
	
	public void InitializeStats(CapturedData hunt)
	{
		data = hunt;
		SetSprite();
		SetName();
		SetCount();
		SetStats();
		screenVisible = true;
	}
	
	private void SetSprite()
	{
		sprite.Texture = (Texture2D)GD.Load($"res://Sprites/{data.huntFolder}/Shiny/{data.pokemon}.png");
		float scaleFactor = 100f / sprite.Texture.GetHeight();
		sprite.Scale = new Vector2(scaleFactor, scaleFactor);
		
	}
	
	private void SetName()
	{
		if (data.nickname == "" || data.nickname == null)
		{
			nameLabel.Text = data.pokemon;
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
		string stats = "";
		
		string startDate = data.startDate.Split('T')[0];
		string endDate = data.endDate.Split('T')[0];
		
		if (startDate != "")
		{
			stats += $"Started on: {startDate}\n"; // Start date
		}
		if (endDate != "")
		{
			stats += $"Ended on: {endDate}\n"; // End date
		}
		if (data.huntGame != "")
		{
			stats += $"Hunted in: {data.huntGame}\n"; // Game
		}
		if (data.huntRoute != "")
		{
			stats += $"Found at: {data.huntRoute}\n"; // Route
		}
		if (data.huntMethod != "")
		{
			stats += $"Method: {data.huntMethod}\n"; // Method
		}
		if (data.capturedGender != "")
		{
			stats += $"Gender: {data.capturedGender}\n"; // Gender
		}
		if (data.capturedBall != "")
		{
			stats += $"Ball: {data.capturedBall}\n"; // Ball
		}
		
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
	
	private void EditHunt()
	{
		CapturedCreator startHuntScreen = (CapturedCreator)GD.Load<PackedScene>("res://Scenes/CapturedCreator.tscn").Instantiate();
		AddChild(startHuntScreen);
		
		startHuntScreen.AddHunt += ChangeHunt;
		startHuntScreen.BackButtonPressed += CloseCreator;
		screenVisible = false;
		startHuntScreen.SetPreSelections(data);
	}
	
	private void ChangeHunt()
	{
		CapturedCreator startHuntScreen = GetNode<CapturedCreator>("CapturedCreator");
		
		data.startDate = startHuntScreen.startDate.date;
		data.endDate = startHuntScreen.endDate.date;
		data.huntGame = startHuntScreen.selections[0];
		data.pokemon = startHuntScreen.selections[1];
		data.huntMethod = startHuntScreen.selections[2];
		data.capturedGender = startHuntScreen.selections[3];
		data.capturedBall = startHuntScreen.selections[4];
		data.huntRoute = startHuntScreen.selections[5];
		data.nickname = startHuntScreen.nickname.Text;
		data.count = (int)startHuntScreen.counter.Value;
		data.timeSpent = startHuntScreen.timer.totalTime;
		data.charm = startHuntScreen.charmButton.ButtonPressed;
		
		SetSprite();
		SetName();
		SetCount();
		SetStats();
		HuntChangedEmitter(); // Tell SceneManager that the hunt values have changed
		
		CloseCreator();
	}
	
	private void CloseCreator()
	{
		CapturedCreator startHuntScreen = GetNode<CapturedCreator>("CapturedCreator");
		startHuntScreen.Visible = false;
		screenVisible = true;
		startHuntScreen.Cleanup();
	}
	
	private void HuntChangedEmitter()
	{
		EmitSignal("HuntChanged");
	}
	
	// Close this screen
	private void BackToMenu()
	{
		screenVisible = false;
		EmitSignal("BackButtonPressed");
	}
	
	private void DeleteButtonPressed()
	{
		screenVisible = false;
		verify.Visible = true;
	}
	
	private void VerifyCancelPressed()
	{
		screenVisible = true;
		verify.Visible = false;
	}
	
	private void VerifyDeletePressed()
	{
		screenVisible = false;
		EmitSignal("DeleteHunt");
	}
	
	// Destroy this UI element
	public void Cleanup()
	{
		QueueFree();
	}
}
