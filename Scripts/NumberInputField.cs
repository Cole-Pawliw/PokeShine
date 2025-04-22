using Godot;
using System;

[Tool]
public partial class NumberInputField : TextEdit
{
	[Export]
	public int MaxValue = 100; // The maximum value allowed in the field
	[Export]
	public int MinValue = 0; // The minimum value allowed in the field
	[Export]
	public int Step = 1; // Value must be a multiple of this
	[Export]
	public int Value = 0; // The actual value shown in the input field
	[Export]
	public bool AllowGreater = false; // Allows value to be greater than maxValue
	[Export]
	public bool AllowLesser = false; // Allows value to be smaller than minValue
	
	// Called whenever the text in the field is changed
	// Changes value to match the text after removing non-numeric characters
	private void UpdateValue()
	{
		VerifyText(); // Remove any non-numeric characters from Text
		int textAsInt;
		
		if (Text == "")
		{
			textAsInt = 0;
		}
		else
		{
			textAsInt = Int32.Parse(Text); // Convert text to value
		}
		
		if (!AllowGreater && textAsInt > MaxValue)
		{
			Value = MaxValue;
		}
		else if (!AllowLesser && textAsInt < MinValue)
		{
			Value = MinValue;
		}
		else if (textAsInt % Step != 0)
		{
			Value -= textAsInt % Step;
			if (!AllowLesser && textAsInt < MinValue)
			{
				Value += Step; // Increase to next lowest step if it goes too low
			}
		}
		else
		{
			Value = textAsInt;
		}
	}
	
	// Checks that there are no non-numeric characters before removing them
	private void VerifyText()
	{
		bool allNumbers = true;
		foreach (char digit in Text)
		{
			if (digit < '0' || digit > '9')
			{
				allNumbers = false;
			}
		}
		
		if (!allNumbers)
		{
			RemoveNonNumeric();
		}
	}
	
	// Used to remove any values that aren't numbers
	private void RemoveNonNumeric()
	{
		string input = Text;
		int caret = GetCaretColumn();
		for (int i = input.Length - 1; i >= 0; i--)
		{
			if (input[i] > '9' || input[i] < '0')
			{
				input = input.Remove(i, 1);
				if (i <= caret)
				{
					caret--;
				}
			}
		}
		Text = input;
		SetCaretColumn(caret);
	}
	
	private void UpdateText()
	{
		Text = $"{Value}";
		SetCaretColumn(Text.Length);
	}
}
