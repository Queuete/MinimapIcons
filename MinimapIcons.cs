using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using JM.LinqFaster;
using SharpDX;
using Map = ExileCore.PoEMemory.Elements.Map;

namespace MinimapIcons
{
    public class MinimapIcons : BaseSettingsPlugin<MapIconsSettings>
    {


        private IngameUIElements IngameStateIngameUi => GameController.Game.IngameState.IngameUi;
        private Map MapWindow => IngameStateIngameUi.Map;
        private Camera Camera => GameController.Game.IngameState.Camera;
        private CachedValue<float> _diag;
        private float Diag =>
            _diag?.Value ?? (_diag = new TimeCache<float>(() =>
            {
                if (IngameStateIngameUi.Map.SmallMiniMap.IsVisibleLocal)
                {
                    var mapRect = IngameStateIngameUi.Map.SmallMiniMap.GetClientRect();
                    return (float) (Math.Sqrt(mapRect.Width * mapRect.Width + mapRect.Height * mapRect.Height) / 2f);
                }

                return (float) Math.Sqrt(Camera.Width * Camera.Width + Camera.Height * Camera.Height);
            }, 100)).Value;

        private Vector2 ScreenCenter
        {
            get
            {
                if (MapWindow.LargeMap.IsVisible)
                {
                    // large map
                    var mapRectangle = MapWindow.GetClientRectCache;
                    return new Vector2(
                        mapRectangle.Width / 2 + mapRectangle.X + MapWindow.LargeMapShiftX,
                        mapRectangle.Height / 2 - 20 + mapRectangle.Y + MapWindow.LargeMapShiftY
                    );
                }
                else
                {
                    // mini map
                    var miniMapRectangle = MapWindow.SmallMiniMap.GetClientRectCache;
                    return new Vector2(
                        miniMapRectangle.X + miniMapRectangle.Width / 2, 
                        miniMapRectangle.Y + miniMapRectangle.Height / 2
                    );
                }
            }
        }

        public override bool Initialise()
        {
            Graphics.InitImage("sprites.png");
            Graphics.InitImage("Icons.png");
            return true;
        }

        public override void Render()
        {
            try
            {
                if (!ShouldRender()) return;

                var playerPos = GameController.Player.GetComponent<Positioned>().GridPos;
                var posZ = GameController.Player.GetComponent<Render>().Pos.Z;
                var mapWindowLargeMapZoom = MapWindow.LargeMapZoom;

                var baseIcons = GameController.EntityListWrapper.OnlyValidEntities
                    .SelectWhereF(x => x.GetHudComponent<BaseIcon>(), icon => icon != null).OrderByF(x => x.Priority)
                    .ToList();

                foreach (var icon in baseIcons)
                {
                    if (icon.Entity.Type == EntityType.WorldItem)
                        continue;

                    if (!Settings.DrawMonsters && icon.Entity.Type == EntityType.Monster)
                        continue;

                    if (icon.HasIngameIcon)
                        continue;

                    if (!icon.Show())
                        continue;

                    var component = icon?.Entity?.GetComponent<Render>();
                    if (component == null) continue;
                    var iconZ = component.Pos.Z;
                    Vector2 position;

                    if (MapWindow.LargeMap.IsVisible)
                    {
                        var k = Camera.Width < 1024f ? 1120f : 1024f;
                        var scale = k / Camera.Height * Camera.Width * 3f / 4f / MapWindow.LargeMapZoom;
                        position = ScreenCenter + 
                                   MapIcon.DeltaInWorldToMinimapDelta(
                                       icon.GridPosition() - playerPos, 
                                       Diag, 
                                       scale, 
                                       (iconZ - posZ) / (9f / mapWindowLargeMapZoom)
                                   );
                    }
                    else
                    {
                        position = ScreenCenter +
                                   MapIcon.DeltaInWorldToMinimapDelta(
                                       icon.GridPosition() - playerPos, 
                                       Diag, 
                                       240f, 
                                       (iconZ - posZ) / 20
                                   );
                    }

                    var iconValueMainTexture = icon.MainTexture;
                    var size = iconValueMainTexture.Size;
                    var halfSize = size / 2f;
                    icon.DrawRect = new RectangleF(position.X - halfSize, position.Y - halfSize, size, size);
                    Graphics.DrawImage(iconValueMainTexture.FileName, icon.DrawRect, iconValueMainTexture.UV, iconValueMainTexture.Color);

                    if (icon.Hidden())
                    {
                        var s = icon.DrawRect.Width * 0.5f;
                        icon.DrawRect.Inflate(-s, -s);

                        Graphics.DrawImage(icon.MainTexture.FileName, icon.DrawRect,
                            SpriteHelper.GetUV(MapIconsIndex.LootFilterSmallCyanCircle), Color.White);

                        icon.DrawRect.Inflate(s, s);
                    }

                    if (!string.IsNullOrEmpty(icon.Text))
                        Graphics.DrawText(icon.Text, position.Translate(0, Settings.ZForText), FontAlign.Center);
                }
            }
            catch (Exception e)
            {
                DebugWindow.LogError($"MinimapIcons.TickLogic: {e.Message}");
            }
        }

        private bool ShouldRender()
        {
            if (!Settings.Enable.Value) return false;
            if (!GameController.InGame) return false;
            if (!MapWindow.LargeMap.IsVisible && Settings.IconsOnLargeMap?.Value == false) return false;
            if (!MapWindow.SmallMiniMap.IsVisible && Settings.IconsOnMinimap?.Value == false) return false;
            if (IngameStateIngameUi.Atlas.IsVisibleLocal 
                || IngameStateIngameUi.DelveWindow.IsVisibleLocal 
                || IngameStateIngameUi.TreePanel.IsVisibleLocal)
                return false;

            return true;
        }
    }
}
