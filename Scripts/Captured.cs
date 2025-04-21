using Godot;
using System;

public class CapturedData
{
	public CapturedData()
	{
		huntID = ++instances;
	}
	public CapturedData(string name, string game)
	{
		pokemon = name;
		huntGame = game;
		huntID = ++instances;
	}
	public CapturedData(string name, string game, string method, bool shinyCharm, string time)
	{
		pokemon = name;
		huntGame = game;
		huntMethod = method;
		charm = shinyCharm;
		startDate = time;
		huntID = ++instances;
	}
	public CapturedData(string start, string end, string game, string pokemonName,
						string method, string gender, string ball, string route,
						string name, bool shinyCharm, int c, int time)
	{
		startDate = start;
		endDate = end;
		pokemon = pokemonName;
		huntGame = game;
		huntMethod = method;
		huntRoute = route;
		capturedBall = ball;
		capturedGender = gender;
		nickname = name;
		charm = shinyCharm;
		count = c;
		timeSpent = time;
		huntID = ++instances;
	}
	public CapturedData(CapturedData src)
	{
		startDate = src.startDate;
		endDate = src.endDate;
		
		pokemon = src.pokemon;
		nickname = src.nickname;
		
		huntGame = src.huntGame;
		
		huntMethod = src.huntMethod;
		huntRoute = src.huntRoute;
		capturedGender = src.capturedGender;
		capturedBall = src.capturedBall;
		charm = src.charm;
		count = src.count;
		timeSpent = src.timeSpent;
		
		huntIndex = src.huntIndex;
		huntID = src.huntID;
	}
	public CapturedData(HuntData src, string date, string name, string ball, string gender)
	{
		startDate = src.startDate;
		endDate = date;
		
		pokemon = src.pokemon[0];
		nickname = name;
		
		huntGame = src.huntGame;
		
		huntMethod = src.huntMethod;
		huntRoute = src.huntRoute;
		capturedGender = gender;
		capturedBall = ball;
		charm = src.charm;
		count = src.count;
		timeSpent = src.timeSpent;
		
		huntID = ++instances;
	}
	public CapturedData(HuntData src)
	{
		startDate = src.startDate;
		
		pokemon = src.pokemon[0];
		
		huntGame = src.huntGame;
		
		huntMethod = src.huntMethod;
		huntRoute = src.huntRoute;
		charm = src.charm;
		count = src.count;
		timeSpent = src.timeSpent;
		
		huntID = ++instances;
	}
	
	public bool Equals(CapturedData other)
	{
		return Equals(other, this);
	}
	
	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType()) {
			return false;
		}
		
		var other = (CapturedData)obj;
		return (other.startDate, other.endDate, other.pokemon, other.huntGame, other.count).Equals((startDate, endDate, pokemon, huntGame, count));
	}
	
	public override int GetHashCode()
	{
		return (startDate, endDate, pokemon, huntGame, count).GetHashCode();
	}
	
	public static bool operator ==(CapturedData hunt1, CapturedData hunt2)
	{
		return hunt1.Equals(hunt2);
	}
	
	public static bool operator !=(CapturedData hunt1, CapturedData hunt2)
	{
		return !hunt1.Equals(hunt2);
	}
	
	public string startDate { get; set; } = ""; // Datetime formatted string storing when the hunt was created
	public string endDate { get; set; } = ""; // Datetime formatted string storing when the hunt ended
	
	public string pokemon { get; set; } // The name of the pokemon being hunted
	public string nickname { get; set; } = ""; // The user set nickname after catching the pokemon
	
	public string huntFolder { get; private set; } // The folder to be used to access the sprites for this game
	private string _huntGame; // The game the pokemon is being hunted in
	public string huntGame
	{
		get
		{
			return _huntGame;
		}
		set
		{
			_huntGame = value;
			
			// Set the containing folder based on the game being hunted in
			if (value == "Gold" || value == "Silver" || value == "Crystal")
			{
				huntFolder = "GS";
			}
			else if (value == "Ruby" || value == "Sapphire" || value == "Emerald")
			{
				huntFolder = "RS";
			}
			else if (value == "Fire Red" || value == "Leaf Green")
			{
				huntFolder = "FL";
			}
			else if (value == "Diamond" || value == "Pearl" || value == "Platinum")
			{
				huntFolder = "DP";
			}
			else if (value == "Heart Gold" || value == "Soul Silver")
			{
				huntFolder = "HS";
			}
			else if (value == "Black" || value == "White" || value == "Black 2" || value == "White 2")
			{
				huntFolder = "BW";
			}
			else if (value == "X" || value == "Y" || value == "Alpha Sapphire" || value == "Omega Ruby" || 
					value == "Sun" || value == "Moon" || value == "Ultra Sun" || value == "Ultra Moon")
			{
				huntFolder = "BankModels";
			}
			else
			{
				huntFolder = "HomeModels";
			}
		}
	}
	
	public string huntMethod { get; set; } = ""; // The method being performed for the hunt, used to determine odds
	public string huntRoute { get; set; } = ""; // Unused for now, will help to creat multi hunts
	public string capturedGender { get; set; } = ""; // The gender of the pokemon
	public string capturedBall { get; set; } = ""; // The ball used to catch the pokemon
	public bool charm { get; set; } = false; // An item that increases shiny odds
	public int count { get; set; } = 0; // The current number of encounters/resets for the pokemon
	public int timeSpent { get; set; } = 0; // The number of seconds spent hunting
	
	public int huntIndex { get; set; } // An index used for sorting the hunts, indexing is separate for active and completed hunts
	public int huntID { get; private set; } // Unique identifier for each HuntData
	public static int instances;
}

public partial class Captured : Control
{
	public CapturedData data;
	
	[Signal]
	public delegate void SelectButtonPressedEventHandler(int selectedHuntID);
	
	public void InitializeInfo(CapturedData hunt)
	{
		data = hunt;
		Sprite2D sprite = GetNode<Sprite2D>("ShinySprite");
		sprite.Texture = (Texture2D)GD.Load($"res://Sprites/{data.huntFolder}/Shiny/{data.pokemon}.png");
		
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
