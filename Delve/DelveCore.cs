using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using CoPilot;
using DefaultNamespace;
using Delve.Libs;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.AtlasHelper;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;
using Hexa.NET.ImGui;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SharpDX;

namespace Delve;

public class DelveCore : BaseSettingsPlugin<DelveSettings>
{
	public class LargeMapData
	{
		public Camera Camera { get; set; }

		public ExileCore.PoEMemory.Elements.Map MapWindow { get; set; }

		public RectangleF MapRec { get; set; }

		public SharpDX.Vector2 PlayerPos { get; set; }

		public float PlayerPosZ { get; set; }

		public SharpDX.Vector2 ScreenCenter { get; set; }

		public float Diag { get; set; }

		public float K { get; set; }

		public float Scale { get; set; }

		public LargeMapData(GameController GC)
		{
			Camera = GC.Game.IngameState.Camera;
			MapWindow = GC.Game.IngameState.IngameUi.Map;
			MapRec = ((Element)MapWindow).GetClientRect();
			PlayerPos = GC.Player.GetComponent<Positioned>().GridPos;
			PlayerPosZ = GC.Player.GetComponent<Render>().Z;
			ScreenCenter = MathHepler.Translate(new SharpDX.Vector2(MapRec.Width / 2f, MapRec.Height / 2f), 0f, -20f) + new SharpDX.Vector2(MapRec.X, MapRec.Y) + new SharpDX.Vector2(MapWindow.LargeMapShiftX, MapWindow.LargeMapShiftY);
			Diag = (float)Math.Sqrt(Camera.Width * Camera.Width + Camera.Height * Camera.Height);
			K = (((float)Camera.Width < 1024f) ? 1120f : 1024f);
			Scale = K / (float)Camera.Height * (float)Camera.Width * 3f / 4f / MapWindow.LargeMapZoom;
		}
	}

	public class FossilTiers
	{
		[JsonProperty("t1")]
		public string[] T1 { get; set; }

		[JsonProperty("t2")]
		public string[] T2 { get; set; }

		[JsonProperty("t3")]
		public string[] T3 { get; set; }
	}

	private RectangleF DrawRect;

	public float CurrentDelveMapZoom = 0.635625f;

	private List<DelveEntity> DelveEntities;

	public Version version = Assembly.GetExecutingAssembly().GetName().Version;

	public string PluginVersion;

	public DateTime buildDate;

	public static int Selected;

	public static int idPop;

	private bool IsAzuriteMine;

	public FossilTiers FossilList = new FossilTiers();

	private IconStore iconStore;

	private float updateCooldown = 250f;

	public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
	{
		MetadataPropertyHandling = (MetadataPropertyHandling)2,
		DateParseHandling = (DateParseHandling)0,
		Converters = { (JsonConverter)new IsoDateTimeConverter
		{
			DateTimeStyles = DateTimeStyles.AssumeUniversal
		} }
	};

	private AtlasTexture _iconAbberantFossil;

	private AtlasTexture _iconAbberantFossilT1;

	private AtlasTexture _iconAbberantFossilT2;

	private AtlasTexture _iconAbberantFossilT3;

	private AtlasTexture _iconAbyssChest;

	private AtlasTexture _iconAbyssCrack;

	private AtlasTexture _iconAbyssJewelChest;

	private AtlasTexture _iconAdditionalSockets;

	private AtlasTexture _iconAmbushInvasionChest;

	private AtlasTexture _iconAzurite;

	private AtlasTexture _iconAzuriteT1;

	private AtlasTexture _iconAzuriteT2;

	private AtlasTexture _iconAzuriteT3;

	private AtlasTexture _iconBestiaryChest;

	private AtlasTexture _iconBeyondChest;

	private AtlasTexture _iconBloodlinesChest;

	private AtlasTexture _iconBombs;

	private AtlasTexture _iconBreachChest;

	private AtlasTexture _iconCorrupted;

	private AtlasTexture _iconCurrency;

	private AtlasTexture _iconDelveChest;

	private AtlasTexture _iconDivinationCard;

	private AtlasTexture _iconDominationChest;

	private AtlasTexture _iconEnchant;

	private AtlasTexture _iconEssence;

	private AtlasTexture _iconFlare;

	private AtlasTexture _iconFragment;

	private AtlasTexture _iconGate;

	private AtlasTexture _iconHarbingerChest;

	private AtlasTexture _iconHiddenDoor;

	private AtlasTexture _iconHighLevelGem;

	private AtlasTexture _iconIncursionChest;

	private AtlasTexture _iconMap;

	private AtlasTexture _iconNemesisChest;

	private AtlasTexture _iconOfferingChest;

	private AtlasTexture _iconOnslaughtChest;

	private AtlasTexture _iconPaleCourtComplete;

	private AtlasTexture _iconPerandusChest;

	private AtlasTexture _iconPerandusCoins;

	private AtlasTexture _iconProphecy;

	private AtlasTexture _iconRampageChest;

	private AtlasTexture _iconRareAmulet;

	private AtlasTexture _iconResonatorT1;

	private AtlasTexture _iconResonatorT2;

	private AtlasTexture _iconResonatorT3;

	private AtlasTexture _iconSilverCoin;

	private AtlasTexture _iconSixLink;

	private AtlasTexture _iconSpeedArmour;

	private AtlasTexture _iconStrongbox;

	private AtlasTexture _iconStygianViseChest;

	private AtlasTexture _iconTalismanChest;

	private AtlasTexture _iconTempestChest;

	private AtlasTexture _iconTormentChest;

	private AtlasTexture _iconUniqueManaFlask;

	private AtlasTexture _iconUpgrade2x1A;

	private AtlasTexture _iconUpgrade2x2A;

	private AtlasTexture _iconWarbandsChest;

	private AtlasTexture _iconWisdomCurrency;

	public LargeMapData LargeMapInformation { get; set; }

	private string SourceDir;

	private AtlasTexturesProcessor _delveAtlas;

	private string ResolveSourceDirectory()
	{
		var sourceDir = Path.Combine(base.PluginManager.SourcePluginDirectoryPath, base.DirectoryName);
		if (Directory.Exists(Path.Combine(sourceDir, "Textures")))
		{
			return sourceDir;
		}
		if (Directory.Exists(Path.Combine(base.DirectoryFullName, "Textures")) || Directory.Exists(Path.Combine(base.DirectoryFullName, "textures")))
		{
			return base.DirectoryFullName;
		}
		return sourceDir;
	}

