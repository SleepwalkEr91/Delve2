using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Forms;
using ExileCore;
using ExileCore.Shared.Nodes;
using Hexa.NET.ImGui;
using SharpDX;

namespace CoPilot;

public class ImGuiExtension
{
	public static bool BeginWindow(string title, ref bool isOpened, int x, int y, int width, int height, bool autoResize = false)
	{
		ImGui.SetNextWindowPos(new System.Numerics.Vector2(width + x, height + y), (ImGuiCond)8, new System.Numerics.Vector2(1f, 1f));
		ImGui.SetNextWindowSize(new System.Numerics.Vector2(width, height), (ImGuiCond)8);
		return ImGui.BeginPopupModal(title, ref isOpened, (ImGuiWindowFlags)(autoResize ? 64 : 0));
	}

	public static bool BeginWindow(string title, ref bool isOpened, float x, float y, float width, float height, bool autoResize = false)
	{
		ImGui.SetNextWindowPos(new System.Numerics.Vector2(width + x, height + y), (ImGuiCond)8, new System.Numerics.Vector2(1f, 1f));
		ImGui.SetNextWindowSize(new System.Numerics.Vector2(width, height), (ImGuiCond)8);
		return ImGui.BeginPopupModal(title, ref isOpened, (ImGuiWindowFlags)(autoResize ? 64 : 0));
	}

	public static int IntSlider(string labelString, int value, int minValue, int maxValue)
	{
		int result = value;
		ImGui.SliderInt(labelString, ref result, minValue, maxValue);
		return result;
	}

	public static int IntSlider(string labelString, RangeNode<int> setting, int minValue, int maxValue)
	{
		int value = setting.Value;
		ImGui.SliderInt(labelString, ref value, minValue, maxValue);
		return value;
	}

	public static int IntSlider(string labelString, string sliderString, int value, int minValue, int maxValue)
	{
		int result = value;
		ImGui.SliderInt(labelString, ref result, minValue, maxValue, $"{sliderString}: {value}");
		return result;
	}

	public static int IntSlider(string labelString, RangeNode<int> setting)
	{
		int value = setting.Value;
		ImGui.SliderInt(labelString, ref value, setting.Min, setting.Max);
		return value;
	}

	public static int IntSlider(string labelString, string sliderString, RangeNode<int> setting)
	{
		int value = setting.Value;
		ImGui.SliderInt(labelString, ref value, setting.Min, setting.Max, $"{sliderString}: {setting.Value}");
		return value;
	}

	public static float FloatSlider(string labelString, float value, float minValue, float maxValue)
	{
		float result = value;
		ImGui.SliderFloat(labelString, ref result, minValue, maxValue, "%.00f");
		return result;
	}

	public static float FloatSlider(string labelString, float value, float minValue, float maxValue, float power)
	{
		float result = value;
		ImGui.SliderFloat(labelString, ref result, minValue, maxValue, "%.00f");
		return result;
	}

	public static float FloatSlider(string labelString, string sliderString, float value, float minValue, float maxValue)
	{
		float result = value;
		ImGui.SliderFloat(labelString, ref result, minValue, maxValue, $"{sliderString}: {value}");
		return result;
	}

	public static float FloatSlider(string labelString, string sliderString, float value, float minValue, float maxValue, float power)
	{
		float result = value;
		ImGui.SliderFloat(labelString, ref result, minValue, maxValue, $"{sliderString}: {value}");
		return result;
	}

	public static float FloatSlider(string labelString, RangeNode<float> setting)
	{
		float value = setting.Value;
		ImGui.SliderFloat(labelString, ref value, setting.Min, setting.Max, "%.00f");
		return value;
	}

	public static float FloatSlider(string labelString, RangeNode<float> setting, float power)
	{
		float value = setting.Value;
		ImGui.SliderFloat(labelString, ref value, setting.Min, setting.Max, "%.00f");
		return value;
	}

	public static float FloatSlider(string labelString, string sliderString, RangeNode<float> setting)
	{
		float value = setting.Value;
		ImGui.SliderFloat(labelString, ref value, setting.Min, setting.Max, $"{sliderString}: {setting.Value}");
		return value;
	}

