using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using World;
using World.Player_Tool;

namespace OneWayTicket.ToolShortcutMod
{
    internal sealed class ToolShortcutController
    {
        private sealed class CachedTool
        {
            public ToolView View;
            public ToolCategory Category;
            public uint Id;
        }

        private readonly GameToolBridge _gameToolBridge = new();
        private readonly ManualLogSource _logger;

        private readonly ConfigEntry<KeyboardShortcut> _repairHotkey;
        private readonly ConfigEntry<KeyboardShortcut> _extinguisherHotkey;
        private readonly ConfigEntry<KeyboardShortcut> _cleanHotkey;
        private readonly ConfigEntry<KeyboardShortcut> _sawHotkey;
        private readonly ConfigEntry<KeyboardShortcut> _wakeUpHotkey;

        private readonly List<CachedTool> _cachedTools = [];
        private readonly Dictionary<uint, int> _cachedToolLevels = [];

        private WorldSceneRoot _cachedWorldSceneRoot;
        private PlayerToolMgr _cachedToolManager;
        private PlayerToolMgrView _cachedToolManagerView;

        public ToolShortcutController(ConfigFile config, ManualLogSource logger)
        {
            _logger = logger;

            _repairHotkey = config.Bind(
                "工具快捷键",
                "维修",
                new KeyboardShortcut(KeyCode.Alpha1),
                "使用维修工具。");

            _extinguisherHotkey = config.Bind(
                "工具快捷键",
                "灭火",
                new KeyboardShortcut(KeyCode.Alpha2),
                "使用灭火工具。");

            _cleanHotkey = config.Bind(
                "工具快捷键",
                "清理",
                new KeyboardShortcut(KeyCode.Alpha3),
                "使用清理工具。");

            _sawHotkey = config.Bind(
                "工具快捷键",
                "切割",
                new KeyboardShortcut(KeyCode.Alpha4),
                "使用切割工具。");

            _wakeUpHotkey = config.Bind(
                "工具快捷键",
                "工头",
                new KeyboardShortcut(KeyCode.Alpha5),
                "工头喊一嗓子。");
        }

        public void ProcessShortcuts()
        {
            if (_repairHotkey.Value.IsDown())
                TryActivateTool(ToolCategory.Repair);

            if (_extinguisherHotkey.Value.IsDown())
                TryActivateTool(ToolCategory.Extinguish);

            if (_cleanHotkey.Value.IsDown())
                TryActivateTool(ToolCategory.Clean);

            if (_sawHotkey.Value.IsDown())
                TryActivateTool(ToolCategory.Saw);

            if (_wakeUpHotkey.Value.IsDown())
                TryActivateTool(ToolCategory.WakeUp);
        }

        private void TryActivateTool(ToolCategory category)
        {
            try
            {
                var worldSceneRoot = _gameToolBridge.GetWorldSceneRoot();
                if (worldSceneRoot == null)
                    return;

                if (_gameToolBridge.IsTextInputActive())
                    return;

                if (!_gameToolBridge.TryGetToolManager(out var toolManager))
                    return;

                var toolManagerView = GetToolManagerView(toolManager);
                if (toolManagerView == null)
                    return;

                RefreshToolCacheIfNeeded(worldSceneRoot, toolManager, toolManagerView);

                var selectedTool = FindAvailableCachedTool(category, toolManager, toolManagerView);
                if (selectedTool == null || !_gameToolBridge.IsToolReady(selectedTool.View))
                    return;

                // 空闲工具在切换分类时放下；正在持续作业的工具保持原状。
                CancelIdlePickedTools(toolManagerView);

                var pickedTools = _gameToolBridge.FindPickedTools(toolManagerView);
                if (pickedTools.Count >= _gameToolBridge.GetPickAnchorCapacity(toolManagerView))
                    return;

                _gameToolBridge.ActivateTool(toolManagerView, selectedTool.View);
            }
            catch (Exception exception)
            {
                _logger.LogError("[Tool Shortcut Mod] " + exception);
            }
        }

        private void CancelIdlePickedTools(PlayerToolMgrView toolManagerView)
        {
            var idlePickedTools = new List<ToolView>();

            foreach (var pickedTool in _gameToolBridge.FindPickedTools(toolManagerView))
            {
                if (!_gameToolBridge.IsToolBusy(pickedTool.logic))
                    idlePickedTools.Add(pickedTool);
            }

            foreach (var idlePickedTool in idlePickedTools)
                _gameToolBridge.CancelTool(idlePickedTool);
        }

        private CachedTool FindAvailableCachedTool(
            ToolCategory category,
            PlayerToolMgr toolManager,
            PlayerToolMgrView toolManagerView)
        {
            foreach (var cachedTool in _cachedTools)
            {
                if (cachedTool.Category != category)
                    continue;

                if (!IsCachedToolUsable(cachedTool, toolManager))
                    continue;

                if (_gameToolBridge.IsToolPicked(toolManagerView, cachedTool.View))
                    continue;

                return cachedTool;
            }

            return null;
        }

        private void RefreshToolCacheIfNeeded(
            WorldSceneRoot worldSceneRoot,
            PlayerToolMgr toolManager,
            PlayerToolMgrView toolManagerView)
        {
            if (_cachedWorldSceneRoot == worldSceneRoot
                && _cachedToolManager == toolManager
                && _cachedToolManagerView == toolManagerView
                && CachedToolLevelsMatch(toolManager))
            {
                return;
            }

            _cachedWorldSceneRoot = worldSceneRoot;
            _cachedToolManager = toolManager;
            _cachedToolManagerView = toolManagerView;

            _cachedTools.Clear();
            _cachedToolLevels.Clear();

            foreach (var toolLevel in toolManager.tools)
                _cachedToolLevels[toolLevel.Key] = toolLevel.Value;

            foreach (var toolView in toolManagerView.tool_view_list)
            {
                if (!_gameToolBridge.IsToolReady(toolView))
                    continue;

                var toolId = _gameToolBridge.GetToolId(toolView);
                if (!_cachedToolLevels.ContainsKey(toolId))
                    continue;

                var category = _gameToolBridge.GetToolCategory(toolView.logic);
                if (!category.HasValue)
                    continue;

                _cachedTools.Add(new CachedTool
                {
                    View = toolView,
                    Category = category.Value,
                    Id = toolId
                });
            }
        }

        private bool CachedToolLevelsMatch(PlayerToolMgr toolManager)
        {
            if (_cachedToolLevels.Count != toolManager.tools.Count)
                return false;

            foreach (var toolLevel in toolManager.tools)
            {
                if (!_cachedToolLevels.TryGetValue(toolLevel.Key, out var cachedLevel)
                    || cachedLevel != toolLevel.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private PlayerToolMgrView GetToolManagerView(PlayerToolMgr toolManager)
        {
            if (_gameToolBridge.IsToolManagerViewValid(_cachedToolManagerView, toolManager))
                return _cachedToolManagerView;

            _cachedToolManagerView = _gameToolBridge.FindToolManagerView(toolManager);
            return _cachedToolManagerView;
        }

        private bool IsCachedToolUsable(CachedTool cachedTool, PlayerToolMgr toolManager)
        {
            return _gameToolBridge.IsToolReady(cachedTool.View)
                && _gameToolBridge.IsToolOwned(toolManager, cachedTool.Id)
                && _gameToolBridge.GetToolId(cachedTool.View) == cachedTool.Id;
        }
    }
}
