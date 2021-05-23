using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace MinimapIcons
{
    public class MapIconsSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        [Menu("Draw Monster")]
        public ToggleNode DrawMonsters { get; set; } = new ToggleNode(true);
        [Menu("Icons on minimap")]
        public ToggleNode IconsOnMinimap { get; set; } = new ToggleNode(true);
        [Menu("Icons on large map")]
        public ToggleNode IconsOnLargeMap { get; set; } = new ToggleNode(true);
        [Menu("Z for text")]
        public RangeNode<float> ZForText { get; set; } = new RangeNode<float>(-10, -50, 50);
    }
}