	private AtlasTexture GetDelveAtlasTexture(string textureName)
	{
		return _delveAtlas?.GetTextureByName(textureName);
	}

	public override bool Initialise()
	{
		SourceDir = ResolveSourceDirectory();
		var atlasJsonPath = Path.Combine(SourceDir, "Textures", "DelveAtlas.json");
		var atlasPngPath = Path.Combine(SourceDir, "Textures", "DelveAtlas.png");
		if (File.Exists(atlasJsonPath) && File.Exists(atlasPngPath))
		{
			_delveAtlas = base.CreateAtlas(atlasJsonPath, atlasPngPath);
		}
		else
		{
			base.LogError($"Delve atlas not found at: {atlasJsonPath}", 10f);
		}
		AtlasTextureInit();
		Input.RegisterKey(base.Settings.DebugHotkey.Value);
		iconStore = new IconStore(base.CreateAtlas(Path.Join(SourceDir, "Textures", "chart", "icons.json"), Path.Join(SourceDir, "Textures", "chart", "icons.png")), Path.Join(SourceDir, "chart_icons.json"));
		buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
		base.Name = "Delve";
		PluginVersion = $"{version}";
		DelveEntities = new List<DelveEntity>();
		base.GameController.Area.OnAreaChange += ((BaseSettingsPlugin<DelveSettings>)this).AreaChange;
		if (File.Exists(SourceDir + "\\Fossil_Tiers.json"))
		{
			string text = File.ReadAllText(SourceDir + "\\Fossil_Tiers.json");
			FossilList = JsonConvert.DeserializeObject<FossilTiers>(text, JsonSettings);
		}
		else
		{
			base.LogError("Error loading Fossil_Tiers.json, Please re download from Random Features github repository", 10f);
		}
		return true;
	}

	public override void AreaChange(AreaInstance area)
	{
		base.AreaChange(area);
		DelveEntities = new List<DelveEntity>();
		IsAzuriteMine = base.GameController.Area.CurrentArea.Name.Contains("Azurite Mine");
	}

	private void DrawData(string icon, string data)
	{
		System.Numerics.Vector2 vector = base.Graphics.MeasureText(data, 20);
		DrawRect.Width = DrawRect.Height + vector.X + 10f;
		base.Graphics.DrawBox(DrawRect, Color.Black);
		RectangleF drawRect = DrawRect;
		drawRect.Width = drawRect.Height;
		base.Graphics.DrawImage(Path.Combine(SourceDir, icon), drawRect);
		base.Graphics.DrawFrame(DrawRect, Color.Gray, 2);
		SharpDX.Vector2 topLeft = DrawRect.TopLeft;
		topLeft.Y += DrawRect.Height / 2f;
		topLeft.X += DrawRect.Height + 3f;
		base.Graphics.DrawText(data, topLeft, Color.White, 20, (FontAlign)1);
		DrawRect.X += DrawRect.Width + 1f;
	}

	public override void Render()
	{
		base.Render();
		if (!base.Settings.Enable.Value || !IsAzuriteMine)
		{
			return;
		}
		Helper.GetDeltaTime();
		updateCooldown = Helper.MoveTowards(updateCooldown, 0f, Helper._deltaTime);
		if (updateCooldown == 0f)
		{
			UpdateEntitys();
		}
		ExileCore.PoEMemory.Elements.Map map = base.GameController.Game.IngameState.IngameUi.Map;
		SubMap val = ((RemoteMemoryObject)map.LargeMap).AsObject<SubMap>();
		MapIcon._mapScale = (float)base.GameController.IngameState.Camera.Height / 677f * val.Zoom;
		SubterraneanChart delveWindow = base.GameController.Game.IngameState.IngameUi.DelveWindow;
		if (base.Settings.DelveMineMapConnections.Value)
		{
			DrawMineMapConnections(delveWindow);
		}
		if (base.Settings.DelveGridMap.Value)
		{
			DelveMapNodes(delveWindow);
		}
		if (!((Element)delveWindow).IsVisible)
		{
			RenderMapImages();
		}
		try
		{
			CurrentDelveMapZoom = ((Element)delveWindow).Children[0].Children[0].Children[2].Scale;
		}
		catch { }
		if (base.Settings.DebugHotkey.PressedOnce())
		{
			base.Settings.DebugMode.Value = !base.Settings.DebugMode.Value;
		}
		if (!base.Settings.DebugMode.Value)
		{
			return;
		}
		foreach (DelveEntity delveEntity in DelveEntities)
		{
			if (!delveEntity.Path.StartsWith("Metadata/Terrain/Leagues/Delve/Objects/DelveWall") && !delveEntity.Path.StartsWith("Metadata/Terrain/Leagues/Delve/Objects/DelveLight"))
			{
				bool? flag = delveEntity?.IsOpened;
				if (!base.Settings.ShouldHideOnOpen.Value || flag != true)
				{
					string text = delveEntity.Path.Replace("Metadata/Chests/DelveChests/", "");
					System.Numerics.Vector2 vector = base.Graphics.MeasureText(text, 0);
					SharpDX.Vector2 vector2 = base.GameController.Game.IngameState.Camera.WorldToScreen(delveEntity.Pos);
					base.Graphics.DrawBox(new RectangleF(vector2.X - vector.X / 2f - 10f, vector2.Y - vector.Y / 2f, vector.X + 20f, vector.Y * 2f), Color.White);
					base.Graphics.DrawText(text, vector2, Color.Black, 20, (FontAlign)1);
				}
			}
		}
	}

	private void DelveMapNodes(SubterraneanChart mineMap)
	{
		if (!base.Settings.DelveGridMap.Value || !((Element)mineMap).IsVisible)
		{
			return;
		}
		try
		{
			List<Element> list = ((Element)mineMap).Children[0].Children[0].Children[2].Children.ToList();
			if (Math.Abs((CurrentDelveMapZoom = ((Element)mineMap).Children[0].Children[0].Children[2].Scale) - base.Settings.DelveGridMapScale) > 0.005f)
			{
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				Element val = list[i];
				if (!val.GetClientRect().Intersects(((Element)mineMap).GetClientRect()))
				{
					continue;
				}
				List<Element> list2 = val.Children.ToList();
				for (int j = 0; j < list2.Count - 1; j++)
				{
					Element val2 = list2[j];
					if (val2.GetClientRect().Intersects(((Element)mineMap).GetClientRect()))
					{
						base.Graphics.DrawFrame(val2.GetClientRect(), Color.DarkGray, 1);
					}
				}
			}
		}
		catch
		{
		}
	}

