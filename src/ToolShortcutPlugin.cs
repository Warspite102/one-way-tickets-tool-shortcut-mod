using BepInEx;

namespace OneWayTicket.ToolShortcutMod
{
    [BepInPlugin("com.warspite102.onewaytickets.toolshortcutmod", "Tool Shortcut Mod", "0.1.0")]
    public sealed class ToolShortcutPlugin : BaseUnityPlugin
    {
        private ToolShortcutController _shortcutController;

        private void Awake()
        {
            _shortcutController = new ToolShortcutController(Config, Logger);
        }

        private void Update()
        {
            _shortcutController?.ProcessShortcuts();
        }
    }
}
