﻿using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;

namespace Tooltippery
{
    public class Tooltippery : NeosMod
    {
        public override string Name => "Tooltippery";
        public override string Author => "Psychpsyo";
        public override string Version => "1.0.1";
        public override string Link => "https://github.com/Psychpsyo/Tooltippery";

        private static Dictionary<IButton, Tooltip> openTooltips = new Dictionary<IButton, Tooltip>();
        public static List<Func<IButton, ButtonEventData, string>> labelProviders = new List<Func<IButton, ButtonEventData, string>>();

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<BaseX.color> textColor = new ModConfigurationKey<BaseX.color>("Text Color", "Sets the text color of a tooltip.", () => new BaseX.color(1, 1, 1, 1));
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<BaseX.color> bgColor = new ModConfigurationKey<BaseX.color>("Background Color", "Sets the background color of a tooltip.", () => new BaseX.color(0, 0, 0, .75f));
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<float> textScale = new ModConfigurationKey<float>("Text Size", "Sets the size of the text on a tooltip.", () => 1);

        public static ModConfiguration config;
        public override void OnEngineInit()
        {
            config = GetConfiguration();
            labelProviders.Insert(0, commentLabels);

            Harmony harmony = new Harmony("Psychpsyo.Tooltippery");
            harmony.PatchAll();
        }

        public static Tooltip showTooltip(string label, Slot parent, BaseX.float3 localPosition)
        {
            Tooltip newTooltip = new Tooltip(label, parent, localPosition);
            return newTooltip;
        }
        public static void hideTooltip(Tooltip tooltip)
        {
            tooltip.hide();
        }
        private static string determineLabel(IButton button, ButtonEventData eventData)
        {
            string label = null;
            foreach (Func<IButton, ButtonEventData, string> provider in labelProviders)
            {
                label = provider(button, eventData);
                if (label != null)
                {
                    return label;
                }
            }
            return null;
        }
        private static string commentLabels(IButton button, ButtonEventData eventData)
        {
            string comment = button.Slot.GetComponent<Comment>()?.Text.Value;
            if (comment == null) return null;
            if (comment.StartsWith("TooltipperyLabel:")) return comment.Substring(17);
            return null;
        }
        
        // UIX button tooltips
        [HarmonyPatch(typeof(Button), "RunHoverEnter")]
        class ButtonTooltipOpen
        {
            static void Postfix(Button __instance, ButtonEventData eventData)
            {
                if (openTooltips.ContainsKey(__instance)) return;

                string label = determineLabel(__instance, eventData);
                if (label != null)
                {
                    Slot tooltipParent = __instance.Slot.GetComponentInParents<Canvas>().Slot;
                    openTooltips.Add(__instance, showTooltip(label, tooltipParent, new BaseX.float3(eventData.localPoint.X, eventData.localPoint.Y, -1 * __instance.World.LocalUserViewScale.Z * (.001f / tooltipParent.GlobalScale.Z))));
                }
            }
        }

        [HarmonyPatch(typeof(Button), "RunHoverLeave")]
        class ButtonTooltipClose
        {
            static void Postfix(Button __instance)
            {
                Tooltip toClose;
                if (openTooltips.TryGetValue(__instance, out toClose))
                {
                    openTooltips.Remove(__instance);
                    hideTooltip(toClose);
                }
            }
        }

        // closes all tooltips for a button when that button gets destroyed.
        [HarmonyPatch(typeof(Button), "OnDispose")]
        class ButtonTooltipDispose
        {
            static void Postfix(Button __instance)
            {
                Tooltip toClose;
                if (openTooltips.TryGetValue(__instance, out toClose))
                {
                    openTooltips.Remove(__instance);
                    hideTooltip(toClose);
                }
            }
        }
    }
}