	private void DrawToLargeMiniMap(DelveEntity entity)
	{
		if (entity.IsOpened)
		{
			return;
		}
		MapIcon mapIcon = GetMapIcon(entity);
		if (mapIcon == null)
		{
			return;
		}
		float rendererPosZ = entity.RendererPosZ;
		try
		{
			SharpDX.Vector2 vector = LargeMapInformation.ScreenCenter + MapIcon.TranslateGridDeltaToMapDelta(entity.GridPos - LargeMapInformation.PlayerPos, rendererPosZ - LargeMapInformation.PlayerPosZ);
			float num = mapIcon.Size * 2f;
			base.Graphics.DrawImage(mapIcon.Texture, new RectangleF(vector.X - num / 2f, vector.Y - num / 2f, num, num), mapIcon.Color);
		}
		catch (NullReferenceException)
		{
		}
	}

	private void DrawToSmallMiniMap(DelveEntity entity)
	{
		if (entity.IsOpened)
		{
			return;
		}
		MapIcon mapIcon = GetMapIcon(entity);
		if (mapIcon == null)
		{
			return;
		}
		Element smallMiniMap = base.GameController.Game.IngameState.IngameUi.Map.SmallMiniMap;
		SharpDX.Vector2 gridPos = base.GameController.Player.GetComponent<Positioned>().GridPos;
		float z = base.GameController.Player.GetComponent<Render>().Z;
		RectangleF clientRect = smallMiniMap.GetClientRect();
		SharpDX.Vector2 vector = MathHepler.Translate(new SharpDX.Vector2(clientRect.X + clientRect.Width / 2f, clientRect.Y + clientRect.Height / 2f), 0f, 0f);
		double diag = Math.Sqrt(clientRect.Width * clientRect.Width + clientRect.Height * clientRect.Height) / 2.0;
		float rendererPosZ = entity.RendererPosZ;
		try
		{
			SharpDX.Vector2 vector2 = vector + MapIcon.DeltaInWorldToMinimapDelta(entity.GridPos - gridPos, diag, 240f, (rendererPosZ - z) / 20f);
			float size = mapIcon.Size;
			RectangleF value = new RectangleF(vector2.X - size / 2f, vector2.Y - size / 2f, size, size);
			clientRect.Contains(ref value, out var result);
			if (result)
			{
				base.Graphics.DrawImage(mapIcon.Texture, value, mapIcon.Color);
			}
		}
		catch (NullReferenceException)
		{
		}
	}

	private static bool IsRadarLineTarget(DelveEntity entity)
	{
		// Exclude terrain objects (walls, lights/path waypoints) - only chests are clickable targets
		return entity.Path.StartsWith("Metadata/Chests/DelveChests/");
	}

	private void DrawRadarLinesToLargeMiniMap()
	{
		if (!base.Settings.MinimapRadarLines.Value)
			return;
		foreach (DelveEntity entity in DelveEntities)
		{
			if (entity == null || entity.IsOpened || !IsRadarLineTarget(entity))
				continue;
			MapIcon mapIcon = GetMapIcon(entity);
			if (mapIcon == null)
				continue;
			float rendererPosZ = entity.RendererPosZ;
			try
			{
				SharpDX.Vector2 entityMapPos = LargeMapInformation.ScreenCenter + MapIcon.TranslateGridDeltaToMapDelta(entity.GridPos - LargeMapInformation.PlayerPos, rendererPosZ - LargeMapInformation.PlayerPosZ);
				base.Graphics.DrawLine(LargeMapInformation.ScreenCenter, entityMapPos, base.Settings.MinimapRadarLineWidth.Value, mapIcon.Color);
			}
			catch (NullReferenceException)
			{
			}
		}
	}

	private void DrawRadarLinesToSmallMiniMap()
	{
		if (!base.Settings.MinimapRadarLines.Value)
			return;
		Element smallMiniMap = base.GameController.Game.IngameState.IngameUi.Map.SmallMiniMap;
		SharpDX.Vector2 playerGridPos = base.GameController.Player.GetComponent<Positioned>().GridPos;
		float playerZ = base.GameController.Player.GetComponent<Render>().Z;
		RectangleF clientRect = smallMiniMap.GetClientRect();
		SharpDX.Vector2 mapCenter = new SharpDX.Vector2(clientRect.X + clientRect.Width / 2f, clientRect.Y + clientRect.Height / 2f);
		double diag = Math.Sqrt(clientRect.Width * clientRect.Width + clientRect.Height * clientRect.Height) / 2.0;
		foreach (DelveEntity entity in DelveEntities)
		{
			if (entity == null || entity.IsOpened || !IsRadarLineTarget(entity))
				continue;
			MapIcon mapIcon = GetMapIcon(entity);
			if (mapIcon == null)
				continue;
			float rendererPosZ = entity.RendererPosZ;
			try
			{
				SharpDX.Vector2 entityMapPos = mapCenter + MapIcon.DeltaInWorldToMinimapDelta(entity.GridPos - playerGridPos, diag, 240f, (rendererPosZ - playerZ) / 20f);
				SharpDX.Vector2 clampedEntityPos = entityMapPos.Clamp(clientRect);
				base.Graphics.DrawLine(mapCenter, clampedEntityPos, base.Settings.MinimapRadarLineWidth.Value, mapIcon.Color);
			}
			catch (NullReferenceException)
			{
			}
		}
	}

	private void UpdateEntitys()
	{
		updateCooldown = 250f;
		if (!IsAzuriteMine)
		{
			return;
		}
		ICollection<Entity> entities = base.GameController.Entities;
		foreach (Entity entity in entities)
		{
			if (entity != null && (entity.Path.StartsWith("Metadata/Chests/DelveChests/") || entity.Path.StartsWith("Metadata/Terrain/Leagues/Delve/Objects/DelveWall") || entity.Path.StartsWith("Metadata/Terrain/Leagues/Delve/Objects/DelveLight")))
			{
				DelveEntity delveEntity = DelveEntities.FirstOrDefault((DelveEntity x) => x != null && x.Id == entity.Id);
				if (delveEntity != null)
				{
					delveEntity.IsAlive = entity.IsAlive;
					delveEntity.IsOpened = entity.IsOpened;
				}
			}
		}
	}