	public static float FloatSlider(string labelString, string sliderString, RangeNode<float> setting, float power)
	{
		float value = setting.Value;
		ImGui.SliderFloat(labelString, ref value, setting.Min, setting.Max, $"{sliderString}: {setting.Value}");
		return value;
	}

	public static bool Checkbox(string labelString, bool boolValue)
	{
		ImGui.Checkbox(labelString, ref boolValue);
		return boolValue;
	}

	public static bool Checkbox(string labelString, bool boolValue, out bool outBool)
	{
		ImGui.Checkbox(labelString, ref boolValue);
		outBool = boolValue;
		return boolValue;
	}

	public static IEnumerable<Keys> KeyCodes()
	{
		return Enum.GetValues(typeof(Keys)).Cast<Keys>();
	}

	public static Keys HotkeySelector(string buttonName, Keys currentKey)
	{
		bool flag = true;
		if (ImGui.Button(buttonName))
		{
			ImGui.OpenPopup(buttonName);
		}
		if (ImGui.BeginPopupModal(buttonName, ref flag, (ImGuiWindowFlags)35))
		{
			if (Input.GetKeyState(Keys.Escape))
			{
				ImGui.CloseCurrentPopup();
			}
			else
			{
				foreach (object value in Enum.GetValues(typeof(Keys)))
				{
					if (Input.GetKeyState((Keys)value))
					{
						currentKey = (Keys)value;
						ImGui.CloseCurrentPopup();
						break;
					}
				}
			}
			ImGui.Text($" Press new key to change '{currentKey}' or Esc for exit.");
			ImGui.EndPopup();
		}
		return currentKey;
	}

	public static Keys HotkeySelector(string buttonName, string popupTitle, Keys currentKey)
	{
		if (ImGui.Button($"{buttonName}: {currentKey} "))
		{
			ImGui.OpenPopup(popupTitle);
		}
		if (ImGui.BeginPopup(popupTitle, (ImGuiWindowFlags)35))
		{
			ImGui.Text("Press a key to set as " + buttonName);
			foreach (Keys item in KeyCodes())
			{
				if (!Input.IsKeyDown(item))
				{
					continue;
				}
				if (item != Keys.Escape && item != Keys.RButton && item != Keys.LButton)
				{
					ImGui.CloseCurrentPopup();
					ImGui.EndPopup();
					return item;
				}
				break;
			}
			ImGui.EndPopup();
		}
		return currentKey;
	}

	public static Color ColorPicker(string labelName, Color inputColor)
	{
		SharpDX.Vector4 vector = inputColor.ToVector4();
		System.Numerics.Vector4 vector2 = new System.Numerics.Vector4(vector.X, vector.Y, vector.Z, vector.W);
		return ImGui.ColorEdit4(labelName, ref vector2, (ImGuiColorEditFlags)65536) ? new Color(vector2.X, vector2.Y, vector2.Z, vector2.W) : inputColor;
	}

	public static int ComboBox(string sideLabel, int currentSelectedItem, List<string> objectList, ImGuiComboFlags imGuiComboFlags = (ImGuiComboFlags)4)
	{
		ImGui.Combo(sideLabel, ref currentSelectedItem, objectList.ToArray(), objectList.Count);
		return currentSelectedItem;
	}

