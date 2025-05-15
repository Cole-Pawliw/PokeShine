using Godot;
using System;
using System.IO;

public partial class JsonManager : Node
{
	// Saves a string to the specified file in the specified path
	public void SaveJsonToFile(string path, string fileName, string data)
	{
		if (!Directory.Exists(path))
		{
			try
			{
				Directory.CreateDirectory(path);
			}
			catch (Exception e)
			{
				// Do nothing
			}
		}
		
		string fullPath = path + fileName;
		using var saveFile = Godot.FileAccess.Open(fullPath, Godot.FileAccess.ModeFlags.Write);
		try
		{
			saveFile.StoreString(data);
		}
		catch (System.Exception e)
		{
			GD.Print(e);
		}
	}
	
	// Loads a json string from the specified file in the specified path
	public string LoadJsonFromFile(string path, string fileName)
	{
		string data = null;
		path = Path.Join(path, fileName);
		
		if (!File.Exists(path))
		{
			return null;
		}
		
		try
		{
			data = File.ReadAllText(path);
		}
		catch (System.Exception e)
		{
			GD.Print(e);
		}
		
		return data;
	}
	
	// Loads the specified file from the specified path and returns all the contents as a string
	public string LoadResourceFromFile(string path, string filename)
	{
		string data = null;
		path = Path.Join(path, filename);
		
		if (!Godot.FileAccess.FileExists(path))
		{
			return null;
		}
		
		try
		{
			using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
			data = file.GetAsText();
		}
		catch (System.Exception e)
		{
			GD.Print(e);
		}
		
		return data;
	}
}