	public override void EntityAdded(Entity entity)
	{
		if (IsAzuriteMine && entity != null && !DelveEntities.Exists((DelveEntity x) => x.Id == entity.Id) && (entity.Path.StartsWith("Metadata/Chests/DelveChests/") || entity.Path.StartsWith("Metadata/Terrain/Leagues/Delve/Objects/DelveWall") || entity.Path.StartsWith("Metadata/Terrain/Leagues/Delve/Objects/DelveLight")))
		{
			bool isOpened = entity.IsOpened;
			Render component = entity.GetComponent<Render>();
			float rendererPosZ = ((component != null) ? component.Pos.Z : entity.Pos.Z);
			SharpDX.Vector2 gridPos = entity.GridPos;
			uint id = entity.Id;
			DelveEntities.Add(new DelveEntity(entity.Pos, entity.Path, isOpened, rendererPosZ, gridPos, entity.IsAlive, id));
		}
	}

	private void RenderMapImages()
	{
		if (base.GameController?.Player == null)
		{
			return;
		}

		if (base.GameController.Game.IngameState.IngameUi.Map.LargeMap.IsVisible)
		{
			try
			{
				LargeMapInformation = new LargeMapData(base.GameController);
			}
			catch (NullReferenceException)
			{
				return;
			}
			foreach (DelveEntity delveEntity in DelveEntities)
			{
				if (delveEntity != null)
				{
					DrawToLargeMiniMap(delveEntity);
				}
			}
			DrawRadarLinesToLargeMiniMap();
			return;
		}
		if (!base.GameController.Game.IngameState.IngameUi.Map.SmallMiniMap.IsVisible)
		{
			return;
		}
		foreach (DelveEntity delveEntity2 in DelveEntities)
		{
			if (delveEntity2 != null)
			{
				DrawToSmallMiniMap(delveEntity2);
			}
		}
		DrawRadarLinesToSmallMiniMap();
	}

	private void DrawMineMapConnections(SubterraneanChart MineMap)
	{
		if (!((Element)MineMap).IsVisible || !base.Settings.DrawPaths.Value)
		{
			return;
		}
		int num = 0;
		RectangleF empty = RectangleF.Empty;
		RectangleF MineMapArea = ((Element)MineMap).GetClientRect();
		float num2 = (float)(100 - base.Settings.ShowRadiusPercentage.Value) * MineMapArea.Width / 200f;
		float num3 = (float)(100 - base.Settings.ShowRadiusPercentage.Value) * MineMapArea.Height / 200f;
		MineMapArea.Inflate(0f - num2, 0f - num3);
		foreach (DelveBigCell item in MineMap.GridElement.Cells.Where((DelveBigCell cell) => ((Element)cell).GetClientRect().Intersects(MineMapArea)))
		{
			foreach (DelveCell item2 in item.Cells.Where((DelveCell x) => x.Type != ""))
			{
				RectangleF clientRect = ((Element)item2).GetClientRect();
				SharpDX.Vector2 center = clientRect.Center;
				if ((!item2.PathIsHidden() && item2.IsCellVisible() && base.Settings.OnlyShowHiddenPaths.Value) || !MineMapArea.Intersects(clientRect))
				{
					continue;
				}
				Element[] array = ((Element)item2).Children.Where((Element x) => x != null && !x.IsVisible && ((int)x.Width == 10 || (int)x.Width == 4)).ToArray();
				int num4 = 0;
				int num5 = array.Count();
				Color color2;
				if (base.Settings.ColoredPaths.Value)
				{
					string text = item2.BiomeName();
					if (1 == 0)
					{
					}
					Color color = text switch
					{
						"AbyssalDepths" => Color.DarkGreen, 
						"AbyssalCity" => Color.DarkGreen, 
						"PetrifiedForest" => Color.SandyBrown, 
						"MagmaFissure" => Color.DarkRed, 
						"FungalCaverns" => Color.DarkViolet, 
						"FrozenHollow" => Color.DarkBlue, 
						"PrimevalRuins" => Color.Blue, 
						"VaalOutpost" => Color.Orange, 
						"SulphurVents" => Color.Brown, 
						_ => Color.Yellow, 
					};
					if (1 == 0)
					{
					}
					color2 = color;
				}
				else
				{
					color2 = Color.Yellow;
				}
				Color color3 = color2;
				if (base.Settings.DrawStraightPaths.Value)
				{
					List<SharpDX.Vector2> list = new List<SharpDX.Vector2>();
					if (((Element)item2).Children.Any((Element x) => x.Width == 10f && x.Y != 0f))
					{
						list.Add(clientRect.BottomMiddle());
					}
					if (((Element)item2).Children.Any((Element x) => x.Width == 10f && x.Y == 0f))
					{
						list.Add(clientRect.TopMiddle());
					}
					if (((Element)item2).Children.Any((Element x) => x.Width == 4f && x.X == 0f))
					{
						list.Add(clientRect.LeftMiddle());
					}
					if (((Element)item2).Children.Any((Element x) => x.Width == 4f && x.X != 0f))
					{
						list.Add(clientRect.RightMiddle());
					}
					foreach (SharpDX.Vector2 item3 in list)
					{
						base.Graphics.DrawLine(center, item3, 4f, color3);
					}
				}
				else
				{
					Element[] array2 = array;
					foreach (Element val in array2)
					{
						int num7 = 15;
						SharpDX.Vector2 center2 = val.GetClientRect().Center;
						if (num4 < num5)
						{
							base.Graphics.DrawLine(center2, center, 4f, color3);
						}
						num4++;
					}
				}
				if (!item2.IsCellVisible() && item2.HasEncounter() && MineMapArea.Contains(center))
				{
					AtlasTexture byFeatureId = iconStore.GetByFeatureId(item2.Type);
					if (byFeatureId != null)
					{
						RectangleF rectangleF = clientRect;
						rectangleF.Inflate((0f - rectangleF.Width) / 7f, (0f - rectangleF.Height) / 7f);
						base.Graphics.DrawImage(byFeatureId, rectangleF);
					}
				}
			}
		}
	}

