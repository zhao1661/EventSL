using System;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace EventSL
{
    // Automatically backup when entering a vacation (fistSet == true) and attach UI to banner
    [HarmonyPatch(typeof(VacationEventPanel), "Init")]
    public static class VacationEventPanelInitPatch
    {
        public static void Postfix(VacationEventPanel __instance, EventGroup eventGroup, int setDays, bool fistSet)
        {
            if (fistSet && EventSLMod.Config != null && EventSLMod.Config.EnableVacationBackup)
            {
                try
                {
                    var savePack = Singleton<SaveManager>.Instance()?.saveDataPack;
                    if (savePack != null)
                    {
                        // Schedule deferred backup: waits for ChoicePanel (if any) to complete AddItem/AddTreasure
                        EventSLMod.ScheduleVacationBackup(savePack.currentSaveData);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[EventSL] Vacation backup schedule failed: " + e);
                }
            }

            TryEnsureBannerButtons();
        }

        public static void TryEnsureBannerButtons()
        {
            try
            {
                var banner = Singleton<UIManager>.Instance()?.GetPanel<BannerPanel>(FilePath.bannerPanel);
                if (banner != null)
                {
                    EventSLUI.EnsureBannerButtons(banner);
                }
            }
            catch (Exception)
            {
            }
        }
    }

    // Attach when VacationEventPanel is shown
    [HarmonyPatch(typeof(VacationEventPanel), "Show")]
    public static class VacationEventPanelShowPatch
    {
        public static void Postfix()
        {
            VacationEventPanelInitPatch.TryEnsureBannerButtons();
        }
    }

    // Attach buttons when EncounterEventPanel is shown or inited
    [HarmonyPatch(typeof(EncounterEventPanel), "Show")]
    public static class EncounterEventPanelShowPatch
    {
        public static void Postfix()
        {
            VacationEventPanelInitPatch.TryEnsureBannerButtons();
        }
    }

    [HarmonyPatch(typeof(EncounterEventPanel), "Init")]
    public static class EncounterEventPanelInitPatch
    {
        public static void Postfix()
        {
            VacationEventPanelInitPatch.TryEnsureBannerButtons();
        }
    }

    // Attach buttons on BannerPanel directly
    [HarmonyPatch(typeof(BannerPanel), "Init")]
    public static class BannerPanelInitPatch
    {
        public static void Postfix(BannerPanel __instance)
        {
            EventSLUI.EnsureBannerButtons(__instance);
        }
    }

    [HarmonyPatch(typeof(BannerPanel), "Show")]
    public static class BannerPanelShowPatch
    {
        public static void Postfix(BannerPanel __instance)
        {
            EventSLUI.EnsureBannerButtons(__instance);
        }
    }

    // Handle exiting to launcher from ESC panel
    [HarmonyPatch(typeof(ESCPanel), "ToLauncherPanel")]
    public static class ESCPanelToLauncherPanelPatch
    {
        public static void Prefix()
        {
            CheckAndHandleExitToLauncher();
        }

        public static void CheckAndHandleExitToLauncher()
        {
            try
            {
                // Ensure any pending deferred backup is finalized first
                if (EventSLMod.IsPendingVacationBackup)
                {
                    EventSLMod.FinalizePendingVacationBackup();
                }

                var savePack = Singleton<SaveManager>.Instance()?.saveDataPack;
                if (savePack == null) return;
                int slot = savePack.currentSaveData;

                if (EventSLMod.Config != null && EventSLMod.Config.SLToVacationStart)
                {
                    if (EventSLMod.IsInVacation() || EventSLMod.IsInEvent())
                    {
                        EventSLMod.RestoreVacationEntry(slot);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[EventSL] CheckAndHandleExitToLauncher failed: " + e);
            }
        }
    }

    // Also guard WantedManager.ShowLauncherPanel when exiting without ESCPanel
    [HarmonyPatch(typeof(WantedManager), "ShowLauncherPanel", new Type[] { typeof(EnemyType) })]
    public static class WantedManagerShowLauncherPanelPatch
    {
        public static void Prefix()
        {
            ESCPanelToLauncherPanelPatch.CheckAndHandleExitToLauncher();
        }
    }

    // Clear backup when vacation is normally ended
    [HarmonyPatch(typeof(VacationEventPanel), "Continue")]
    public static class VacationEventPanelContinuePatch
    {
        public static void Postfix()
        {
            try
            {
                EventSLMod.CancelPendingVacationBackup();

                var savePack = Singleton<SaveManager>.Instance()?.saveDataPack;
                if (savePack != null)
                {
                    EventSLMod.ClearVacationBackup(savePack.currentSaveData);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
