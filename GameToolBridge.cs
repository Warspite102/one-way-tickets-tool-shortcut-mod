using System.Collections.Generic;
using System.Reflection;
using Foundations;
using UnityEngine;
using UnityEngine.EventSystems;
using World;
using World.Player_Tool;
using World.Player_Tool.ToolsLogic;
using World.Player_Tool.ToolsLogic.Rags;
using World.Player_Tool.ToolsLogic.Saws;
using World.Player_Tool.ToolsLogic.WakeUp;

namespace OneWayTicket.ToolShortcutMod
{
    internal sealed class GameToolBridge
    {
        public bool TryGetToolManager(out PlayerToolMgr toolManager)
        {
            return Singleton<Mission>.instance.try_get_mgr("Tool", out toolManager);
        }

        public PlayerToolMgrView FindToolManagerView(PlayerToolMgr toolManager)
        {
            foreach (var view in Object.FindObjectsOfType<PlayerToolMgrView>(true))
            {
                if (view != null && view.owner == toolManager)
                    return view;
            }

            return null;
        }

        public List<ToolView> FindPickedTools(PlayerToolMgrView toolManagerView)
        {
            var pickedTools = new List<ToolView>();

            if (toolManagerView == null)
                return pickedTools;

            foreach (var toolView in toolManagerView.tool_view_list)
            {
                if (toolView != null && toolManagerView.IsPicking(toolView))
                    pickedTools.Add(toolView);
            }

            return pickedTools;
        }

        public void ActivateTool(PlayerToolMgrView toolManagerView, ToolView toolView)
        {
            if (toolView == null || toolView.logic == null || EventSystem.current == null)
                return;

            ResetRepairToolState(toolView.logic);
            toolView.Init(toolManagerView);

            var pointerEventData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = new Vector2(-10000f, -10000f)
            };

            toolView.logic.OnPointerClick(pointerEventData);
        }

        public void CancelTool(ToolView toolView)
        {
            if (toolView == null || toolView.logic == null || EventSystem.current == null)
                return;

            var pointerEventData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Right,
                position = new Vector2(-10000f, -10000f)
            };

            toolView.logic.OnPointerClick(pointerEventData);
            toolView.logic.OnPointerUp(pointerEventData);
            ResetRepairToolState(toolView.logic);
        }

        private void ResetRepairToolState(ToolLogic logic)
        {
            if (!(logic is FixWrench))
                return;

            SetField(logic, "fix_mask", null);
            SetField(logic, "is_wrenching", false);
            SetField(logic, "is_using", false);
        }

        // 只能通过反射拿到工具的具体工作状态。
        private static bool ReadBoolField(object target, string fieldName)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field != null && field.FieldType == typeof(bool))
                    return (bool)field.GetValue(target);
            }

            return false;
        }

        public WorldSceneRoot GetWorldSceneRoot()
        {
            return MonoBehaviourSingleton<WorldSceneRoot>.instance;
        }

        public uint GetToolId(ToolView toolView)
        {
            return toolView.tool_data.id;
        }

        public ToolCategory? GetToolCategory(ToolLogic logic)
        {
            if (logic == null)
                return null;

            if (logic is FixWrench)
                return ToolCategory.Repair;

            if (logic is Extinguisher)
                return ToolCategory.Extinguish;

            if (logic is Rag)
                return ToolCategory.Clean;

            if (logic is Saw || logic is Scissors)
                return ToolCategory.Saw;

            // 唤醒工具不共享同一个基类，充气锤和尖叫鸡直接派生于 ToolLogic，而其他唤醒工具都派生于 Clock 类。
            if (logic is Clock || logic.GetType().Namespace == typeof(Clock).Namespace)
                return ToolCategory.WakeUp;

            return null;
        }

        public int GetPickAnchorCapacity(PlayerToolMgrView toolManagerView)
        {
            if (toolManagerView?.pick_anchor_pool == null)
                return 0;

            var capacity = 0;
            foreach (var anchor in toolManagerView.pick_anchor_pool)
            {
                if (anchor != null)
                    capacity++;
            }

            return capacity;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field == null)
                    continue;

                if (value != null && !field.FieldType.IsInstanceOfType(value))
                    continue;

                if (value == null && field.FieldType.IsValueType)
                    continue;

                field.SetValue(target, value);
                return;
            }
        }

        public bool IsToolManagerViewValid(PlayerToolMgrView view, PlayerToolMgr toolManager)
        {
            return view != null && view.owner == toolManager;
        }

        public bool IsTextInputActive()
        {
            var eventSystem = EventSystem.current;
            var selectedObject = eventSystem?.currentSelectedGameObject;

            if (selectedObject == null)
                return false;

            return selectedObject.GetComponent("TMP_InputField") != null
                || selectedObject.GetComponent("InputField") != null;
        }

        public bool IsToolReady(ToolView toolView)
        {
            return toolView != null && toolView.tool_data != null && toolView.logic != null;
        }

        public bool IsToolOwned(PlayerToolMgr toolManager, uint toolId)
        {
            return toolManager.tools.ContainsKey(toolId);
        }

        public bool IsToolPicked(PlayerToolMgrView toolManagerView, ToolView toolView)
        {
            return toolManagerView != null && toolView != null && toolManagerView.IsPicking(toolView);
        }

        public bool IsToolBusy(ToolLogic logic)
        {
            if (logic == null)
                return false;

            try
            {
                return ReadBoolField(logic, "is_wrenching")
                    || ReadBoolField(logic, "pressing")
                    || ReadBoolField(logic, "press_extinguisher");
            }
            catch
            {
                // 如果游戏更新后字段发生变化，保守地把工具视为 busy，避免误放下。
                return true;
            }
        }
    }
}