	public void DelveMenu(int idIn, out int idPop)
	{
		idPop = idIn;
		if (ImGui.TreeNode("Delve Path's"))
		{
			ImGui.PushID(idPop);
			base.Settings.DelvePathWays.Value = ImGuiExtension.Checkbox(base.Settings.DelvePathWays.Value ? "Show" : "Hidden", base.Settings.DelvePathWays.Value);
			ImGui.PopID();
			idPop++;
			ImGui.Spacing();
			ImGui.PushID(idPop);
			base.Settings.DelvePathWaysNodeSize.Value = ImGuiExtension.IntSlider("Size", base.Settings.DelvePathWaysNodeSize);
			ImGui.PopID();
			idPop++;
			ImGui.PushID(idPop);
			base.Settings.DelvePathWaysNodeColor = ImGuiExtension.ColorPicker("Color", base.Settings.DelvePathWaysNodeColor);
			ImGui.PopID();
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelveWall.Value = ImGuiExtension.Checkbox($"Breakable Wall##{idPop}", base.Settings.DelveWall.Value);
			idPop++;
			base.Settings.DelveWallSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelveWallSize);
			idPop++;
			base.Settings.DelveWallColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelveWallColor.Value);
			idPop++;
			ImGui.TreePop();
		}
		if (ImGui.TreeNode("Delve Chests"))
		{
			ImGui.PushID(idPop);
			base.Settings.DelveChests.Value = ImGuiExtension.Checkbox(base.Settings.DelveChests.Value ? "Show" : "Hidden", base.Settings.DelveChests.Value);
			ImGui.PopID();
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelvePathwayChest.Value = ImGuiExtension.Checkbox($"Hidden Chests on the way##{idPop}", base.Settings.DelvePathwayChest.Value);
			idPop++;
			base.Settings.DelvePathwayChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelvePathwayChestSize);
			idPop++;
			base.Settings.DelvePathwayChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelvePathwayChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelveMiningSuppliesDynamiteChest.Value = ImGuiExtension.Checkbox($"Dynamite Supplies##{idPop}", base.Settings.DelveMiningSuppliesDynamiteChest.Value);
			idPop++;
			base.Settings.DelveMiningSuppliesDynamiteChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelveMiningSuppliesDynamiteChestSize);
			idPop++;
			base.Settings.DelveMiningSuppliesDynamiteChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelveMiningSuppliesDynamiteChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelveMiningSuppliesFlaresChest.Value = ImGuiExtension.Checkbox($"Flare Supplies##{idPop}", base.Settings.DelveMiningSuppliesFlaresChest.Value);
			idPop++;
			base.Settings.DelveMiningSuppliesFlaresChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelveMiningSuppliesFlaresChestSize);
			idPop++;
			base.Settings.DelveMiningSuppliesFlaresChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelveMiningSuppliesFlaresChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelveCurrencyChest.Value = ImGuiExtension.Checkbox($"Currency Chests##{idPop}", base.Settings.DelveCurrencyChest.Value);
			idPop++;
			base.Settings.DelveCurrencyChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelveCurrencyChestSize);
			idPop++;
			base.Settings.DelveCurrencyChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelveCurrencyChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelveAzuriteVeinChest.Value = ImGuiExtension.Checkbox($"Azurite Veins##{idPop}", base.Settings.DelveAzuriteVeinChest.Value);
			idPop++;
			base.Settings.DelveAzuriteVeinChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelveAzuriteVeinChestSize);
			idPop++;
			base.Settings.DelveAzuriteVeinChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelveAzuriteVeinChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelveResonatorChest.Value = ImGuiExtension.Checkbox($"Resonator Chests##{idPop}", base.Settings.DelveResonatorChest.Value);
			idPop++;
			base.Settings.DelveResonatorChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelveResonatorChestSize);
			idPop++;
			base.Settings.DelveResonatorChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelveResonatorChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			base.Settings.DelveFossilChest.Value = ImGuiExtension.Checkbox($"Fossil Chests##{idPop}", base.Settings.DelveFossilChest.Value);
			idPop++;
			base.Settings.DelveFossilChestSize.Value = ImGuiExtension.IntSlider($"Size##{idPop}", base.Settings.DelveFossilChestSize);
			idPop++;
			base.Settings.DelveFossilChestColor.Value = ImGuiExtension.ColorPicker($"Color##{idPop}", base.Settings.DelveFossilChestColor.Value);
			idPop++;
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.TreePop();
		}
		if (ImGui.TreeNode("Delve Mine Map Stuff"))
		{
			ImGui.PushID(idPop);
			base.Settings.DelveMineMapConnections.Value = ImGuiExtension.Checkbox($"Show Connections###{idPop}", base.Settings.DelveMineMapConnections.Value);
			ImGui.PopID();
			idPop++;
			base.Settings.ShowRadiusPercentage.Value = ImGuiExtension.IntSlider($"Radius (%)##{idPop}", base.Settings.ShowRadiusPercentage);
			idPop++;
			base.Settings.DrawPaths.Value = ImGuiExtension.Checkbox($"Draw Path's###{idPop}", base.Settings.DrawPaths.Value);
			idPop++;
			base.Settings.OnlyShowHiddenPaths.Value = ImGuiExtension.Checkbox($"Draw only Hidden Path's###{idPop}", base.Settings.OnlyShowHiddenPaths.Value);
			idPop++;
			base.Settings.ColoredPaths.Value = ImGuiExtension.Checkbox($"Draw Path's with Biom Color###{idPop}", base.Settings.ColoredPaths.Value);
			idPop++;
			base.Settings.DrawStraightPaths.Value = ImGuiExtension.Checkbox($"Draw Straight Path's###{idPop}", base.Settings.DrawStraightPaths.Value);
			idPop++;
			ImGui.Spacing();
			base.Settings.MinimapRadarLines.Value = ImGuiExtension.Checkbox($"Draw Minimap Radar Lines to Chests##{idPop}", base.Settings.MinimapRadarLines.Value);
			idPop++;
			base.Settings.MinimapRadarLineWidth.Value = ImGuiExtension.IntSlider($"  Minimap Radar Line Width##{idPop}", base.Settings.MinimapRadarLineWidth);
			idPop++;
			ImGui.TreePop();
		}
	if (ImGui.TreeNode("Delve Map Grid"))
		{
			ImGui.PushID(idPop);
			base.Settings.DelveGridMap.Value = ImGuiExtension.Checkbox(base.Settings.DelveGridMap.Value ? "Show" : "Hidden", base.Settings.DelveGridMap.Value);
			ImGui.PopID();
			idPop++;
			ImGui.PushID(idPop);
			if (ImGui.Button("Set current Delve Zoom as the zoom you want to show grid"))
			{
				base.Settings.DelveGridMapScale = CurrentDelveMapZoom;
			}
			ImGui.PopID();
			idPop++;
			ImGui.TreePop();
		}
		if (ImGui.TreeNode("Debug Mode"))
		{
			ImGui.PushID(idPop);
			base.Settings.DebugHotkey.Value = ImGuiExtension.HotkeySelector("Debug Mode Hotkey", base.Settings.DebugHotkey.Value);
			ImGui.PopID();
			idPop++;
			base.Settings.DebugMode.Value = ImGuiExtension.Checkbox($"Debug Mode##{idPop}", base.Settings.DebugMode.Value);
			idPop++;
			base.Settings.ShouldHideOnOpen.Value = ImGuiExtension.Checkbox($"Hide Chest Name When Opened##{idPop}", base.Settings.ShouldHideOnOpen.Value);
			idPop++;
			ImGui.TreePop();
		}
	}

	public override void DrawSettings()
	{
		ImGui.BulletText(base.Name + ": v" + PluginVersion);
		ImGui.BulletText($"Last Updated: {buildDate}");
		idPop = 1;
		ImGuiTreeNodeFlags val = (ImGuiTreeNodeFlags)26;
		ImGui.PushID(1000);
		if (ImGui.TreeNodeEx("Settings", val))
		{
			DelveMenu(idPop, out var num);
			idPop = num;
			ImGui.TreePop();
		}
		ImGui.PopID();
	}

	public void AtlasTextureInit()
	{
		_iconAbberantFossil = GetDelveAtlasTexture("AbberantFossil");
		_iconAbberantFossilT1 = GetDelveAtlasTexture("AbberantFossilT1");
		_iconAbberantFossilT2 = GetDelveAtlasTexture("AbberantFossilT2");
		_iconAbberantFossilT3 = GetDelveAtlasTexture("AbberantFossilT3");
		_iconAbyssChest = GetDelveAtlasTexture("AbyssChest");
		_iconAbyssCrack = GetDelveAtlasTexture("AbyssCrack");
		_iconAbyssJewelChest = GetDelveAtlasTexture("AbyssJewelChest");
		_iconAdditionalSockets = GetDelveAtlasTexture("AdditionalSockets");
		_iconAmbushInvasionChest = GetDelveAtlasTexture("AmbushInvasionChest");
		_iconAzurite = GetDelveAtlasTexture("Azurite");
		_iconAzuriteT1 = GetDelveAtlasTexture("AzuriteT1");
		_iconAzuriteT2 = GetDelveAtlasTexture("AzuriteT2");
		_iconAzuriteT3 = GetDelveAtlasTexture("AzuriteT3");
		_iconBestiaryChest = GetDelveAtlasTexture("BestiaryChest");
		_iconBeyondChest = GetDelveAtlasTexture("BeyondChest");
		_iconBloodlinesChest = GetDelveAtlasTexture("BloodlinesChest");
		_iconBombs = GetDelveAtlasTexture("Bombs");
		_iconBreachChest = GetDelveAtlasTexture("BreachChest");
		_iconCorrupted = GetDelveAtlasTexture("Corrupted");
		_iconCurrency = GetDelveAtlasTexture("Currency");
		_iconDelveChest = GetDelveAtlasTexture("DelveChest");
		_iconDivinationCard = GetDelveAtlasTexture("DivinationCard");
		_iconDominationChest = GetDelveAtlasTexture("DominationChest");
		_iconEnchant = GetDelveAtlasTexture("Enchant");
		_iconEssence = GetDelveAtlasTexture("Essence");
		_iconFlare = GetDelveAtlasTexture("Flare");
		_iconFragment = GetDelveAtlasTexture("Fragment");
		_iconGate = GetDelveAtlasTexture("Gate");
		_iconHarbingerChest = GetDelveAtlasTexture("HarbingerChest");
		_iconHiddenDoor = GetDelveAtlasTexture("HiddenDoor");
		_iconHighLevelGem = GetDelveAtlasTexture("HighLevelGem");
		_iconIncursionChest = GetDelveAtlasTexture("IncursionChest");
		_iconMap = GetDelveAtlasTexture("Map");
		_iconNemesisChest = GetDelveAtlasTexture("NemesisChest");
		_iconOfferingChest = GetDelveAtlasTexture("OfferingChest");
		_iconOnslaughtChest = GetDelveAtlasTexture("OnslaughtChest");
		_iconPaleCourtComplete = GetDelveAtlasTexture("PaleCourtComplete");
		_iconPerandusChest = GetDelveAtlasTexture("PerandusChest");
		_iconPerandusCoins = GetDelveAtlasTexture("PerandusCoins");
		_iconProphecy = GetDelveAtlasTexture("Prophecy");
		_iconRampageChest = GetDelveAtlasTexture("RampageChest");
		_iconRareAmulet = GetDelveAtlasTexture("RareAmulet");
		_iconResonatorT1 = GetDelveAtlasTexture("ResonatorT1");
		_iconResonatorT2 = GetDelveAtlasTexture("ResonatorT2");
		_iconResonatorT3 = GetDelveAtlasTexture("ResonatorT3");
		_iconSilverCoin = GetDelveAtlasTexture("SilverCoin");
		_iconSixLink = GetDelveAtlasTexture("SixLink");
		_iconSpeedArmour = GetDelveAtlasTexture("SpeedArmour");
		_iconStrongbox = GetDelveAtlasTexture("Strongbox");
		_iconStygianViseChest = GetDelveAtlasTexture("StygianViseChest");
		_iconTalismanChest = GetDelveAtlasTexture("TalismanChest");
		_iconTempestChest = GetDelveAtlasTexture("TempestChest");
		_iconTormentChest = GetDelveAtlasTexture("TormentChest");
		_iconUniqueManaFlask = GetDelveAtlasTexture("UniqueManaFlask");
		_iconUpgrade2x1A = GetDelveAtlasTexture("Upgrade2x1A");
		_iconUpgrade2x2A = GetDelveAtlasTexture("Upgrade2x2A");
		_iconWarbandsChest = GetDelveAtlasTexture("WarbandsChest");
		_iconWisdomCurrency = GetDelveAtlasTexture("WisdomCurrency");
	}

	private MapIcon GetMapIcon(DelveEntity e)
	{
		if (e == null)
		{
			return null;
		}
		if (base.Settings.DelvePathWays.Value && e.Path == "Metadata/Terrain/Leagues/Delve/Objects/DelveLight")
		{
			return new MapIcon(_iconAbyssCrack, base.Settings.DelvePathWaysNodeColor, base.Settings.DelvePathWaysNodeSize.Value);
		}
		if (base.Settings.DelveChests.Value && !e.IsOpened)
		{
			if (e.Path.EndsWith("Encounter"))
			{
				return null;
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveMiningSuppliesDynamite") || e.Path.StartsWith("Metadata/Chests/DelveChests/DynamiteGeneric"))
			{
				if (!base.Settings.DelveMiningSuppliesDynamiteChest.Value) return null;
				return new MapIcon(_iconBombs, base.Settings.DelveMiningSuppliesDynamiteChestColor.Value, base.Settings.DelveMiningSuppliesDynamiteChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveMiningSuppliesFlares"))
			{
				if (!base.Settings.DelveMiningSuppliesFlaresChest.Value) return null;
				return new MapIcon(_iconFlare, base.Settings.DelveMiningSuppliesFlaresChestColor.Value, base.Settings.DelveMiningSuppliesFlaresChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/OffPathCurrency") || e.Path.StartsWith("Metadata/Chests/DelveChests/PathCurrency") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericCurrency"))
			{
				if (!base.Settings.DelveCurrencyChest.Value) return null;
				return new MapIcon(_iconCurrency, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericRandomEnchant"))
			{
				return new MapIcon(_iconEnchant, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmour6LinkedUniqueBody") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeapon6LinkedTwoHanded") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourFullyLinkedBody"))
			{
				return new MapIcon(_iconSixLink, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DynamiteCurrency") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCurrency"))
			{
				if (!base.Settings.DelveCurrencyChest.Value) return null;
				string text = e.Path.Replace("Metadata/Chests/DelveChests/", "");
				if (text.Contains("Currency1") || text == "DynamiteCurrency" || text.Contains("DelveChestCurrencyHighShards"))
				{
					return new MapIcon(_iconCurrency, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
				}
				if (text.Contains("Currency2"))
				{
					return new MapIcon(_iconCurrency, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value * 2);
				}
				if (text.Contains("Currency3"))
				{
					return new MapIcon(_iconCurrency, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value * 3);
				}
				if (text.Contains("DelveChestCurrencySilverCoins"))
				{
					return new MapIcon(_iconSilverCoin, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
				}
				if (text.Contains("DelveChestCurrencyWisdomScrolls"))
				{
					return new MapIcon(_iconWisdomCurrency, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
				}
				if (text.Contains("DelveChestCurrencyDivination"))
				{
					return new MapIcon(_iconDivinationCard, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
				}
				if (text.Contains("DelveChestCurrencyMaps"))
				{
					return new MapIcon(_iconMap, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
				}
				if (text.Contains("DelveChestCurrencyVaal"))
				{
					return new MapIcon(_iconCorrupted, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
				}
				if (text.Contains("DelveChestCurrencySockets"))
				{
					return new MapIcon(_iconAdditionalSockets, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
				}
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourBody2AdditionalSockets"))
			{
				return new MapIcon(_iconAdditionalSockets, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericAtziriFragment"))
			{
				return new MapIcon(_iconFragment, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericPaleCourtFragment"))
			{
				return new MapIcon(_iconPaleCourtComplete, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericRandomEssence") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialGenericEssence"))
			{
				return new MapIcon(_iconEssence, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericDivination") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourDivination") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponDivination") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsDivination"))
			{
				return new MapIcon(_iconDivinationCard, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveAzuriteVein"))
			{
				if (!base.Settings.DelveAzuriteVeinChest.Value) return null;
				int num = base.Settings.DelveAzuriteVeinChestSize.Value;
				AtlasTexture texture = _iconAzurite;
				if (e.Path.EndsWith("1_1"))
				{
					texture = _iconAzuriteT3;
				}
				else if (e.Path.EndsWith("1_2"))
				{
					texture = _iconAzuriteT3;
				}
				else if (e.Path.EndsWith("1_3"))
				{
					texture = _iconAzuriteT2;
					num *= 2;
				}
				else if (e.Path.EndsWith("2_1"))
				{
					texture = _iconAzuriteT1;
					num *= 2;
				}
				return new MapIcon(texture, num);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/Resonator3") || e.Path.StartsWith("Metadata/Chests/DelveChests/Resonator4") || e.Path.StartsWith("Metadata/Chests/DelveChests/Resonator5"))
			{
				if (!base.Settings.DelveResonatorChest.Value) return null;
				return new MapIcon(_iconResonatorT1, base.Settings.DelveResonatorChestColor.Value, (float)base.Settings.DelveResonatorChestSize.Value * 0.7f);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/Resonator2"))
			{
				if (!base.Settings.DelveResonatorChest.Value) return null;
				return new MapIcon(_iconResonatorT2, base.Settings.DelveResonatorChestColor.Value, (float)base.Settings.DelveResonatorChestSize.Value * 0.7f);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/Resonator1"))
			{
				if (!base.Settings.DelveResonatorChest.Value) return null;
				return new MapIcon(_iconResonatorT3, base.Settings.DelveResonatorChestColor.Value, (float)base.Settings.DelveResonatorChestSize.Value * 0.7f);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourMovementSpeed"))
			{
				return new MapIcon(_iconSpeedArmour, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniqueMana"))
			{
				return new MapIcon(_iconUniqueManaFlask, base.Settings.DelveCurrencyChestColor.Value, (float)base.Settings.DelveCurrencyChestSize.Value * 1.3f);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericRandomEnchant"))
			{
				return new MapIcon(_iconEnchant, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests") && (e.Path.EndsWith("FossilChest") || e.Path.EndsWith("FossilChestDynamite")))
			{
				if (!base.Settings.DelveFossilChest.Value) return null;
				string[] t = FossilList?.T1;
				if (t != null) foreach (string text2 in t)
				{
					if (e.Path.ToLower().Contains(text2.ToLower()))
					{
						return new MapIcon(_iconAbberantFossilT1, base.Settings.DelveFossilChestColor.Value, base.Settings.DelveFossilChestSize.Value);
					}
				}
				string[] t2 = FossilList?.T2;
				if (t2 != null) foreach (string text3 in t2)
				{
					if (e.Path.ToLower().Contains(text3.ToLower()))
					{
						return new MapIcon(_iconAbberantFossilT2, base.Settings.DelveFossilChestColor.Value, base.Settings.DelveFossilChestSize.Value);
					}
				}
				string[] t3 = FossilList?.T3;
				if (t3 != null) foreach (string text4 in t3)
				{
					if (e.Path.ToLower().Contains(text4.ToLower()))
					{
						return new MapIcon(_iconAbberantFossilT3, base.Settings.DelveFossilChestColor.Value, base.Settings.DelveFossilChestSize.Value);
					}
				}
				return new MapIcon(_iconAbberantFossil, base.Settings.DelveFossilChestColor.Value, base.Settings.DelveFossilChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityProtoVaalResonator") || e.Path.StartsWith("Metadata/Chests/DelveChests/Resonator"))
			{
				if (!base.Settings.DelveResonatorChest.Value) return null;
				return new MapIcon(_iconResonatorT1, base.Settings.DelveResonatorChestColor.Value, (float)base.Settings.DelveResonatorChestSize.Value * 0.7f);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestMap"))
			{
				return new MapIcon(_iconMap, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourCorrupted") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponCorrupted") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsCorrupted") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityVaalDoubleCorrupted"))
			{
				return new MapIcon(_iconCorrupted, base.Settings.DelveCurrencyChestColor.Value, base.Settings.DelveCurrencyChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Terrain/Leagues/Delve/Objects/DelveWall"))
			{
				if (!e.IsAlive)
				{
					return new MapIcon(_iconHiddenDoor, base.Settings.DelveWallColor.Value, base.Settings.DelveWallSize.Value);
				}
				return new MapIcon(_iconGate, base.Settings.DelveWallColor.Value, base.Settings.DelveWallSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/PathGeneric") || e.Path.StartsWith("Metadata/Chests/DelveChests/OffPathGeneric"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/PathArmour") || e.Path.StartsWith("Metadata/Chests/DelveChests/OffPathArmour") || e.Path.StartsWith("Metadata/Chests/DelveChests/DynamiteArmour"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/PathWeapon") || e.Path.StartsWith("Metadata/Chests/DelveChests/OffPathWeapon") || e.Path.StartsWith("Metadata/Chests/DelveChests/DynamiteWeapon"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/PathTrinkets") || e.Path.StartsWith("Metadata/Chests/DelveChests/OffPathTrinkets") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericTrinkets") || e.Path.StartsWith("Metadata/Chests/DelveChests/DynamiteTrinkets"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconRareAmulet, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/ProsperoChest"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconPerandusCoins, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericAdditionalUniques") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourMultipleUnique") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponMultipleUnique") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsMultipleUnique"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericAdditionalUnique") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsUnique") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourUnique") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponUnique") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniquePhysical") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniqueFire") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniqueCold") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniqueLightning") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniqueChaos") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialUniqueMana"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericProphecyItem"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconProphecy, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericElderItem") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsElder") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourElder") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponElder"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericShaperItem") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponShaper") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsShaper") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourShaper"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericOffering"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconOfferingChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericDelveUnique"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconDelveChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueOnslaught"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconOnslaughtChest, base.Settings.DelvePathwayChestSize.Value * 2);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueAnarchy"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconOnslaughtChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueAmbushInvasion"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconAmbushInvasionChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueDomination"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconDominationChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueNemesis"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconNemesisChest, base.Settings.DelvePathwayChestSize.Value * 2);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueRampage"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconRampageChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueBeyond"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconBeyondChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityAbyssUnique"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconAbyssChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueBloodlines"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconBloodlinesChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueTorment"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconTormentChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueWarbands"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconWarbandsChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueTempest"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconTempestChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueTalisman"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconTalismanChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeaguePerandus"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconPerandusChest, base.Settings.DelvePathwayChestSize.Value * 2);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueBreach"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconBreachChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueHarbinger"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconHarbingerChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueAbyss"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconAbyssChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueBestiary"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconBestiaryChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGenericLeagueIncursion"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconIncursionChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourMultipleResists"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourLife"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourAtlas"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmourOfCrafting") || e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsOfCrafting"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponPhysicalDamage"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponCaster"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponExtraQuality"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponCannotRollCaster"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeaponCannotRollAttacker"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeapon30QualityUnique"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsAmulet"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsRing"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsJewel"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsAtlas"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinketsEyesOfTheGreatwolf"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGemGCP"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGemHighQuality"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGemHighLevel"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGemHighLevelQuality"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGemLevel4Special"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestMapChisels"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestMapCorrupted"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestAssortedFossils"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialArmourMinion"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialTrinketsMinion"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecialGenericMinion"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityVaalImplicit"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityVaalAtzoatlRare"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityVaalUberAtziri"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityVaalDoubleCorrupted"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityAbyssStygian"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStygianViseChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityAbyssJewels"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconAbyssJewelChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityAbyssHighJewel"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconAbyssJewelChest, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityProtoVaalAzurite"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconAzuriteT1, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityProtoVaalFossils"))
			{
				return new MapIcon(_iconAbberantFossil, base.Settings.DelveFossilChestColor.Value, base.Settings.DelveFossilChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestCityProtoVaalEmblem"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestSpecial"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGem"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestTrinkets"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestWeapon"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestArmour"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (e.Path.StartsWith("Metadata/Chests/DelveChests/DelveChestGeneric"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
			if (base.Settings.DelvePathwayChest.Value && e.Path.StartsWith("Metadata/Chests/DelveChests") && !e.Path.Contains("Encounter"))
			{
				if (!base.Settings.DelvePathwayChest.Value) return null;
				return new MapIcon(_iconStrongbox, base.Settings.DelvePathwayChestColor.Value, base.Settings.DelvePathwayChestSize.Value);
			}
		}
		return null;
	}
}
