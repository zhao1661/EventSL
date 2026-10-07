using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mods;
using UnityEngine;

namespace EventSL
{
    public class EventSLMod : UserMod
    {
        public class EventSLConfig
        {
            [BoolField(
                "显示游戏内SL按钮 / Show In-Game SL Button",
                "在度假界面和事件界面中，人物头像下方显示贴合原版画风的【整体 SL】按钮，点击即可直接一键SL回退整轮度假，无需二次确认。\nShow in-game SL button under pilot portrait in vacation and event panels.")]
            public bool ShowInGameButtons = true;

            [BoolField(
                "整轮度假起点备份 / Backup Vacation Start",
                "每次刚进入度假界面时，自动备份起点存档。\nAutomatically creates a backup of the save upon entering a vacation.")]
            public bool EnableVacationBackup = true;

            [BoolField(
                "返回主菜单自动重置度假 / Reset Vacation on Return to Menu",
                "若开启，在度假界面（包括地图或事件中）通过ESC返回主界面再读档，将直接回退到刚进入该轮度假时的起点（整轮度假重置）。\nIf true, exiting to main menu during vacation resets back to when vacation first started.")]
            public bool SLToVacationStart = false;
        }

        public static EventSLConfig Config = new EventSLConfig();

        public override void OnAllModsLoad(IReadOnlyList<UserMod> mods)
        {
            base.OnAllModsLoad(mods);
            Debug.Log("[EventSL] EventSL Mod loaded successfully!");
        }

