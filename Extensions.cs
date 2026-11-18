using SharpDX;

public static class Extensions
{
	public static Vector2 Clamp(this Vector2 v, RectangleF rectangle)
	{
		v = Vector2.Max(v, rectangle.TopLeft);
		v = Vector2.Min(v, rectangle.BottomRight);
		return v;
	}
}
