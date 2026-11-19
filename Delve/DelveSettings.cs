using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace Delve;

public class DelveSettings : ISettings
{
	public ToggleNode DelvePathWays = new ToggleNode(true);

	public RangeNode<int> DelvePathWaysNodeSize = new RangeNode<int>(7, 1, 200);

	public ColorBGRA DelvePathWaysNodeColor = new ColorBGRA(byte.MaxValue, 140, 0, byte.MaxValue);

	public ToggleNode DelveChests = new ToggleNode(true);

	public ToggleNode DelveWall { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelveWallSize { get; set; } = new RangeNode<int>(18, 1, 200);

	public ColorNode DelveWallColor { get; set; } = (Color)(new ColorBGRA(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	public ToggleNode DelveMiningSuppliesDynamiteChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelveMiningSuppliesDynamiteChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelveMiningSuppliesDynamiteChestColor { get; set; } = (Color)(new ColorBGRA(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	public ToggleNode DelveMiningSuppliesFlaresChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelveMiningSuppliesFlaresChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelveMiningSuppliesFlaresChestColor { get; set; } = (Color)(new ColorBGRA(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	public ToggleNode DelveAzuriteVeinChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelveAzuriteVeinChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelveAzuriteVeinChestColor { get; set; } = (Color)(new ColorBGRA(0, 115, byte.MaxValue, byte.MaxValue));

	public ToggleNode DelveCurrencyChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelveCurrencyChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelveCurrencyChestColor { get; set; } = (Color)(new ColorBGRA(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	public ToggleNode DelveResonatorChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelveResonatorChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelveResonatorChestColor { get; set; } = (Color)(new ColorBGRA(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	public ToggleNode DelveFossilChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelveFossilChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelveFossilChestColor { get; set; } = (Color)(new ColorBGRA(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));

	public ToggleNode DelvePathwayChest { get; set; } = new ToggleNode(true);

	public RangeNode<int> DelvePathwayChestSize { get; set; } = new RangeNode<int>(15, 1, 200);

	public ColorNode DelvePathwayChestColor { get; set; } = (Color)(new ColorBGRA(0, 131, 0, byte.MaxValue));

	public ToggleNode DebugMode { get; set; } = new ToggleNode(false);

	public ToggleNode ShouldHideOnOpen { get; set; } = new ToggleNode(false);

	public HotkeyNode DebugHotkey { get; set; } = new HotkeyNode(Keys.None);

	public ToggleNode DelveGridMap { get; set; } = new ToggleNode(true);

	public float DelveGridMapScale { get; set; } = 0.635625f;

	public ToggleNode Enable { get; set; } = new ToggleNode(false);

	// Minimap: radar lines from player to chest entities
	public ToggleNode MinimapRadarLines { get; set; } = new ToggleNode(true);
	public RangeNode<int> MinimapRadarLineWidth { get; set; } = new RangeNode<int>(2, 1, 8);

	// Max distance (grid units) for radar lines, 0 = no limit
	public RangeNode<int> MinimapRadarLineMaxRange { get; set; } = new RangeNode<int>(250, 0, 1000);

	// Direction arrows at the player pointing to nearby breakable walls
	public ToggleNode WallArrows { get; set; } = new ToggleNode(true);
	public RangeNode<int> WallArrowMaxRange { get; set; } = new RangeNode<int>(300, 50, 1000);
	public RangeNode<int> WallArrowAlertRange { get; set; } = new RangeNode<int>(75, 10, 300);
	public RangeNode<int> WallArrowSize { get; set; } = new RangeNode<int>(40, 20, 100);
}
