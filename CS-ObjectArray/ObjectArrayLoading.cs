using System;
using ICities;
using UnityEngine;
using ColossalFramework;

namespace ObjectArray
{
    public class ObjectArrayLoading : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            if (ObjectArrayTool.instance == null)
            {
                Debug.Log("Reload");
                ToolController toolController = GameObject.FindObjectOfType<ToolController>();
                ObjectArrayTool.instance = toolController.gameObject.AddComponent<ObjectArrayTool>();
                ObjectArrayTool.instance.enabled = false;
            }
        }

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            Debug.Log("reloaded");
            Debug.Log(loading.currentMode);
            if (ObjectArrayTool.instance == null)
            {
                Debug.Log("Reload");
                ToolController toolController = GameObject.FindObjectOfType<ToolController>();
                ObjectArrayTool.instance = toolController.gameObject.AddComponent<ObjectArrayTool>();
                ObjectArrayTool.instance.enabled = false;
            }
        }

        public override void OnReleased()
        {
            UnityEngine.Object.DestroyImmediate(ObjectArrayTool.instance.gameObject);
            ObjectArrayTool.instance = null;
        }
    }

    public class TEMPORARYThreadingExtension : ThreadingExtensionBase
    {

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            Debug.Log("hello");
            if(Input.GetKeyUp(KeyCode.H)){
                ObjectArrayTool.instance.enabled = !ObjectArrayTool.instance.enabled;
            }
        }
    }
}