	public static string ComboBox(string sideLabel, string currentSelectedItem, List<string> objectList, ImGuiComboFlags imGuiComboFlags = (ImGuiComboFlags)4)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		if (ImGui.BeginCombo(sideLabel, currentSelectedItem, imGuiComboFlags))
		{
			for (int i = 0; i < objectList.Count; i++)
			{
				bool flag = currentSelectedItem == objectList[i];
				if (ImGui.Selectable(objectList[i], flag))
				{
					ImGui.EndCombo();
					return objectList[i];
				}
				if (flag)
				{
					ImGui.SetItemDefaultFocus();
				}
			}
			ImGui.EndCombo();
		}
		return currentSelectedItem;
	}

	public static string ComboBox(string sideLabel, string currentSelectedItem, List<string> objectList, out bool didChange, ImGuiComboFlags imGuiComboFlags = (ImGuiComboFlags)4)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		if (ImGui.BeginCombo(sideLabel, currentSelectedItem, imGuiComboFlags))
		{
			for (int i = 0; i < objectList.Count; i++)
			{
				bool flag = currentSelectedItem == objectList[i];
				if (ImGui.Selectable(objectList[i], flag))
				{
					didChange = true;
					ImGui.EndCombo();
					return objectList[i];
				}
				if (flag)
				{
					ImGui.SetItemDefaultFocus();
				}
			}
			ImGui.EndCombo();
		}
		didChange = false;
		return currentSelectedItem;
	}

	public static string InputText(string label, string currentValue, uint maxLength, ImGuiInputTextFlags flags)
	{
		string value = currentValue ?? string.Empty;
		ImGui.InputText(label, ref value, maxLength, flags);
		return value;
	}

	public static System.Numerics.Vector4 ImColor_HSV(float h, float s, float v)
	{
		float x = 0f, y = 0f, z = 0f;
		ImGui.ColorConvertHSVtoRGB(h, s, v, ref x, ref y, ref z);
		return new System.Numerics.Vector4(x, y, z, 255f);
	}

	public static System.Numerics.Vector4 ImColor_HSV(float h, float s, float v, float a)
	{
		float x = 0f, y = 0f, z = 0f;
		ImGui.ColorConvertHSVtoRGB(h, s, v, ref x, ref y, ref z);
		return new System.Numerics.Vector4(x, y, z, a);
	}

	public static void ImGuiExtension_ColorTabs(string idString, int height, IReadOnlyList<string> settingList, ref int selectedItem, ref int uniqueIdPop)
	{
		System.Numerics.Vector2 vector = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, height);
		if (ImGui.BeginChild(idString, vector, ImGuiChildFlags.Borders, (ImGuiWindowFlags)2048))
		{
			for (int i = 0; i < settingList.Count; i++)
			{
				ImGui.PushID(uniqueIdPop);
				float h = 1f / (float)settingList.Count * (float)i;
				ImGui.PushStyleColor((ImGuiCol)21, ImColor_HSV(h, 0.6f, 0.6f, 0.8f));
				ImGui.PushStyleColor((ImGuiCol)22, ImColor_HSV(h, 0.7f, 0.7f, 0.9f));
				ImGui.PushStyleColor((ImGuiCol)23, ImColor_HSV(h, 0.8f, 0.8f, 1f));
				ImGui.PushStyleVar((ImGuiStyleVar)11, 3f);
				ImGui.PushStyleVar((ImGuiStyleVar)10, 2f);
				if (i > 0)
				{
					ImGui.SameLine();
				}
				if (ImGui.Button(settingList[i]))
				{
					selectedItem = i;
				}
				uniqueIdPop++;
				ImGui.PopStyleVar(2);
				ImGui.PopStyleColor(3);
				ImGui.PopID();
			}
		}
		ImGui.EndChild();
	}

	public static bool BeginChild(string id, bool border, ImGuiWindowFlags flags)
	{
		System.Numerics.Vector2 vector = ImGui.GetContentRegionAvail();
		return ImGui.BeginChild(id, new System.Numerics.Vector2(vector.X, vector.Y), border ? ImGuiChildFlags.Borders : ImGuiChildFlags.None, flags);
	}

	public static void Spacing(int amount)
	{
		for (int i = 0; i < amount; i++)
		{
			ImGui.Spacing();
		}
	}

	public static void EndColumn()
	{
		ImGui.Columns(1, (string)null, false);
	}

	public static void ToolTip(string desc)
	{
		ImGui.SameLine();
		ImGui.TextDisabled("(?)");
		if (ImGui.IsItemHovered((ImGuiHoveredFlags)0))
		{
			ImGui.SetTooltip(desc);
		}
	}
}