        public static bool IsInEvent()
        {
            try
            {
                var wantedMgr = Singleton<WantedManager>.Instance();
                if (wantedMgr != null && wantedMgr.eventProcess != null && wantedMgr.eventProcess.currentEvent != 0)
                {
                    return true;
                }

                var encounterPanel = Singleton<UIManager>.Instance()?.GetPanel<EncounterEventPanel>(FilePath.encounterEventPanel);
                if (encounterPanel != null && encounterPanel.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        public static bool IsInVacation()
        {
            try
            {
                return VacationEventPanel.onVacation;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetSaveDir(int slot)
        {
            return Path.Combine(Application.persistentDataPath, "Save", slot.ToString(), "Save");
        }

        public static string GetVacationBackupDir(int slot)
        {
            return Path.Combine(Application.persistentDataPath, "Save", slot.ToString(), "VacationBackup");
        }

        public static void BackupVacationEntry(int slot)
        {
            try
            {
                string src = GetSaveDir(slot);
                string dst = GetVacationBackupDir(slot);
                if (!Directory.Exists(src)) return;

                if (!Directory.Exists(dst))
                {
                    Directory.CreateDirectory(dst);
                }

                foreach (string file in Directory.GetFiles(src, "*.json"))
                {
                    string destFile = Path.Combine(dst, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }
                Debug.Log($"[EventSL] Backed up vacation entry files to {dst}");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[EventSL] BackupVacationEntry error: " + e);
            }
        }

        public static void RestoreVacationEntry(int slot)
        {
            try
            {
                string src = GetVacationBackupDir(slot);
                string dst = GetSaveDir(slot);
                if (!Directory.Exists(src)) return;

                if (!Directory.Exists(dst))
                {
                    Directory.CreateDirectory(dst);
                }

                foreach (string file in Directory.GetFiles(src, "*.json"))
                {
                    string destFile = Path.Combine(dst, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }
                Debug.Log($"[EventSL] Restored vacation entry files from {src} to {dst}");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[EventSL] RestoreVacationEntry error: " + e);
            }
        }

        public static void ClearVacationBackup(int slot)
        {
            try
            {
                string dst = GetVacationBackupDir(slot);
                if (Directory.Exists(dst))
                {
                    Directory.Delete(dst, true);
                    Debug.Log($"[EventSL] Cleared vacation backup at {dst}");
                }
            }
            catch (Exception)
            {
            }
        }

        public static bool IsPendingVacationBackup { get; private set; } = false;
        private static int pendingBackupSlot = -1;
        private static Coroutine pendingBackupCoroutine = null;

        public static void ScheduleVacationBackup(int slot)
        {
            pendingBackupSlot = slot;
            IsPendingVacationBackup = true;
            if (pendingBackupCoroutine != null)
            {
                try
                {
                    CoroutineRunner.Instance?.StopCoroutine(pendingBackupCoroutine);
                }
                catch { }
                pendingBackupCoroutine = null;
            }
            pendingBackupCoroutine = CoroutineRunner.Instance.StartCoroutine(DeferredVacationBackupRoutine(slot));
            Debug.Log($"[EventSL] Scheduled deferred vacation backup for slot {slot}");
        }

        public static void FinalizePendingVacationBackup()
        {
            if (!IsPendingVacationBackup || pendingBackupSlot < 0) return;
            int slot = pendingBackupSlot;
            try
            {
                if (pendingBackupCoroutine != null)
                {
                    CoroutineRunner.Instance?.StopCoroutine(pendingBackupCoroutine);
                    pendingBackupCoroutine = null;
                }

                var saveMgr = Singleton<SaveManager>.Instance();
                if (saveMgr != null && saveMgr.saveDataPack != null)
                {
                    // 1. Force the game to save so any rewards chosen in ChoicePanel after VacationEventPanel.Init are written to disk
                    saveMgr.Save();
                    // 2. Backup the complete save to VacationBackup
                    BackupVacationEntry(slot);
                    Debug.Log($"[EventSL] Finalized deferred vacation backup for slot {slot}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[EventSL] FinalizePendingVacationBackup error: " + e);
            }
            finally
            {
                IsPendingVacationBackup = false;
                pendingBackupSlot = -1;
                pendingBackupCoroutine = null;
            }
        }

        public static void CancelPendingVacationBackup()
        {
            if (pendingBackupCoroutine != null)
            {
                try
                {
                    CoroutineRunner.Instance?.StopCoroutine(pendingBackupCoroutine);
                }
                catch { }
                pendingBackupCoroutine = null;
            }
            IsPendingVacationBackup = false;
            pendingBackupSlot = -1;
        }

        private static IEnumerator DeferredVacationBackupRoutine(int slot)
        {
            // Wait 1 frame (yield return null) and until end of frame (WaitForEndOfFrame).
            // This ensures ChoicePanel.ItemClick / TreasureClick has completely executed
            // ItemManager.AddItem(...) / TreasureManager.AddTreasure(...) and closed.
            yield return null;
            yield return new WaitForEndOfFrame();

            if (IsPendingVacationBackup && pendingBackupSlot == slot)
            {
                FinalizePendingVacationBackup();
            }
        }

        public static bool HasVacationBackup()
        {
            try
            {
                if (IsPendingVacationBackup) return true;

                var savePack = Singleton<SaveManager>.Instance()?.saveDataPack;
                if (savePack == null) return false;
                int slot = savePack.currentSaveData;
                string backupDir = GetVacationBackupDir(slot);
                return Directory.Exists(backupDir) && Directory.GetFiles(backupDir, "*.json").Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public static void PerformFullVacationSL()
        {
            try
            {
                // Ensure any pending deferred backup is finalized first
                if (IsPendingVacationBackup)
                {
                    FinalizePendingVacationBackup();
                }

                int slot = Singleton<SaveManager>.Instance().saveDataPack.currentSaveData;
                string backupDir = GetVacationBackupDir(slot);
                if (!Directory.Exists(backupDir) || Directory.GetFiles(backupDir, "*.json").Length == 0)
                {
                    bool isZh = EventSLUI.IsChineseLanguage();
                    Singleton<UIManager>.Instance()?.ShowTips(isZh ? "未检测到当前度假的起点备份文件。" : "Vacation start backup not found.");
                    return;
                }

                RestoreVacationEntry(slot);

                Singleton<UIManager>.Instance().ShowPanel(FilePath.loadPanel, UI_Layer.Overlay, delegate(LoadPanel loadPanel)
                {
                    loadPanel.Init(delegate
                    {
                        Singleton<UIManager>.Instance().ClearAllPanel();
                        if (Singleton<WantedManager>.Instance().wantedProcess != null)
                        {
                            Singleton<WantedManager>.Instance().wantedProcess.ClearListener();
                            Singleton<WantedManager>.Instance().wantedProcess = null;
                        }
                        Singleton<WantedManager>.Instance().eventProcess = null;
                        Singleton<ShipDatasManager>.Instance().Clear();
                        Singleton<SaveManager>.Instance().Load();
                    }, 0.25f, 0.2f, 0.1f, 0.1f);
                });
            }
            catch (Exception e)
            {
                Debug.LogError("[EventSL] PerformFullVacationSL error: " + e);
            }
        }
    }

    /// <summary>
    /// Persistent GameObject runner for coroutines that will never be destroyed or stopped across scene/panel transitions.
    /// </summary>
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;
        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("EventSL_CoroutineRunner");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _instance = go.AddComponent<CoroutineRunner>();
                }
                return _instance;
            }
        }
    }
}
