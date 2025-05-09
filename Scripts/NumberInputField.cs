using Godot;
using System;

[Tool]
public partial class NumberInputField : TextEdit
{
	private int _MaxValue = 100;
	[Export]
	public int MaxValue // The maximum value allowed in the field
	{
		get
		{
			return _MaxValue;
		}
		set
		{
			_MaxValue = value;
			if (Value > _MaxValue && !AllowGreater)
			{
				Value = _MaxValue;
				UpdateText();
			}
		}
	}
	private int _MinValue = 0;
	[Export]
	public int MinValue // The minimum value allowed in the field
	{
		get
		{
			return _MinValue;
		}
		set
		{
			_MinValue = value;
			if (Value < _MinValue && !AllowLesser)
			{
				Value = _MinValue;
				UpdateText();
			}
		}
	}
	[Export]
	public int Step = 1; // Value must be a multiple of this
	private int _Value = 0;
	[Export]
	public int Value // The actual value shown in the input field
	{
		get
		{
			return _Value;
		}
		set
		{
			_Value = value;
			EmitValueChanged();
		}
	}
	[Export]
	public bool AllowGreater = false; // Allows value to be greater than maxValue
	[Export]
	public bool AllowLesser = false; // Allows value to be smaller than minValue
	
	[Signal]
	public delegate void ValueChangedEventHandler();
	
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
			textAsInt = MaxValue;
		}
		else if (!AllowLesser && textAsInt < MinValue)
		{
			textAsInt = MinValue;
		}
		else if (textAsInt % Step != 0)
		{
			textAsInt -= textAsInt % Step;
			if (!AllowLesser && textAsInt < MinValue)
			{
				textAsInt += Step; // Increase to next lowest step if it goes too low
			}
		}
		Value = textAsInt; // Only assign Value once so ValueChanged is only emitted once
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
	
	private void EmitValueChanged()
	{
		EmitSignal("ValueChanged");
	}
}
